import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  Calendar,
  Check,
  Eye,
  Users,
  FileText,
  Plus,
  Trash2,
  Edit3,
  UserPlus,
  Power,
  Lock,
  BookOpen,
  X
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { AppButton } from '../../components/ui/AppButton'
import { IconButton } from '../../components/ui/IconButton'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { CourseStatusText } from '../../components/ui/CourseStatusBadge'
import { Modal } from '../../components/ui/Modal'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import {
  DetailLayout,
  Fact,
  FactGrid,
} from '../../components/ui/detail'
import { Card } from '../../components/ui/Card'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { formatDate } from '../../lib/format'
import { learnerStatusLabel } from '../../lib/learnerStatus'
import { DetailTabs } from '../../components/ui/DetailTabs'
import { DETAIL_TABLE_CHUNK_SIZE } from '../../lib/tableStandards'

type LookupResult<T> = T[] | { data?: T[] }

// Mirrors CourseContentItemDto (iLearn.Application/DTOs/CourseDetailDto.cs)
type CourseContentItem = {
  id: number
  name: string
  typeId: number
  typeName: string
  isActive: boolean
  url?: string | null
}

// Mirrors CourseDetailDto (iLearn.Application/DTOs/CourseDetailDto.cs)
type CourseDetail = {
  id: number
  courseCode: string
  courseName: string
  description?: string | null
  courseType: number
  categoryId: number
  isActive: boolean
  status: number
  statusName: string
  canAssign: boolean
  canLearnerAccess: boolean
  contentItems: CourseContentItem[]
}

// Mirrors CourseVersionDto (iLearn.Application/DTOs/CourseDetailDto.cs)
type CourseVersion = {
  id: number
  courseId: number
  versionNumber: number
  note: string
  isActive: boolean
  versionState: string
  createdAt: string
  contentItems: CourseContentItem[]
}

// Mirrors CourseDashboardKpiDto (iLearn.Application/DTOs/CourseDashboardDtos.cs)
type CourseKPI = {
  versionCount: number
  learnerCount: number
  completedCount: number
  assignmentCount: number
}

// Mirrors CourseDashboardDto (iLearn.Application/DTOs/CourseDashboardDtos.cs)
type CourseDashboardData = {
  course: CourseDetail
  versions: CourseVersion[]
  kpi?: CourseKPI
}

// Mirrors CourseLearnerDto (iLearn.Application/DTOs/CourseDashboardDtos.cs)
type CourseLearner = {
  id: number
  learnerCode: string
  learnerName: string
  division: string
  department: string
  position: string
  progress: number
  isCompleted: boolean
  completedDate: string | null
  startDate: string
  dueDate: string
  status: string
}

// Mirrors CourseAssignmentHistoryDto (iLearn.Application/DTOs/CourseDashboardDtos.cs)
type CourseAssignment = {
  id: number
  assignmentNo: string
  description: string
  startDate: string
  dueDate: string
  status: string
  completedEnrollmentCount: number
  totalEnrollmentCount: number
  completionPct: number
}

type DivisionLookup = {
  id: number
  name: string
}

type CategoryLookup = {
  id: number
  name: string
  divisionId?: number
}

type CourseTypeLookup = {
  id: number
  name: string
}

type CourseEditFormData = {
  courseCode: string
  courseName: string
  description: string
  divisionId: number
  categoryId: number
  courseType: number
}

const unwrapList = <T,>(value: LookupResult<T> | undefined): T[] => {
  if (!value) return []
  return Array.isArray(value) ? value : value.data ?? []
}

