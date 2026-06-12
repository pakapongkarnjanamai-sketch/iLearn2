import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Download, Printer } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { StatusText } from '../../components/ui/StatusText'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { formatDate } from '../../lib/format'

// Mirrors LearnerProgressDto (iLearn.Application/DTOs/AssignmentDashboardDto.cs)
type LearnerRow = {
  learnerCode: string
  learnerName?: string | null
  assignmentRuleId?: number | null
  courseCode?: string | null
  courseTitle?: string | null
  progress: number
  isCompleted: boolean
  status: string
  completedDate?: string | null
  startDate?: string | null
  dueDate?: string | null
}

// Mirrors CourseSummaryDto (iLearn.Application/DTOs/AssignmentDashboardDto.cs)
type CourseRow = {
  assignmentRuleId: number
  courseCode: string
  courseTitle: string
  completedLearners: number
  totalLearners: number
  isCourseDeleted: boolean
}

// Mirrors AssignmentDashboardDto returned by GET Assignments/dashboard/{id}
type AssignmentDashboard = {
  assignmentNo: string
  description: string
  startDate: string | null
  dueDate: string | null
  totalEmployees: number
  totalCourses: number
  completionRate: number
  chartData: { completed: number; inProgress: number; notStarted: number }
  courses: CourseRow[]
  learners: LearnerRow[]
}

const STATUS_BUCKETS = ['Completed', 'In Progress', 'Not Started', 'Overdue'] as const

