import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Info, Layers, Loader2, Plus } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { AppTable, type AdminGridColumn } from '../../components/ui/AppTable'
import { AppTreeView, type TreeViewNode } from '../../components/ui/AppTreeView'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { createRestDataSource } from '../../lib/createRestDataSource'
import { toast } from '../../lib/toast'

type ApiEnvelope<T> = {
  success?: boolean
  data?: T
  totalCount?: number
}

// Mirrors LearnerGroupCategoryDto (iLearn.Application/DTOs/LearnerGroupCategoryDto.cs)
type CategoryLookup = {
  id: number
  name: string
  parentId?: number | null
  depth?: number
}

// Mirrors division lookup row from admin/DivisionsCRUD/Get
type DivisionLookup = {
  id: number
  name: string
}

// Mirrors LearnerGroupDto (iLearn.Application/DTOs/LearnerGroupDto.cs)
type LearnerGroupRow = Record<string, unknown> & {
  id?: number
  name?: string
  description?: string
  memberCount?: number
  divisionId?: number | null
  categoryId?: number | null
  createdAt?: string
}

type FilterExpression = unknown[]

function unwrapList<T>(value: ApiEnvelope<T[]> | { data?: T[] } | T[] | undefined): T[] {
  if (!value) return []
  if (Array.isArray(value)) return value

  const boxed = value as { data?: T[] }
  return Array.isArray(boxed.data) ? boxed.data : []
}