export function CourseDetailPage() {
  const { id } = useParams()
  const { setLabel } = useBreadcrumbs()
  const navigate = useNavigate()
  const { confirm, confirmDialog } = useConfirm()

  const [loading, setLoading] = useState(true)

  const handleDeleteCourse = async () => {
    if (!(await confirm({
      title: 'Delete Course',
      message: 'Are you sure you want to delete this course? This action cannot be undone and will delete all associated versions.',
      confirmLabel: 'Delete Course',
      danger: true,
    }))) return
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Courses/${id}`, {
        method: 'DELETE'
      })
      if (resp.success) {
        toast.success(resp.message || 'Course deleted successfully')
        navigate('/courses')
      }
    } catch (err: any) {
      console.error(err)
      const errorMsg = err?.message || 'Failed to delete this course.'
      if (errorMsg.includes('in progress') || errorMsg.includes('currently taking') || errorMsg.includes('learner(s)')) {
        const forceDelete = await confirm({
          title: 'Force Delete Course?',
          message: `${errorMsg}\n\nDo you want to Force Delete this course and automatically clean up all linked enrollments and assignments?`,
          confirmLabel: 'Force Delete',
          danger: true,
        })
        if (forceDelete) {
          try {
            const forceResp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Courses/${id}?force=true`, {
              method: 'DELETE'
            })
            if (forceResp.success) {
              toast.success(forceResp.message || 'Course force-deleted successfully')
              navigate('/courses')
            }
          } catch (forceErr: any) {
            console.error(forceErr)
            toast.error(forceErr?.message || 'Force delete failed.')
          }
        }
      } else {
        toast.error(errorMsg)
      }
    }
  }
  const [data, setData] = useState<CourseDashboardData | null>(null)
  const [learners, setLearners] = useState<CourseLearner[]>([])
  const [loadingLearners, setLoadingLearners] = useState(false)
  const [hasLoadedLearners, setHasLoadedLearners] = useState(false)
  const [assignments, setAssignments] = useState<CourseAssignment[]>([])
  const [loadingAssignments, setLoadingAssignments] = useState(false)
  const [hasLoadedAssignments, setHasLoadedAssignments] = useState(false)
  const [mutatingStatus, setMutatingStatus] = useState(false)
  const [activeDetailTab, setActiveDetailTab] = useState<'versions' | 'learners' | 'assignments'>('versions')
  const [visibleVersionRows, setVisibleVersionRows] = useState(DETAIL_TABLE_CHUNK_SIZE)
  const [visibleLearnerRows, setVisibleLearnerRows] = useState(DETAIL_TABLE_CHUNK_SIZE)
  const [visibleAssignmentRows, setVisibleAssignmentRows] = useState(DETAIL_TABLE_CHUNK_SIZE)

  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [categories, setCategories] = useState<CategoryLookup[]>([])
  const [courseTypes, setCourseTypes] = useState<CourseTypeLookup[]>([])

  const [showEditPropertiesModal, setShowEditPropertiesModal] = useState(false)
  const [savingProperties, setSavingProperties] = useState(false)
  const [editForm, setEditForm] = useState<CourseEditFormData>({
    courseCode: '',
    courseName: '',
    description: '',
    divisionId: 0,
    categoryId: 0,
    courseType: 0,
  })

  const categoryNames = useMemo(
    () => Object.fromEntries(categories.map(item => [item.id, item.name])),
    [categories],
  )
  const courseTypeNames = useMemo(
    () => Object.fromEntries(courseTypes.map(item => [item.id, item.name])),
    [courseTypes],
  )

  useEffect(() => {
    const code = data?.course?.courseCode
    if (code) {
      setLabel(String(id), code)
    }
  }, [data, id, setLabel])

  const loadLookups = useCallback(async () => {
    try {
      const [divisionData, categoryData, courseTypeData] = await Promise.all([
        fetchWithAccessControl<LookupResult<DivisionLookup>>('Divisions/lookup'),
        fetchWithAccessControl<LookupResult<CategoryLookup>>('Categories/lookup'),
        fetchWithAccessControl<LookupResult<CourseTypeLookup>>('Courses/course-types-lookup'),
      ])

      setDivisions(unwrapList(divisionData))
      setCategories(unwrapList(categoryData))
      setCourseTypes(unwrapList(courseTypeData))
    } catch {
      toast.error('Failed to load course lookup metadata')
    }
  }, [])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadLookups()
    }, 0)

    return () => window.clearTimeout(timeoutId)
  }, [loadLookups])

  const loadDashboardData = useCallback(async () => {
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; data: CourseDashboardData }>(`Courses/${id}/dashboard`)
      if (resp.success) {
        setData(resp.data)
      }
    } catch (err) {
      console.error('Failed to load course details', err)
      toast.error('Unable to fetch course dashboard')
    } finally {
      setLoading(false)
    }
  }, [id])

  const loadLearners = useCallback(async () => {
    setLoadingLearners(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; data: CourseLearner[] }>(`Courses/${id}/learners`)
      if (resp.success) {
        setLearners(resp.data)
        setHasLoadedLearners(true)
      }
    } catch (err) {
      console.error(err)
      toast.error('Unable to load learners list')
    } finally {
      setLoadingLearners(false)
    }
  }, [id])

  const loadAssignments = useCallback(async () => {
    setLoadingAssignments(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; data: CourseAssignment[] }>(`Courses/${id}/assignments`)
      if (resp.success) {
        setAssignments(resp.data)
        setHasLoadedAssignments(true)
      }
    } catch (err) {
      console.error(err)
      toast.error('Unable to load assignment history')
    } finally {
      setLoadingAssignments(false)
    }
  }, [id])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadDashboardData()
    }, 0)

    return () => window.clearTimeout(timeoutId)
  }, [loadDashboardData])

  const handleDetailTabChange = useCallback((nextTab: 'versions' | 'learners' | 'assignments') => {
    setActiveDetailTab(nextTab)

    if (nextTab === 'learners' && !hasLoadedLearners && !loadingLearners) {
      void loadLearners()
      return
    }

    if (nextTab === 'assignments' && !hasLoadedAssignments && !loadingAssignments) {
      void loadAssignments()
    }
  }, [hasLoadedAssignments, hasLoadedLearners, loadAssignments, loadLearners, loadingAssignments, loadingLearners])

  useEffect(() => {
    setVisibleVersionRows(DETAIL_TABLE_CHUNK_SIZE)
    setVisibleLearnerRows(DETAIL_TABLE_CHUNK_SIZE)
    setVisibleAssignmentRows(DETAIL_TABLE_CHUNK_SIZE)
  }, [id])

  const filteredCategories = useMemo(() => {
    if (!editForm.divisionId) return categories
    return categories.filter(category => category.divisionId === editForm.divisionId)
  }, [categories, editForm.divisionId])

  const openEditPropertiesModal = () => {
    if (!data) return

    const divisionId = categories.find(item => item.id === data.course.categoryId)?.divisionId ?? 0
    setEditForm({
      courseCode: data.course.courseCode || '',
      courseName: data.course.courseName || '',
      description: data.course.description || '',
      divisionId,
      categoryId: data.course.categoryId || 0,
      courseType: data.course.courseType || 0,
    })
    setShowEditPropertiesModal(true)
  }

  const handleEditFormChange = (field: keyof CourseEditFormData, value: string | number) => {
    setEditForm(prev => ({
      ...prev,
      [field]: value,
      ...(field === 'divisionId' ? { categoryId: 0 } : {}),
    }))
  }

  const handleSaveProperties = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!data || !id) return

    if (!editForm.courseCode.trim()) {
      toast.error('Course Code is required')
      return
    }
    if (!editForm.courseName.trim()) {
      toast.error('Course Title is required')
      return
    }
    if (editForm.categoryId === 0) {
      toast.error('Please select a Category')
      return
    }
    if (editForm.courseType === 0) {
      toast.error('Please select a Course Type')
      return
    }

    setSavingProperties(true)
    try {
      const payload = {
        courseCode: editForm.courseCode.trim(),
        courseName: editForm.courseName.trim(),
        description: editForm.description.trim(),
        courseType: editForm.courseType,
        categoryId: editForm.categoryId,
        contentItemIds: data.course.contentItems.map(item => item.id),
      }

      const resp = await fetchWithAccessControl<{ success: boolean; message?: string }>(`Courses/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })

      if (resp.success) {
        toast.success(resp.message || 'Course properties updated')
        setShowEditPropertiesModal(false)
        await loadDashboardData()
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to update course properties')
    } finally {
      setSavingProperties(false)
    }
  }

  // Handle Course Publish / Retire status transitions
  const handleStatusChange = async (targetStatus: number) => {
    setMutatingStatus(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Courses/${id}/status`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status: targetStatus })
      })
      if (resp.success) {
        toast.success(resp.message)
        loadDashboardData()
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to change course lifecycle status.')
    } finally {
      setMutatingStatus(false)
    }
  }

  // Version Set Active operation
  const handleSetActiveVersion = async (versionId: number) => {
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Courses/${id}/versions/${versionId}/set-active`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ policy: 1 }) // New Learners Only default policy
      })
      if (resp.success) {
        toast.success(resp.message)
        loadDashboardData()
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to switch active version')
    }
  }

  // Delete version handler
  const handleDeleteVersion = async (versionId: number) => {
    if (!(await confirm({
      title: 'Delete Version',
      message: 'Delete this version? Action cannot be undone.',
      confirmLabel: 'Delete',
      danger: true,
    }))) return
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Courses/versions/${versionId}`, {
        method: 'DELETE'
      })
      if (resp.success) {
        toast.success(resp.message)
        loadDashboardData()
      }
    } catch (err) {
      console.error(err)
      toast.error('Cannot delete this version. It may have enrolled learners.')
    }
  }

  if (loading) {
    return <LoadingState />
  }

  if (!data) {
    return (
      <NotFoundState
        title="Course Not Found"
        message="The requested course catalog identity is missing or has been deleted."
        backTo="/courses"
        backLabel="Back to courses"
      />
    )
  }

  const { course, versions } = data
  const isDraft = course.status === 0
  const isOpen = course.status === 1
  const isClosed = course.status === 2
  const detailTabs: Array<{ key: 'versions' | 'learners' | 'assignments'; label: string }> = [
    { key: 'versions', label: 'Versions' },
    { key: 'learners', label: 'Learners' },
    { key: 'assignments', label: 'Assignments' },
  ]

  const visibleVersions = versions.slice(0, visibleVersionRows)
  const visibleLearners = learners.slice(0, visibleLearnerRows)
  const visibleAssignments = assignments.slice(0, visibleAssignmentRows)

  return (
    <>
      <DetailLayout
        sidebar={
          <CourseControls
            courseId={id ?? String(course.id)}
            isDraft={isDraft}
            isOpen={isOpen}
            isClosed={isClosed}
            mutatingStatus={mutatingStatus}
            onStatusChange={handleStatusChange}
            onDeleteCourse={handleDeleteCourse}
            onEditProperties={openEditPropertiesModal}
          />
        }
      >
        <main className="space-y-6">
          <Card icon={BookOpen} title="Overview" bodyClassName="p-5 space-y-5">
              {course.description && (
                <p className="text-sm text-slate-500 leading-relaxed max-w-2xl border-l-2 border-slate-200 pl-3 whitespace-pre-wrap">
                  {course.description}
                </p>
              )}

              <FactGrid className={`text-sm ${course.description ? 'border-t border-slate-100 pt-5' : 'pt-2'}`}>
                <Fact label="Course Code" mono valueClassName="font-semibold">
                  {course.courseCode}
                </Fact>
                <Fact label="Status">
                  <CourseStatusText status={course.statusName} statusCode={course.status} />
                </Fact>
                <Fact label="Category" valueClassName="font-semibold">
                  {categoryNames[course.categoryId] || '-'}
                </Fact>
                <Fact label="Course Type" valueClassName="font-semibold">
                  {courseTypeNames[course.courseType] || '-'}
                </Fact>
                <Fact label="Content Items" valueClassName="font-semibold">
                  {course.contentItems.length}
                </Fact>
                {data.kpi && (
                  <>
                    <Fact label="Versions" valueClassName="font-bold text-slate-800">
                      {data.kpi.versionCount}
                    </Fact>
                    <Fact label="Active Learners" valueClassName="font-bold text-slate-800">
                      {data.kpi.learnerCount}
                    </Fact>
                    <Fact label="Assignment Batches" valueClassName="font-bold text-slate-800">
                      {data.kpi.assignmentCount}
                    </Fact>
                  </>
                )}
              </FactGrid>
          </Card>

          <DetailTabs
            tabs={detailTabs}
            active={activeDetailTab}
            onChange={handleDetailTabChange}
          />

          {activeDetailTab === 'versions' && (
            <Card icon={FileText} title="Versions">

              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-xs text-slate-500 font-bold uppercase">
                      <th className="p-3">Version No.</th>
                      <th className="p-3">Status</th>
                      <th className="p-3">Content Items</th>
                      <th className="p-3">Created Date</th>
                      <th className="p-3 text-center">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-slate-700">
                    {versions.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="p-8 text-center text-slate-400">
                          No versions.
                        </td>
                      </tr>
                    ) : (
                      visibleVersions.map(v => (
                        <tr key={v.id} className="hover:bg-slate-50 transition">
                          <td className="p-3 font-bold text-slate-900">
                            v{v.versionNumber}
                            {v.note && <span className="block text-xxs font-normal text-slate-400 mt-0.5">{v.note}</span>}
                          </td>
                          <td className="p-3">
                            {v.isActive ? (
                              <StatusBadge tone="success">Active Version</StatusBadge>
                            ) : (
                              <StatusBadge tone="neutral">Inactive</StatusBadge>
                            )}
                          </td>
                          <td className="p-3 text-xs text-slate-500">
                            {v.contentItems.length === 0
                              ? '—'
                              : v.contentItems.map(ci => ci.name).join(', ')}
                          </td>
                          <td className="p-3 text-slate-400 text-xs">
                            {formatDate(v.createdAt)}
                          </td>
                          <td className="p-3 text-center">
                            <div className="inline-flex items-center gap-2">
                              {!v.isActive && (
                                <IconButton
                                  onClick={() => handleSetActiveVersion(v.id)}
                                  icon={Check}
                                  title="Set active version"
                                  tone="success"
                                  size="sm"
                                />
                              )}
                              <Link
                                to={`/courses/${id}/version/${v.id}`}
                                className="p-1 text-slate-500 hover:bg-slate-100 rounded-md transition"
                                title="View version details"
                              >
                                <Eye className="h-4 w-4" />
                              </Link>
                       
                              <IconButton
                                onClick={() => handleDeleteVersion(v.id)}
                                icon={Trash2}
                                tone="danger"
                                size="sm"
                                title="Delete version"
                              />
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>

              {versions.length > 0 && (
                <div className="flex items-center justify-between gap-2 border-t border-slate-100 bg-slate-50/40 px-3 py-2">
                  <span className="text-xxs font-semibold uppercase tracking-wide text-slate-500">
                    Showing {visibleVersions.length} of {versions.length}
                  </span>
                  {versions.length > visibleVersions.length && (
                    <AppButton
                      variant="ghost"
                      onClick={() => setVisibleVersionRows(prev => prev + DETAIL_TABLE_CHUNK_SIZE)}
                      className="px-3 py-1 text-xxs font-bold"
                    >
                      Load more
                    </AppButton>
                  )}
                </div>
              )}
            </Card>
          )}

          {activeDetailTab === 'learners' && (
            <Card icon={Users} title="Learners">

              {loadingLearners || !hasLoadedLearners ? (
                <LoadingState size="section" />
              ) : (
                <>
                  <div className="overflow-x-auto">
                    <table className="w-full text-left text-sm border-collapse">
                      <thead>
                        <tr className="bg-slate-50 border-b border-slate-200 text-xs text-slate-500 font-bold uppercase">
                          <th className="p-3">Learner Code (EId)</th>
                          <th className="p-3">Name</th>
                          <th className="p-3">Department</th>
                          <th className="p-3">Progress</th>
                          <th className="p-3">Timeline</th>
                          <th className="p-3">Access Status</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 text-slate-700">
                        {learners.length === 0 ? (
                          <tr>
                            <td colSpan={6} className="p-8 text-center text-slate-400">
                              No learners.
                            </td>
                          </tr>
                        ) : (
                          visibleLearners.map(l => {
                            const isDone = l.isCompleted
                            return (
                              <tr key={l.id} className="hover:bg-slate-50 transition">
                                <td className="p-3 font-mono font-bold text-slate-800">{l.learnerCode}</td>
                                <td className="p-3 font-semibold text-slate-900">{l.learnerName}</td>
                                <td className="p-3 text-slate-500 text-xs">
                                  {l.division || '-'} {l.department ? `/ ${l.department}` : ''}
                                </td>
                                <td className="p-3">
                                  <ProgressBar value={l.progress} completed={isDone} maxWidthClass="max-w-30" />
                                </td>
                                <td className="p-3 text-slate-400 text-xs">
                                  <div>Start: {formatDate(l.startDate)}</div>
                                  <div className="mt-0.5">Due: {formatDate(l.dueDate)}</div>
                                </td>
                                <td className="p-3">
                                  <StatusBadge>{learnerStatusLabel(l.status)}</StatusBadge>
                                </td>
                              </tr>
                            )
                          })
                        )}
                      </tbody>
                    </table>
                  </div>

                  {learners.length > 0 && (
                    <div className="flex items-center justify-between gap-2 border-t border-slate-100 bg-slate-50/40 px-3 py-2">
                      <span className="text-xxs font-semibold uppercase tracking-wide text-slate-500">
                        Showing {visibleLearners.length} of {learners.length}
                      </span>
                      {learners.length > visibleLearners.length && (
                        <AppButton
                          variant="ghost"
                          onClick={() => setVisibleLearnerRows(prev => prev + DETAIL_TABLE_CHUNK_SIZE)}
                          className="px-3 py-1 text-xxs font-bold"
                        >
                          Load more
                        </AppButton>
                      )}
                    </div>
                  )}
                </>
              )}
            </Card>
          )}

          {activeDetailTab === 'assignments' && (
            <Card icon={Calendar} title="Assignments">

              {loadingAssignments || !hasLoadedAssignments ? (
                <LoadingState size="section" />
              ) : (
                <>
                  <div className="overflow-x-auto">
                    <table className="w-full text-left text-sm border-collapse">
                      <thead>
                        <tr className="bg-slate-50 border-b border-slate-200 text-xs text-slate-500 font-bold uppercase">
                          <th className="p-3">Batch No</th>
                          <th className="p-3">Description</th>
                          <th className="p-3">Start Date</th>
                          <th className="p-3">Due Date</th>
                          <th className="p-3">Status</th>
                          <th className="p-3">Progress</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 text-slate-700">
                        {assignments.length === 0 ? (
                          <tr>
                            <td colSpan={6} className="p-8 text-center text-slate-400">
                              No assignments.
                            </td>
                          </tr>
                        ) : (
                          visibleAssignments.map(a => (
                            <tr key={a.id} className="hover:bg-slate-50 transition">
                              <td className="p-3 font-mono font-bold text-indigo-500">
                                <Link to={`/assignments/${a.id}`} className="hover:underline">
                                  {a.assignmentNo}
                                </Link>
                              </td>
                              <td className="p-3 text-slate-700 font-medium">{a.description || '-'}</td>
                              <td className="p-3 text-slate-400 text-xs">{formatDate(a.startDate)}</td>
                              <td className="p-3 text-slate-400 text-xs">{formatDate(a.dueDate)}</td>
                              <td className="p-3">
                                <StatusBadge>{learnerStatusLabel(a.status)}</StatusBadge>
                              </td>
                              <td className="p-3">
                                <div className="flex flex-col font-bold text-xs text-slate-600">
                                  <span>{a.completedEnrollmentCount} / {a.totalEnrollmentCount} Learner</span>
                                  <span className="text-slate-400 font-normal mt-0.5">({a.completionPct}% completed)</span>
                                </div>
                              </td>
                            </tr>
                          ))
                        )}
                      </tbody>
                    </table>
                  </div>

                  {assignments.length > 0 && (
                    <div className="flex items-center justify-between gap-2 border-t border-slate-100 bg-slate-50/40 px-3 py-2">
                      <span className="text-xxs font-semibold uppercase tracking-wide text-slate-500">
                        Showing {visibleAssignments.length} of {assignments.length}
                      </span>
                      {assignments.length > visibleAssignments.length && (
                        <AppButton
                          variant="ghost"
                          onClick={() => setVisibleAssignmentRows(prev => prev + DETAIL_TABLE_CHUNK_SIZE)}
                          className="px-3 py-1 text-xxs font-bold"
                        >
                          Load more
                        </AppButton>
                      )}
                    </div>
                  )}
                </>
              )}
            </Card>
          )}
        </main>
      </DetailLayout>

      <Modal
        open={showEditPropertiesModal}
        onClose={() => setShowEditPropertiesModal(false)}
        size="lg"
        as="form"
        onSubmit={handleSaveProperties}
        windowClassName="p-5 animate-scale-in"
        ariaLabel="Edit Course Properties"
      >
            <IconButton
              type="button"
              onClick={() => setShowEditPropertiesModal(false)}
              icon={X}
              title="Close"
              tone="neutral"
              size="sm"
              className="absolute top-4 right-4"
            />

            <div className="mb-4 border-b border-slate-100 pb-3 pr-8 select-none">
              <h3 className="text-sm font-bold text-slate-800">Edit Course Properties</h3>
              <p className="text-xs text-slate-500 mt-1">Update course metadata without leaving this page.</p>
            </div>

            <div className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label htmlFor="edit-course-type" className="wiz-label">Course Type</label>
                  <select
                    id="edit-course-type"
                    value={editForm.courseType}
                    onChange={event => handleEditFormChange('courseType', Number(event.target.value))}
                    className="wiz-input"
                  >
                    <option value={0}>Select type</option>
                    {courseTypes.map(type => (
                      <option key={type.id} value={type.id}>{type.name}</option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1.5">
                  <label htmlFor="edit-course-code" className="wiz-label">Course Code</label>
                  <input
                    id="edit-course-code"
                    type="text"
                    value={editForm.courseCode}
                    onChange={event => handleEditFormChange('courseCode', event.target.value)}
                    className="wiz-input"
                    placeholder="e.g. CS-101"
                  />
                </div>

                <div className="space-y-1.5 sm:col-span-2">
                  <label htmlFor="edit-course-name" className="wiz-label">Course Title</label>
                  <input
                    id="edit-course-name"
                    type="text"
                    value={editForm.courseName}
                    onChange={event => handleEditFormChange('courseName', event.target.value)}
                    className="wiz-input"
                    placeholder="Course title"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label htmlFor="edit-division" className="wiz-label">Division</label>
                  <select
                    id="edit-division"
                    value={editForm.divisionId}
                    onChange={event => handleEditFormChange('divisionId', Number(event.target.value))}
                    className="wiz-input"
                  >
                    <option value={0}>Select division</option>
                    {divisions.map(division => (
                      <option key={division.id} value={division.id}>{division.name}</option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1.5">
                  <label htmlFor="edit-category" className="wiz-label">Category</label>
                  <select
                    id="edit-category"
                    value={editForm.categoryId}
                    onChange={event => handleEditFormChange('categoryId', Number(event.target.value))}
                    className="wiz-input"
                  >
                    <option value={0}>Select category</option>
                    {filteredCategories.map(category => (
                      <option key={category.id} value={category.id}>{category.name}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="space-y-1.5">
                <label htmlFor="edit-description" className="wiz-label">Description</label>
                <textarea
                  id="edit-description"
                  rows={5}
                  value={editForm.description}
                  onChange={event => handleEditFormChange('description', event.target.value)}
                  className="wiz-input resize-y"
                  placeholder="Course summary and objectives"
                />
              </div>
            </div>

            <div className="mt-5 pt-4 border-t border-slate-100 flex items-center justify-end gap-2">
              <AppButton
                variant="ghost"
                onClick={() => setShowEditPropertiesModal(false)}
              >
                Cancel
              </AppButton>
              <AppButton
                type="submit"
                variant="primary"
                loading={savingProperties}
                className="px-4 py-2 text-sm font-semibold"
              >
                Save Changes
              </AppButton>
            </div>
      </Modal>

      {confirmDialog}
    </>
  )
}

type CourseControlsProps = {
  courseId: string
  isDraft: boolean
  isOpen: boolean
  isClosed: boolean
  mutatingStatus: boolean
  onStatusChange: (status: number) => void
  onDeleteCourse: () => void
  onEditProperties: () => void
}

function CourseControls({
  courseId,
  isDraft,
  isOpen,
  isClosed,
  mutatingStatus,
  onStatusChange,
  onDeleteCourse,
  onEditProperties,
}: CourseControlsProps) {
  return (
    <ControlsSidebar>
      <ControlAction to={`/courses/${courseId}/version/new`} icon={Plus}>
        Add Version Package
      </ControlAction>
      <ControlAction
        to={`/assignments/bulk?courseId=${courseId}`}
        icon={UserPlus}
        disabled={!isOpen}
        title={isOpen ? undefined : 'Only Open courses can be assigned'}
      >
        Assign Courses
      </ControlAction>
      <ControlAction icon={Edit3} onClick={onEditProperties}>
        Edit Properties
      </ControlAction>
      <ControlAction
        icon={Power}
        disabled={isOpen || mutatingStatus}
        title={isOpen ? 'Course is already Open' : undefined}
        onClick={() => onStatusChange(1)}
      >
        Publish Course
      </ControlAction>
      <ControlAction
        icon={Lock}
        disabled={isClosed || mutatingStatus}
        title={isClosed ? 'Course is already Closed' : undefined}
        onClick={() => onStatusChange(2)}
      >
        Close Course
      </ControlAction>
      <ControlAction
        icon={FileText}
        disabled={isDraft || mutatingStatus}
        title={isDraft ? 'Course is already Draft' : undefined}
        onClick={() => onStatusChange(0)}
      >
        Revert to Draft
      </ControlAction>
      <ControlAction
        icon={Trash2}
        onClick={onDeleteCourse}
        variant="danger"
        disabled={!isClosed || mutatingStatus}
        title={!isClosed ? 'Course must be Closed before it can be deleted' : undefined}
      >
        Delete Course
      </ControlAction>
    </ControlsSidebar>
  )
}
