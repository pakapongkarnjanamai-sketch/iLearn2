import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  ChevronLeft,
  Folder,
  BookOpen,
  Plus,
  X,
  Info,
  ArrowUpRight,
  FolderPlus,
  Edit3,
  Trash2,
  Building2
} from 'lucide-react'

import { AppButton } from '../../components/ui/AppButton'
import { Badge } from '../../components/ui/Badge'
import { CourseStatusBadge } from '../../components/ui/CourseStatusBadge'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { IconButton } from '../../components/ui/IconButton'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { Modal } from '../../components/ui/Modal'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
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

// Mirrors DivisionDto (iLearn.Application/DTOs/DivisionDto.cs) via GET api/Divisions
type DivisionLookup = {
  id: number
  name: string
  isActive?: boolean
}

// Mirrors Category (iLearn.Domain.Entities.Category)
type CategoryLookup = {
  id: number
  name: string
  divisionId: number
  sortOrder: number
  courseCount?: number
  description?: string | null
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

function sortCategoriesByOrder<T extends { sortOrder?: number; id: number }>(a: T, b: T) {
  return ((a.sortOrder ?? 0) - (b.sortOrder ?? 0)) || (a.id - b.id)
}

export function CourseListPage() {
  const navigate = useNavigate()
  const { isSuperAdmin } = useSession()
  const { confirm, confirmDialog } = useConfirm()

  // Category CRUD modals state
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [newCategoryName, setNewCategoryName] = useState('')
  const [newCategoryDescription, setNewCategoryDescription] = useState('')
  const [newCategorySortOrder, setNewCategorySortOrder] = useState<number | ''>('')
  const [newCategoryDivisionId, setNewCategoryDivisionId] = useState<number | ''>('')
  const [submittingCreate, setSubmittingCreate] = useState(false)

  const [editingCategory, setEditingCategory] = useState<CategoryLookup | null>(null)
  const [editCategoryName, setEditCategoryName] = useState('')
  const [editCategoryDescription, setEditCategoryDescription] = useState('')
  const [editCategorySortOrder, setEditCategorySortOrder] = useState<number | ''>('')
  const [submittingRename, setSubmittingRename] = useState(false)

  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [categories, setCategories] = useState<CategoryLookup[]>([])
  const [courses, setCourses] = useState<CourseDto[]>([])
  const [courseTypes, setCourseTypes] = useState<CourseTypeLookup[]>([])

  const [loading, setLoading] = useState(true)
  const [selectedTypeKey, setSelectedTypeKey] = useState('all')

  const singleDivision = useMemo(() => {
    return divisions.length === 1 ? (divisions[0] ?? null) : null
  }, [divisions])

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
    map.forEach(children => children.sort(sortCategoriesByOrder))
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
      if (singleDivision !== null) {
        if (currentPath.categoryId !== null || currentPath.divisionId !== null) {
          return { divisionId: null, categoryId: null }
        }
        return null
      }

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
          if (singleDivision !== null) {
            return [
              ...rootCrumbs,
              { to: `/courses?categoryId=${category.id}`, label: category.name }
            ]
          }

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
        fetchWithAccessControl<DivisionLookup[]>('Divisions'),
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
    setNewCategoryDescription('')
    setNewCategorySortOrder('')
    setNewCategoryDivisionId('')
    setIsCreateModalOpen(true)
  }, [])

  const openRenameCategoryModal = useCallback((category: CategoryLookup) => {
    setEditingCategory(category)
    setEditCategoryName(category.name)
    setEditCategoryDescription(category.description || '')
    setEditCategorySortOrder(category.sortOrder !== undefined && category.sortOrder !== null ? category.sortOrder : '')
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
      : (singleDivision !== null
        ? singleDivision.id
        : (newCategoryDivisionId !== '' ? Number(newCategoryDivisionId) : null))

    if (!divisionIdVal) {
      toast.error('Division is required')
      return
    }

    setSubmittingCreate(true)
    try {
      const payload: Record<string, any> = { 
        name: nameVal, 
        divisionId: divisionIdVal, 
        isActive: true,
        description: newCategoryDescription.trim() || null
      }
      if (newCategorySortOrder !== '') {
        payload.sortOrder = Number(newCategorySortOrder)
      }

      const fd = new FormData()
      fd.append('values', JSON.stringify(payload))

      await fetchWithAccessControl('admin/CategoriesCRUD/Post', {
        method: 'POST',
        body: fd
      })

      toast.success(`Category "${nameVal}" created successfully`)
      setIsCreateModalOpen(false)
      setNewCategoryName('')
      setNewCategoryDescription('')
      setNewCategorySortOrder('')
      setNewCategoryDivisionId('')
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error('Failed to create category')
    } finally {
      setSubmittingCreate(false)
    }
  }, [newCategoryName, newCategoryDescription, newCategorySortOrder, currentDivisionId, newCategoryDivisionId, loadData, singleDivision])

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
      const valuesPayload: Record<string, any> = { 
        name: nameVal,
        description: editCategoryDescription.trim() || null
      }
      if (editCategorySortOrder !== '') {
        valuesPayload.sortOrder = Number(editCategorySortOrder)
      }

      const fd = new FormData()
      fd.append('key', String(editingCategory.id))
      fd.append('values', JSON.stringify(valuesPayload))

      await fetchWithAccessControl('admin/CategoriesCRUD/Put', {
        method: 'PUT',
        body: fd
      })

      toast.success(`Category changes saved successfully`)
      setEditingCategory(null)
      setEditCategoryName('')
      setEditCategoryDescription('')
      setEditCategorySortOrder('')
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error('Failed to save category changes')
    } finally {
      setSubmittingRename(false)
    }
  }, [editingCategory, editCategoryName, editCategoryDescription, editCategorySortOrder, loadData])

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
      if (item.isDivision) {
        navigateToPath({ divisionId: item.id, categoryId: null })
      } else {
        const cat = item.original as CategoryLookup
        const divId = cat?.divisionId ?? currentDivisionId ?? (singleDivision !== null ? singleDivision.id : null)
        navigateToPath({ divisionId: divId, categoryId: item.id })
      }
      return
    }

    navigate(`/courses/${item.id}`)
  }, [currentDivisionId, singleDivision, navigate, navigateToPath])

  const currentFolderName = useMemo(() => {
    if (currentCategoryId !== null) {
      if (currentCategoryId === 0) return 'Uncategorized Category'
      return categoriesById.get(currentCategoryId)?.name ?? 'Courses Explorer'
    }
    if (currentDivisionId !== null) {
      if (currentDivisionId === 0) return 'Uncategorized Division'
      return divisionsById.get(currentDivisionId)?.name ?? 'Courses Explorer'
    }
    if (singleDivision !== null) {
      return singleDivision.name
    }
    return 'Courses Explorer'
  }, [categoriesById, divisionsById, currentCategoryId, currentDivisionId, singleDivision])

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
          name: cat.sortOrder > 0 ? `${cat.sortOrder}. ${cat.name}` : cat.name,
          description: cat.description || 'Category folder',
          isFolder: true,
          countText: `${count} courses`,
          original: cat
        }
      })

      return items
    }

    if (singleDivision !== null) {
      const childCategories = categoriesByDivision.get(singleDivision.id) ?? []
      const list: ExplorerItem[] = childCategories.map(cat => {
        const count = coursesByCategory.get(cat.id)?.length ?? 0
        return {
          id: cat.id,
          name: cat.sortOrder > 0 ? `${cat.sortOrder}. ${cat.name}` : cat.name,
          description: cat.description || 'Category folder',
          isFolder: true,
          countText: `${count} courses`,
          original: cat
        }
      })

      list.sort((a, b) => sortCategoriesByOrder(a.original as CategoryLookup, b.original as CategoryLookup))

      if (uncategorizedCourses.length > 0) {
        list.push({
          id: 0,
          name: 'Uncategorized',
          description: 'Courses without division/category',
          isFolder: true,
          countText: '1 category',
          original: { id: 0, name: 'Uncategorized', divisionId: 0, sortOrder: 0 }
        })
      }

      return list
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
  }, [currentCategoryId, currentDivisionId, divisions, categoriesByDivision, coursesByCategory, uncategorizedCourses, selectedTypeKey, singleDivision])

  const allExplorerItems = useMemo<ExplorerItem[]>(() => {
    const list: ExplorerItem[] = []

    // All Divisions
    for (const div of divisions) {
      const childCategories = categoriesByDivision.get(div.id) ?? []
      list.push({
        id: div.id,
        name: div.name,
        description: 'โฟลเดอร์สายงาน (Division)',
        isFolder: true,
        countText: `${childCategories.length} หมวดหมู่`,
        original: div,
        isDivision: true,
      })
    }

    // All Categories
    for (const cat of categories) {
      const count = coursesByCategory.get(cat.id)?.length ?? 0
      const div = divisionsById.get(cat.divisionId)
      const divName = div ? div.name : 'ไม่ระบุสายงาน'
      list.push({
        id: cat.id,
        name: cat.sortOrder > 0 ? `${cat.sortOrder}. ${cat.name}` : cat.name,
        description: cat.description ? `[${divName}] ${cat.description}` : `หมวดหมู่ใน ${divName}`,
        isFolder: true,
        countText: `${count} คอร์ส`,
        original: cat,
      })
    }

    // All Courses
    for (const course of courses) {
      if (selectedTypeKey !== 'all') {
        const typeId = Number(selectedTypeKey.replace('type-', ''))
        if (course.courseTypeId !== typeId) continue
      }

      const cat = categoriesById.get(course.categoryId)
      const catName = cat ? cat.name : (course.categoryName || 'ไม่ระบุหมวดหมู่')

      list.push({
        id: course.id,
        name: course.title,
        description: course.description ? `[${catName}] ${course.description}` : `หมวดหมู่: ${catName}`,
        isFolder: false,
        countText: course.typeName || 'General',
        typeName: course.typeName,
        statusName: course.statusName || (course.isActive ? 'Open' : 'Closed'),
        code: course.code,
        original: course,
      })
    }

    return list
  }, [divisions, categories, courses, categoriesByDivision, coursesByCategory, divisionsById, categoriesById, selectedTypeKey])

  const filteredItems = useMemo(() => {
    const normalizedTerm = searchTerm.trim().toLowerCase()
    if (!normalizedTerm) {
      return currentItems
    }

    return allExplorerItems.filter(item => {
      return (
        item.name.toLowerCase().includes(normalizedTerm) ||
        item.description.toLowerCase().includes(normalizedTerm) ||
        (item.code && item.code.toLowerCase().includes(normalizedTerm)) ||
        (item.typeName && item.typeName.toLowerCase().includes(normalizedTerm)) ||
        (item.statusName && item.statusName.toLowerCase().includes(normalizedTerm))
      )
    })
  }, [searchTerm, currentItems, allExplorerItems])

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
          <Badge variant="tag" tone="warning">
            Folder
          </Badge>
        ) : (
          <Badge tone={(item.typeName || '').toLowerCase().includes('special') ? 'warning' : 'info'} size="xxs">
            {item.typeName || 'General'}
          </Badge>
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
          <Badge tone="neutral">{item.countText}</Badge>
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
          <IconButton
            type="button"
            onClick={() => handleOpenItem(item)}
            icon={item.isFolder ? ArrowUpRight : Info}
            tone="neutral"
            size="sm"
            title={item.isFolder ? 'Open Folder' : 'Open Course Details'}
          />
          {currentCategoryId === null && item.isFolder && item.id > 0 && ((currentDivisionId !== null && currentDivisionId > 0) || singleDivision !== null) && (
            <>
              <IconButton
                type="button"
                onClick={() => openRenameCategoryModal(item.original as CategoryLookup)}
                icon={Edit3}
                tone="primary"
                size="sm"
                title="Rename Category"
              />
              <IconButton
                type="button"
                onClick={() => handleDeleteCategory(item.original as CategoryLookup)}
                icon={Trash2}
                tone="danger"
                size="sm"
                title="Delete Category"
              />
            </>
          )}
        </div>
      ),
    },
  ], [currentCategoryId, currentDivisionId, handleDeleteCategory, handleOpenItem, openRenameCategoryModal, singleDivision])

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
            {currentCategoryId === null && (currentDivisionId !== null ? currentDivisionId > 0 : (isSuperAdmin || singleDivision !== null)) && (
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
        <div className="flex min-h-0 flex-1 flex-col">
          <ListToolbar
            count={filteredItems.length}
            countUnit={searchTerm.trim() ? "items found" : "items in this folder"}
            searchValue={searchTerm}
            onSearchChange={setSearchTerm}
            searchPlaceholder="Search folders or courses across all directories..."
            toolbarContent={
              currentCategoryId !== null ? (
                <SegmentedToggle
                  variant="filter"
                  options={[
                    { value: 'all', label: 'All Types' },
                    ...courseTypes.map(chip => ({ value: `type-${chip.id}`, label: chip.name })),
                  ]}
                  value={selectedTypeKey}
                  onChange={setSelectedTypeKey}
                  className="min-w-0 overflow-x-auto custom-scrollbar max-sm:pb-1 flex-nowrap"
                />
              ) : undefined
            }
          />

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
              <IconButton
                type="button"
                onClick={() => setIsCreateModalOpen(false)}
                icon={X}
                title="Close"
                tone="neutral"
              />
            </div>

            <div className="px-6 py-4 space-y-3">
              {isSuperAdmin && currentDivisionId === null && !singleDivision && (
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

              <div className="space-y-1">
                <label htmlFor="newCategorySortOrder" className="wiz-label">Sort Order (ลำดับ)</label>
                <input
                  id="newCategorySortOrder"
                  type="number"
                  min={1}
                  value={newCategorySortOrder}
                  onChange={event => setNewCategorySortOrder(event.target.value === '' ? '' : Number(event.target.value))}
                  className="wiz-input"
                  placeholder="e.g. 1, 2, 3..."
                />
              </div>

              <div className="space-y-1">
                <label htmlFor="categoryDescription" className="wiz-label">Description (คำอธิบาย)</label>
                <textarea
                  id="categoryDescription"
                  value={newCategoryDescription}
                  onChange={event => setNewCategoryDescription(event.target.value)}
                  placeholder="e.g. Courses related to technical skills and engineering..."
                  maxLength={500}
                  rows={3}
                  className="wiz-input resize-y font-semibold"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <AppButton
                variant="ghost"
                onClick={() => setIsCreateModalOpen(false)}
              >
                Cancel
              </AppButton>
              <AppButton
                type="submit"
                variant="primary"
                loading={submittingCreate}
                disabled={submittingCreate || !newCategoryName.trim() || (isSuperAdmin && currentDivisionId === null && !singleDivision && !newCategoryDivisionId)}
                className="px-4 py-2 text-xs font-bold shadow-3xs"
              >
                Create Category
              </AppButton>
            </div>
      </Modal>

      <Modal
        open={!!editingCategory}
        onClose={() => setEditingCategory(null)}
        as="form"
        onSubmit={handleRenameCategory}
        ariaLabel="Edit Category"
      >
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <Edit3 className="h-5 w-5 text-indigo-500" />
                <h3 className="text-sm font-extrabold uppercase tracking-wide text-slate-800">Edit Category</h3>
              </div>
              <IconButton
                type="button"
                onClick={() => setEditingCategory(null)}
                icon={X}
                title="Close"
                tone="neutral"
              />
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

              <div className="space-y-1">
                <label htmlFor="editCategorySortOrder" className="wiz-label">Sort Order (ลำดับ) <span className="text-red-500">*</span></label>
                <input
                  id="editCategorySortOrder"
                  type="number"
                  min={1}
                  required
                  value={editCategorySortOrder}
                  onChange={event => setEditCategorySortOrder(event.target.value === '' ? '' : Number(event.target.value))}
                  className="wiz-input"
                  placeholder="e.g. 1, 2, 3..."
                />
              </div>

              <div className="space-y-1">
                <label htmlFor="editCategoryDescription" className="wiz-label">Description (คำอธิบาย)</label>
                <textarea
                  id="editCategoryDescription"
                  value={editCategoryDescription}
                  onChange={event => setEditCategoryDescription(event.target.value)}
                  placeholder="e.g. Courses related to technical skills and engineering..."
                  maxLength={500}
                  rows={3}
                  className="wiz-input resize-y font-semibold"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <AppButton
                variant="ghost"
                onClick={() => setEditingCategory(null)}
              >
                Cancel
              </AppButton>
              <AppButton
                type="submit"
                variant="primary"
                loading={submittingRename}
                disabled={submittingRename || !editCategoryName.trim() || editCategorySortOrder === ''}
                className="px-4 py-2 text-xs font-bold shadow-3xs"
              >
                Save Changes
              </AppButton>
            </div>
      </Modal>

      {confirmDialog}
    </>
  )
}
