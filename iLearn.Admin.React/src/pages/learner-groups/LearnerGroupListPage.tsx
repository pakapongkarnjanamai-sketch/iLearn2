import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ArrowRightLeft, Folder, FolderOpen, FolderPlus, Info, Layers, Loader2, Plus, Trash2, X } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { AppTable, type AdminGridColumn } from '../../components/ui/AppTable'
import { AppTreeView, type TreeViewNode } from '../../components/ui/AppTreeView'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { ApiError, fetchWithAccessControl } from '../../lib/apiClient'
import { createRestDataSource } from '../../lib/createRestDataSource'
import { toast } from '../../lib/toast'

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
  parentId?: number | null
  depth?: number
  childCount?: number
  learnerGroupCount?: number
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

type MoveDialogState = {
  id: number
  name: string
  description: string
  categoryId: number
}

type FilterExpression = unknown[]

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

const ROOT_NODE: TreeViewNode = {
  id: 'all-groups-root',
  text: 'All Groups (Root)',
  isRoot: true,
  categoryId: 0,
  items: [],
}

export function LearnerGroupListPage() {
  const navigate = useNavigate()
  const { confirm, confirmDialog } = useConfirm()

  const [categories, setCategories] = useState<CategoryLookup[]>([])
  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [loadingLookups, setLoadingLookups] = useState(true)

  const [selectedTreeNode, setSelectedTreeNode] = useState<TreeViewNode>(ROOT_NODE)
  const [isTreeExpanded, setIsTreeExpanded] = useState(false)
  const [gridFilters, setGridFilters] = useState<FilterExpression>([])
  const [tableReloadToken, setTableReloadToken] = useState(0)

  const [isCreateFolderOpen, setIsCreateFolderOpen] = useState(false)
  const [newFolderName, setNewFolderName] = useState('')
  const [newFolderDescription, setNewFolderDescription] = useState('')
  const [creatingFolder, setCreatingFolder] = useState(false)

  const [movingGroup, setMovingGroup] = useState<MoveDialogState | null>(null)
  const [moveTargetCategoryId, setMoveTargetCategoryId] = useState<number>(0)
  const [movingInProgress, setMovingInProgress] = useState(false)

  const selectedCategoryId = selectedTreeNode.categoryId ?? 0

  const loadLookups = useCallback(async () => {
    setLoadingLookups(true)
    try {
      const [categoryResp, divisionResp] = await Promise.all([
        fetchWithAccessControl<ApiEnvelope<CategoryLookup[]>>('LearnerGroupCategories'),
        fetchWithAccessControl<{ data?: DivisionLookup[] } | DivisionLookup[]>('admin/DivisionsCRUD/Get'),
      ])

      setCategories(unwrapList(categoryResp))
      setDivisions(unwrapList(divisionResp))
    } catch (error) {
      console.error('Failed to load learner group lookups', error)
      toast.error(getApiErrorText(error, 'Failed to load group folder tree'))
    } finally {
      setLoadingLookups(false)
    }
  }, [])

  useEffect(() => {
    let cancelled = false

    const load = async () => {
      if (cancelled) return
      await loadLookups()
    }

    void load()

    return () => {
      cancelled = true
    }
  }, [loadLookups])

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
        id: ROOT_NODE.id,
        text: ROOT_NODE.text,
        isRoot: true,
        categoryId: 0,
        items: rootChildren.map(toTreeNode),
      },
    ]
  }, [categoriesByParent])

  const moveTreeData = useMemo<TreeViewNode[]>(() => {
    const rootChildren = treeData[0]?.items ?? []
    return [
      {
        id: 'move-root',
        text: 'Root Folder (No Category)',
        isRoot: true,
        categoryId: 0,
        items: rootChildren,
      },
    ]
  }, [treeData])

  const subFolders = useMemo(() => {
    return categoriesByParent.get(selectedCategoryId) ?? []
  }, [categoriesByParent, selectedCategoryId])

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

  const selectedFolderPath = useMemo(() => {
    if (!selectedCategoryId) return 'Root folder'
    return getCategoryPath(selectedCategoryId)
  }, [getCategoryPath, selectedCategoryId])

  const moveTargetPath = useMemo(() => {
    if (!moveTargetCategoryId) return 'Root folder'
    return getCategoryPath(moveTargetCategoryId)
  }, [getCategoryPath, moveTargetCategoryId])

  const currentCreateGroupLink = selectedCategoryId > 0
    ? `/learner-groups/new?categoryId=${selectedCategoryId}`
    : '/learner-groups/new'

  const handleTreeSelection = useCallback((event: { itemData: TreeViewNode }) => {
    const node = event.itemData
    setSelectedTreeNode(node)
    setIsTreeExpanded(false)
  }, [])

  const handleEnterSubFolder = useCallback((folder: CategoryLookup) => {
    setSelectedTreeNode({
      id: `category-${folder.id}`,
      text: folder.name,
      categoryId: folder.id,
    })
  }, [])

  const handleOpenMoveDialog = useCallback((row: LearnerGroupRow) => {
    if (!row.id) return

    const groupCategoryId = row.categoryId ? Number(row.categoryId) : 0
    setMovingGroup({
      id: Number(row.id),
      name: String(row.name ?? ''),
      description: String(row.description ?? ''),
      categoryId: groupCategoryId,
    })
    setMoveTargetCategoryId(groupCategoryId)
  }, [])

  const handleOpenCreateFolder = useCallback(() => {
    setNewFolderName('')
    setNewFolderDescription('')
    setIsCreateFolderOpen(true)
  }, [])

  const handleCreateFolder = useCallback(async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const normalizedName = newFolderName.trim()
    if (!normalizedName) {
      toast.error('Folder name is required')
      return
    }

    setCreatingFolder(true)
    try {
      const payload = {
        name: normalizedName,
        description: newFolderDescription.trim() || null,
        parentId: selectedCategoryId > 0 ? selectedCategoryId : null,
      }

      const response = await fetchWithAccessControl<ApiEnvelope<CategoryLookup>>('LearnerGroupCategories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })

      if (response.success === false) {
        throw new Error(response.message || 'Failed to create folder')
      }

      toast.success(`Folder "${normalizedName}" created successfully`)
      setIsCreateFolderOpen(false)
      await loadLookups()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to create folder'))
    } finally {
      setCreatingFolder(false)
    }
  }, [loadLookups, newFolderDescription, newFolderName, selectedCategoryId])

  const handleDeleteFolder = useCallback(async (folder: CategoryLookup) => {
    const hasChildren = (folder.childCount ?? 0) > 0
    const hasGroups = (folder.learnerGroupCount ?? 0) > 0

    if (hasChildren || hasGroups) {
      toast.error('This folder is not empty. Move sub-folders and groups before deleting.')
      return
    }

    const confirmed = await confirm({
      title: 'Delete Folder',
      message: `Delete folder "${folder.name}"? This action cannot be undone.`,
      danger: true,
      confirmLabel: 'Delete Folder',
    })

    if (!confirmed) return

    try {
      await fetchWithAccessControl<void>(`LearnerGroupCategories/${folder.id}`, {
        method: 'DELETE',
      })
      toast.success(`Folder "${folder.name}" deleted successfully`)
      await loadLookups()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to delete folder'))
    }
  }, [confirm, loadLookups])

  const handleConfirmMove = useCallback(async () => {
    if (!movingGroup) return

    if (moveTargetCategoryId === movingGroup.categoryId) {
      setMovingGroup(null)
      return
    }

    setMovingInProgress(true)
    try {
      let nextName = movingGroup.name.trim()
      let nextDescription = movingGroup.description.trim()

      if (!nextName || !nextDescription) {
        const detailResp = await fetchWithAccessControl<ApiEnvelope<{ name: string; description?: string }>>(`LearnerGroups/${movingGroup.id}`)
        if (!detailResp.data?.name) {
          throw new Error('Unable to load current group details for moving')
        }

        nextName = detailResp.data.name.trim()
        nextDescription = (detailResp.data.description || '').trim()
      }

      if (!nextName || !nextDescription) {
        throw new Error('Cannot move this group because required name/description is missing')
      }

      const response = await fetchWithAccessControl<ApiEnvelope<unknown>>(`LearnerGroups/${movingGroup.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: nextName,
          description: nextDescription,
          categoryId: moveTargetCategoryId > 0 ? moveTargetCategoryId : null,
        }),
      })

      if (response.success === false) {
        throw new Error(response.message || 'Failed to move learner group')
      }

      toast.success(`Moved "${movingGroup.name}" successfully`)
      setMovingGroup(null)
      await loadLookups()
      setTableReloadToken(prev => prev + 1)
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to move learner group'))
    } finally {
      setMovingInProgress(false)
    }
  }, [loadLookups, moveTargetCategoryId, movingGroup])

  useEffect(() => {
    if (selectedCategoryId === 0) {
      setGridFilters([['rootCategoryOnly', '=', true]])
      return
    }

    setGridFilters([['categoryId', '=', selectedCategoryId]])
  }, [selectedCategoryId])

  useEffect(() => {
    if (!selectedCategoryId) return

    const exists = categories.some(category => category.id === selectedCategoryId)
    if (!exists) {
      setSelectedTreeNode(ROOT_NODE)
    }
  }, [categories, selectedCategoryId])

  const handleRowDoubleClick = useCallback((event: { data: LearnerGroupRow }) => {
    if (event.data?.id) {
      navigate(`/learner-groups/${event.data.id}`)
    }
  }, [navigate])

  const selectedFolderLabel = selectedTreeNode && !selectedTreeNode.isRoot
    ? `Folder: ${selectedFolderPath}`
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
        hint: 'Move Group To Another Folder',
        icon: <ArrowRightLeft className="h-3.5 w-3.5" />,
        onClick: (event: { row: { data: LearnerGroupRow } }) => {
          handleOpenMoveDialog(event.row.data)
        },
      },
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
  }, [handleOpenMoveDialog, navigate])

  return (
    <>
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
            note="Double-click folder cards to navigate deeper. The table lists groups directly inside the current folder."
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
                <AppButton variant="secondary" icon={FolderPlus} onClick={handleOpenCreateFolder}>
                  New Folder
                </AppButton>
                <Link to={currentCreateGroupLink}>
                  <AppButton variant="primary" icon={Plus}>
                    Create Group
                  </AppButton>
                </Link>
              </div>
            }
          >
            <div className="flex min-h-0 flex-1 flex-col gap-4">
              <section className="shrink-0">
                <div className="mb-2 flex items-center justify-between">
                  <div className="text-xxs font-extrabold uppercase tracking-wider text-slate-400">
                    Sub-Folders ({subFolders.length})
                  </div>
                  <div className="text-xxs font-bold text-slate-400">
                    Current Path: {selectedFolderPath}
                  </div>
                </div>

                {subFolders.length === 0 ? (
                  <div className="rounded-md border border-dashed border-slate-200 bg-slate-50/60 px-4 py-3 text-xs font-semibold text-slate-500">
                    No sub-folders in this location.
                  </div>
                ) : (
                  <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 xl:grid-cols-3">
                    {subFolders.map(folder => {
                      const canDelete = (folder.childCount ?? 0) === 0 && (folder.learnerGroupCount ?? 0) === 0

                      return (
                        <div
                          key={folder.id}
                          onDoubleClick={() => handleEnterSubFolder(folder)}
                          className="group relative rounded-lg border border-slate-200 bg-slate-50/70 p-3 transition hover:border-indigo-200 hover:bg-indigo-50/40"
                        >
                          <div className="flex items-start gap-2.5">
                            <Folder className="mt-0.5 h-4 w-4 shrink-0 text-amber-500" />
                            <div className="min-w-0 flex-1">
                              <div className="truncate text-xs font-bold text-slate-800" title={folder.name}>
                                {folder.name}
                              </div>
                              <div className="mt-0.5 text-xxs font-semibold text-slate-500">
                                {(folder.childCount ?? 0)} sub-folders • {(folder.learnerGroupCount ?? 0)} groups
                              </div>
                            </div>
                          </div>

                          <div className="mt-2 flex items-center justify-end gap-1">
                            <button
                              type="button"
                              onClick={() => handleEnterSubFolder(folder)}
                              className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xxs font-bold text-indigo-600 hover:bg-indigo-100/60"
                              title="Open Folder"
                            >
                              <FolderOpen className="h-3.5 w-3.5" />
                              Open
                            </button>

                            <button
                              type="button"
                              onClick={() => void handleDeleteFolder(folder)}
                              disabled={!canDelete}
                              className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xxs font-bold text-red-600 hover:bg-red-50 disabled:cursor-not-allowed disabled:text-slate-300 disabled:hover:bg-transparent"
                              title={canDelete ? 'Delete Folder' : 'Folder must be empty before deleting'}
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                              Delete
                            </button>
                          </div>
                        </div>
                      )
                    })}
                  </div>
                )}
              </section>

              <section className="min-h-0 flex-1">
                <div className="mb-2 text-xxs font-extrabold uppercase tracking-wider text-slate-400">
                  Learner Groups In This Folder
                </div>
                <div className="min-h-0 h-full border border-slate-200/70 rounded-lg overflow-hidden">
                  <AppTable
                    key={`learner-groups-table-${selectedCategoryId}-${tableReloadToken}`}
                    store={store}
                    columns={gridColumns}
                    actionButtons={actionButtons}
                    noDataText="No learner groups in this folder"
                    onRowDblClick={handleRowDoubleClick}
                    searchPlaceholder="Search by group name or description..."
                    searchExpr={['name', 'description', 'createdBy']}
                    externalFilters={gridFilters}
                  />
                </div>
              </section>
            </div>
          </DataGridSurface>
        </main>
      </div>

      {isCreateFolderOpen && (
        <div className="modal-overlay" onClick={() => setIsCreateFolderOpen(false)}>
          <form className="modal-window" onClick={event => event.stopPropagation()} onSubmit={handleCreateFolder}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <FolderPlus className="h-5 w-5 text-indigo-500" />
                <h3 className="text-sm font-extrabold uppercase tracking-wide text-slate-800">Create Folder</h3>
              </div>
              <button
                type="button"
                onClick={() => setIsCreateFolderOpen(false)}
                className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="px-6 py-4 space-y-3">
              <div className="space-y-1">
                <label htmlFor="new-folder-name" className="wiz-label">
                  Folder Name <span className="text-red-500">*</span>
                </label>
                <input
                  id="new-folder-name"
                  type="text"
                  autoFocus
                  value={newFolderName}
                  onChange={event => setNewFolderName(event.target.value)}
                  className="wiz-input"
                  placeholder="e.g. Finance / Accounting"
                />
              </div>

              <div className="space-y-1">
                <label htmlFor="new-folder-description" className="wiz-label">Description (optional)</label>
                <textarea
                  id="new-folder-description"
                  value={newFolderDescription}
                  onChange={event => setNewFolderDescription(event.target.value)}
                  rows={3}
                  className="wiz-input resize-none"
                  placeholder="Short note for admins"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <button
                type="button"
                onClick={() => setIsCreateFolderOpen(false)}
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
                  items={moveTreeData}
                  onItemClick={event => setMoveTargetCategoryId(event.itemData.categoryId ?? 0)}
                />
              </div>
            </div>

            <div className="px-6 py-3 bg-slate-50/60 border-b border-slate-100 text-xs">
              <span className="font-bold text-slate-400 uppercase text-xxs mr-1.5">Destination:</span>
              <span className="font-semibold text-indigo-700">{moveTargetPath}</span>
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
                Move Group
              </button>
            </div>
          </div>
        </div>
      )}

      {confirmDialog}
    </>
  )
}
