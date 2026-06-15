import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import {
  ChevronLeft,
  Folder,
  Layers,
  Loader2,
  Plus,
  Search,
  X,
  Info,
  ArrowUpRight
} from 'lucide-react'

import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { fetchWithAccessControl } from '../../lib/apiClient'
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

type CategoryLookup = {
  id: number
  name: string
  divisionId: number
}

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
  const [searchParams, setSearchParams] = useSearchParams()
  const { setCustomCrumbs } = useBreadcrumbs()

  const rawDivisionId = searchParams.get('divisionId')
  const rawCategoryId = searchParams.get('categoryId')

  const currentDivisionId = rawDivisionId !== null ? Number(rawDivisionId) : null
  const currentCategoryId = rawCategoryId !== null ? Number(rawCategoryId) : null

  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [categories, setCategories] = useState<CategoryLookup[]>([])
  const [courses, setCourses] = useState<CourseDto[]>([])
  const [courseTypes, setCourseTypes] = useState<CourseTypeLookup[]>([])

  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState('')
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

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      // GET api/Courses filters by status: isActive=true → Open only; isActive=false → Draft/Closed/Retired only.
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

  // Deep-link guard: validate query parameters after data loads
  useEffect(() => {
    if (loading || divisions.length === 0) return

    if (currentCategoryId !== null) {
      if (currentCategoryId === 0) {
        if (uncategorizedCourses.length === 0) {
          setSearchParams({}, { replace: true })
        }
        return
      }
      const exists = categories.some(cat => cat.id === currentCategoryId)
      if (!exists) {
        setSearchParams({}, { replace: true })
      }
    } else if (currentDivisionId !== null) {
      if (currentDivisionId === 0) {
        if (uncategorizedCourses.length === 0) {
          setSearchParams({}, { replace: true })
        }
        return
      }
      const exists = divisions.some(div => div.id === currentDivisionId)
      if (!exists) {
        setSearchParams({}, { replace: true })
      }
    }
  }, [loading, divisions, categories, currentCategoryId, currentDivisionId, uncategorizedCourses, setSearchParams])

  // Manage Breadcrumbs
  useEffect(() => {
    const rootCrumbs = [{ to: '/courses', label: 'Courses' }]

    if (currentCategoryId !== null) {
      if (currentCategoryId === 0) {
        setCustomCrumbs([
          ...rootCrumbs,
          { to: '/courses?divisionId=0', label: 'Uncategorized' },
          { to: '/courses?categoryId=0', label: 'Uncategorized' }
        ])
        return
      }

      const category = categoriesById.get(currentCategoryId)
      if (category) {
        const division = divisionsById.get(category.divisionId)
        const crumbs = [...rootCrumbs]
        if (division) {
          crumbs.push({ to: `/courses?divisionId=${division.id}`, label: division.name })
        } else {
          crumbs.push({ to: `/courses?divisionId=0`, label: 'Uncategorized' })
        }
        crumbs.push({ to: `/courses?categoryId=${category.id}`, label: category.name })
        setCustomCrumbs(crumbs)
      }
      return
    }

    if (currentDivisionId !== null) {
      if (currentDivisionId === 0) {
        setCustomCrumbs([
          ...rootCrumbs,
          { to: '/courses?divisionId=0', label: 'Uncategorized' }
        ])
        return
      }

      const division = divisionsById.get(currentDivisionId)
      if (division) {
        setCustomCrumbs([
          ...rootCrumbs,
          { to: `/courses?divisionId=${division.id}`, label: division.name }
        ])
      }
      return
    }

    setCustomCrumbs(rootCrumbs)
  }, [currentCategoryId, currentDivisionId, categoriesById, divisionsById, setCustomCrumbs])

  useEffect(() => {
    return () => {
      setCustomCrumbs(null)
    }
  }, [setCustomCrumbs])

  const handleNavigate = useCallback((divisionId: number | null, categoryId: number | null) => {
    const params: Record<string, string> = {}
    if (divisionId !== null) params.divisionId = String(divisionId)
    if (categoryId !== null) params.categoryId = String(categoryId)
    setSearchParams(params)
    setSearchTerm('')
  }, [setSearchParams])

  const handleGoBack = useCallback(() => {
    if (currentCategoryId !== null) {
      if (currentCategoryId === 0) {
        handleNavigate(0, null)
        return
      }
      const category = categoriesById.get(currentCategoryId)
      if (category) {
        handleNavigate(category.divisionId, null)
      } else {
        handleNavigate(null, null)
      }
    } else if (currentDivisionId !== null) {
      handleNavigate(null, null)
    }
  }, [currentCategoryId, currentDivisionId, categoriesById, handleNavigate])

  const handleOpenItem = useCallback((item: ExplorerItem) => {
    if (item.isFolder) {
      if (currentDivisionId !== null) {
        handleNavigate(currentDivisionId, item.id)
      } else {
        handleNavigate(item.id, null)
      }
      return
    }

    navigate(`/courses/${item.id}`)
  }, [currentDivisionId, handleNavigate, navigate])

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
        original: div
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
    const term = searchTerm.trim().toLowerCase()
    if (!term) return currentItems

    return currentItems.filter(item => {
      return (
        item.name.toLowerCase().includes(term) ||
        item.description.toLowerCase().includes(term) ||
        (item.code && item.code.toLowerCase().includes(term))
      )
    })
  }, [currentItems, searchTerm])

  return (
    <>
      <DataGridSurface
        title={currentFolderName}
        note="Unified directory for managing training courses structured by Division and Category"
        actions={
          <div className="flex items-center gap-2">
            {(currentCategoryId !== null || currentDivisionId !== null) && (
              <AppButton variant="ghost" icon={ChevronLeft} onClick={handleGoBack}>
                Back
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
                      <th className="px-4 py-2.5 w-36 text-center">Status / Count</th>
                      <th className="px-4 py-2.5 w-32 text-center">Actions</th>
                    </tr>
                  </thead>

                  <tbody className="divide-y divide-slate-100 bg-white">
                    {filteredItems.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="px-4 py-12 text-center text-xs font-semibold text-slate-400">
                          This folder is empty.
                        </td>
                      </tr>
                    ) : (
                      filteredItems.map(item => {
                        return (
                          <tr
                            key={`${item.isFolder ? 'folder' : 'course'}-${item.id}`}
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
                                <div className="flex flex-col text-left py-0.5">
                                  <span className="truncate font-bold text-slate-800 leading-tight">{item.name}</span>
                                  {item.code && (
                                    <span className="text-slate-400 font-mono text-[10px] mt-0.5">{item.code}</span>
                                  )}
                                </div>
                              </div>
                            </td>

                            <td className="px-4 py-2.5 text-xs font-semibold text-slate-500">
                              <span className="block truncate max-w-sm" title={item.description}>{item.description}</span>
                            </td>

                            <td className="px-4 py-2.5 text-center">
                              {item.isFolder ? (
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
                              )}
                            </td>

                            <td className="px-4 py-2.5 text-center text-xs font-bold text-slate-500">
                              {item.isFolder ? (
                                <span className="text-xs font-bold text-slate-500">{item.countText}</span>
                              ) : (
                                (() => {
                                  const status = item.statusName || '—'
                                  const isDraft = status.toLowerCase() === 'draft'
                                  const isRetired = status.toLowerCase() === 'retired'
                                  const isOpen = status.toLowerCase() === 'open' || status.toLowerCase() === 'active'
                                  
                                  let toneClass = 'bg-slate-100 text-slate-800 border-slate-200'
                                  if (isOpen) toneClass = 'bg-emerald-100 text-emerald-800 border-emerald-200 font-bold'
                                  else if (isDraft) toneClass = 'bg-amber-100 text-amber-800 border-amber-200'
                                  else if (isRetired) toneClass = 'bg-rose-100 text-rose-800 border-rose-200'

                                  return (
                                    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xxs font-semibold border ${toneClass}`}>
                                      {status}
                                    </span>
                                  )
                                })()
                              )}
                            </td>

                            <td className="px-4 py-2.5" onClick={event => event.stopPropagation()}>
                              <div className="flex items-center justify-center gap-1.5">
                                <button
                                  type="button"
                                  onClick={() => handleOpenItem(item)}
                                  className="rounded-full p-1 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
                                  title={item.isFolder ? 'Open Folder' : 'Open Course Details'}
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
    </>
  )
}
