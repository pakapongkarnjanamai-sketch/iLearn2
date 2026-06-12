import { useCallback, useEffect, useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import {
  Calendar,
  Users,
  FileText,
  Plus,
  Trash2,
  Edit3,
  UserPlus,
  Power,
  Lock
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { StatusText } from '../../components/ui/StatusText'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { formatDate } from '../../lib/format'

type CourseDetail = {
  id: number
  code?: string
  courseCode?: string
  title?: string
  courseName?: string
  description: string
  isActive: boolean
  status: number
  statusName: string
  categoryId: number
  categoryName: string
  divisionId: number
  divisionName: string
  courseTypeId: number
  courseTypeName: string
}

type CourseVersion = {
  id: number
  versionNo: string
  description: string
  isActive: boolean
  isDraft: boolean
  schemaVersion: string
  launchHref: string
  updatedAt: string
}

type CourseKPI = {
  versionCount: number
  learnerCount: number
  completedCount: number
  assignmentCount: number
}

type CourseDashboardData = {
  course: CourseDetail
  versions: CourseVersion[]
  kpi?: CourseKPI
}

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
    } catch (err) {
      console.error(err)
      toast.error('Failed to delete this course. It may have active assignments or enrolled learners.')
    }
  }
  const [data, setData] = useState<CourseDashboardData | null>(null)
  const [learners, setLearners] = useState<CourseLearner[]>([])
  const [loadingLearners, setLoadingLearners] = useState(false)
  const [assignments, setAssignments] = useState<CourseAssignment[]>([])
  const [loadingAssignments, setLoadingAssignments] = useState(false)
  
  const [activeTab, setActiveTab] = useState<'overview' | 'versions' | 'learners' | 'assignments'>('overview')
  const [mutatingStatus, setMutatingStatus] = useState(false)

  useEffect(() => {
    const code = data?.course?.courseCode || data?.course?.code
    if (code) {
      setLabel(String(id), code)
    }
  }, [data, id, setLabel])

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

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      if (activeTab === 'learners') {
        void loadLearners()
      } else if (activeTab === 'assignments') {
        void loadAssignments()
      }
    }, 0)

    return () => window.clearTimeout(timeoutId)
  }, [activeTab, loadAssignments, loadLearners])

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
  const isRetired = course.status === 2

  return (
    <>
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start">
        <div className="min-w-0">
          {/* Tab controls */}
          <div className="border-b border-slate-200 mb-6 flex gap-1">
            {(['overview', 'versions', 'learners', 'assignments'] as const).map(tab => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                className={`pb-3 px-3 font-semibold text-sm transition relative cursor-pointer ${
                  activeTab === tab 
                    ? 'text-indigo-600 font-bold border-b-2 border-indigo-500' 
                    : 'text-slate-400 hover:text-slate-700'
                }`}
              >
                {tab.charAt(0).toUpperCase() + tab.slice(1)}
              </button>
            ))}
          </div>

          {/* Tab Content Panels */}
          <main className="space-y-6">
            
            {activeTab === 'overview' && (
              <section className="rounded-lg border border-slate-200 bg-white p-5 space-y-5">
                {/* Course Title & Code */}
                <div>
                  <h1 className="text-xl font-extrabold text-slate-900 leading-tight">{course.courseName || course.title}</h1>
                  <span className="inline-block mt-1 font-mono text-xs text-slate-400">{course.courseCode || course.code}</span>
                </div>

                {/* Description */}
                {course.description && (
                  <p className="text-sm text-slate-500 leading-relaxed max-w-2xl border-l-2 border-slate-200 pl-3 whitespace-pre-wrap">
                    {course.description}
                  </p>
                )}

                {/* Metadata Grid */}
                <dl className="grid grid-cols-2 sm:grid-cols-3 gap-x-6 gap-y-4 text-sm border-t border-slate-100 pt-5">
                  <div>
                    <dt className="text-xs text-slate-400 font-bold uppercase tracking-wide">Status</dt>
                    <dd className="mt-1">
                      <StatusText tone={isOpen ? 'success' : isDraft ? 'warning' : 'danger'}>
                        {course.statusName}
                      </StatusText>
                    </dd>
                  </div>
                  <div>
                    <dt className="text-xs text-slate-400 font-bold uppercase tracking-wide">Category</dt>
                    <dd className="mt-1 font-semibold text-slate-700">{course.categoryName || '-'}</dd>
                  </div>
                  <div>
                    <dt className="text-xs text-slate-400 font-bold uppercase tracking-wide">Division</dt>
                    <dd className="mt-1 font-semibold text-slate-700">{course.divisionName || '-'}</dd>
                  </div>
                  <div>
                    <dt className="text-xs text-slate-400 font-bold uppercase tracking-wide">Course Type</dt>
                    <dd className="mt-1 font-semibold text-slate-700">{course.courseTypeName || '-'}</dd>
                  </div>
                  {data.kpi && (
                    <>
                      <div>
                        <dt className="text-xs text-slate-400 font-bold uppercase tracking-wide">Versions</dt>
                        <dd className="mt-1 font-bold text-slate-800 text-lg">{data.kpi.versionCount}</dd>
                      </div>
                      <div>
                        <dt className="text-xs text-slate-400 font-bold uppercase tracking-wide">Active Learners</dt>
                        <dd className="mt-1 font-bold text-slate-800 text-lg">{data.kpi.learnerCount}</dd>
                      </div>
                      <div>
                        <dt className="text-xs text-slate-400 font-bold uppercase tracking-wide">Assignment Batches</dt>
                        <dd className="mt-1 font-bold text-slate-800 text-lg">{data.kpi.assignmentCount}</dd>
                      </div>
                    </>
                  )}
                </dl>
              </section>
            )}

            {activeTab === 'versions' && (
              <section className="overflow-hidden rounded-lg border border-slate-200 bg-white">
            <SectionHeader icon={FileText} variant="card">Versions</SectionHeader>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-xs text-slate-500 font-bold uppercase">
                    <th className="p-3">Version No.</th>
                    <th className="p-3">Status</th>
                    <th className="p-3">SCORM Metadata</th>
                    <th className="p-3">Updated Date</th>
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
                    versions.map(v => (
                      <tr key={v.id} className="hover:bg-slate-50 transition">
                        <td className="p-3 font-bold text-slate-900">{v.versionNo}</td>
                        <td className="p-3">
                          {v.isActive ? (
                            <StatusBadge tone="success">Active Version</StatusBadge>
                          ) : (
                            <StatusBadge tone="neutral">Inactive</StatusBadge>
                          )}
                        </td>
                        <td className="p-3 font-mono text-xs text-slate-500">
                          {v.schemaVersion || 'SCORM 1.2'} ({v.launchHref})
                        </td>
                        <td className="p-3 text-slate-400 text-xs">
                          {formatDate(v.updatedAt)}
                        </td>
                        <td className="p-3 text-center">
                          <div className="inline-flex items-center gap-2">
                            {!v.isActive && (
                              <button
                                onClick={() => handleSetActiveVersion(v.id)}
                                className="px-2 py-1 bg-indigo-50 text-indigo-500 border border-blue-200 rounded text-xs font-semibold hover:bg-blue-100 transition"
                              >
                                Set Active
                              </button>
                            )}
                            <Link
                              to={`/courses/${id}/version/${v.id}/edit`}
                              className="px-2 py-1 bg-slate-50 text-slate-600 border border-slate-200 rounded text-xs font-semibold hover:bg-slate-100 transition"
                            >
                              Edit
                            </Link>
                            <button
                              onClick={() => handleDeleteVersion(v.id)}
                              className="p-1 text-slate-400 hover:text-red-600 rounded transition"
                              title="Delete version"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
            )}

            {activeTab === 'learners' && (
              <section className="overflow-hidden rounded-lg border border-slate-200 bg-white">
            <SectionHeader icon={Users} variant="card">Learners</SectionHeader>

            {loadingLearners ? (
              <LoadingState size="section" />
            ) : (
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
                      learners.map(l => {
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
                              <StatusBadge>{l.status}</StatusBadge>
                            </td>
                          </tr>
                        )
                      })
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </section>
            )}

            {activeTab === 'assignments' && (
              <section className="overflow-hidden rounded-lg border border-slate-200 bg-white">
            <SectionHeader icon={Calendar} variant="card">Assignments</SectionHeader>

            {loadingAssignments ? (
              <LoadingState size="section" />
            ) : (
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
                      assignments.map(a => (
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
                            <StatusBadge>{a.status}</StatusBadge>
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
            )}
          </section>
            )}

          </main>
        </div>

        <CourseControls
          courseId={id ?? String(course.id)}
          isDraft={isDraft}
          isOpen={isOpen}
          isRetired={isRetired}
          mutatingStatus={mutatingStatus}
          onStatusChange={handleStatusChange}
          onDeleteCourse={handleDeleteCourse}
        />
      </div>

      {confirmDialog}
    </>
  )
}

type CourseControlsProps = {
  courseId: string
  isDraft: boolean
  isOpen: boolean
  isRetired: boolean
  mutatingStatus: boolean
  onStatusChange: (status: number) => void
  onDeleteCourse: () => void
}

function CourseControls({
  courseId,
  isDraft,
  isOpen,
  isRetired,
  mutatingStatus,
  onStatusChange,
  onDeleteCourse,
}: CourseControlsProps) {
  return (
    <ControlsSidebar backTo="/courses" backLabel="Back to Directory">
      <ControlAction to={`/courses/${courseId}/version/new`} icon={Plus}>
        Add Version Package
      </ControlAction>
      <ControlAction
        to={`/assignments/bulk?courseId=${courseId}`}
        icon={UserPlus}
        disabled={!isOpen}
        title={isOpen ? undefined : 'Only Open courses can be assigned'}
      >
        Bulk Assign
      </ControlAction>
      <ControlAction to={`/courses/${courseId}/edit`} icon={Edit3}>
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
        disabled={isRetired || mutatingStatus}
        title={isRetired ? 'Course is already Retired' : undefined}
        onClick={() => onStatusChange(2)}
      >
        Retire Course
      </ControlAction>
      <ControlAction
        icon={FileText}
        disabled={isDraft || mutatingStatus}
        title={isDraft ? 'Course is already Draft' : undefined}
        onClick={() => onStatusChange(0)}
      >
        Revert to Draft
      </ControlAction>
      <ControlAction icon={Trash2} onClick={onDeleteCourse} variant="danger">
        Delete Course
      </ControlAction>
    </ControlsSidebar>
  )
}
