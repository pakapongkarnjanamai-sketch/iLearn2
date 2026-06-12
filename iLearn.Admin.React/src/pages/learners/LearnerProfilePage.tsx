import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import {
  User,
  AlertTriangle,
  FileBadge
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { SectionHeader } from '../../components/ui/SectionHeader'
import {
  DetailCard,
  DetailSubSection,
  Fact,
  FactGrid,
} from '../../components/ui/detail'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { formatDate } from '../../lib/format'

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
    return <LoadingState />
  }

  if (!profile) {
    return (
      <NotFoundState
        title="Learner Profile Missing"
        message="The learner's corporate identity could not be verified."
        backTo="/learners"
        backLabel="Back to Directory"
      />
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
    <>
      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_320px] xl:items-start">
        {/* Transcript (main) */}
        <div className="min-w-0">
          <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            <SectionHeader icon={FileBadge} variant="card">Transcript</SectionHeader>

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
                    profile.enrollments.map((e) => {
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
                            <ProgressBar value={e.progress} completed={isFinished} />
                          </td>
                          <td className="p-3 font-mono font-bold text-slate-800 text-xs">
                            {e.totalScore > 0 ? `${Math.round(e.totalScore)}pt` : '—'}
                          </td>
                          <td className="p-3 font-mono text-slate-500 text-xs">
                            {formatTimeSpent(e.totalTimeSpent)}
                          </td>
                          <td className="p-3 text-slate-400 text-xxs">
                            {e.completedDate ? (
                              <div className="text-emerald-600 font-semibold">Done: {formatDate(e.completedDate)}</div>
                            ) : (
                              <>
                                {e.startDate && <div>Start: {formatDate(e.startDate)}</div>}
                                {e.dueDate && <div className="mt-0.5">Due: {formatDate(e.dueDate)}</div>}
                              </>
                            )}
                          </td>
                          <td className="p-3">
                            {isFinished ? (
                              <StatusBadge tone="success" size="xxs">Passed</StatusBadge>
                            ) : isCancelled ? (
                              <StatusBadge tone="warning" size="xxs">
                                <span className="inline-flex items-center gap-1" title="Rule Deleted">
                                  <AlertTriangle className="h-3 w-3" />
                                  Cancelled
                                </span>
                              </StatusBadge>
                            ) : e.hasActiveAssignment ? (
                              <StatusBadge tone="info" size="xxs">Assigned</StatusBadge>
                            ) : (
                              <StatusBadge tone="neutral" size="xxs">Self-Enroll</StatusBadge>
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
        <DetailCard className="xl:sticky xl:top-5 shadow-xs">
          <div className="flex flex-col items-center text-center pb-4 border-b border-slate-200">
            <div className="h-14 w-14 bg-indigo-50 text-indigo-500 rounded-full flex items-center justify-center mb-3">
              <User className="h-7 w-7" />
            </div>
            <h2 className="text-base font-bold text-slate-800 leading-tight">{profile.name}</h2>
            <span className="text-xs font-mono text-slate-400 mt-1">{profile.code}</span>
          </div>

          <FactGrid cols={2} className="grid-cols-1 sm:grid-cols-1 gap-y-3.5 text-xs">
            <Fact label="Division" valueClassName="text-slate-800 font-semibold mt-0.5">
              {profile.division || '—'}
            </Fact>
            <Fact label="Department" valueClassName="text-slate-800 font-semibold mt-0.5">
              {profile.department || '—'}
            </Fact>
            {profile.section && (
              <Fact label="Section" valueClassName="text-slate-800 font-semibold mt-0.5">
                {profile.section}
              </Fact>
            )}
            <Fact label="Position" valueClassName="text-slate-800 font-semibold mt-0.5">
              {profile.position || '—'}
            </Fact>
          </FactGrid>

          <DetailSubSection title="Summary">
            <FactGrid cols={2} className="text-xs">
              <Fact label="Courses" valueClassName="mt-1 text-lg font-extrabold leading-tight text-slate-800">
                {profile.kpi.totalCourses}
              </Fact>
              <Fact label="Completed" valueClassName="mt-1 text-lg font-extrabold leading-tight text-emerald-600">
                {profile.kpi.completedCourses}
              </Fact>
              <Fact label="In Progress" valueClassName="mt-1 text-lg font-extrabold leading-tight text-amber-600">
                {profile.kpi.inProgressCourses}
              </Fact>
              <Fact label="Hours" valueClassName="mt-1 text-lg font-bold leading-tight text-slate-800">
                {formatTimeSpent(profile.kpi.totalTimeSpentSeconds)}
              </Fact>
            </FactGrid>
          </DetailSubSection>
        </DetailCard>
      </div>
    </>
  )
}
