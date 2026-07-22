import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { ChevronDown, Download, Printer, Users } from 'lucide-react'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { DetailCard, DetailLayout, DetailSubSection } from '../../components/ui/detail'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { Card } from '../../components/ui/Card'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { AppButton } from '../../components/ui/AppButton'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { exportRowsAsCsv } from '../../lib/csvExport'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { toast } from '../../lib/toast'
import { formatDate, formatPercent } from '../../lib/format'
import { LEARNER_STATUS_KEYS, learnerStatusLabel } from '../../lib/learnerStatus'
import { DETAIL_TABLE_CHUNK_SIZE } from '../../lib/tableStandards'
import { StatusDonut, CourseCompletionBars, buildStatusData, buildCourseBarData } from './AssignmentReportCharts'

// Mirrors LearnerProgressDto (iLearn.Application/DTOs/AssignmentDashboardDto.cs)
type LearnerRow = {
  learnerCode: string
  learnerName?: string | null
  division?: string | null
  department?: string | null
  learnerGroups?: string[] | null
  assignmentRuleId?: number | null
  courseCode?: string | null
  courseTitle?: string | null
  progress: number
  isCompleted: boolean
  // AssignmentStatusKeys.Learner: Completed | InProgress | NotStarted | Overdue | Upcoming
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

type GroupSummary = {
  groupName: string
  learnerCount: number
  enrollments: number
  completed: number
  overdue: number
  completionRate: number
}

const STATUS_FILTERS = ['All', ...LEARNER_STATUS_KEYS] as const

export function AssignmentReportPage() {
  const { id } = useParams()
  const { setLabel } = useBreadcrumbs()
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<AssignmentDashboard | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('All')
  const [courseFilter, setCourseFilter] = useState<'All' | number>('All')
  const [groupFilter, setGroupFilter] = useState<string>('All')
  const [search, setSearch] = useState('')
  const [visibleRows, setVisibleRows] = useState(DETAIL_TABLE_CHUNK_SIZE)

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

  useEffect(() => {
    setVisibleRows(DETAIL_TABLE_CHUNK_SIZE)
  }, [statusFilter, courseFilter, groupFilter, search])

  const groupOptions = useMemo(() => {
    if (!data) return []
    const names = new Set<string>()
    data.learners.forEach((row) => {
      row.learnerGroups?.forEach((g) => {
        if (g.trim()) names.add(g.trim())
      })
    })
    return Array.from(names).sort()
  }, [data])

  const filtered = useMemo(() => {
    if (!data) return []
    const q = search.trim().toLowerCase()
    return data.learners.filter((row) => {
      if (statusFilter !== 'All' && row.status !== statusFilter) return false
      if (courseFilter !== 'All' && row.assignmentRuleId !== courseFilter) return false
      if (groupFilter !== 'All') {
        if (groupFilter === 'Ungrouped') {
          if (row.learnerGroups && row.learnerGroups.length > 0) return false
        } else {
          if (!row.learnerGroups || !row.learnerGroups.includes(groupFilter)) return false
        }
      }
      if (!q) return true
      const matchGroups = row.learnerGroups?.some((g) => g.toLowerCase().includes(q)) ?? false
      return (
        row.learnerCode.toLowerCase().includes(q) ||
        (row.learnerName ?? '').toLowerCase().includes(q) ||
        (row.division ?? '').toLowerCase().includes(q) ||
        (row.department ?? '').toLowerCase().includes(q) ||
        (row.courseCode ?? '').toLowerCase().includes(q) ||
        (row.courseTitle ?? '').toLowerCase().includes(q) ||
        matchGroups
      )
    })
  }, [data, statusFilter, courseFilter, groupFilter, search])

  const overdueLearnerCount = useMemo(() => {
    if (!data) return 0
    return new Set(
      data.learners.filter((row) => row.status === 'Overdue').map((row) => row.learnerCode),
    ).size
  }, [data])

  const statusChartData = useMemo(() => {
    if (!data) return []
    return buildStatusData(data.learners)
  }, [data])

  const courseBarData = useMemo(() => {
    if (!data) return []
    return buildCourseBarData(data.courses)
  }, [data])

  const groupSummaries = useMemo<GroupSummary[]>(() => {
    if (!data) return []
    const groups = new Map<string, LearnerRow[]>()
    data.learners.forEach((row) => {
      const learnerGroups = row.learnerGroups
      if (!learnerGroups || learnerGroups.length === 0) {
        const key = 'Ungrouped'
        const list = groups.get(key)
        if (list) {
          list.push(row)
        } else {
          groups.set(key, [row])
        }
      } else {
        learnerGroups.forEach((gName) => {
          const key = gName.trim() || 'Ungrouped'
          const list = groups.get(key)
          if (list) {
            list.push(row)
          } else {
            groups.set(key, [row])
          }
        })
      }
    })

    return Array.from(groups.entries())
      .map(([groupName, rows]) => {
        const completed = rows.filter((row) => row.isCompleted).length
        return {
          groupName,
          learnerCount: new Set(rows.map((row) => row.learnerCode)).size,
          enrollments: rows.length,
          completed,
          overdue: rows.filter((row) => row.status === 'Overdue').length,
          completionRate: rows.length === 0 ? 0 : (completed / rows.length) * 100,
        }
      })
      .sort((a, b) => {
        if (a.groupName === 'Ungrouped') return 1
        if (b.groupName === 'Ungrouped') return -1
        return a.groupName.localeCompare(b.groupName)
      })
  }, [data])

  const exportCsv = (rows: LearnerRow[], scope: 'all' | 'filtered') => {
    if (!data || rows.length === 0) {
      toast.info('No rows to export')
      return
    }
    const header = [
      'Learner Code',
      'Name',
      'Division',
      'Department',
      'Learner Groups',
      'Course Code',
      'Course Title',
      'Status',
      'Progress %',
      'Start Date',
      'Due Date',
      'Completed Date',
    ]
    const body = rows.map((l) => [
      l.learnerCode,
      l.learnerName ?? l.learnerCode,
      l.division ?? '',
      l.department ?? '',
      l.learnerGroups ? l.learnerGroups.join('; ') : '',
      l.courseCode ?? '',
      l.courseTitle ?? '',
      learnerStatusLabel(l.status),
      formatPercent(l.progress).replace('%', ''),
      l.startDate ? formatDate(l.startDate) : '',
      l.dueDate ? formatDate(l.dueDate) : '',
      l.completedDate ? formatDate(l.completedDate) : '',
    ])
    const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    const filename = `assignment-${data.assignmentNo || id}-report-${scope}-${stamp}.csv`
    exportRowsAsCsv(filename, header, body)
  }

  const handlePrint = () => {
    // Reveal every row first so the printout is not cut off at the current chunk
    setVisibleRows(filtered.length)
    setTimeout(() => window.print(), 150)
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

  const visible = filtered.slice(0, visibleRows)
  const isFiltered = statusFilter !== 'All' || courseFilter !== 'All' || groupFilter !== 'All' || search.trim() !== ''

  return (
    <div className="space-y-6">
      <DetailLayout
        sidebar={
          <ControlsSidebar className="print:hidden">
            <ControlAction icon={Printer} onClick={handlePrint}>
              Print Report
            </ControlAction>
            <ControlAction icon={Download} onClick={() => exportCsv(data.learners, 'all')}>
              Export CSV (All)
            </ControlAction>
            <ControlAction
              icon={Download}
              onClick={() => exportCsv(filtered, 'filtered')}
              disabled={!isFiltered}
              title={isFiltered ? undefined : 'Apply a filter or search to export a subset'}
            >
              Export CSV (Filtered)
            </ControlAction>
          </ControlsSidebar>
        }
      >
        <div className="space-y-6">
          {/* Summary stats */}
          <DetailCard>
            <SectionHeader>Report Summary</SectionHeader>

            {/* Header row: Assignment No + date range */}
            <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1 pt-2 px-1">
              <span className="font-mono font-bold text-slate-900 text-lg">{data.assignmentNo || `Assignment ${id}`}</span>
              {(data.startDate || data.dueDate) && (
                <span className="text-xs text-slate-500">
                  {data.startDate ? formatDate(data.startDate) : '—'} → {data.dueDate ? formatDate(data.dueDate) : '—'}
                </span>
              )}
            </div>

            {/* Stat tiles */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-3 px-1">
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center">
                <div className="text-[10px] font-extrabold text-slate-400 uppercase">Learners</div>
                <div className="text-lg font-bold text-slate-800 tabular-nums mt-0.5">{data.totalEmployees}</div>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center">
                <div className="text-[10px] font-extrabold text-slate-400 uppercase">Completed</div>
                <div className="text-lg font-bold text-slate-800 tabular-nums mt-0.5">{data.chartData.completed}</div>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center">
                <div className="text-[10px] font-extrabold text-slate-400 uppercase">Overdue</div>
                <div className={`text-lg font-bold tabular-nums mt-0.5 ${overdueLearnerCount > 0 ? 'text-red-600' : 'text-slate-800'}`}>{overdueLearnerCount}</div>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center">
                <div className="text-[10px] font-extrabold text-slate-400 uppercase">Courses</div>
                <div className="text-lg font-bold text-slate-800 tabular-nums mt-0.5">{data.totalCourses}</div>
              </div>
            </div>

            {/* Print-only fallback: full stats without charts */}
            <div className="hidden print:block pt-3 px-1">
              <div className="grid grid-cols-3 gap-2 text-xs">
                <div><span className="font-bold text-slate-500">Not Started:</span> {data.chartData.notStarted}</div>
                <div><span className="font-bold text-slate-500">In Progress:</span> {data.chartData.inProgress}</div>
                <div><span className="font-bold text-slate-500">Completion:</span> {formatPercent(data.completionRate)}</div>
              </div>
            </div>

            {/* Charts: donut + bars */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 pt-4 print:hidden">
              <div>
                <DetailSubSection title="Status Overview">
                  <StatusDonut
                    data={statusChartData}
                    completionRate={data.completionRate}
                    activeStatus={statusFilter}
                  />
                </DetailSubSection>
              </div>
              {data.courses.length > 0 && (
                <div>
                  <DetailSubSection title="Completion by Course">
                    <CourseCompletionBars
                      data={courseBarData}
                      activeCourse={courseFilter}
                    />
                  </DetailSubSection>
                </div>
              )}
            </div>
          </DetailCard>

          {/* Learner Group breakdown */}
          {groupSummaries.length > 0 && (
            <Card icon={Users} title="By Learner Group">
              <div className="overflow-x-auto custom-scrollbar">
                <table className="w-full text-left text-sm border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                      <th className="p-3 pl-5">Learner Group</th>
                      <th className="p-3 text-center">Learners</th>
                      <th className="p-3 text-center">Enrollments</th>
                      <th className="p-3 text-center">Completed</th>
                      <th className="p-3 text-center">Overdue</th>
                      <th className="p-3 pr-5">Completion</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-slate-700">
                    {groupSummaries.map((row) => (
                      <tr key={row.groupName} className="hover:bg-slate-50/50 transition duration-100">
                        <td className="p-3 pl-5 font-semibold text-slate-800 text-xs">{row.groupName}</td>
                        <td className="p-3 text-center text-xs">{row.learnerCount}</td>
                        <td className="p-3 text-center text-xs">{row.enrollments}</td>
                        <td className="p-3 text-center text-xs">{row.completed}</td>
                        <td className="p-3 text-center text-xs">
                          <span className={row.overdue > 0 ? 'font-bold text-red-600' : 'text-slate-400'}>
                            {row.overdue}
                          </span>
                        </td>
                        <td className="p-3 pr-5">
                          <div className="flex items-center gap-3">
                            <ProgressBar value={row.completionRate} completed={row.completionRate >= 100} maxWidthClass="max-w-28" />
                            <span className="text-xxs font-bold text-slate-500">{formatPercent(row.completionRate)}</span>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Card>
          )}

          {/* Learner table */}
          <Card>
            {/* Filter & Search bar */}
            <div className="border-b border-slate-100 bg-slate-50/20 px-5 print:hidden">
              <ListToolbar
                searchValue={search}
                onSearchChange={setSearch}
                searchPlaceholder="Search code, name, group, department, course..."
                toolbarContent={
                  <div className="flex flex-wrap items-center gap-1.5">
                    <SegmentedToggle
                      variant="filter"
                      options={STATUS_FILTERS.map(s => ({
                        value: s,
                        label: s === 'All' ? 'All' : learnerStatusLabel(s),
                      }))}
                      value={statusFilter}
                      onChange={setStatusFilter}
                    />

                    {data.courses.length > 1 && (
                      <div className="relative shrink-0">
                        <select
                          value={courseFilter === 'All' ? 'All' : String(courseFilter)}
                          onChange={(e) =>
                            setCourseFilter(e.target.value === 'All' ? 'All' : Number(e.target.value))
                          }
                          className="appearance-none rounded-lg border border-slate-200 bg-white pl-3 pr-8 py-1.5 text-xs font-semibold text-slate-600 hover:border-slate-300 focus:outline-none focus:border-indigo-500 cursor-pointer"
                        >
                          <option value="All">All Courses</option>
                          {data.courses.map((c) => (
                            <option key={c.assignmentRuleId} value={c.assignmentRuleId}>
                              {c.courseCode} — {c.courseTitle}
                            </option>
                          ))}
                        </select>
                        <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400 pointer-events-none" />
                      </div>
                    )}

                    <div className="relative shrink-0">
                      <select
                        value={groupFilter}
                        onChange={(e) => setGroupFilter(e.target.value)}
                        className="appearance-none rounded-lg border border-slate-200 bg-white pl-3 pr-8 py-1.5 text-xs font-semibold text-slate-600 hover:border-slate-300 focus:outline-none focus:border-indigo-500 cursor-pointer"
                      >
                        <option value="All">All Groups</option>
                        {groupOptions.map((g) => (
                          <option key={g} value={g}>
                            {g}
                          </option>
                        ))}
                        <option value="Ungrouped">Ungrouped</option>
                      </select>
                      <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400 pointer-events-none" />
                    </div>
                  </div>
                }
              />
            </div>

            <div className="overflow-x-auto custom-scrollbar">
              <table className="w-full text-left text-sm border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                    <th className="p-3 pl-5">Learner</th>
                    <th className="p-3">Course Code & Title</th>
                    <th className="p-3">Status</th>
                    <th className="p-3">Progress</th>
                    <th className="p-3">Timeline</th>
                    <th className="p-3 pr-5">Completed Date</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {visible.map((row) => (
                    <tr
                      key={`${row.learnerCode}-${row.assignmentRuleId ?? row.courseCode ?? ''}`}
                      className="hover:bg-slate-50/50 transition duration-100"
                    >
                      <td className="p-3 pl-5">
                        <div className="font-bold text-slate-800 text-xs sm:text-[13px]">{row.learnerName || '—'}</div>
                        <div className="text-xxs font-mono text-slate-400 mt-0.5">{row.learnerCode}</div>
                        {(row.division || row.department) && (
                          <div className="text-xxs text-slate-400 mt-0.5">
                            {[row.division, row.department].filter(Boolean).join(' · ')}
                          </div>
                        )}
                      </td>
                      <td className="p-3 select-all">
                        <div className="font-bold text-slate-700 text-xs">{row.courseTitle || '—'}</div>
                        <div className="text-xxs font-mono text-slate-400 mt-0.5">{row.courseCode}</div>
                      </td>
                      <td className="p-3">
                        <StatusBadge size="xxs">{learnerStatusLabel(row.status)}</StatusBadge>
                      </td>
                      <td className="p-3">
                        <ProgressBar value={row.progress} completed={row.isCompleted} />
                      </td>
                      <td className="p-3 text-slate-400 text-xxs leading-relaxed">
                        {row.startDate && <div>Start: {formatDate(row.startDate)}</div>}
                        {row.dueDate && <div className="mt-0.5">Due: {formatDate(row.dueDate)}</div>}
                      </td>
                      <td className="p-3 pr-5 text-slate-600 text-xs">
                        {row.completedDate ? formatDate(row.completedDate) : '—'}
                      </td>
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr>
                      <td className="p-6 text-center text-slate-400" colSpan={6}>
                        No learners found.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {filtered.length > 0 && (
              <div className="flex items-center justify-between gap-2 border-t border-slate-100 bg-slate-50/40 px-3 py-2 print:hidden">
                <span className="text-xxs font-semibold uppercase tracking-wide text-slate-500">
                  Showing {visible.length} of {filtered.length}
                </span>
                {filtered.length > visible.length && (
                  <AppButton
                    variant="ghost"
                    onClick={() => setVisibleRows((prev) => prev + DETAIL_TABLE_CHUNK_SIZE)}
                    className="px-3 py-1 text-xxs font-bold"
                  >
                    Load more
                  </AppButton>
                )}
              </div>
            )}
          </Card>
        </div>
      </DetailLayout>
    </div>
  )
}