export function LearnerGroupListPage() {
  const navigate = useNavigate()

  const [categories, setCategories] = useState<CategoryLookup[]>([])
  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [loadingLookups, setLoadingLookups] = useState(true)

  const [selectedTreeNode, setSelectedTreeNode] = useState<TreeViewNode | null>(null)
  const [isTreeExpanded, setIsTreeExpanded] = useState(false)
  const [gridFilters, setGridFilters] = useState<FilterExpression>([])

  useEffect(() => {
    let cancelled = false

    const loadLookups = async () => {
      setLoadingLookups(true)
      try {
        const [categoryResp, divisionResp] = await Promise.all([
          fetchWithAccessControl<ApiEnvelope<CategoryLookup[]>>('LearnerGroupCategories'),
          fetchWithAccessControl<{ data?: DivisionLookup[] } | DivisionLookup[]>('admin/DivisionsCRUD/Get'),
        ])

        if (cancelled) return

        setCategories(unwrapList(categoryResp))
        setDivisions(unwrapList(divisionResp))
      } catch (error) {
        console.error('Failed to load learner group lookups', error)
        toast.error('Failed to load group folder tree')
      } finally {
        if (!cancelled) {
          setLoadingLookups(false)
        }
      }
    }

    void loadLookups()

    return () => {
      cancelled = true
    }
  }, [])

  const store = useMemo(() => {
    return createRestDataSource<LearnerGroupRow>({
      controller: 'LearnerGroups',
      key: 'id',
    })
  }, [])

  const categoriesById = useMemo(() => {
    const map = new Map<number, CategoryLookup>()
    categories.forEach(category => {
      map.set(category.id, category)
    })
    return map
  }, [categories])

  const categoriesByParent = useMemo(() => {
    const map = new Map<number, CategoryLookup[]>()

    categories.forEach(category => {
      const parentId = category.parentId ?? 0
      const children = map.get(parentId)
      if (children) {
        children.push(category)
      } else {
        map.set(parentId, [category])
      }
    })

    map.forEach(children => {
      children.sort((a, b) => a.name.localeCompare(b.name))
    })

    return map
  }, [categories])

  const getDescendantCategoryIds = useCallback((startCategoryId: number) => {
    const results: number[] = []
    const stack: number[] = [startCategoryId]

    while (stack.length > 0) {
      const currentId = stack.pop()
      if (!currentId) continue

      results.push(currentId)

      const children = categoriesByParent.get(currentId) ?? []
      for (const child of children) {
        stack.push(child.id)
      }
    }

    return Array.from(new Set(results))
  }, [categoriesByParent])

  const treeData = useMemo<TreeViewNode[]>(() => {
    const toTreeNode = (category: CategoryLookup): TreeViewNode => {
      const children = categoriesByParent.get(category.id) ?? []
      return {
        id: `category-${category.id}`,
        text: category.name,
        categoryId: category.id,
        items: children.map(toTreeNode),
      }
    }

    const rootChildren = categoriesByParent.get(0) ?? []
    return [
      {
        id: 'all-groups-root',
        text: 'All Groups',
        isRoot: true,
        categoryId: 0,
        items: rootChildren.map(toTreeNode),
      },
    ]
  }, [categoriesByParent])

  const getCategoryPath = useCallback((categoryId: number | null | undefined) => {
    if (!categoryId) return 'Root folder'

    const names: string[] = []
    const visited = new Set<number>()
    let currentId: number | null | undefined = categoryId

    while (currentId && !visited.has(currentId)) {
      visited.add(currentId)

      const category = categoriesById.get(currentId)
      if (!category) break

      names.unshift(category.name)
      currentId = category.parentId ?? null
    }

    return names.length > 0 ? names.join(' / ') : 'Root folder'
  }, [categoriesById])

  const handleTreeSelection = useCallback((event: { itemData: TreeViewNode }) => {
    const node = event.itemData
    setSelectedTreeNode(node)
    setIsTreeExpanded(false)

    if (node.isRoot || !node.categoryId) {
      setGridFilters([])
      return
    }

    const targetIds = getDescendantCategoryIds(node.categoryId)
    if (targetIds.length === 0) {
      setGridFilters([])
      return
    }

    if (targetIds.length === 1) {
      setGridFilters(['categoryId', '=', targetIds[0]])
      return
    }

    let expression: FilterExpression = ['categoryId', '=', targetIds[0]]
    for (let index = 1; index < targetIds.length; index += 1) {
      expression = [expression, 'or', ['categoryId', '=', targetIds[index]]]
    }

    setGridFilters(expression)
  }, [getDescendantCategoryIds])

  const handleRowDoubleClick = useCallback((event: { data: LearnerGroupRow }) => {
    if (event.data?.id) {
      navigate(`/learner-groups/${event.data.id}`)
    }
  }, [navigate])

  const selectedFolderLabel = selectedTreeNode && !selectedTreeNode.isRoot
    ? `Folder: ${selectedTreeNode.text}`
    : 'Learner Group Directory'

  const gridColumns = useMemo<AdminGridColumn<LearnerGroupRow>[]>(() => {
    return [
      {
        dataField: 'name',
        caption: 'Group Name',
        minWidth: 240,
        cellRender: ({ value }) => (
          <span className="font-bold text-slate-800">{String(value || '—')}</span>
        ),
      },
      {
        dataField: 'description',
        caption: 'Description',
        minWidth: 260,
        cellRender: ({ value }) => (
          <span className="text-slate-500 font-semibold text-xs">{String(value || '—')}</span>
        ),
      },
      {
        dataField: 'divisionId',
        caption: 'Division',
        width: 140,
        cellRender: ({ value }) => {
          if (value === null || value === undefined) return '—'
          const division = divisions.find(item => item.id === Number(value))
          return (
            <span className="text-slate-600 font-bold text-xs">
              {division ? division.name : `Division ${String(value)}`}
            </span>
          )
        },
      },
      {
        dataField: 'categoryId',
        caption: 'LMS Folder Category',
        minWidth: 220,
        cellRender: ({ value }) => {
          const path = getCategoryPath(value as number | null | undefined)
          return (
            <span className="text-slate-500 font-semibold text-xs" title={path}>
              {path}
            </span>
          )
        },
      },
      {
        dataField: 'memberCount',
        caption: 'Members',
        dataType: 'number',
        width: 100,
        alignment: 'right',
      },
      {
        dataField: 'createdAt',
        caption: 'Created',
        dataType: 'datetime',
        width: 170,
      },
    ]
  }, [divisions, getCategoryPath])

  const actionButtons = useMemo(() => {
    return [
      {
        hint: 'Open Group Details',
        icon: <Info className="h-3.5 w-3.5" />,
        onClick: (event: { row: { data: LearnerGroupRow } }) => {
          if (event.row?.data?.id) {
            navigate(`/learner-groups/${event.row.data.id}`)
          }
        },
      },
    ]
  }, [navigate])

  return (
    <div className="grid min-h-0 flex-1 grid-cols-1 items-stretch gap-5 md:grid-cols-4">
      <aside
        className={`border border-slate-200 rounded-lg bg-white shadow-xs min-h-0 flex-col p-4 md:col-span-1 transition-all ${
          isTreeExpanded ? 'flex max-md:max-h-80' : 'hidden md:flex'
        }`}
      >
        <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-3 select-none">
          <Layers className="h-4 w-4 text-slate-600" />
          <h2 className="text-sm font-bold text-slate-700">Group Folders</h2>
        </div>

        <div className="custom-scrollbar min-h-0 flex-1 overflow-y-auto">
          {loadingLookups ? (
            <div className="flex h-40 items-center justify-center">
              <Loader2 className="h-5 w-5 animate-spin text-slate-400" />
            </div>
          ) : (
            <AppTreeView items={treeData} onItemClick={handleTreeSelection} />
          )}
        </div>
      </aside>

      <main className="flex min-h-0 flex-col md:col-span-3">
        <DataGridSurface
          title={selectedFolderLabel}
          note="Double-click any learner group to manage members and assignment behavior."
          actions={
            <div className="flex items-center gap-2">
              <AppButton
                variant="secondary"
                icon={Layers}
                className="md:hidden"
                onClick={() => setIsTreeExpanded(prev => !prev)}
              >
                {isTreeExpanded ? 'Hide Folders' : 'Folders'}
              </AppButton>
              <Link to="/learner-groups/new">
                <AppButton variant="primary" icon={Plus}>
                  Create Group
                </AppButton>
              </Link>
            </div>
          }
        >
          <AppTable
            store={store}
            columns={gridColumns}
            actionButtons={actionButtons}
            noDataText="No learner groups match the active folder filter"
            onRowDblClick={handleRowDoubleClick}
            searchPlaceholder="Search by group name or description..."
            searchExpr={['name', 'description', 'createdBy']}
            externalFilters={gridFilters}
          />
        </DataGridSurface>
      </main>
    </div>
  )
}
