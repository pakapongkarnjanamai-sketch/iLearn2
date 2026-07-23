import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  ArrowRightLeft,
  ArrowUpRight,
  ChevronLeft,
  Edit3,
  Folder,
  FolderPlus,
  Info,
  Layers,
  Plus,
  Trash2,
  X,
} from 'lucide-react'

import { AppButton } from '../../components/ui/AppButton'
import { Badge } from '../../components/ui/Badge'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { IconButton } from '../../components/ui/IconButton'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { AppTreeView, type TreeViewNode } from '../../components/ui/AppTreeView'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { ExplorerTable, type ExplorerColumn } from '../../components/ui/explorer/ExplorerTable'
import { useExplorer } from '../../components/ui/explorer/useExplorer'
import { ApiError, fetchWithAccessControl } from '../../lib/apiClient'
import { formatDate } from '../../lib/format'
import { COMMON_LABELS, LEARNER_LABELS, UI_LABELS, t, tf } from '../../lib/labels'
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
  divisionId?: number | null
  parentId?: number | null
  depth?: number
  childCount?: number
  learnerGroupCount?: number
  createdAt?: string
}

// Mirrors division lookup row from api/Divisions/lookup
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

function toDateText(value: string | undefined | null, emptyValue: string) {
  if (!value) return emptyValue

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return emptyValue
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

  const [editingFolder, setEditingFolder] = useState<CategoryLookup | null>(null)
  const [editFolderName, setEditFolderName] = useState('')
  const [editFolderDesc, setEditFolderDesc] = useState('')
  const [editFolderDivisionId, setEditFolderDivisionId] = useState<number | ''>('')
  const [updatingFolder, setUpdatingFolder] = useState(false)

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
      const rootCrumbs = [{ to: '/learner-groups', label: t(LEARNER_LABELS.learnerGroups) }]
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
        fetchWithAccessControl<{ data?: DivisionLookup[] } | DivisionLookup[]>('Divisions/lookup'),
        fetchWithAccessControl<ApiEnvelope<GroupDto[]>>('LearnerGroups'),
      ])

      setCategories(unwrapList(categoryResp))
      setDivisions(unwrapList(divisionResp))
      setGroups(unwrapList(groupResp))
    } catch (error) {
      console.error('Failed to load explorer data', error)
      toast.error(getApiErrorText(error, t(LEARNER_LABELS.failedToLoadExplorer)))
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
        description: folder.description || t(LEARNER_LABELS.categoryFolderFallback),
        isFolder: true,
        countText: tf(LEARNER_LABELS.itemCount, nestedFolderCount + nestedGroupCount),
        updatedAt: folder.createdAt || '',
        original: folder,
      }
    })

    const childGroups = (groupsByCategory.get(currentCategoryId) ?? []).map(group => {
      return {
        id: group.id,
        name: group.name,
        description: group.description || t(LEARNER_LABELS.learnerGroupFallback),
        isFolder: false,
        countText: tf(LEARNER_LABELS.memberCount, group.memberCount || 0),
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
        text: t(LEARNER_LABELS.rootFolderNoCategory),
        isRoot: true,
        categoryId: 0,
        items: roots.map(toNode),
      },
    ]
  }, [categoriesByParent])

  const relocateTargetCategoryPath = useMemo(() => {
    if (relocateCategoryId === 0) return t(LEARNER_LABELS.rootFolder)

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

    return path.length > 0 ? path.join(' / ') : t(LEARNER_LABELS.rootFolder)
  }, [categoriesById, relocateCategoryId])

  const handleOpenItem = useCallback((item: ExplorerItem) => {
    if (item.isFolder) {
      navigateToPath({ categoryId: item.id })
      return
    }

    navigate(`/learner-groups/${item.id}`)
  }, [navigate, navigateToPath])

  const currentFolderName = useMemo(() => {
    if (currentCategoryId === 0) return t(LEARNER_LABELS.learnerGroupExplorer)
    return categoriesById.get(currentCategoryId)?.name ?? t(LEARNER_LABELS.learnerGroupExplorer)
  }, [categoriesById, currentCategoryId])

  const handleCreateFolder = useCallback(async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const normalizedName = newFolderName.trim()
    if (!normalizedName) {
      toast.error(t(LEARNER_LABELS.folderNameRequired))
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
        throw new Error(response.message || t(LEARNER_LABELS.failedToCreateFolder))
      }

      toast.success(tf(LEARNER_LABELS.folderCreated, normalizedName))
      setIsNewFolderOpen(false)
      setNewFolderName('')
      setNewFolderDesc('')
      setNewFolderDivisionId('')
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, t(LEARNER_LABELS.failedToCreateFolder)))
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

  const handleOpenEditFolder = useCallback((folder: CategoryLookup) => {
    setEditingFolder(folder)
    setEditFolderName(folder.name)
    setEditFolderDesc(folder.description ?? '')
    setEditFolderDivisionId(folder.divisionId ?? '')
  }, [])

  const handleUpdateFolder = useCallback(async (event: FormEvent) => {
    event.preventDefault()
    if (!editingFolder) return

    const normalizedName = editFolderName.trim()
    if (!normalizedName) {
      toast.error(t(LEARNER_LABELS.folderNameRequired))
      return
    }

    setUpdatingFolder(true)
    try {
      const response = await fetchWithAccessControl<ApiEnvelope<void>>(`LearnerGroupCategories/${editingFolder.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: normalizedName,
          description: editFolderDesc.trim() || null,
          parentId: editingFolder.parentId ?? null,
          divisionId: isSuperAdmin && editingFolder.parentId == null && editFolderDivisionId !== '' ? Number(editFolderDivisionId) : (editingFolder.divisionId ?? null),
        }),
      })

      if (response.success === false) {
        throw new Error(response.message || t(LEARNER_LABELS.failedToUpdateFolder))
      }

      toast.success(tf(LEARNER_LABELS.folderUpdated, normalizedName))
      setEditingFolder(null)
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, t(LEARNER_LABELS.failedToUpdateFolder)))
    } finally {
      setUpdatingFolder(false)
    }
  }, [editFolderDesc, editFolderDivisionId, editFolderName, editingFolder, isSuperAdmin, loadData])


  const handleDeleteFolder = useCallback(async (folder: CategoryLookup) => {
    const hasChildren = (folder.childCount ?? 0) > 0
    const hasGroups = (folder.learnerGroupCount ?? 0) > 0

    if (hasChildren || hasGroups) {
      toast.error(t(LEARNER_LABELS.folderNotEmpty))
      return
    }

    const ok = await confirm({
      title: t(LEARNER_LABELS.deleteFolder),
      message: tf(LEARNER_LABELS.deleteFolderConfirm, folder.name),
      danger: true,
      confirmLabel: t(LEARNER_LABELS.deleteFolder),
    })

    if (!ok) return

    try {
      await fetchWithAccessControl<void>(`LearnerGroupCategories/${folder.id}`, {
        method: 'DELETE',
      })

      toast.success(tf(LEARNER_LABELS.folderDeleted, folder.name))
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, t(LEARNER_LABELS.failedToDeleteFolder)))
    }
  }, [confirm, loadData])

  const handleDeleteGroup = useCallback(async (group: GroupDto) => {
    const ok = await confirm({
      title: t(LEARNER_LABELS.deleteLearnerGroup),
      message: tf(LEARNER_LABELS.deleteGroupConfirm, group.name),
      danger: true,
      confirmLabel: t(LEARNER_LABELS.deleteGroup),
    })

    if (!ok) return

    try {
      await fetchWithAccessControl<void>(`LearnerGroups/${group.id}`, {
        method: 'DELETE',
      })

      toast.success(tf(LEARNER_LABELS.groupDeleted, group.name))
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, t(LEARNER_LABELS.failedToDeleteGroup)))
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
          throw new Error(t(LEARNER_LABELS.failedToLoadLatestGroup))
        }

        name = detailResp.data.name.trim()
        description = (detailResp.data.description || '').trim()
      }

      if (!name || !description) {
        throw new Error(t(LEARNER_LABELS.groupNameDescriptionRequiredToMove))
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
        throw new Error(response.message || t(LEARNER_LABELS.failedToRelocateGroup))
      }

      toast.success(tf(LEARNER_LABELS.groupMoved, movingGroup.name))
      setMovingGroup(null)
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error(getApiErrorText(error, t(LEARNER_LABELS.failedToRelocateGroup)))
    } finally {
      setMovingInProgress(false)
    }
  }, [loadData, movingGroup, relocateCategoryId])

  const getDivisionName = useCallback((divisionId: number | null | undefined) => {
    if (!divisionId) return t(LEARNER_LABELS.emptyValue)

    const division = divisions.find(item => item.id === divisionId)
    return division ? division.name : tf(LEARNER_LABELS.divisionWithId, divisionId)
  }, [divisions])

  const tableColumns = useMemo<ExplorerColumn<ExplorerItem>[]>(() => [
    {
      key: 'name',
      title: t(LEARNER_LABELS.name),
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
      title: t(LEARNER_LABELS.description),
      headerClassName: 'w-80',
      cellClassName: 'text-xs font-semibold text-slate-500',
      render: item => <span className="block truncate" title={item.description}>{item.description}</span>,
    },
    {
      key: 'type',
      title: t(LEARNER_LABELS.type),
      headerClassName: 'w-32 text-center',
      cellClassName: 'text-center',
      render: item => (
        <Badge variant="tag" tone={item.isFolder ? 'warning' : 'info'}>
          {t(item.isFolder ? COMMON_LABELS.folder : COMMON_LABELS.group)}
        </Badge>
      ),
    },
    {
      key: 'size',
      title: t(LEARNER_LABELS.sizeMembers),
      headerClassName: 'w-36 text-center',
      cellClassName: 'text-center text-xs font-bold text-slate-500',
      render: item => item.countText,
    },
    {
      key: 'meta',
      title: t(LEARNER_LABELS.divisionUpdated),
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
              {item.isFolder ? t(LEARNER_LABELS.created) : t(LEARNER_LABELS.updated)}
              {toDateText(item.updatedAt, t(LEARNER_LABELS.emptyValue))}
            </span>
          </div>
        )
      },
    },
    {
      key: 'actions',
      title: t(LEARNER_LABELS.action),
      headerClassName: 'w-32 text-center',
      render: item => (
        <div className="flex items-center justify-center gap-1.5" onClick={event => event.stopPropagation()}>
          {item.isFolder ? (
            <>
              <IconButton
                type="button"
                onClick={() => handleOpenEditFolder(item.original as CategoryLookup)}
                icon={Edit3}
                tone="primary"
                size="sm"
                title={t(LEARNER_LABELS.editFolder)}
              />
              <IconButton
                type="button"
                onClick={() => void handleDeleteFolder(item.original as CategoryLookup)}
                icon={Trash2}
                tone="danger"
                size="sm"
                title={t(LEARNER_LABELS.deleteFolder)}
              />
            </>
          ) : (
            <>
              <IconButton
                type="button"
                onClick={() => handleOpenMove(item.original as GroupDto)}
                icon={ArrowRightLeft}
                tone="primary"
                size="sm"
                title={t(LEARNER_LABELS.moveGroup)}
              />
              <IconButton
                type="button"
                onClick={() => void handleDeleteGroup(item.original as GroupDto)}
                icon={Trash2}
                tone="danger"
                size="sm"
                title={t(LEARNER_LABELS.deleteGroup)}
              />
            </>
          )}

          <IconButton
            type="button"
            onClick={() => handleOpenItem(item)}
            icon={item.isFolder ? ArrowUpRight : Info}
            tone="neutral"
            size="sm"
            title={t(item.isFolder ? LEARNER_LABELS.openFolder : LEARNER_LABELS.openGroupDetails)}
          />
        </div>
      ),
    },
  ], [getDivisionName, handleDeleteFolder, handleDeleteGroup, handleOpenEditFolder, handleOpenItem, handleOpenMove])

  return (
    <>
      <DataGridSurface
        title={currentFolderName}
        note={t(LEARNER_LABELS.manageDirectory)}
        actions={
          <div className="flex items-center gap-2">
            {currentCategoryId > 0 && (
              <AppButton variant="ghost" icon={ChevronLeft} onClick={goBack}>
                {t(UI_LABELS.previous)}
              </AppButton>
            )}
            <AppButton variant="secondary" icon={FolderPlus} onClick={openNewFolderModal}>
              {t(LEARNER_LABELS.newFolder)}
            </AppButton>
            <Link to={currentCategoryId > 0 ? `/learner-groups/new?categoryId=${currentCategoryId}` : '/learner-groups/new'}>
              <AppButton variant="primary" icon={Plus}>
                {t(LEARNER_LABELS.createGroup)}
              </AppButton>
            </Link>
          </div>
        }
      >
        <div className="flex min-h-0 flex-1 flex-col">
          <ListToolbar
            count={filteredItems.length}
            countUnit={t(LEARNER_LABELS.itemsInFolder)}
            searchValue={searchTerm}
            onSearchChange={setSearchTerm}
            searchPlaceholder={t(LEARNER_LABELS.searchFoldersOrGroups)}
          />

          <ExplorerTable
            loading={loading}
            loadingLabel={t(UI_LABELS.loadingDirectory)}
            emptyText={t(LEARNER_LABELS.emptyFolder)}
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
                <h3 className="text-sm font-extrabold uppercase tracking-wide text-slate-800">{t(LEARNER_LABELS.newFolder)}</h3>
              </div>
              <IconButton
                type="button"
                onClick={() => setIsNewFolderOpen(false)}
                icon={X}
                title={t(LEARNER_LABELS.close)}
                tone="neutral"
              />
            </div>

            <div className="px-6 py-4 space-y-3">
              {isSuperAdmin && currentCategoryId === 0 && (
                <div className="space-y-1">
                  <label htmlFor="newFolderDivisionId" className="wiz-label">
                    {t(LEARNER_LABELS.division)}
                  </label>
                  <select
                    id="newFolderDivisionId"
                    value={newFolderDivisionId}
                    onChange={event =>
                      setNewFolderDivisionId(event.target.value === '' ? '' : Number(event.target.value))
                    }
                    className="wiz-input"
                  >
                    <option value="">{t(LEARNER_LABELS.global)}</option>
                    {divisions.map(div => (
                      <option key={div.id} value={div.id}>
                        {div.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <div className="space-y-1">
                <label htmlFor="folderName" className="wiz-label">{t(LEARNER_LABELS.folderName)} <span className="text-red-500">*</span></label>
                <input
                  id="folderName"
                  type="text"
                  autoFocus
                  value={newFolderName}
                  onChange={event => setNewFolderName(event.target.value)}
                  className="wiz-input"
                  placeholder={t(LEARNER_LABELS.folderNamePlaceholder)}
                />
              </div>

              <div className="space-y-1">
                <label htmlFor="folderDesc" className="wiz-label">{t(LEARNER_LABELS.optionalDescription)}</label>
                <textarea
                  id="folderDesc"
                  value={newFolderDesc}
                  onChange={event => setNewFolderDesc(event.target.value)}
                  rows={3}
                  className="wiz-input resize-none"
                  placeholder={t(LEARNER_LABELS.folderDescriptionPlaceholder)}
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <AppButton
                variant="ghost"
                onClick={() => setIsNewFolderOpen(false)}
              >
                {t(UI_LABELS.cancel)}
              </AppButton>
              <AppButton
                type="submit"
                variant="primary"
                loading={creatingFolder}
                disabled={creatingFolder || !newFolderName.trim()}
                className="px-4 py-2 text-xs font-bold shadow-3xs"
              >
                {t(LEARNER_LABELS.newFolder)}
              </AppButton>
            </div>
          </form>
        </div>
      )}

      {editingFolder && (
        <div className="modal-overlay" onClick={() => setEditingFolder(null)}>
          <form
            className="modal-window"
            onClick={event => event.stopPropagation()}
            onSubmit={handleUpdateFolder}
          >
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <Edit3 className="h-5 w-5 text-indigo-500" />
                <h3 className="text-sm font-extrabold uppercase tracking-wide text-slate-800">{t(LEARNER_LABELS.editFolder)}</h3>
              </div>
              <IconButton
                type="button"
                onClick={() => setEditingFolder(null)}
                icon={X}
                title={t(LEARNER_LABELS.close)}
                tone="neutral"
              />
            </div>

            <div className="px-6 py-4 space-y-3">
              {isSuperAdmin && editingFolder.parentId == null && (
                <div className="space-y-1">
                  <label htmlFor="editFolderDivisionId" className="wiz-label">
                    {t(LEARNER_LABELS.division)}
                  </label>
                  <select
                    id="editFolderDivisionId"
                    value={editFolderDivisionId}
                    onChange={event =>
                      setEditFolderDivisionId(event.target.value === '' ? '' : Number(event.target.value))
                    }
                    className="wiz-input"
                  >
                    <option value="">{t(LEARNER_LABELS.global)}</option>
                    {divisions.map(div => (
                      <option key={div.id} value={div.id}>
                        {div.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <div className="space-y-1">
                <label htmlFor="editFolderName" className="wiz-label">{t(LEARNER_LABELS.folderName)} <span className="text-red-500">*</span></label>
                <input
                  id="editFolderName"
                  type="text"
                  autoFocus
                  value={editFolderName}
                  onChange={event => setEditFolderName(event.target.value)}
                  className="wiz-input"
                  placeholder={t(LEARNER_LABELS.folderNamePlaceholder)}
                />
              </div>

              <div className="space-y-1">
                <label htmlFor="editFolderDesc" className="wiz-label">{t(LEARNER_LABELS.optionalDescription)}</label>
                <textarea
                  id="editFolderDesc"
                  value={editFolderDesc}
                  onChange={event => setEditFolderDesc(event.target.value)}
                  rows={3}
                  className="wiz-input resize-none"
                  placeholder={t(LEARNER_LABELS.folderDescriptionPlaceholder)}
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <AppButton
                variant="ghost"
                onClick={() => setEditingFolder(null)}
              >
                {t(UI_LABELS.cancel)}
              </AppButton>
              <AppButton
                type="submit"
                variant="primary"
                loading={updatingFolder}
                disabled={updatingFolder || !editFolderName.trim()}
                className="px-4 py-2 text-xs font-bold shadow-3xs"
              >
                {t(LEARNER_LABELS.saveChanges)}
              </AppButton>
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
                <h3 className="text-sm font-extrabold uppercase tracking-wide text-slate-800">{t(LEARNER_LABELS.moveGroup)}</h3>
              </div>
              <IconButton
                type="button"
                onClick={() => setMovingGroup(null)}
                icon={X}
                title={t(LEARNER_LABELS.close)}
                tone="neutral"
              />
            </div>

            <div className="px-6 py-3 border-b border-slate-100 bg-indigo-50/40 text-xs font-semibold text-slate-600">
              {tf(LEARNER_LABELS.moveGroupMessage, movingGroup.name)}
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
              <span className="font-bold text-slate-400 uppercase text-xxs mr-1.5">{t(LEARNER_LABELS.destination)}</span>
              <span className="font-semibold text-indigo-700">{relocateTargetCategoryPath}</span>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 bg-slate-50/50">
              <AppButton
                variant="ghost"
                onClick={() => setMovingGroup(null)}
              >
                {t(UI_LABELS.cancel)}
              </AppButton>
              <AppButton
                type="button"
                onClick={() => void handleConfirmMove()}
                variant="primary"
                loading={movingInProgress}
                disabled={movingInProgress}
                className="px-4 py-2 text-xs font-bold shadow-3xs"
              >
                {t(LEARNER_LABELS.relocateGroup)}
              </AppButton>
            </div>
          </div>
        </div>
      )}

      {confirmDialog}
    </>
  )
}
