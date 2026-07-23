import { useState, useEffect, useCallback } from 'react'
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
import { Card } from '../../components/ui/Card'
import {
  DetailCard,
  DetailSubSection,
  Fact,
  FactGrid,
} from '../../components/ui/detail'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { formatDate } from '../../lib/format'
import { COMMON_LABELS, LEARNER_LABELS, REPORT_LABELS, t, tf } from '../../lib/labels'

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

  const loadProfile = useCallback(async () => {
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<LearnerProfileResponse>(`Learners/profile/${id}`)
      if (resp.success && resp.data) {
        setProfile(resp.data)
      }
    } catch (err) {
      console.error(err)
      toast.error(t(LEARNER_LABELS.failedToLoadProfile))
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void loadProfile()
  }, [loadProfile])

  if (loading) {
    return <LoadingState />
  }

  if (!profile) {
    return (
      <NotFoundState
        title={t(LEARNER_LABELS.learnerProfileMissing)}
        message={t(LEARNER_LABELS.learnerIdentityUnverified)}
        backTo="/learners"
        backLabel={t(LEARNER_LABELS.backToDirectory)}
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
          <Card icon={FileBadge} title={t(LEARNER_LABELS.transcript)} bodyClassName="overflow-x-auto custom-scrollbar">
            <table className="w-full text-left text-sm border-collapse">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                  <th className="p-3">{t(LEARNER_LABELS.courseIdentity)}</th>
                  <th className="p-3">{t(REPORT_LABELS.colProgress)}</th>
                  <th className="p-3">{t(LEARNER_LABELS.gradeScore)}</th>
                  <th className="p-3">{t(LEARNER_LABELS.timeSpent)}</th>
                  <th className="p-3">{t(LEARNER_LABELS.timeline)}</th>
                  <th className="p-3">{t(LEARNER_LABELS.status)}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-700">
                {profile.enrollments.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="p-8 text-center text-slate-400">
                      {t(LEARNER_LABELS.noEnrollments)}
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
                              {e.courseCode} {e.isCourseDeleted && `(${t(LEARNER_LABELS.syllabusDeleted)})`}
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
                            <div className="text-emerald-600 font-semibold">{tf(LEARNER_LABELS.doneDate, formatDate(e.completedDate))}</div>
                          ) : (
                            <>
                              {e.startDate && <div>{tf(LEARNER_LABELS.startDate, formatDate(e.startDate))}</div>}
                              {e.dueDate && <div className="mt-0.5">{tf(LEARNER_LABELS.dueDate, formatDate(e.dueDate))}</div>}
                            </>
                          )}
                        </td>
                        <td className="p-3">
                          {isFinished ? (
                            <StatusBadge tone="success" size="xxs">{t(COMMON_LABELS.passed)}</StatusBadge>
                          ) : isCancelled ? (
                            <StatusBadge tone="warning" size="xxs">
                              <span className="inline-flex items-center gap-1" title="Rule Deleted">
                                <AlertTriangle className="h-3 w-3" />
                                {t(COMMON_LABELS.cancelled)}
                              </span>
                            </StatusBadge>
                          ) : e.hasActiveAssignment ? (
                            <StatusBadge tone="info" size="xxs">{t(COMMON_LABELS.assigned)}</StatusBadge>
                          ) : (
                            <StatusBadge tone="neutral" size="xxs">{t(COMMON_LABELS.selfEnroll)}</StatusBadge>
                          )}
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </table>
          </Card>
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
            <Fact label={t(LEARNER_LABELS.division)} valueClassName="text-slate-800 font-semibold mt-0.5">
              {profile.division || '—'}
            </Fact>
            <Fact label={t(LEARNER_LABELS.department)} valueClassName="text-slate-800 font-semibold mt-0.5">
              {profile.department || '—'}
            </Fact>
            {profile.section && (
              <Fact label={t(LEARNER_LABELS.section)} valueClassName="text-slate-800 font-semibold mt-0.5">
                {profile.section}
              </Fact>
            )}
            <Fact label={t(LEARNER_LABELS.position)} valueClassName="text-slate-800 font-semibold mt-0.5">
              {profile.position || '—'}
            </Fact>
          </FactGrid>

          <DetailSubSection title={t(LEARNER_LABELS.summary)}>
            <FactGrid cols={2} className="text-xs">
              <Fact label={t(LEARNER_LABELS.courses)} valueClassName="mt-1 text-lg font-extrabold leading-tight text-slate-800">
                {profile.kpi.totalCourses}
              </Fact>
              <Fact label={t(LEARNER_LABELS.completed)} valueClassName="mt-1 text-lg font-extrabold leading-tight text-emerald-600">
                {profile.kpi.completedCourses}
              </Fact>
              <Fact label={t(LEARNER_LABELS.inProgress)} valueClassName="mt-1 text-lg font-extrabold leading-tight text-amber-600">
                {profile.kpi.inProgressCourses}
              </Fact>
              <Fact label={t(LEARNER_LABELS.hours)} valueClassName="mt-1 text-lg font-bold leading-tight text-slate-800">
                {formatTimeSpent(profile.kpi.totalTimeSpentSeconds)}
              </Fact>
            </FactGrid>
          </DetailSubSection>
        </DetailCard>
      </div>
    </>
  )
}
