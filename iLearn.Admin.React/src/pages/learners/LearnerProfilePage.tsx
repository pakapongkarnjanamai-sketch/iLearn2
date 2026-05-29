import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { 
  ArrowLeft, 
  User, 
  AlertTriangle,
  Loader2,
  FileBadge
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'

type LearnerKpis = {
  totalCourses: number
  completedCourses: number
  inProgressCourses: number
  totalTimeSpentSeconds: number
}

type LearnerEnrollmentHistory = {
  enrollmentId: number
  courseId: number
  courseCode: string
  courseTitle: string
  isCourseDeleted: boolean
  progress: number
  isCompleted: boolean
  startDate: string | null
  dueDate: string | null
  completedDate: string | null
  totalScore: number
  totalTimeSpent: number
  hasActiveAssignment: boolean
  isAssignmentCancelled: boolean
}

type LearnerProfileResponse = {
  success: boolean
  data: {
    code: string
    name: string
    division: string | null
    department: string | null
    section: string | null
    position: string | null
    kpi: LearnerKpis
    enrollments: LearnerEnrollmentHistory[]
  }
}

export function LearnerProfilePage() {
  const { id } = useParams() // NID / employeeCode
  const { setLabel } = useBreadcrumbs()
  const [loading, setLoading] = useState(true)
  const [profile, setProfile] = useState<LearnerProfileResponse['data'] | null>(null)

  useEffect(() => {
    if (profile?.name) {
      setLabel(String(id), profile.name)
    }
  }, [profile, id, setLabel])

  const loadProfile = async () => {
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<LearnerProfileResponse>(`Learners/profile/${id}`)
      if (resp.success && resp.data) {
        setProfile(resp.data)
      }
    } catch (err) {
      console.error(err)
      toast.error('Unable to fetch learner transcript profile')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadProfile()
  }, [id])

  if (loading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-indigo-500" />
      </div>
    )
  }

  if (!profile) {
    return (
      <div className="text-center py-12">
        <AlertTriangle className="h-12 w-12 text-amber-500 mx-auto" />
        <h2 className="text-lg font-bold text-slate-700 mt-4">Learner Profile Missing</h2>
        <p className="text-slate-400 mt-2">The learner's corporate identity could not be verified.</p>
        <Link to="/learners" className="mt-6 inline-flex items-center text-indigo-500 font-semibold hover:underline">
          <ArrowLeft className="h-4 w-4 mr-1" /> Back to Directory
        </Link>
      </div>
    )
  }

  // Convert time spent (seconds) to hours and minutes
  const formatTimeSpent = (totalSeconds: number) => {
    const hours = Math.floor(totalSeconds / 3600)
    const minutes = Math.floor((totalSeconds % 3600) / 60)
    
    if (hours === 0 && minutes === 0) return '—'
    if (hours === 0) return `${minutes}m`
    return `${hours}h ${minutes}m`
  }

  return (
    <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_320px] xl:items-start">
      {/* Transcript (main) */}
      <div className="min-w-0">
        <section className="space-y-4">
          <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3">
            <h2 className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600"><FileBadge aria-hidden="true" />Transcript</h2>
          </div>

          <div className="overflow-x-auto custom-scrollbar">
            <table className="w-full text-left text-sm border-collapse">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                  <th className="p-3">Course Identity</th>
                  <th className="p-3">Progress</th>
                  <th className="p-3">Grade / Score</th>
                  <th className="p-3">Time Spent</th>
                  <th className="p-3">Timeline</th>
                  <th className="p-3">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-700">
                {profile.enrollments.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="p-8 text-center text-slate-400">
                      No enrollments.
                    </td>
                  </tr>
                ) : (
                  profile.enrollments.map(e => {
                    const isCancelled = e.isAssignmentCancelled
                    const isFinished = e.isCompleted

                    return (
                      <tr key={e.enrollmentId} className="hover:bg-slate-50 transition">
                        <td className="p-3">
                          <div className="flex flex-col">
                            <span className={`font-bold text-slate-800 leading-tight ${e.isCourseDeleted ? 'line-through text-slate-400' : ''}`}>
                              {e.courseTitle}
                            </span>
                            <span className="text-xxs font-mono text-slate-400 mt-0.5">
                              {e.courseCode} {e.isCourseDeleted && '(Syllabus Deleted)'}
                            </span>
                          </div>
                        </td>
                        <td className="p-3">
                          <div className="flex items-center gap-2 max-w-24">
                            <div className="w-full bg-slate-100 rounded-full h-1.5">
                              <div 
                                className={`h-1.5 rounded-full ${isFinished ? 'bg-emerald-500' : 'bg-blue-600'}`} 
                                style={{ width: `${e.progress}%` }}
                              ></div>
                            </div>
                            <span className="font-bold text-xxs text-slate-500 shrink-0">{Math.round(e.progress)}%</span>
                          </div>
                        </td>
                        <td className="p-3 font-mono font-bold text-slate-800 text-xs">
                          {e.totalScore > 0 ? `${Math.round(e.totalScore)}pt` : '—'}
                        </td>
                        <td className="p-3 font-mono text-slate-500 text-xs">
                          {formatTimeSpent(e.totalTimeSpent)}
                        </td>
                        <td className="p-3 text-slate-400 text-xxs">
                          {e.completedDate ? (
                            <div className="text-emerald-600 font-semibold">Done: {new Date(e.completedDate).toLocaleDateString()}</div>
                          ) : (
                            <>
                              {e.startDate && <div>Start: {new Date(e.startDate).toLocaleDateString()}</div>}
                              {e.dueDate && <div className="mt-0.5">Due: {new Date(e.dueDate).toLocaleDateString()}</div>}
                            </>
                          )}
                        </td>
                        <td className="p-3">
                          {isFinished ? (
                            <span className="inline-flex px-2 py-0.5 rounded text-xxs font-bold bg-emerald-100 text-emerald-800">Passed</span>
                          ) : isCancelled ? (
                            <span className="inline-flex px-2 py-0.5 rounded text-xxs font-bold bg-amber-100 text-amber-800 gap-1 items-center" title="Rule Deleted">
                              <AlertTriangle className="h-3 w-3" />
                              <span>Cancelled</span>
                            </span>
                          ) : e.hasActiveAssignment ? (
                            <span className="inline-flex px-2 py-0.5 rounded text-xxs font-bold bg-blue-100 text-blue-800">Assigned</span>
                          ) : (
                            <span className="inline-flex px-2 py-0.5 rounded text-xxs font-semibold bg-slate-100 text-slate-500">Self-Enroll</span>
                          )}
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </table>
          </div>
        </section>
      </div>

      {/* Learner identity + summary (sidebar) */}
      <aside className="space-y-5 xl:sticky xl:top-5">
        <div className="flex flex-col items-center text-center pb-4 border-b border-slate-200">
          <div className="h-14 w-14 bg-indigo-50 text-indigo-500 rounded-full flex items-center justify-center mb-3">
            <User className="h-7 w-7" />
          </div>
          <h2 className="text-base font-bold text-slate-800 leading-tight">{profile.name}</h2>
          <span className="text-xs font-mono text-slate-400 mt-1">{profile.code}</span>
        </div>

        <dl className="space-y-3.5 text-xs">
          <div>
            <dt className="text-slate-400 font-bold uppercase tracking-wider">Division</dt>
            <dd className="text-slate-800 font-semibold mt-0.5">{profile.division || '—'}</dd>
          </div>
          <div>
            <dt className="text-slate-400 font-bold uppercase tracking-wider">Department</dt>
            <dd className="text-slate-800 font-semibold mt-0.5">{profile.department || '—'}</dd>
          </div>
          {profile.section && (
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Section</dt>
              <dd className="text-slate-800 font-semibold mt-0.5">{profile.section}</dd>
            </div>
          )}
          <div>
            <dt className="text-slate-400 font-bold uppercase tracking-wider">Position</dt>
            <dd className="text-slate-800 font-semibold mt-0.5">{profile.position || '—'}</dd>
          </div>
        </dl>

        <div className="border-t border-slate-200 pt-4">
          <span className="block text-xxs font-extrabold text-slate-400 uppercase mb-3">Summary</span>
          <dl className="grid grid-cols-2 gap-x-6 gap-y-5 text-xs">
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Courses</dt>
              <dd className="mt-1 text-lg font-extrabold leading-tight text-slate-800">{profile.kpi.totalCourses}</dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Completed</dt>
              <dd className="mt-1 text-lg font-extrabold leading-tight text-emerald-600">{profile.kpi.completedCourses}</dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">In Progress</dt>
              <dd className="mt-1 text-lg font-extrabold leading-tight text-amber-600">{profile.kpi.inProgressCourses}</dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Hours</dt>
              <dd className="mt-1 text-lg font-bold leading-tight text-slate-800">{formatTimeSpent(profile.kpi.totalTimeSpentSeconds)}</dd>
            </div>
          </dl>
        </div>
      </aside>
    </div>
  )
}
