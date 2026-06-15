import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  ArrowRightLeft,
  ArrowUpRight,
  ChevronLeft,
  Folder,
  FolderPlus,
  Info,
  Layers,
  Loader2,
  Plus,
  Search,
  Trash2,
  X,
} from 'lucide-react'

import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { AppTreeView, type TreeViewNode } from '../../components/ui/AppTreeView'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { ExplorerTable, type ExplorerColumn } from '../../components/ui/explorer/ExplorerTable'
import { useExplorer } from '../../components/ui/explorer/useExplorer'
import { ApiError, fetchWithAccessControl } from '../../lib/apiClient'
import { formatDate } from '../../lib/format'
import { toast } from '../../lib/toast'
import { useSession } from '../../lib/sessionContext'

type ApiEnvelope<T> = {
  success?: boolean
  message?: string
  data?: T
  totalCount?: number
}

// Mirrors LearnerGroupCategoryDto (iLearn.Application/DTOs/LearnerGroupCategoryDto.cs)
type CategoryLookup = {
  id: number
  name: string
  description?: string | null
  parentId?: number | null
  depth?: number
  childCount?: number
  learnerGroupCount?: number
  createdAt?: string
}

// Mirrors division lookup row from admin/DivisionsCRUD/Get
type DivisionLookup = {
  id: number
  name: string
}

// Mirrors LearnerGroupDto (iLearn.Application/DTOs/LearnerGroupDto.cs)
type GroupDto = {
  id: number
  name: string
  description?: string | null
  divisionId?: number | null
  categoryId?: number | null
  memberCount?: number
  createdAt?: string
  updatedAt?: string
}

type ExplorerItem = {
  id: number
  name: string
  description: string
  isFolder: boolean
  countText: string
  updatedAt: string
  original: CategoryLookup | GroupDto
}

type LearnerGroupExplorerPath = {
  categoryId: number
}

function unwrapList<T>(value: ApiEnvelope<T[]> | { data?: T[] } | T[] | undefined): T[] {
  if (!value) return []
  if (Array.isArray(value)) return value

  const boxed = value as { data?: T[] }
  return Array.isArray(boxed.data) ? boxed.data : []
}

