import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { BookOpen, Download, FileBarChart, Printer, Search, X } from 'lucide-react'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { DetailCard, DetailLayout, DetailSubSection, Fact, FactGrid } from '../../components/ui/detail'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
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
  const { setLabel } = useBreadcrumbs()
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

  useEffect(() => {
    if (data?.assignmentNo) {
      setLabel(String(id), data.assignmentNo)
    }
  }, [data, id, setLabel])

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
      <DetailLayout
        sidebar={
          <ControlsSidebar>
            <ControlAction icon={Printer} onClick={() => window.print()}>
              Print Report
            </ControlAction>
            <ControlAction icon={Download} onClick={exportCsv}>
              Export CSV
            </ControlAction>
          </ControlsSidebar>
        }
      >
        <div className="space-y-6">
          {/* Summary stats */}
          <DetailCard>
            <SectionHeader icon={FileBarChart}>Report Summary</SectionHeader>
            <FactGrid cols={3} className="pt-2">
              <Fact label="Assignment No." colSpan="full" valueClassName="font-mono font-bold text-slate-900 text-lg">
                {data.assignmentNo || `Assignment ${id}`}
              </Fact>
              <Fact label="Total Learners" valueClassName="font-bold text-slate-800 text-lg">
                {data.totalEmployees}
              </Fact>
              <Fact label="Completed" valueClassName="font-bold text-slate-800 text-lg">
                {data.chartData.completed}
              </Fact>
              <Fact label="Completion" valueClassName="font-bold text-slate-800 text-lg">
                {Math.round(data.completionRate)}%
              </Fact>
              <Fact label="Start Date" valueClassName="font-semibold text-slate-700">
                {data.startDate ? formatDate(data.startDate) : '—'}
              </Fact>
              <Fact label="Due Date" valueClassName="font-semibold text-slate-700">
                {data.dueDate ? formatDate(data.dueDate) : '—'}
              </Fact>
              <Fact label="Courses" valueClassName="font-bold text-slate-800 text-lg">
                {data.totalCourses}
              </Fact>
            </FactGrid>

            {/* Courses summary */}
            {data.courses.length > 0 && (
              <DetailSubSection title="Courses">
                <div className="flex flex-wrap gap-2 pt-1">
                  {data.courses.map((c) => (
                    <div
                      key={c.assignmentRuleId}
                      className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-1.5 text-xs shadow-3xs"
                    >
                      <BookOpen className="h-3.5 w-3.5 text-indigo-500 shrink-0" />
                      <div className="flex flex-col text-left">
                        <span className={`font-semibold ${c.isCourseDeleted ? 'text-slate-400 line-through' : 'text-slate-700'}`}>
                          {c.courseTitle}
                          {c.isCourseDeleted && <span className="ml-1 text-[10px] font-medium no-underline text-slate-400">(deleted)</span>}
                        </span>
                        <span className="font-mono text-[10px] text-slate-400 leading-none mt-0.5">
                          {c.courseCode} · <span className="font-sans font-medium text-indigo-600">{c.completedLearners}/{c.totalLearners} Completed</span>
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </DetailSubSection>
            )}
          </DetailCard>

          {/* Learner table */}
          <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            {/* Filter & Search bar */}
            <div className="p-5 border-b border-slate-100 bg-slate-50/20">
              <div className="flex flex-wrap items-center justify-between gap-4">
                <div className="flex flex-wrap items-center gap-1.5">
                  {(['All', ...STATUS_BUCKETS] as const).map((s) => (
                    <button
                      key={s}
                      type="button"
                      onClick={() => setStatusFilter(s)}
                      className={`rounded-lg border px-3 py-1.5 text-xs font-semibold transition-colors shrink-0 cursor-pointer ${
                        statusFilter === s
                          ? 'border-indigo-500 bg-indigo-600 text-white shadow-3xs font-bold'
                          : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300 hover:bg-slate-50'
                      }`}
                    >
                      {s}
                      <span className={`ml-1.5 text-[10px] ${statusFilter === s ? 'text-indigo-100' : 'text-slate-400'}`}>
                        {counts[s] ?? 0}
                      </span>
                    </button>
                  ))}
                </div>

                <div className="relative w-full sm:w-72 shrink-0">
                  <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
                  <input
                    type="text"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder="Search code, name, course..."
                    className="w-full rounded-lg border border-slate-200 bg-white py-2 pl-9 pr-8 text-xs font-semibold text-slate-700 shadow-3xs transition focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-100"
                  />
                  {search && (
                    <button
                      type="button"
                      onClick={() => setSearch('')}
                      className="absolute right-2.5 top-2.5 rounded-full p-0.5 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
                    >
                      <X className="h-3.5 w-3.5" />
                    </button>
                  )}
                </div>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                    <th className="p-3 pl-5">Learner</th>
                    <th className="p-3">Course</th>
                    <th className="p-3">Status</th>
                    <th className="p-3">Progress</th>
                    <th className="p-3 pr-5">Completed</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {filtered.map((row, index) => (
                    <tr
                      key={`${row.learnerCode}-${row.assignmentRuleId ?? index}`}
                      className="hover:bg-slate-50/60 transition"
                    >
                      <td className="p-3 pl-5">
                        <div className="flex flex-col">
                          <span className="font-bold text-slate-800 leading-tight">{row.learnerName || row.learnerCode}</span>
                          <span className="text-xxs font-mono text-slate-400 mt-0.5">{row.learnerCode}</span>
                        </div>
                      </td>
                      <td className="p-3 text-xxs text-slate-500">
                        {row.courseTitle ? (
                          <div className="flex flex-col">
                            <span className="font-semibold text-slate-600">{row.courseTitle}</span>
                            <span className="font-mono text-slate-400 mt-0.5">{row.courseCode}</span>
                          </div>
                        ) : (
                          '—'
                        )}
                      </td>
                      <td className="p-3">
                        <StatusBadge size="xxs">{row.status}</StatusBadge>
                      </td>
                      <td className="p-3">
                        <ProgressBar value={row.progress} completed={row.isCompleted} maxWidthClass="max-w-24" />
                      </td>
                      <td className="p-3 pr-5 text-slate-600 text-xs">
                        {row.completedDate ? formatDate(row.completedDate) : '—'}
                      </td>
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr>
                      <td className="p-6 text-center text-slate-400" colSpan={5}>
                        No learners found.
                  </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      </DetailLayout>
    </div>
  )
}
