import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
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
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { ApiError, fetchWithAccessControl } from '../../lib/apiClient'
import { formatDate } from '../../lib/format'
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
  const [searchParams, setSearchParams] = useSearchParams()
  const { confirm, confirmDialog } = useConfirm()
  const { setCustomCrumbs } = useBreadcrumbs()

  const rawCategoryId = searchParams.get('categoryId')
  const parsedCategoryId = Number(rawCategoryId ?? '0')
  const currentCategoryId = Number.isFinite(parsedCategoryId) && parsedCategoryId > 0
    ? Math.trunc(parsedCategoryId)
    : 0

  const [categories, setCategories] = useState<CategoryLookup[]>([])
  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [groups, setGroups] = useState<GroupDto[]>([])

  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState('')

  const [isNewFolderOpen, setIsNewFolderOpen] = useState(false)
  const [newFolderName, setNewFolderName] = useState('')
  const [newFolderDesc, setNewFolderDesc] = useState('')
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

  useEffect(() => {
    // Wait for categories to load before validating the deep-linked folder —
    // on first render the list is still empty and every categoryId looks invalid.
    if (currentCategoryId === 0 || loading || categories.length === 0) return

    const exists = categories.some(category => category.id === currentCategoryId)
    if (!exists) {
      setSearchParams({}, { replace: true })
    }
  }, [categories, currentCategoryId, loading, setSearchParams])

  useEffect(() => {
    const rootCrumbs = [{ to: '/learner-groups', label: 'Learner Groups' }]

    if (currentCategoryId === 0) {
      setCustomCrumbs(rootCrumbs)
      return
    }

    const trail: { to: string; label: string }[] = []
    const visited = new Set<number>()

    let cursor: number | null = currentCategoryId
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

    setCustomCrumbs([...rootCrumbs, ...trail])
  }, [categoriesById, currentCategoryId, setCustomCrumbs])

  useEffect(() => {
    return () => {
      setCustomCrumbs(null)
    }
  }, [setCustomCrumbs])

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
    const term = searchTerm.trim().toLowerCase()
    if (!term) return currentItems

    return currentItems.filter(item => {
      return item.name.toLowerCase().includes(term) || item.description.toLowerCase().includes(term)
    })
  }, [currentItems, searchTerm])

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

  const handleNavigate = useCallback((categoryId: number) => {
    if (categoryId > 0) {
      setSearchParams({ categoryId: String(categoryId) })
    } else {
      setSearchParams({})
    }

    setSearchTerm('')
  }, [setSearchParams])

  const handleGoBack = useCallback(() => {
    if (currentCategoryId === 0) return

    const current = categoriesById.get(currentCategoryId)
    const parentId = current?.parentId ?? 0
    handleNavigate(parentId)
  }, [categoriesById, currentCategoryId, handleNavigate])

  const handleOpenItem = useCallback((item: ExplorerItem) => {
    if (item.isFolder) {
      handleNavigate(item.id)
      return
    }

    navigate(`/learner-groups/${item.id}`)
  }, [handleNavigate, navigate])

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
        }),
      })

      if (response.success === false) {
        throw new Error(response.message || 'Failed to create folder')
      }

      toast.success(`Folder "${normalizedName}" created successfully`)
      setIsNewFolderOpen(false)
      setNewFolderName('')
      setNewFolderDesc('')
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to create folder'))
    } finally {
      setCreatingFolder(false)
    }
  }, [currentCategoryId, loadData, newFolderDesc, newFolderName])

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

  return (
    <>
      <DataGridSurface
        title={currentFolderName}
        note="Unified list view for folders and learner groups in the current folder"
        actions={
          <div className="flex items-center gap-2">
            {currentCategoryId > 0 && (
              <AppButton variant="ghost" icon={ChevronLeft} onClick={handleGoBack}>
                Back
              </AppButton>
            )}
            <AppButton variant="secondary" icon={FolderPlus} onClick={() => setIsNewFolderOpen(true)}>
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

          <div className="min-h-0 flex-1 overflow-hidden rounded-lg border border-slate-200/80 bg-white shadow-3xs">
            {loading ? (
              <div className="flex h-full items-center justify-center">
                <div className="flex flex-col items-center gap-2 text-slate-400">
                  <Loader2 className="h-6 w-6 animate-spin text-indigo-500" />
                  <span className="text-xs font-bold uppercase tracking-wide">Loading directory...</span>
                </div>
              </div>
            ) : (
              <div className="custom-scrollbar h-full overflow-auto">
                <table className="min-w-full divide-y divide-slate-100 text-left text-xs">
                  <thead className="sticky top-0 z-10 border-b border-slate-200 bg-slate-50/90 text-xxs font-extrabold uppercase tracking-wider text-slate-500">
                    <tr>
                      <th className="px-4 py-2.5">Name</th>
                      <th className="px-4 py-2.5 w-80">Description</th>
                      <th className="px-4 py-2.5 w-32 text-center">Type</th>
                      <th className="px-4 py-2.5 w-36 text-center">Size / Members</th>
                      <th className="px-4 py-2.5 w-44">Division / Updated</th>
                      <th className="px-4 py-2.5 w-32 text-center">Actions</th>
                    </tr>
                  </thead>

                  <tbody className="divide-y divide-slate-100 bg-white">
                    {filteredItems.length === 0 ? (
                      <tr>
                        <td colSpan={6} className="px-4 py-12 text-center text-xs font-semibold text-slate-400">
                          This folder is empty. Create a folder or learner group to start.
                        </td>
                      </tr>
                    ) : (
                      filteredItems.map(item => {
                        const itemDivision = !item.isFolder
                          ? getDivisionName((item.original as GroupDto).divisionId)
                          : '-'

                        return (
                          <tr
                            key={`${item.isFolder ? 'folder' : 'group'}-${item.id}`}
                            className="cursor-pointer transition hover:bg-slate-50/70"
                            onDoubleClick={() => handleOpenItem(item)}
                          >
                            <td className="px-4 py-2.5">
                              <div className="flex items-center gap-2.5">
                                {item.isFolder ? (
                                  <Folder className="h-4.5 w-4.5 shrink-0 text-amber-500" />
                                ) : (
                                  <Layers className="h-4.5 w-4.5 shrink-0 text-indigo-500" />
                                )}
                                <span className="truncate font-bold text-slate-800">{item.name}</span>
                              </div>
                            </td>

                            <td className="px-4 py-2.5 text-xs font-semibold text-slate-500">
                              <span className="block truncate" title={item.description}>{item.description}</span>
                            </td>

                            <td className="px-4 py-2.5 text-center">
                              <span className={`inline-flex rounded border px-2 py-0.5 text-[10px] font-extrabold uppercase ${
                                item.isFolder
                                  ? 'border-amber-100 bg-amber-50 text-amber-700'
                                  : 'border-indigo-100 bg-indigo-50 text-indigo-700'
                              }`}>
                                {item.isFolder ? 'Folder' : 'Group'}
                              </span>
                            </td>

                            <td className="px-4 py-2.5 text-center text-xs font-bold text-slate-500">
                              {item.countText}
                            </td>

                            <td className="px-4 py-2.5 text-xs font-semibold text-slate-500">
                              <div className="flex flex-col gap-0.5">
                                <span className="truncate">{itemDivision}</span>
                                <span className="text-[11px] text-slate-400">
                                  {item.isFolder ? 'Created ' : 'Updated '}
                                  {toDateText(item.updatedAt)}
                                </span>
                              </div>
                            </td>

                            <td className="px-4 py-2.5" onClick={event => event.stopPropagation()}>
                              <div className="flex items-center justify-center gap-1.5">
                                {item.isFolder ? (
                                  <button
                                    type="button"
                                    onClick={() => void handleDeleteFolder(item.original as CategoryLookup)}
                                    className="rounded-full p-1 text-slate-400 transition hover:bg-slate-100 hover:text-red-600"
                                    title="Delete Folder"
                                  >
                                    <Trash2 className="h-3.5 w-3.5" />
                                  </button>
                                ) : (
                                  <>
                                    <button
                                      type="button"
                                      onClick={() => handleOpenMove(item.original as GroupDto)}
                                      className="rounded-full p-1 text-slate-400 transition hover:bg-slate-100 hover:text-indigo-600"
                                      title="Move Group"
                                    >
                                      <ArrowRightLeft className="h-3.5 w-3.5" />
                                    </button>
                                    <button
                                      type="button"
                                      onClick={() => void handleDeleteGroup(item.original as GroupDto)}
                                      className="rounded-full p-1 text-slate-400 transition hover:bg-slate-100 hover:text-red-600"
                                      title="Delete Group"
                                    >
                                      <Trash2 className="h-3.5 w-3.5" />
                                    </button>
                                  </>
                                )}

                                <button
                                  type="button"
                                  onClick={() => handleOpenItem(item)}
                                  className="rounded-full p-1 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
                                  title={item.isFolder ? 'Open Folder' : 'Open Group Details'}
                                >
                                  {item.isFolder ? <ArrowUpRight className="h-3.5 w-3.5" /> : <Info className="h-3.5 w-3.5" />}
                                </button>
                              </div>
                            </td>
                          </tr>
                        )
                      })
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </div>
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