function getApiErrorText(error: unknown, fallback: string) {
  if (error instanceof ApiError) {
    try {
      const parsed = JSON.parse(error.responseBody) as { message?: string; title?: string }
      if (parsed.message) return parsed.message
      if (parsed.title) return parsed.title
    } catch {
      if (error.responseBody) return error.responseBody
    }

    if (error.message) return error.message
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function sortByNameAsc<T extends { name: string }>(a: T, b: T) {
  return a.name.localeCompare(b.name)
}

function toDateText(value: string | undefined | null) {
  if (!value) return '-'

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '-'
  return formatDate(date)
}

export function LearnerGroupListPage() {
  const navigate = useNavigate()
  const { confirm, confirmDialog } = useConfirm()
  const { isSuperAdmin } = useSession()

  const [categories, setCategories] = useState<CategoryLookup[]>([])
  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [groups, setGroups] = useState<GroupDto[]>([])

  const [loading, setLoading] = useState(true)

  const [isNewFolderOpen, setIsNewFolderOpen] = useState(false)
  const [newFolderName, setNewFolderName] = useState('')
  const [newFolderDesc, setNewFolderDesc] = useState('')
  const [newFolderDivisionId, setNewFolderDivisionId] = useState<number | ''>('')
  const [creatingFolder, setCreatingFolder] = useState(false)

  const [movingGroup, setMovingGroup] = useState<GroupDto | null>(null)
  const [relocateCategoryId, setRelocateCategoryId] = useState<number>(0)
  const [movingInProgress, setMovingInProgress] = useState(false)

  const categoriesById = useMemo(() => {
    const map = new Map<number, CategoryLookup>()
    for (const category of categories) {
      map.set(category.id, category)
    }
    return map
  }, [categories])

  const categoriesByParent = useMemo(() => {
    const map = new Map<number, CategoryLookup[]>()

    for (const category of categories) {
      const parentId = category.parentId ?? 0
      const existing = map.get(parentId)
      if (existing) {
        existing.push(category)
      } else {
        map.set(parentId, [category])
      }
    }

    map.forEach(children => children.sort(sortByNameAsc))
    return map
  }, [categories])

  const groupsByCategory = useMemo(() => {
    const map = new Map<number, GroupDto[]>()

    for (const group of groups) {
      const categoryId = group.categoryId ?? 0
      const existing = map.get(categoryId)
      if (existing) {
        existing.push(group)
      } else {
        map.set(categoryId, [group])
      }
    }

    map.forEach(children => children.sort(sortByNameAsc))
    return map
  }, [groups])

  const {
    path,
    searchTerm,
    setSearchTerm,
    navigateToPath,
    goBack,
    filterBySearch,
  } = useExplorer<LearnerGroupExplorerPath>({
    rootPath: { categoryId: 0 },
    parsePath: params => {
      const rawCategoryId = params.get('categoryId')
      const parsedCategoryId = Number(rawCategoryId ?? '0')
      const categoryId = Number.isFinite(parsedCategoryId) && parsedCategoryId > 0
        ? Math.trunc(parsedCategoryId)
        : 0

      return { categoryId }
    },
    toParams: currentPath => {
      return currentPath.categoryId > 0
        ? { categoryId: String(currentPath.categoryId) }
        : {}
    },
    getParentPath: currentPath => {
      if (currentPath.categoryId === 0) return null

      const current = categoriesById.get(currentPath.categoryId)
      const parentId = current?.parentId ?? 0
      return { categoryId: parentId > 0 ? parentId : 0 }
    },
    buildBreadcrumbs: currentPath => {
      const rootCrumbs = [{ to: '/learner-groups', label: 'Learner Groups' }]
      if (currentPath.categoryId === 0) return rootCrumbs

      const trail: { to: string; label: string }[] = []
      const visited = new Set<number>()

      let cursor: number | null = currentPath.categoryId
      while (cursor && !visited.has(cursor)) {
        visited.add(cursor)
        const category = categoriesById.get(cursor)
        if (!category) break

        trail.unshift({
          to: `/learner-groups?categoryId=${category.id}`,
          label: category.name,
        })
        cursor = category.parentId ?? null
      }

      return [...rootCrumbs, ...trail]
    },
    isPathValid: currentPath => {
      if (currentPath.categoryId === 0) return true
      return categories.some(category => category.id === currentPath.categoryId)
    },
    canValidatePath: !loading && categories.length > 0,
  })

  const currentCategoryId = path.categoryId

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      const [categoryResp, divisionResp, groupResp] = await Promise.all([
        fetchWithAccessControl<ApiEnvelope<CategoryLookup[]>>('LearnerGroupCategories'),
        fetchWithAccessControl<{ data?: DivisionLookup[] } | DivisionLookup[]>('admin/DivisionsCRUD/Get'),
        fetchWithAccessControl<ApiEnvelope<GroupDto[]>>('LearnerGroups'),
      ])

      setCategories(unwrapList(categoryResp))
      setDivisions(unwrapList(divisionResp))
      setGroups(unwrapList(groupResp))
    } catch (error) {
      console.error('Failed to load explorer data', error)
      toast.error(getApiErrorText(error, 'Failed to load explorer contents'))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    let cancelled = false

    const load = async () => {
      if (cancelled) return
      await loadData()
    }

    void load()

    return () => {
      cancelled = true
    }
  }, [loadData])

  const currentItems = useMemo<ExplorerItem[]>(() => {
    const childFolders = (categoriesByParent.get(currentCategoryId) ?? []).map(folder => {
      const nestedFolderCount = categoriesByParent.get(folder.id)?.length ?? 0
      const nestedGroupCount = groupsByCategory.get(folder.id)?.length ?? 0

      return {
        id: folder.id,
        name: folder.name,
        description: folder.description || 'Category folder',
        isFolder: true,
        countText: `${nestedFolderCount + nestedGroupCount} items`,
        updatedAt: folder.createdAt || '',
        original: folder,
      }
    })

    const childGroups = (groupsByCategory.get(currentCategoryId) ?? []).map(group => {
      return {
        id: group.id,
        name: group.name,
        description: group.description || 'Learner group',
        isFolder: false,
        countText: `${group.memberCount || 0} members`,
        updatedAt: group.updatedAt || group.createdAt || '',
        original: group,
      }
    })

    childFolders.sort(sortByNameAsc)
    childGroups.sort(sortByNameAsc)

    return [...childFolders, ...childGroups]
  }, [categoriesByParent, currentCategoryId, groupsByCategory])

  const filteredItems = useMemo(() => {
    return filterBySearch(currentItems, (item, normalizedTerm) => {
      return item.name.toLowerCase().includes(normalizedTerm) || item.description.toLowerCase().includes(normalizedTerm)
    })
  }, [currentItems, filterBySearch])

  const relocateTreeNodes = useMemo<TreeViewNode[]>(() => {
    const toNode = (category: CategoryLookup): TreeViewNode => {
      const children = categoriesByParent.get(category.id) ?? []
      return {
        id: `reloc-cat-${category.id}`,
        text: category.name,
        categoryId: category.id,
        items: children.map(toNode),
      }
    }

    const roots = categoriesByParent.get(0) ?? []

    return [
      {
        id: 'reloc-root',
        text: 'Root Folder (No Category)',
        isRoot: true,
        categoryId: 0,
        items: roots.map(toNode),
      },
    ]
  }, [categoriesByParent])

  const relocateTargetCategoryPath = useMemo(() => {
    if (relocateCategoryId === 0) return 'Root Folder'

    const path: string[] = []
    const visited = new Set<number>()

    let cursor: number | null = relocateCategoryId
    while (cursor && !visited.has(cursor)) {
      visited.add(cursor)

      const category = categoriesById.get(cursor)
      if (!category) break

      path.unshift(category.name)
      cursor = category.parentId ?? null
    }

    return path.length > 0 ? path.join(' / ') : 'Root Folder'
  }, [categoriesById, relocateCategoryId])

  const handleOpenItem = useCallback((item: ExplorerItem) => {
    if (item.isFolder) {
      navigateToPath({ categoryId: item.id })
      return
    }

    navigate(`/learner-groups/${item.id}`)
  }, [navigate, navigateToPath])

  const currentFolderName = useMemo(() => {
    if (currentCategoryId === 0) return 'Learner Group Explorer'
    return categoriesById.get(currentCategoryId)?.name ?? 'Learner Group Explorer'
  }, [categoriesById, currentCategoryId])

  const handleCreateFolder = useCallback(async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const normalizedName = newFolderName.trim()
    if (!normalizedName) {
      toast.error('Folder name is required')
      return
    }

    setCreatingFolder(true)
    try {
      const response = await fetchWithAccessControl<ApiEnvelope<CategoryLookup>>('LearnerGroupCategories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: normalizedName,
          description: newFolderDesc.trim() || null,
          parentId: currentCategoryId > 0 ? currentCategoryId : null,
          divisionId: isSuperAdmin && currentCategoryId === 0 && newFolderDivisionId !== '' ? Number(newFolderDivisionId) : null,
        }),
      })

      if (response.success === false) {
        throw new Error(response.message || 'Failed to create folder')
      }

      toast.success(`Folder "${normalizedName}" created successfully`)
      setIsNewFolderOpen(false)
      setNewFolderName('')
      setNewFolderDesc('')
      setNewFolderDivisionId('')
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to create folder'))
    } finally {
      setCreatingFolder(false)
    }
  }, [currentCategoryId, loadData, newFolderDesc, newFolderName, isSuperAdmin, newFolderDivisionId])

  const openNewFolderModal = useCallback(() => {
    setNewFolderName('')
    setNewFolderDesc('')
    setNewFolderDivisionId('')
    setIsNewFolderOpen(true)
  }, [])


  const handleDeleteFolder = useCallback(async (folder: CategoryLookup) => {
    const hasChildren = (folder.childCount ?? 0) > 0
    const hasGroups = (folder.learnerGroupCount ?? 0) > 0

    if (hasChildren || hasGroups) {
      toast.error('This folder is not empty. Move sub-folders and groups before deleting.')
      return
    }

    const ok = await confirm({
      title: 'Delete Folder',
      message: `Delete folder "${folder.name}"? This action cannot be undone.`,
      danger: true,
      confirmLabel: 'Delete Folder',
    })

    if (!ok) return

    try {
      await fetchWithAccessControl<void>(`LearnerGroupCategories/${folder.id}`, {
        method: 'DELETE',
      })

      toast.success(`Folder "${folder.name}" deleted successfully`)
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to delete folder'))
    }
  }, [confirm, loadData])

  const handleDeleteGroup = useCallback(async (group: GroupDto) => {
    const ok = await confirm({
      title: 'Delete Learner Group',
      message: `Delete learner group "${group.name}"? This action cannot be undone.`,
      danger: true,
      confirmLabel: 'Delete Group',
    })

    if (!ok) return

    try {
      await fetchWithAccessControl<void>(`LearnerGroups/${group.id}`, {
        method: 'DELETE',
      })

      toast.success(`Group "${group.name}" deleted successfully`)
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to delete learner group'))
    }
  }, [confirm, loadData])

  const handleOpenMove = useCallback((group: GroupDto) => {
    setMovingGroup(group)
    setRelocateCategoryId(group.categoryId ?? 0)
  }, [])

  const handleConfirmMove = useCallback(async () => {
    if (!movingGroup) return

    if ((movingGroup.categoryId ?? 0) === relocateCategoryId) {
      setMovingGroup(null)
      return
    }

    setMovingInProgress(true)
    try {
      let name = movingGroup.name.trim()
      let description = (movingGroup.description || '').trim()

      if (!name || !description) {
        const detailResp = await fetchWithAccessControl<ApiEnvelope<{ name: string; description?: string | null }>>(`LearnerGroups/${movingGroup.id}`)
        if (!detailResp.data?.name) {
          throw new Error('Unable to load latest group details')
        }

        name = detailResp.data.name.trim()
        description = (detailResp.data.description || '').trim()
      }

      if (!name || !description) {
        throw new Error('Group name and description are required to move this group')
      }

      const response = await fetchWithAccessControl<ApiEnvelope<unknown>>(`LearnerGroups/${movingGroup.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name,
          description,
          categoryId: relocateCategoryId > 0 ? relocateCategoryId : null,
        }),
      })

      if (response.success === false) {
        throw new Error(response.message || 'Failed to relocate learner group')
      }

      toast.success(`Group "${movingGroup.name}" moved successfully`)
      setMovingGroup(null)
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to relocate learner group'))
    } finally {
      setMovingInProgress(false)
    }
  }, [loadData, movingGroup, relocateCategoryId])

  const getDivisionName = useCallback((divisionId: number | null | undefined) => {
    if (!divisionId) return '-'

    const division = divisions.find(item => item.id === divisionId)
    return division ? division.name : `Division ${divisionId}`
  }, [divisions])

  const tableColumns = useMemo<ExplorerColumn<ExplorerItem>[]>(() => [
    {
      key: 'name',
      title: 'Name',
      render: item => (
        <div className="flex items-center gap-2.5">
          {item.isFolder ? (
            <Folder className="h-4.5 w-4.5 shrink-0 text-amber-500" />
          ) : (
            <Layers className="h-4.5 w-4.5 shrink-0 text-indigo-500" />
          )}
          <span className="truncate font-bold text-slate-800">{item.name}</span>
        </div>
      ),
    },
    {
      key: 'description',
      title: 'Description',
      headerClassName: 'w-80',
      cellClassName: 'text-xs font-semibold text-slate-500',
      render: item => <span className="block truncate" title={item.description}>{item.description}</span>,
    },
    {
      key: 'type',
      title: 'Type',
      headerClassName: 'w-32 text-center',
      cellClassName: 'text-center',
      render: item => (
        <span className={`inline-flex rounded border px-2 py-0.5 text-[10px] font-extrabold uppercase ${
          item.isFolder
            ? 'border-amber-100 bg-amber-50 text-amber-700'
            : 'border-indigo-100 bg-indigo-50 text-indigo-700'
        }`}>
          {item.isFolder ? 'Folder' : 'Group'}
        </span>
      ),
    },
    {
      key: 'size',
      title: 'Size / Members',
      headerClassName: 'w-36 text-center',
      cellClassName: 'text-center text-xs font-bold text-slate-500',
      render: item => item.countText,
    },
    {
      key: 'meta',
      title: 'Division / Updated',
      headerClassName: 'w-44',
      cellClassName: 'text-xs font-semibold text-slate-500',
      render: item => {
        const itemDivision = !item.isFolder
          ? getDivisionName((item.original as GroupDto).divisionId)
          : '-'

        return (
          <div className="flex flex-col gap-0.5">
            <span className="truncate">{itemDivision}</span>
            <span className="text-[11px] text-slate-400">
              {item.isFolder ? 'Created ' : 'Updated '}
              {toDateText(item.updatedAt)}
            </span>
          </div>
        )
      },
    },
    {
      key: 'actions',
      title: 'Actions',
      headerClassName: 'w-32 text-center',
      render: item => (
        <div className="flex items-center justify-center gap-1.5" onClick={event => event.stopPropagation()}>
          {item.isFolder ? (
            <button
              type="button"
              onClick={() => void handleDeleteFolder(item.original as CategoryLookup)}
              className="p-1 text-red-500 hover:bg-rose-50 rounded-md transition cursor-pointer"
              title="Delete Folder"
            >
              <Trash2 className="h-3.5 w-3.5" />
            </button>
          ) : (
            <>
              <button
                type="button"
                onClick={() => handleOpenMove(item.original as GroupDto)}
                className="p-1 text-indigo-500 hover:bg-indigo-50 rounded-md transition cursor-pointer"
                title="Move Group"
              >
                <ArrowRightLeft className="h-3.5 w-3.5" />
              </button>
              <button
                type="button"
                onClick={() => void handleDeleteGroup(item.original as GroupDto)}
                className="p-1 text-red-500 hover:bg-rose-50 rounded-md transition cursor-pointer"
                title="Delete Group"
              >
                <Trash2 className="h-3.5 w-3.5" />
              </button>
            </>
          )}

          <button
            type="button"
            onClick={() => handleOpenItem(item)}
            className="p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 rounded-md transition cursor-pointer"
            title={item.isFolder ? 'Open Folder' : 'Open Group Details'}
          >
            {item.isFolder ? <ArrowUpRight className="h-3.5 w-3.5" /> : <Info className="h-3.5 w-3.5" />}
          </button>
        </div>
      ),
    },
  ], [getDivisionName, handleDeleteFolder, handleDeleteGroup, handleOpenItem, handleOpenMove])

  return (
    <>
      <DataGridSurface
        title={currentFolderName}
        note="Manage folders and learner groups in this directory."
        actions={
          <div className="flex items-center gap-2">
            {currentCategoryId > 0 && (
              <AppButton variant="ghost" icon={ChevronLeft} onClick={goBack}>
                Back
              </AppButton>
            )}
            <AppButton variant="secondary" icon={FolderPlus} onClick={openNewFolderModal}>
              New Folder
            </AppButton>
            <Link to={currentCategoryId > 0 ? `/learner-groups/new?categoryId=${currentCategoryId}` : '/learner-groups/new'}>
              <AppButton variant="primary" icon={Plus}>
                Create Group
              </AppButton>
            </Link>
          </div>
        }
      >
        <div className="flex min-h-0 flex-1 flex-col gap-3 pt-4 pb-0">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <div className="text-xs font-semibold text-slate-500">
              Showing <span className="font-bold text-slate-800">{filteredItems.length}</span> items in this folder
            </div>

            <div className="relative w-full sm:w-80">
              <Search className="pointer-events-none absolute left-3 top-2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                value={searchTerm}
                onChange={event => setSearchTerm(event.target.value)}
                placeholder="Search folders or groups in this folder..."
                className="w-full rounded-lg border border-slate-200 bg-white py-1.5 pl-9 pr-8 text-xs font-semibold text-slate-700 shadow-3xs transition focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-100"
              />
              {searchTerm && (
                <button
                  type="button"
                  onClick={() => setSearchTerm('')}
                  className="absolute right-2.5 top-2 rounded-full p-0.5 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
                >
                  <X className="h-3 w-3" />
                </button>
              )}
            </div>
          </div>

          <ExplorerTable
            loading={loading}
            loadingLabel="Loading directory..."
            emptyText="This folder is empty. Create a folder or learner group to start."
            columns={tableColumns}
            items={filteredItems}
            getRowKey={item => `${item.isFolder ? 'folder' : 'group'}-${item.id}`}
            onRowDoubleClick={handleOpenItem}
          />
        </div>
      </DataGridSurface>

      {isNewFolderOpen && (
        <div className="modal-overlay" onClick={() => setIsNewFolderOpen(false)}>
          <form
            className="modal-window"
            onClick={event => event.stopPropagation()}
            onSubmit={handleCreateFolder}
          >
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <FolderPlus className="h-5 w-5 text-indigo-500" />
                <h3 className="text-sm font-extrabold uppercase tracking-wide text-slate-800">Create Folder</h3>
              </div>
              <button
                type="button"
                onClick={() => setIsNewFolderOpen(false)}
                className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="px-6 py-4 space-y-3">
              {isSuperAdmin && currentCategoryId === 0 && (
                <div className="space-y-1">
                  <label htmlFor="newFolderDivisionId" className="wiz-label">
                    Division (แผนก)
                  </label>
                  <select
                    id="newFolderDivisionId"
                    value={newFolderDivisionId}
                    onChange={event =>
                      setNewFolderDivisionId(event.target.value === '' ? '' : Number(event.target.value))
                    }
                    className="wiz-input"
                  >
                    <option value="">Global / ไม่ระบุแผนก</option>
                    {divisions.map(div => (
                      <option key={div.id} value={div.id}>
                        {div.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <div className="space-y-1">
                <label htmlFor="folderName" className="wiz-label">Folder Name <span className="text-red-500">*</span></label>
                <input
                  id="folderName"
                  type="text"
                  autoFocus
                  value={newFolderName}
                  onChange={event => setNewFolderName(event.target.value)}
                  className="wiz-input"
                  placeholder="e.g. Finance & Accounting"
                />
              </div>

              <div className="space-y-1">
                <label htmlFor="folderDesc" className="wiz-label">Description (Optional)</label>
                <textarea
                  id="folderDesc"
                  value={newFolderDesc}
                  onChange={event => setNewFolderDesc(event.target.value)}
                  rows={3}
                  className="wiz-input resize-none"
                  placeholder="Short note for admins"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <button
                type="button"
                onClick={() => setIsNewFolderOpen(false)}
                className="px-4 py-2 text-xs font-bold text-slate-500 hover:text-slate-700 rounded-lg hover:bg-slate-100 transition cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={creatingFolder || !newFolderName.trim()}
                className="inline-flex items-center gap-1.5 rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-xs font-bold text-white hover:bg-indigo-700 cursor-pointer shadow-3xs disabled:opacity-55"
              >
                {creatingFolder && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                Create Folder
              </button>
            </div>
          </form>
        </div>
      )}

      {movingGroup && (
        <div className="modal-overlay" onClick={() => setMovingGroup(null)}>
          <div className="modal-window" onClick={event => event.stopPropagation()}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <ArrowRightLeft className="h-5 w-5 text-indigo-500" />
                <h3 className="text-sm font-extrabold uppercase tracking-wide text-slate-800">Move Learner Group</h3>
              </div>
              <button
                type="button"
                onClick={() => setMovingGroup(null)}
                className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="px-6 py-3 border-b border-slate-100 bg-indigo-50/40 text-xs font-semibold text-slate-600">
              Move group <span className="font-bold text-slate-800">{movingGroup.name}</span> to another folder.
            </div>

            <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/30">
              <div className="rounded-lg border border-slate-200 bg-white p-2 max-h-72 overflow-y-auto custom-scrollbar">
                <AppTreeView
                  items={relocateTreeNodes}
                  onItemClick={event => setRelocateCategoryId(event.itemData.categoryId ?? 0)}
                />
              </div>
            </div>

            <div className="px-6 py-3 bg-slate-50/60 border-b border-slate-100 text-xs">
              <span className="font-bold text-slate-400 uppercase text-xxs mr-1.5">Destination:</span>
              <span className="font-semibold text-indigo-700">{relocateTargetCategoryPath}</span>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 bg-slate-50/50">
              <button
                type="button"
                onClick={() => setMovingGroup(null)}
                className="px-4 py-2 text-xs font-bold text-slate-500 hover:text-slate-700 rounded-lg hover:bg-slate-100 transition cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => void handleConfirmMove()}
                disabled={movingInProgress}
                className="inline-flex items-center gap-1.5 rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-xs font-bold text-white hover:bg-indigo-700 cursor-pointer shadow-3xs disabled:opacity-55"
              >
                {movingInProgress && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                Relocate Group
              </button>
            </div>
          </div>
        </div>
      )}

      {confirmDialog}
    </>
  )
}
