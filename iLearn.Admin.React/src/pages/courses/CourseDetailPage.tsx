import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { 
  ArrowLeft, 
  Settings, 
  Calendar, 
  Users, 
  FileText,
  Plus,
  RefreshCw,
  Trash2,
  AlertTriangle,
  Edit3,
  UserPlus,
  Power,
  Lock
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'

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
  
  const [loading, setLoading] = useState(true)

  const handleDeleteCourse = async () => {
    if (!window.confirm('Are you sure you want to delete this course? This action cannot be undone and will delete all associated versions.')) return
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
    if (!window.confirm('Delete this version? Action cannot be undone.')) return
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
    return (
      <div className="flex h-96 items-center justify-center">
        <RefreshCw className="h-8 w-8 animate-spin text-indigo-500" />
      </div>
    )
  }

  if (!data) {
    return (
      <div className="text-center py-12">
        <AlertTriangle className="h-12 w-12 text-amber-500 mx-auto" />
        <h2 className="text-lg font-bold text-slate-700 mt-4">Course Not Found</h2>
        <p className="text-slate-400 mt-2">The requested course catalog identity is missing or has been deleted.</p>
        <Link to="/courses" className="mt-6 inline-flex items-center text-indigo-500 font-semibold hover:underline">
          <ArrowLeft className="h-4 w-4 mr-1" /> Back to courses
        </Link>
      </div>
    )
  }

  const { course, versions } = data
  const isDraft = course.status === 0
  const isOpen = course.status === 1
  const isRetired = course.status === 2

  return (
    <>
      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_320px] xl:items-start">
        <div className="min-w-0">
          {/* Tab controls */}
          <div className="border-b border-slate-200 mb-6 flex gap-4">
            {(['overview', 'versions', 'learners', 'assignments'] as const).map(tab => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                className={`pb-3 font-semibold text-sm transition relative ${
                  activeTab === tab 
                    ? 'text-indigo-500 font-bold border-b-2 border-indigo-500' 
                    : 'text-slate-500 hover:text-slate-700'
                }`}
              >
                {tab.charAt(0).toUpperCase() + tab.slice(1)}
              </button>
            ))}
          </div>

          {/* Tab Content Panels */}
          <main className="space-y-6">
            
            {activeTab === 'overview' && (
              <section className="space-y-6">
                <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3">
                  <h2 className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600">Course Overview</h2>
                </div>
                
                {/* Minimalist Title */}
                <div>
                  <h1 className="text-xl font-extrabold text-slate-900 leading-tight">{course.courseName || course.title}</h1>
                  <span className="inline-block mt-1 font-mono text-xs text-slate-400">{course.courseCode || course.code}</span>
                </div>

                {/* Minimalist Description */}
                {course.description && (
                  <p className="text-slate-500 text-xs leading-relaxed max-w-2xl border-l-2 border-slate-200 pl-3 whitespace-pre-wrap">
                    {course.description}
                  </p>
                )}

                {/* Clean Horizontal Grid */}
                <dl className="grid grid-cols-2 sm:grid-cols-3 gap-x-6 gap-y-5 text-xs border-t border-slate-100 pt-5">
                  <div>
                    <dt className="text-slate-400 font-bold uppercase tracking-wider">Status</dt>
                    <dd className="mt-1 font-bold text-slate-800 flex items-center gap-1.5">
                      <span className={`h-1.5 w-1.5 rounded-full ${
                        isOpen ? 'bg-emerald-500' : isDraft ? 'bg-amber-500' : 'bg-rose-500'
                      }`} />
                      <span>{course.statusName}</span>
                    </dd>
                  </div>
                  <div>
                    <dt className="text-slate-400 font-bold uppercase tracking-wider">Category</dt>
                    <dd className="mt-1 font-semibold text-slate-700">{course.categoryName || '-'}</dd>
                  </div>
                  <div>
                    <dt className="text-slate-400 font-bold uppercase tracking-wider">Division</dt>
                    <dd className="mt-1 font-semibold text-slate-700">{course.divisionName || '-'}</dd>
                  </div>
                  <div>
                    <dt className="text-slate-400 font-bold uppercase tracking-wider">Course Type</dt>
                    <dd className="mt-1 font-semibold text-slate-700">{course.courseTypeName || '-'}</dd>
                  </div>
                  {data.kpi && (
                    <>
                      <div>
                        <dt className="text-slate-400 font-bold uppercase tracking-wider">Versions</dt>
                        <dd className="mt-1 font-bold text-slate-800">{data.kpi.versionCount}</dd>
                      </div>
                      <div>
                        <dt className="text-slate-400 font-bold uppercase tracking-wider">Active Learners</dt>
                        <dd className="mt-1 font-bold text-slate-800">{data.kpi.learnerCount}</dd>
                      </div>
                      <div>
                        <dt className="text-slate-400 font-bold uppercase tracking-wider">Assignment Batches</dt>
                        <dd className="mt-1 font-bold text-slate-800">{data.kpi.assignmentCount}</dd>
                      </div>
                    </>
                  )}
                </dl>
              </section>
            )}

            {activeTab === 'versions' && (
              <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3 p-4">
              <div className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600">
                <FileText aria-hidden="true" />
                <h2>Versions</h2>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
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
                            <span className="inline-flex px-2 py-0.5 rounded text-xs font-bold bg-emerald-100 text-emerald-800 shadow-xs">Active Version</span>
                          ) : (
                            <span className="inline-flex px-2 py-0.5 rounded text-xs font-semibold bg-slate-100 text-slate-600">Inactive</span>
                          )}
                        </td>
                        <td className="p-3 font-mono text-xs text-slate-500">
                          {v.schemaVersion || 'SCORM 1.2'} ({v.launchHref})
                        </td>
                        <td className="p-3 text-slate-400 text-xs">
                          {new Date(v.updatedAt).toLocaleDateString()}
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
              <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3 p-4">
              <div className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600">
                <Users aria-hidden="true" />
                <h2>Learners</h2>
              </div>
            </div>

            {loadingLearners ? (
              <div className="flex h-32 items-center justify-center">
                <RefreshCw className="h-6 w-6 animate-spin text-slate-400" />
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
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
                              <div className="flex items-center gap-2 max-w-30">
                                <div className="w-full bg-slate-100 rounded-full h-1.5">
                                  <div 
                                    className={`h-1.5 rounded-full ${isDone ? 'bg-emerald-500' : 'bg-blue-600'}`} 
                                    style={{ width: `${l.progress}%` }}
                                  ></div>
                                </div>
                                <span className="font-bold text-xxs text-slate-600 shrink-0">{l.progress}%</span>
                              </div>
                            </td>
                            <td className="p-3 text-slate-400 text-xxs">
                              <div>Start: {new Date(l.startDate).toLocaleDateString()}</div>
                              <div className="mt-0.5">Due: {new Date(l.dueDate).toLocaleDateString()}</div>
                            </td>
                            <td className="p-3">
                              <span className={`inline-flex px-2 py-0.5 rounded text-xxs font-bold ${
                                l.status === 'Completed' ? 'bg-emerald-100 text-emerald-800'
                                  : l.status === 'In Progress' ? 'bg-blue-100 text-blue-800'
                                  : 'bg-slate-100 text-slate-700'
                              }`}>{l.status}</span>
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
              <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3 p-4">
              <div className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600">
                <Calendar aria-hidden="true" />
                <h2>Assignments</h2>
              </div>
            </div>

            {loadingAssignments ? (
              <div className="flex h-32 items-center justify-center">
                <RefreshCw className="h-6 w-6 animate-spin text-slate-400" />
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
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
                          <td className="p-3 text-slate-400 text-xs">{new Date(a.startDate).toLocaleDateString()}</td>
                          <td className="p-3 text-slate-400 text-xs">{new Date(a.dueDate).toLocaleDateString()}</td>
                          <td className="p-3">
                            <span className={`inline-flex px-2 py-0.5 rounded text-xxs font-bold ${
                              a.status === 'Completed' ? 'bg-emerald-100 text-emerald-800'
                                : a.status === 'Enrolling' || a.status === 'In Progress' ? 'bg-blue-100 text-blue-800'
                                : 'bg-slate-100 text-slate-700'
                            }`}>{a.status}</span>
                          </td>
                          <td className="p-3">
                            <div className="flex flex-col font-bold text-xxs text-slate-600">
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
    <aside className="space-y-5 xl:sticky xl:top-5">
      <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3">
        <h2 className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600"><Settings aria-hidden="true" />Course Control Hub</h2>
      </div>

      {/* Primary Actions */}
      <div className="space-y-3">
        <span className="block text-xxs font-extrabold text-slate-400 uppercase">Management Actions</span>
        <ControlLinkButton to={`/courses/${courseId}/version/new`} icon={<Plus aria-hidden="true" />} variant="primary">
          Add Version Package
        </ControlLinkButton>
        <ControlLinkButton
          to={`/assignments/bulk?courseId=${courseId}`}
          icon={<UserPlus aria-hidden="true" />}
          disabled={!isOpen}
          title={isOpen ? undefined : 'Only Open courses can be assigned'}
        >
          Bulk Assign
        </ControlLinkButton>
        <ControlLinkButton to={`/courses/${courseId}/edit`} icon={<Edit3 aria-hidden="true" />}>
          Edit Properties
        </ControlLinkButton>
      </div>

      {/* Lifecycle Status Transitions */}
      <div className="space-y-3 pt-1">
        <span className="block text-xxs font-extrabold text-slate-400 uppercase">Transitions</span>
        <div className="space-y-2">
          <ControlActionButton
            icon={<Power aria-hidden="true" />}
            label="Publish Course"
            tag="Open"
            tone="success"
            disabled={isOpen || mutatingStatus}
            title={isOpen ? 'Course is already Open' : undefined}
            onClick={() => onStatusChange(1)}
          />
          <ControlActionButton
            icon={<Lock aria-hidden="true" />}
            label="Retire Course"
            tag="Retired"
            tone="danger"
            disabled={isRetired || mutatingStatus}
            title={isRetired ? 'Course is already Retired' : undefined}
            onClick={() => onStatusChange(2)}
          />
          <ControlActionButton
            icon={<FileText aria-hidden="true" />}
            label="Revert to Draft"
            tag="Draft"
            tone="neutral"
            disabled={isDraft || mutatingStatus}
            title={isDraft ? 'Course is already Draft' : undefined}
            onClick={() => onStatusChange(0)}
          />
        </div>
      </div>

      {/* Destructive actions & Directory Link */}
      <div className="border-t border-slate-200 pt-4 space-y-3">
        <button
          type="button"
          onClick={onDeleteCourse}
          className="inline-flex min-h-[34px] items-center justify-center gap-[7px] rounded-md border border-transparent px-3 font-semibold cursor-pointer disabled:cursor-not-allowed disabled:opacity-55 bg-red-600 text-white hover:bg-red-700 w-full flex items-center justify-center gap-2 text-xs font-bold transition"
        >
          <Trash2 className="h-4 w-4" />
          <span>Delete Course</span>
        </button>

        <Link 
          to="/courses" 
          className="w-full flex items-center justify-center gap-1 text-slate-500 hover:text-slate-800 transition font-bold text-xs pt-1.5"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          <span>Back to Directory</span>
        </Link>
      </div>
    </aside>
  )
}

type ControlLinkButtonProps = {
  to: string
  icon: ReactNode
  children: ReactNode
  disabled?: boolean
  title?: string | undefined
  variant?: 'primary' | 'secondary'
}

function ControlLinkButton({
  to,
  icon,
  children,
  disabled = false,
  title,
  variant = 'secondary',
}: ControlLinkButtonProps) {
  const button = (
    <button
      type="button"
      disabled={disabled}
      title={title}
      className={`inline-flex min-h-[34px] items-center justify-center gap-[7px] rounded-md border border-transparent px-3 font-semibold cursor-pointer disabled:cursor-not-allowed disabled:opacity-55 w-full ${variant === 'primary' ? 'bg-indigo-600 text-white hover:bg-indigo-700' : 'border-slate-200 bg-white text-slate-900 hover:border-slate-300 hover:bg-slate-50'}`}
    >
      {icon}
      <span>{children}</span>
    </button>
  )

  return disabled ? button : <Link to={to} className="block">{button}</Link>
}

type ControlActionButtonProps = {
  icon: ReactNode
  label: string
  tag: string
  tone: 'success' | 'danger' | 'neutral'
  disabled: boolean
  title?: string | undefined
  onClick: () => void
}

function ControlActionButton({
  icon,
  label,
  tag,
  tone,
  disabled,
  title,
  onClick,
}: ControlActionButtonProps) {
  const toneClasses = {
    success: 'border-emerald-200 bg-emerald-50 text-emerald-800 hover:bg-emerald-100',
    danger: 'border-rose-200 bg-rose-50 text-rose-800 hover:bg-rose-100',
    neutral: 'border-slate-200 bg-slate-50 text-slate-800 hover:bg-slate-100',
  }[tone]

  return (
    <button
      type="button"
      disabled={disabled}
      title={title}
      onClick={onClick}
      className={`w-full flex items-center justify-between gap-2 rounded-md border px-3 py-2 text-xs font-bold transition ${toneClasses} disabled:cursor-not-allowed disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400 disabled:opacity-60`}
    >
      <span className="flex items-center gap-2">
        {icon}
        <span>{label}</span>
      </span>
      <span className="text-xxs font-extrabold uppercase opacity-70">{tag}</span>
    </button>
  )
}
