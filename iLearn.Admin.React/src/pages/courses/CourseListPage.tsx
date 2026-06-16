import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  ChevronLeft,
  Folder,
  BookOpen,
  Loader2,
  Plus,
  Search,
  X,
  Info,
  ArrowUpRight,
  FolderPlus,
  Edit3,
  Trash2,
  Building2
} from 'lucide-react'

import { AppButton } from '../../components/ui/AppButton'
import { CourseStatusBadge } from '../../components/ui/CourseStatusBadge'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { Modal } from '../../components/ui/Modal'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { ExplorerTable, type ExplorerColumn } from '../../components/ui/explorer/ExplorerTable'
import { useExplorer } from '../../components/ui/explorer/useExplorer'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { useSession } from '../../lib/sessionContext'
import { toast } from '../../lib/toast'

type ApiEnvelope<T> = {
  success?: boolean
  message?: string
  data?: T
  totalCount?: number
}

type DivisionLookup = {
  id: number
  name: string
}

// Mirrors Category (iLearn.Domain.Entities.Category)
type CategoryLookup = {
  id: number
  name: string
  divisionId: number
  courseCount?: number
}

// Mirrors CourseDto (iLearn.Application/DTOs/CourseDto.cs)
type CourseDto = {
  id: number
  code: string
  title: string
  description?: string | null
  isActive: boolean
  status: number
  statusName: string
  typeName: string
  courseTypeId: number
  categoryId: number
  categoryName: string
  divisionId?: number | null
}

// Mirrors subset response of GET Courses/course-types-lookup
type CourseTypeLookup = {
  id: number
  name: string
}

type ExplorerItem = {
  id: number
  name: string
  description: string
  isFolder: boolean
  countText: string
  typeName?: string
  statusName?: string
  code?: string
  original: DivisionLookup | CategoryLookup | CourseDto
  isDivision?: boolean
}

type CourseExplorerPath = {
  divisionId: number | null
  categoryId: number | null
}

function unwrapList<T>(value: ApiEnvelope<T[]> | { data?: T[] } | T[] | undefined): T[] {
  if (!value) return []
  if (Array.isArray(value)) return value

  const boxed = value as { data?: T[] }
  return Array.isArray(boxed.data) ? boxed.data : []
}

function sortByNameAsc<T extends { name: string }>(a: T, b: T) {
  return a.name.localeCompare(b.name)
}