export function AssignmentReportPage() {
  const { id } = useParams()
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<AssignmentDashboard | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('All')
  const [search, setSearch] = useState('')

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    fetchWithAccessControl<{ success: boolean; data: AssignmentDashboard }>(
      `Assignments/dashboard/${id}`,
    )
      .then((resp) => {
        if (cancelled) return
        if (resp.success) setData(resp.data)
      })
      .catch(() => toast.error('Failed to load assignment report'))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [id])

  const filtered = useMemo(() => {
    if (!data) return []
    const q = search.trim().toLowerCase()
    return data.learners.filter((row) => {
      if (statusFilter !== 'All' && row.status !== statusFilter) return false
      if (!q) return true
      return (
        row.learnerCode.toLowerCase().includes(q) ||
        (row.learnerName ?? '').toLowerCase().includes(q) ||
        (row.courseCode ?? '').toLowerCase().includes(q) ||
        (row.courseTitle ?? '').toLowerCase().includes(q)
      )
    })
  }, [data, statusFilter, search])

  const counts = useMemo(() => {
    const map: Record<string, number> = { All: data?.learners.length ?? 0 }
    STATUS_BUCKETS.forEach((s) => {
      map[s] = data?.learners.filter((l) => l.status === s).length ?? 0
    })
    return map
  }, [data])

  const exportCsv = () => {
    if (!data) return
    const header = ['Learner Code', 'Name', 'Course Code', 'Course Title', 'Status', 'Progress %', 'Completed Date']
    const rows = filtered.map((l) => [
      l.learnerCode,
      l.learnerName ?? l.learnerCode,
      l.courseCode ?? '',
      l.courseTitle ?? '',
      l.status,
      String(Math.round(l.progress)),
      l.completedDate ? formatDate(l.completedDate) : '',
    ])
    const csv = [header, ...rows]
      .map((r) => r.map((v) => `"${String(v).replace(/"/g, '""')}"`).join(','))
      .join('\n')
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `assignment-${data.assignmentNo || id}-report.csv`
    a.click()
    URL.revokeObjectURL(url)
  }

  if (loading) {
    return <LoadingState label="Loading assignment report..." />
  }

  if (!data) {
    return (
      <NotFoundState
        title="Assignment Not Found"
        message="The requested assignment report could not be loaded."
        backTo="/assignments"
        backLabel="Back to Assignments"
      />
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-wrap items-end justify-between gap-3 border-b border-slate-200 pb-4">
        <div className="min-w-0">
          <div className="text-xxs font-extrabold uppercase tracking-wider text-slate-400">Assignment Report</div>
          <h1 className="text-xl font-extrabold text-slate-900 leading-tight">{data.assignmentNo || `Assignment ${id}`}</h1>
          {data.description && <p className="mt-1 text-xs text-slate-500">{data.description}</p>}
        </div>
        <div className="flex items-center gap-2">
          <AppButton variant="secondary" icon={Printer} onClick={() => window.print()}>
            Print
          </AppButton>
          <AppButton variant="primary" icon={Download} onClick={exportCsv}>
            Export CSV
          </AppButton>
        </div>
      </div>

      {/* Summary stats */}
      <section className="rounded-lg border border-slate-200 bg-white p-5 space-y-5 shadow-xs">
      <dl className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-x-6 gap-y-5 text-xs">
        {[
          { label: 'Total Learners', value: data.totalEmployees },
          { label: 'Completed', value: data.chartData.completed },
          { label: 'Completion', value: `${Math.round(data.completionRate)}%` },
          { label: 'Start', value: data.startDate ? formatDate(data.startDate) : '—' },
          { label: 'Due', value: data.dueDate ? formatDate(data.dueDate) : '—' },
          { label: 'Courses', value: data.totalCourses },
        ].map((kpi) => (
          <div key={kpi.label} className="min-w-0">
            <dt className="text-slate-400 font-bold uppercase tracking-wider">{kpi.label}</dt>
            <dd className="mt-1 font-bold text-slate-800">{kpi.value}</dd>
          </div>
        ))}
      </dl>

      {/* Courses summary */}
      {data.courses.length > 0 && (
        <div className="border-t border-slate-100 pt-5">
          <div className="mb-1.5 text-xxs font-extrabold uppercase tracking-wider text-slate-400">Courses</div>
          <div className="flex flex-wrap gap-1.5">
            {data.courses.map((c) => (
              <span
                key={c.assignmentRuleId}
                className="inline-flex items-center gap-1.5 rounded border border-slate-200 bg-white px-2 py-0.5 text-xs"
              >
                <span className="font-mono text-slate-500">{c.courseCode}</span>
                <span className="font-semibold text-slate-700">{c.courseTitle}</span>
                <span className="text-slate-400">({c.completedLearners}/{c.totalLearners})</span>
              </span>
            ))}
          </div>
        </div>
      )}
      </section>

      {/* Learner table */}
      <section className="rounded-lg border border-slate-200 bg-white p-5 space-y-3 shadow-xs">
        {/* Filter bar */}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2">
            {(['All', ...STATUS_BUCKETS] as const).map((s) => (
              <button
                key={s}
                type="button"
                onClick={() => setStatusFilter(s)}
                className={`rounded border px-2.5 py-1 text-xs font-semibold transition-colors ${
                  statusFilter === s
                    ? 'border-indigo-600 bg-indigo-50 text-indigo-700'
                    : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300'
                }`}
              >
                {s} <span className="ml-1 text-slate-400">{counts[s] ?? 0}</span>
              </button>
            ))}
          </div>
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search..."
            className="w-72 rounded border border-slate-200 px-2 py-1 text-sm focus:outline-none focus:border-indigo-500"
          />
        </div>

        <div className="overflow-x-auto">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="text-left text-xxs font-extrabold uppercase text-slate-500">
              <th className="border-b border-slate-200/60 py-2 pr-3">Learner</th>
              <th className="border-b border-slate-200/60 py-2 pr-3">Course</th>
              <th className="border-b border-slate-200/60 py-2 pr-3">Status</th>
              <th className="border-b border-slate-200/60 py-2 pr-3 text-right">Progress</th>
              <th className="border-b border-slate-200/60 py-2 pr-3">Completed</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((row, index) => (
              <tr key={`${row.learnerCode}-${row.assignmentRuleId ?? index}`} className="border-b border-slate-100/60 hover:bg-slate-50/60">
                <td className="py-1.5 pr-3">
                  <div className="font-semibold text-slate-800">{row.learnerName ?? row.learnerCode}</div>
                  <div className="font-mono text-xs text-slate-500">{row.learnerCode}</div>
                </td>
                <td className="py-1.5 pr-3 text-slate-600">
                  {row.courseTitle ? (
                    <>
                      <div className="font-semibold text-slate-700">{row.courseTitle}</div>
                      <div className="font-mono text-xs text-slate-400">{row.courseCode}</div>
                    </>
                  ) : '—'}
                </td>
                <td className="py-1.5 pr-3">
                  <StatusText
                    tone={
                      row.status === 'Completed'
                        ? 'success'
                        : row.status === 'Overdue'
                        ? 'danger'
                        : row.status === 'In Progress'
                        ? 'warning'
                        : 'neutral'
                    }
                  >
                    {row.status}
                  </StatusText>
                </td>
                <td className="py-1.5 pr-3 text-right font-mono">{Math.round(row.progress)}%</td>
                <td className="py-1.5 pr-3 text-slate-600">
                  {row.completedDate ? formatDate(row.completedDate) : '—'}
                </td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr>
                <td className="py-6 text-center text-slate-400" colSpan={5}>
                  No learners.
                </td>
              </tr>
            )}
          </tbody>
        </table>
        </div>
      </section>
    </div>
  )
}