export function CourseListPage() {
  const navigate = useNavigate()
  const { isSuperAdmin } = useSession()
  const { confirm, confirmDialog } = useConfirm()

  // Category CRUD modals state
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [newCategoryName, setNewCategoryName] = useState('')
  const [newCategoryDivisionId, setNewCategoryDivisionId] = useState<number | ''>('')
  const [submittingCreate, setSubmittingCreate] = useState(false)

  const [editingCategory, setEditingCategory] = useState<CategoryLookup | null>(null)
  const [editCategoryName, setEditCategoryName] = useState('')
  const [submittingRename, setSubmittingRename] = useState(false)

  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [categories, setCategories] = useState<CategoryLookup[]>([])
  const [courses, setCourses] = useState<CourseDto[]>([])
  const [courseTypes, setCourseTypes] = useState<CourseTypeLookup[]>([])

  const [loading, setLoading] = useState(true)
  const [selectedTypeKey, setSelectedTypeKey] = useState('all')

  const divisionsById = useMemo(() => {
    const map = new Map<number, DivisionLookup>()
    for (const division of divisions) {
      map.set(division.id, division)
    }
    return map
  }, [divisions])

  const categoriesById = useMemo(() => {
    const map = new Map<number, CategoryLookup>()
    for (const category of categories) {
      map.set(category.id, category)
    }
    return map
  }, [categories])

  const categoriesByDivision = useMemo(() => {
    const map = new Map<number, CategoryLookup[]>()
    for (const category of categories) {
      const divId = category.divisionId ?? 0
      const existing = map.get(divId) || []
      existing.push(category)
      map.set(divId, existing)
    }
    map.forEach(children => children.sort(sortByNameAsc))
    return map
  }, [categories])

  const coursesByCategory = useMemo(() => {
    const map = new Map<number, CourseDto[]>()
    for (const course of courses) {
      const catId = course.categoryId ?? 0
      const existing = map.get(catId) || []
      existing.push(course)
      map.set(catId, existing)
    }
    return map
  }, [courses])

  const uncategorizedCourses = useMemo(() => {
    return courses.filter(c => {
      if (!c.categoryId || c.categoryId === 0) return true
      const cat = categoriesById.get(c.categoryId)
      if (!cat) return true
      return false
    })
  }, [courses, categoriesById])

  const {
    path,
    searchTerm,
    setSearchTerm,
    navigateToPath,
    goBack,
    filterBySearch,
  } = useExplorer<CourseExplorerPath>({
    rootPath: { divisionId: null, categoryId: null },
    parsePath: params => {
      const rawDivisionId = params.get('divisionId')
      const rawCategoryId = params.get('categoryId')

      return {
        divisionId: rawDivisionId !== null ? Number(rawDivisionId) : null,
        categoryId: rawCategoryId !== null ? Number(rawCategoryId) : null,
      }
    },
    toParams: currentPath => {
      const params: Record<string, string> = {}
      if (currentPath.divisionId !== null) params.divisionId = String(currentPath.divisionId)
      if (currentPath.categoryId !== null) params.categoryId = String(currentPath.categoryId)
      return params
    },
    getParentPath: currentPath => {
      if (currentPath.categoryId !== null) {
        if (currentPath.categoryId === 0) {
          return { divisionId: 0, categoryId: null }
        }

        const category = categoriesById.get(currentPath.categoryId)
        if (category) {
          return { divisionId: category.divisionId, categoryId: null }
        }

        return { divisionId: null, categoryId: null }
      }

      if (currentPath.divisionId !== null) {
        return { divisionId: null, categoryId: null }
      }

      return null
    },
    buildBreadcrumbs: currentPath => {
      const rootCrumbs = [{ to: '/courses', label: 'Courses' }]

      if (currentPath.categoryId !== null) {
        if (currentPath.categoryId === 0) {
          return [
            ...rootCrumbs,
            { to: '/courses?divisionId=0', label: 'Uncategorized' },
            { to: '/courses?categoryId=0', label: 'Uncategorized' },
          ]
        }

        const category = categoriesById.get(currentPath.categoryId)
        if (category) {
          const division = divisionsById.get(category.divisionId)
          const crumbs = [...rootCrumbs]
          if (division) {
            crumbs.push({ to: `/courses?divisionId=${division.id}`, label: division.name })
          } else {
            crumbs.push({ to: '/courses?divisionId=0', label: 'Uncategorized' })
          }
          crumbs.push({ to: `/courses?categoryId=${category.id}`, label: category.name })
          return crumbs
        }
      }

      if (currentPath.divisionId !== null) {
        if (currentPath.divisionId === 0) {
          return [...rootCrumbs, { to: '/courses?divisionId=0', label: 'Uncategorized' }]
        }

        const division = divisionsById.get(currentPath.divisionId)
        if (division) {
          return [...rootCrumbs, { to: `/courses?divisionId=${division.id}`, label: division.name }]
        }
      }

      return rootCrumbs
    },
    isPathValid: currentPath => {
      if (currentPath.categoryId !== null) {
        if (currentPath.categoryId === 0) {
          return uncategorizedCourses.length > 0
        }

        return categories.some(cat => cat.id === currentPath.categoryId)
      }

      if (currentPath.divisionId !== null) {
        if (currentPath.divisionId === 0) {
          return uncategorizedCourses.length > 0
        }

        return divisions.some(div => div.id === currentPath.divisionId)
      }

      return true
    },
    canValidatePath: !loading && divisions.length > 0,
  })

  const currentDivisionId = path.divisionId
  const currentCategoryId = path.categoryId

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      // GET api/Courses filters by status: isActive=true → Open only; isActive=false → Draft/Closed only.
      // Neither returns all, so fetch both and merge for the admin explorer (disjoint sets, no dupes).
      const [activeResp, inactiveResp, divisionsResp, categoriesResp, typesResp] = await Promise.all([
        fetchWithAccessControl<ApiEnvelope<CourseDto[]>>('Courses?isActive=true'),
        fetchWithAccessControl<ApiEnvelope<CourseDto[]>>('Courses?isActive=false'),
        fetchWithAccessControl<{ data?: DivisionLookup[] } | DivisionLookup[]>('admin/DivisionsCRUD/Get'),
        fetchWithAccessControl<{ data?: CategoryLookup[] } | CategoryLookup[]>('admin/CategoriesCRUD/Get'),
        fetchWithAccessControl<CourseTypeLookup[]>('Courses/course-types-lookup'),
      ])

      setCourses([...unwrapList(activeResp), ...unwrapList(inactiveResp)])
      setDivisions(unwrapList(divisionsResp))
      setCategories(unwrapList(categoriesResp))
      setCourseTypes(Array.isArray(typesResp) ? typesResp : [])
    } catch (error) {
      console.error('Failed to load course explorer data', error)
      toast.error('Failed to load explorer contents')
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

  const openCreateCategoryModal = useCallback(() => {
    setNewCategoryName('')
    setNewCategoryDivisionId('')
    setIsCreateModalOpen(true)
  }, [])

  const openRenameCategoryModal = useCallback((category: CategoryLookup) => {
    setEditingCategory(category)
    setEditCategoryName(category.name)
  }, [])

  const handleCreateCategory = useCallback(async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const nameVal = newCategoryName.trim()
    if (!nameVal) {
      toast.error('Category name is required')
      return
    }

    const divisionIdVal = currentDivisionId !== null && currentDivisionId > 0
      ? currentDivisionId
      : (newCategoryDivisionId !== '' ? Number(newCategoryDivisionId) : null)

    if (!divisionIdVal) {
      toast.error('Division is required')
      return
    }

    setSubmittingCreate(true)
    try {
      const fd = new FormData()
      fd.append('values', JSON.stringify({ name: nameVal, divisionId: divisionIdVal, isActive: true }))

      await fetchWithAccessControl('admin/CategoriesCRUD/Post', {
        method: 'POST',
        body: fd
      })

      toast.success(`Category "${nameVal}" created successfully`)
      setIsCreateModalOpen(false)
      setNewCategoryName('')
      setNewCategoryDivisionId('')
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error('Failed to create category')
    } finally {
      setSubmittingCreate(false)
    }
  }, [newCategoryName, currentDivisionId, newCategoryDivisionId, loadData])

  const handleRenameCategory = useCallback(async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!editingCategory) return

    const nameVal = editCategoryName.trim()
    if (!nameVal) {
      toast.error('Category name is required')
      return
    }

    setSubmittingRename(true)
    try {
      const fd = new FormData()
      fd.append('key', String(editingCategory.id))
      fd.append('values', JSON.stringify({ name: nameVal }))

      await fetchWithAccessControl('admin/CategoriesCRUD/Put', {
        method: 'PUT',
        body: fd
      })

      toast.success(`Category renamed to "${nameVal}"`)
      setEditingCategory(null)
      setEditCategoryName('')
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error('Failed to rename category')
    } finally {
      setSubmittingRename(false)
    }
  }, [editingCategory, editCategoryName, loadData])

  const handleDeleteCategory = useCallback(async (category: CategoryLookup) => {
    const coursesCount = coursesByCategory.get(category.id)?.length ?? 0
    if (coursesCount > 0) {
      toast.error('Cannot delete: category has courses inside.')
      return
    }

    const ok = await confirm({
      title: 'Delete Category',
      message: `Are you sure you want to delete category "${category.name}"? This action cannot be undone.`,
      danger: true,
      confirmLabel: 'Delete Category',
    })

    if (!ok) return

    try {
      const fd = new FormData()
      fd.append('key', String(category.id))
      await fetchWithAccessControl('admin/CategoriesCRUD/Delete', {
        method: 'DELETE',
        body: fd
      })

      toast.success(`Category "${category.name}" deleted successfully`)
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error('Failed to delete category')
    }
  }, [confirm, coursesByCategory, loadData])

  const handleOpenItem = useCallback((item: ExplorerItem) => {
    if (item.isFolder) {
      if (currentDivisionId !== null) {
        navigateToPath({ divisionId: currentDivisionId, categoryId: item.id })
      } else {
        navigateToPath({ divisionId: item.id, categoryId: null })
      }
      return
    }

    navigate(`/courses/${item.id}`)
  }, [currentDivisionId, navigate, navigateToPath])

  const currentFolderName = useMemo(() => {
    if (currentCategoryId !== null) {
      if (currentCategoryId === 0) return 'Uncategorized Category'
      return categoriesById.get(currentCategoryId)?.name ?? 'Courses Explorer'
    }
    if (currentDivisionId !== null) {
      if (currentDivisionId === 0) return 'Uncategorized Division'
      return divisionsById.get(currentDivisionId)?.name ?? 'Courses Explorer'
    }
    return 'Courses Explorer'
  }, [categoriesById, divisionsById, currentCategoryId, currentDivisionId])

  const currentItems = useMemo<ExplorerItem[]>(() => {
    if (currentCategoryId !== null) {
      let childCourses = currentCategoryId === 0
        ? uncategorizedCourses
        : (coursesByCategory.get(currentCategoryId) ?? [])

      if (selectedTypeKey !== 'all') {
        const typeId = Number(selectedTypeKey.replace('type-', ''))
        childCourses = childCourses.filter(c => c.courseTypeId === typeId)
      }

      const items = childCourses.map(course => ({
        id: course.id,
        name: course.title,
        description: course.description || 'No description available',
        isFolder: false,
        countText: course.typeName || 'General',
        typeName: course.typeName,
        statusName: course.statusName || (course.isActive ? 'Open' : 'Closed'),
        code: course.code,
        original: course
      }))

      items.sort(sortByNameAsc)
      return items
    }

    if (currentDivisionId !== null) {
      if (currentDivisionId === 0) {
        return [{
          id: 0,
          name: 'Uncategorized',
          description: 'Courses without category',
          isFolder: true,
          countText: `${uncategorizedCourses.length} courses`,
          original: { id: 0, name: 'Uncategorized', divisionId: 0 }
        }]
      }

      const childCategories = categoriesByDivision.get(currentDivisionId) ?? []
      const items = childCategories.map(cat => {
        const count = coursesByCategory.get(cat.id)?.length ?? 0
        return {
          id: cat.id,
          name: cat.name,
          description: 'Category folder under division',
          isFolder: true,
          countText: `${count} courses`,
          original: cat
        }
      })

      return items
    }

    // Root - Divisions
    const list: ExplorerItem[] = divisions.map(div => {
      const childCategories = categoriesByDivision.get(div.id) ?? []
      return {
        id: div.id,
        name: div.name,
        description: 'Division folder',
        isFolder: true,
        countText: `${childCategories.length} categories`,
        original: div,
        isDivision: true
      }
    })

    list.sort(sortByNameAsc)

    if (uncategorizedCourses.length > 0) {
      list.push({
        id: 0,
        name: 'Uncategorized',
        description: 'Courses without division/category',
        isFolder: true,
        countText: '1 category',
        original: { id: 0, name: 'Uncategorized' }
      })
    }

    return list
  }, [currentCategoryId, currentDivisionId, divisions, categoriesByDivision, coursesByCategory, uncategorizedCourses, selectedTypeKey])

  const filteredItems = useMemo(() => {
    return filterBySearch(currentItems, (item, normalizedTerm) => {
      return (
        item.name.toLowerCase().includes(normalizedTerm) ||
        item.description.toLowerCase().includes(normalizedTerm) ||
        (item.code && item.code.toLowerCase().includes(normalizedTerm)) ||
        false
      )
    })
  }, [currentItems, filterBySearch])

  const tableColumns = useMemo<ExplorerColumn<ExplorerItem>[]>(() => [
    {
      key: 'name',
      title: 'Name',
      render: item => (
        <div className="flex items-center gap-2.5">
          {item.isFolder ? (
            item.isDivision ? (
              <Building2 className="h-4.5 w-4.5 shrink-0 text-indigo-500" />
            ) : (
              <Folder className="h-4.5 w-4.5 shrink-0 text-amber-500" />
            )
          ) : (
            <BookOpen className="h-4.5 w-4.5 shrink-0 text-indigo-500" />
          )}
          <div className="flex flex-col text-left py-0.5">
            <span className="truncate font-bold text-slate-800 leading-tight">{item.name}</span>
            {item.code && (
              <span className="text-slate-400 font-mono text-[10px] mt-0.5">{item.code}</span>
            )}
          </div>
        </div>
      ),
    },
    {
      key: 'description',
      title: 'Description',
      headerClassName: 'w-80',
      cellClassName: 'text-xs font-semibold text-slate-500',
      render: item => (
        <span className="block truncate max-w-sm" title={item.description}>{item.description}</span>
      ),
    },
    {
      key: 'type',
      title: 'Type',
      headerClassName: 'w-32 text-center',
      cellClassName: 'text-center',
      render: item => (
        item.isFolder ? (
          <span className="inline-flex rounded border border-amber-100 bg-amber-50 px-2 py-0.5 text-[10px] font-extrabold uppercase text-amber-700">
            Folder
          </span>
        ) : (
          <span className={`inline-flex items-center px-2 py-0.5 rounded text-xxs font-bold ${
            (item.typeName || '').toLowerCase().includes('special')
              ? 'bg-purple-100 text-purple-800 border border-purple-200/50'
              : 'bg-blue-100 text-blue-800 border border-blue-200/50'
          }`}>
            {item.typeName || 'General'}
          </span>
        )
      ),
    },
    {
      key: 'status',
      title: 'Status / Count',
      headerClassName: 'w-36 text-center',
      cellClassName: 'text-center text-xs font-bold text-slate-500',
      render: item => (
        item.isFolder ? (
          <span className="text-xs font-bold text-slate-500">{item.countText}</span>
        ) : (
          <CourseStatusBadge status={item.statusName} />
        )
      ),
    },
    {
      key: 'actions',
      title: 'Actions',
      headerClassName: 'w-32 text-center',
      render: item => (
        <div className="flex items-center justify-center gap-1.5" onClick={event => event.stopPropagation()}>
          <button
            type="button"
            onClick={() => handleOpenItem(item)}
            className="p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 rounded-md transition cursor-pointer"
            title={item.isFolder ? 'Open Folder' : 'Open Course Details'}
          >
            {item.isFolder ? <ArrowUpRight className="h-3.5 w-3.5" /> : <Info className="h-3.5 w-3.5" />}
          </button>
          {currentDivisionId !== null && currentDivisionId > 0 && currentCategoryId === null && item.isFolder && item.id > 0 && (
            <>
              <button
                type="button"
                onClick={() => openRenameCategoryModal(item.original as CategoryLookup)}
                className="p-1 text-indigo-500 hover:bg-indigo-50 rounded-md transition cursor-pointer"
                title="Rename Category"
              >
                <Edit3 className="h-3.5 w-3.5" />
              </button>
              <button
                type="button"
                onClick={() => handleDeleteCategory(item.original as CategoryLookup)}
                className="p-1 text-red-500 hover:bg-rose-50 rounded-md transition cursor-pointer"
                title="Delete Category"
              >
                <Trash2 className="h-3.5 w-3.5" />
              </button>
            </>
          )}
        </div>
      ),
    },
  ], [currentCategoryId, currentDivisionId, handleDeleteCategory, handleOpenItem, openRenameCategoryModal])

  return (
    <>
      <DataGridSurface
        title={currentFolderName}
        note="Manage folders and training courses in this directory."
        actions={
          <div className="flex items-center gap-2">
            {(currentCategoryId !== null || currentDivisionId !== null) && (
              <AppButton variant="ghost" icon={ChevronLeft} onClick={goBack}>
                Back
              </AppButton>
            )}
            {currentCategoryId === null && (currentDivisionId !== null ? currentDivisionId > 0 : isSuperAdmin) && (
              <AppButton variant="secondary" icon={FolderPlus} onClick={openCreateCategoryModal}>
                New Category
              </AppButton>
            )}
            <Link to="/courses/new">
              <AppButton variant="primary" icon={Plus}>
                Create Course
              </AppButton>
            </Link>
          </div>
        }
      >
        <div className="flex min-h-0 flex-1 flex-col gap-3 pt-4 pb-0">
          <div className="flex flex-col gap-2 lg:flex-row lg:items-center lg:justify-between">
            <div className="text-xs font-semibold text-slate-500 shrink-0">
              Showing <span className="font-bold text-slate-800">{filteredItems.length}</span> items in this folder
            </div>

            {/* Course Type chips + search on the same row */}
            <div className="flex w-full min-w-0 flex-col gap-2 sm:flex-row sm:items-center lg:w-auto lg:justify-end">
              {currentCategoryId !== null && (
                <div className="flex min-w-0 items-center gap-2 overflow-x-auto custom-scrollbar max-sm:pb-1">
                 
                  <button
                    key="all"
                    type="button"
                    onClick={() => setSelectedTypeKey('all')}
                    className={`rounded-md border px-3 py-1.5 text-xs font-semibold transition-colors shrink-0 cursor-pointer ${
                      selectedTypeKey === 'all'
                        ? 'border-indigo-500 bg-blue-600 text-white'
                        : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300 hover:bg-slate-50'
                    }`}
                  >
                    All Types
                  </button>
                  {courseTypes.map(chip => (
                    <button
                      key={chip.id}
                      type="button"
                      onClick={() => setSelectedTypeKey(`type-${chip.id}`)}
                      className={`rounded-md border px-3 py-1.5 text-xs font-semibold transition-colors shrink-0 cursor-pointer ${
                        selectedTypeKey === `type-${chip.id}`
                          ? 'border-indigo-500 bg-blue-600 text-white'
                          : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300 hover:bg-slate-50'
                      }`}
                    >
                      {chip.name}
                    </button>
                  ))}
                </div>
              )}

              <div className="relative w-full sm:w-72 shrink-0">
                <Search className="pointer-events-none absolute left-3 top-2 h-4 w-4 text-slate-400" />
                <input
                  type="text"
                  value={searchTerm}
                  onChange={event => setSearchTerm(event.target.value)}
                  placeholder="Search folders or courses in this folder..."
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
          </div>

          <ExplorerTable
            loading={loading}
            loadingLabel="Loading directory..."
            emptyText="This folder is empty."
            columns={tableColumns}
            items={filteredItems}
            getRowKey={item => `${item.isFolder ? 'folder' : 'course'}-${item.id}`}
            onRowDoubleClick={handleOpenItem}
          />
        </div>
      </DataGridSurface>

      <Modal
        open={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        as="form"
        onSubmit={handleCreateCategory}
        ariaLabel="Create Category"
      >
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <FolderPlus className="h-5 w-5 text-indigo-500" />
                <h3 className="text-sm font-extrabold uppercase tracking-wide text-slate-800">Create Category</h3>
              </div>
              <button
                type="button"
                onClick={() => setIsCreateModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="px-6 py-4 space-y-3">
              {isSuperAdmin && currentDivisionId === null && (
                <div className="space-y-1">
                  <label htmlFor="newCategoryDivisionId" className="wiz-label">Division (แผนก) <span className="text-red-500">*</span></label>
                  <select
                    id="newCategoryDivisionId"
                    value={newCategoryDivisionId}
                    onChange={event => setNewCategoryDivisionId(event.target.value === '' ? '' : Number(event.target.value))}
                    className="wiz-input"
                  >
                    <option value="">— Select Division —</option>
                    {divisions.map(div => (
                      <option key={div.id} value={div.id}>
                        {div.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <div className="space-y-1">
                <label htmlFor="categoryName" className="wiz-label">Category Name <span className="text-red-500">*</span></label>
                <input
                  id="categoryName"
                  type="text"
                  autoFocus
                  value={newCategoryName}
                  onChange={event => setNewCategoryName(event.target.value)}
                  className="wiz-input"
                  placeholder="e.g. Technical Skills"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <button
                type="button"
                onClick={() => setIsCreateModalOpen(false)}
                className="px-4 py-2 text-xs font-bold text-slate-500 hover:text-slate-700 rounded-lg hover:bg-slate-100 transition cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={submittingCreate || !newCategoryName.trim() || (isSuperAdmin && currentDivisionId === null && !newCategoryDivisionId)}
                className="inline-flex items-center gap-1.5 rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-xs font-bold text-white hover:bg-indigo-700 cursor-pointer shadow-3xs disabled:opacity-55"
              >
                {submittingCreate && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                Create Category
              </button>
            </div>
      </Modal>

      <Modal
        open={!!editingCategory}
        onClose={() => setEditingCategory(null)}
        as="form"
        onSubmit={handleRenameCategory}
        ariaLabel="Rename Category"
      >
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <Edit3 className="h-5 w-5 text-indigo-500" />
                <h3 className="text-sm font-extrabold uppercase tracking-wide text-slate-800">Rename Category</h3>
              </div>
              <button
                type="button"
                onClick={() => setEditingCategory(null)}
                className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="px-6 py-4 space-y-3">
              <div className="space-y-1">
                <label htmlFor="editCategoryName" className="wiz-label">Category Name <span className="text-red-500">*</span></label>
                <input
                  id="editCategoryName"
                  type="text"
                  autoFocus
                  value={editCategoryName}
                  onChange={event => setEditCategoryName(event.target.value)}
                  className="wiz-input"
                  placeholder="e.g. Technical Skills"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <button
                type="button"
                onClick={() => setEditingCategory(null)}
                className="px-4 py-2 text-xs font-bold text-slate-500 hover:text-slate-700 rounded-lg hover:bg-slate-100 transition cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={submittingRename || !editCategoryName.trim()}
                className="inline-flex items-center gap-1.5 rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-xs font-bold text-white hover:bg-indigo-700 cursor-pointer shadow-3xs disabled:opacity-55"
              >
                {submittingRename && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                Rename Category
              </button>
            </div>
      </Modal>

      {confirmDialog}
    </>
  )
}
