import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { ChevronDown, FileSpreadsheet, Printer, Users } from 'lucide-react'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { DetailCard, DetailLayout, DetailSubSection } from '../../components/ui/detail'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { Card } from '../../components/ui/Card'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { toast } from '../../lib/toast'
import { formatDate, formatDateTime } from '../../lib/format'
import { ASSIGNMENT_LABELS, COMMON_LABELS, LEARNER_STATUS_KEYS, REPORT_LABELS, learnerStatusLabel, t } from '../../lib/labels'
import { exportWorkbook } from '../../lib/tableExport'
import { DETAIL_TABLE_CHUNK_SIZE, shouldLoadMoreOnScroll } from '../../lib/tableStandards'
import { StatusDonut, buildStatusData } from './AssignmentReportCharts'

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
  overdue: number
}

type AssignmentReportExportKey = 'admin-workbook'

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
  const [exportingKey, setExportingKey] = useState<AssignmentReportExportKey | null>(null)

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
      .catch(() => toast.error(t(ASSIGNMENT_LABELS.failedToLoadReport)))
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
        return {
          groupName,
          learnerCount: new Set(rows.map((row) => row.learnerCode)).size,
          enrollments: rows.length,
          overdue: rows.filter((row) => row.status === 'Overdue').length,
        }
      })
      .sort((a, b) => {
        if (a.groupName === 'Ungrouped') return 1
        if (b.groupName === 'Ungrouped') return -1
        return a.groupName.localeCompare(b.groupName)
      })
  }, [data])

  const exportAdminWorkbook = async () => {
    if (!data || data.learners.length === 0) {
      toast.info(t(ASSIGNMENT_LABELS.noRowsToExport))
      return
    }

    setExportingKey('admin-workbook')
    try {
      const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '')
      const incompleteRows = data.learners.filter((row) => !row.isCompleted)
      const exceptionRows = data.learners.filter((row) => row.status === 'Overdue' || !row.isCompleted)

      await exportWorkbook(`assignment-${data.assignmentNo || id}-admin-workbook-${stamp}`, [
        {
          sheet: 'Overview',
          header: ['Metric', 'Value'],
          rows: [
            ['Assignment No', data.assignmentNo || `Assignment ${id}`],
            ['Description', data.description || ''],
            ['Start Date', data.startDate ? formatDate(data.startDate) : ''],
            ['Due Date', data.dueDate ? formatDate(data.dueDate) : ''],
            ['Exported At', formatDateTime(new Date())],
            ['Learners', data.totalEmployees],
            ['Courses', data.totalCourses],
            ['In Progress', data.chartData.inProgress],
            ['Not Started', data.chartData.notStarted],
            ['Overdue Learners', overdueLearnerCount],
          ],
          columns: [24, 40],
        },
        {
          sheet: 'Learner Detail',
          header: [
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
          ],
          rows: data.learners.map((row) => [
            row.learnerCode,
            row.learnerName ?? row.learnerCode,
            row.division ?? '',
            row.department ?? '',
            row.learnerGroups ? row.learnerGroups.join('; ') : '',
            row.courseCode ?? '',
            row.courseTitle ?? '',
            learnerStatusLabel(row.status),
            row.progress,
            row.startDate ? formatDate(row.startDate) : '',
            row.dueDate ? formatDate(row.dueDate) : '',
          ]),
          columns: [16, 28, 18, 22, 28, 16, 40, 16, 12, 14, 14],
        },
        {
          sheet: 'Course Summary',
          header: ['Course Code', 'Course Title', 'Total Learners', 'Deleted'],
          rows: data.courses.map((row) => [
            row.courseCode,
            row.courseTitle,
            row.totalLearners,
            row.isCourseDeleted ? 'Yes' : 'No',
          ]),
          columns: [18, 42, 16, 12],
        },
        {
          sheet: 'Group Summary',
          header: ['Learner Group', 'Learners', 'Enrollments', 'Overdue'],
          rows: groupSummaries.map((row) => [
            row.groupName,
            row.learnerCount,
            row.enrollments,
            row.overdue,
          ]),
          columns: [32, 12, 14, 12],
        },
        {
          sheet: 'Status Summary',
          header: ['Status', 'Enrollments', 'Share %'],
          rows: statusChartData.map((row) => [
            row.label,
            row.count,
            data.learners.length === 0 ? 0 : (row.count / data.learners.length) * 100,
          ]),
          columns: [18, 14, 12],
        },
        {
          sheet: 'Exceptions',
          header: [
            'Exception Type',
            'Learner Code',
            'Name',
            'Division',
            'Department',
            'Learner Groups',
            'Course Code',
            'Course Title',
            'Status',
            'Progress %',
            'Due Date',
          ],
          rows: exceptionRows.map((row) => [
            row.status === 'Overdue' ? 'Overdue' : 'Incomplete',
            row.learnerCode,
            row.learnerName ?? row.learnerCode,
            row.division ?? '',
            row.department ?? '',
            row.learnerGroups ? row.learnerGroups.join('; ') : '',
            row.courseCode ?? '',
            row.courseTitle ?? '',
            learnerStatusLabel(row.status),
            row.progress,
            row.dueDate ? formatDate(row.dueDate) : '',
          ]),
          columns: [16, 16, 28, 18, 22, 28, 16, 40, 16, 12, 14],
        },
        {
          sheet: 'Incomplete Only',
          header: ['Learner Code', 'Name', 'Course Code', 'Course Title', 'Status', 'Progress %', 'Due Date'],
          rows: incompleteRows.map((row) => [
            row.learnerCode,
            row.learnerName ?? row.learnerCode,
            row.courseCode ?? '',
            row.courseTitle ?? '',
            learnerStatusLabel(row.status),
            row.progress,
            row.dueDate ? formatDate(row.dueDate) : '',
          ]),
          columns: [16, 28, 16, 40, 16, 12, 14],
        },
      ])
    } catch (error) {
      console.error(error)
      toast.error(t(REPORT_LABELS.exportExcelFailed))
    } finally {
      setExportingKey(null)
    }
  }

  const handlePrint = () => {
    // Reveal every row first so the printout is not cut off at the current chunk
    setVisibleRows(filtered.length)
    setTimeout(() => window.print(), 150)
  }

  if (loading) {
    return <LoadingState label={t(ASSIGNMENT_LABELS.loadingReport)} />
  }

  if (!data) {
    return (
      <NotFoundState
        title={t(ASSIGNMENT_LABELS.assignmentNotFound)}
        message={t(ASSIGNMENT_LABELS.assignmentReportUnavailable)}
        backTo="/assignments"
        backLabel={t(ASSIGNMENT_LABELS.backToAssignments)}
      />
    )
  }

  const visible = filtered.slice(0, visibleRows)
  const handleRowsScroll = (event: React.UIEvent<HTMLDivElement>) => {
    if (visibleRows < filtered.length && shouldLoadMoreOnScroll(event.currentTarget)) {
      setVisibleRows((prev) => prev + DETAIL_TABLE_CHUNK_SIZE)
    }
  }

  return (
    <div className="space-y-6">
      <DetailLayout
        sidebar={
          <ControlsSidebar className="print:hidden">
            <ControlAction icon={Printer} onClick={handlePrint}>
              {t(ASSIGNMENT_LABELS.printReport)}
            </ControlAction>
            <div className="pt-2 text-xxs font-extrabold uppercase tracking-wider text-slate-400">
              {t(ASSIGNMENT_LABELS.exportData)}
            </div>
            <ControlAction
              icon={FileSpreadsheet}
              onClick={() => void exportAdminWorkbook()}
              disabled={data.learners.length === 0 || exportingKey !== null}
              loading={exportingKey === 'admin-workbook'}
            >
              {t(ASSIGNMENT_LABELS.exportExcelWorkbook)}
            </ControlAction>
          </ControlsSidebar>
        }
      >
        <div className="space-y-6">
          {/* Summary stats */}
          <DetailCard>
            <SectionHeader>{t(ASSIGNMENT_LABELS.reportSummary)}</SectionHeader>

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
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 pt-3 px-1">
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center">
                <div className="text-[10px] font-extrabold text-slate-400 uppercase">{t(ASSIGNMENT_LABELS.learners)}</div>
                <div className="text-lg font-bold text-slate-800 tabular-nums mt-0.5">{data.totalEmployees}</div>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center">
                <div className="text-[10px] font-extrabold text-slate-400 uppercase">{t(ASSIGNMENT_LABELS.overdue)}</div>
                <div className={`text-lg font-bold tabular-nums mt-0.5 ${overdueLearnerCount > 0 ? 'text-red-600' : 'text-slate-800'}`}>{overdueLearnerCount}</div>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center">
                <div className="text-[10px] font-extrabold text-slate-400 uppercase">{t(ASSIGNMENT_LABELS.courses)}</div>
                <div className="text-lg font-bold text-slate-800 tabular-nums mt-0.5">{data.totalCourses}</div>
              </div>
            </div>

            {/* Print-only fallback: full stats without charts */}
            <div className="hidden print:block pt-3 px-1">
              <div className="grid grid-cols-2 gap-2 text-xs">
                <div><span className="font-bold text-slate-500">Not Started:</span> {data.chartData.notStarted}</div>
                <div><span className="font-bold text-slate-500">In Progress:</span> {data.chartData.inProgress}</div>
              </div>
            </div>

            {/* Charts: status only */}
            <div className="pt-4 print:hidden">
              <DetailSubSection title={t(ASSIGNMENT_LABELS.statusOverview)}>
                <StatusDonut
                  data={statusChartData}
                  activeStatus={statusFilter}
                />
              </DetailSubSection>
            </div>
          </DetailCard>

          {/* Learner Group breakdown */}
          {groupSummaries.length > 0 && (
            <Card icon={Users} title={t(ASSIGNMENT_LABELS.byLearnerGroup)}>
              <div className="overflow-x-auto custom-scrollbar">
                <table className="w-full text-left text-sm border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                      <th className="p-3 pl-5">{t(ASSIGNMENT_LABELS.byLearnerGroup)}</th>
                      <th className="p-3 text-center">{t(ASSIGNMENT_LABELS.learners)}</th>
                      <th className="p-3 text-center">{t(ASSIGNMENT_LABELS.enrollments)}</th>
                      <th className="p-3 text-center">{t(ASSIGNMENT_LABELS.overdue)}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-slate-700">
                    {groupSummaries.map((row) => (
                      <tr key={row.groupName} className="hover:bg-slate-50/50 transition duration-100">
                        <td className="p-3 pl-5 font-semibold text-slate-800 text-xs">{row.groupName}</td>
                        <td className="p-3 text-center text-xs">{row.learnerCount}</td>
                        <td className="p-3 text-center text-xs">{row.enrollments}</td>
                        <td className="p-3 text-center text-xs">
                          <span className={row.overdue > 0 ? 'font-bold text-red-600' : 'text-slate-400'}>
                            {row.overdue}
                          </span>
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
                searchPlaceholder={t(ASSIGNMENT_LABELS.searchReport)}
                toolbarContent={
                  <div className="flex flex-wrap items-center gap-1.5">
                    <SegmentedToggle
                      variant="filter"
                      options={STATUS_FILTERS.map(s => ({
                        value: s,
                        label: s === 'All' ? t(COMMON_LABELS.all) : learnerStatusLabel(s),
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
                          <option value="All">{t(ASSIGNMENT_LABELS.allCourses)}</option>
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
                        <option value="All">{t(ASSIGNMENT_LABELS.allGroups)}</option>
                        {groupOptions.map((g) => (
                          <option key={g} value={g}>
                            {g}
                          </option>
                        ))}
                        <option value="Ungrouped">{t(ASSIGNMENT_LABELS.ungrouped)}</option>
                      </select>
                      <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400 pointer-events-none" />
                    </div>
                  </div>
                }
              />
            </div>

            <div onScroll={handleRowsScroll} className="overflow-x-auto max-h-140 custom-scrollbar">
              <table className="w-full text-left text-sm border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                    <th className="p-3 pl-5">{t(REPORT_LABELS.colLearner)}</th>
                    <th className="p-3">{t(ASSIGNMENT_LABELS.courses)}</th>
                    <th className="p-3">{t(ASSIGNMENT_LABELS.status)}</th>
                    <th className="p-3">{t(REPORT_LABELS.colProgress)}</th>
                    <th className="p-3 pr-5">{t(ASSIGNMENT_LABELS.dueDate)}</th>
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
                      <td className="p-3 pr-5 text-slate-600 text-xs">
                        {row.dueDate ? formatDate(row.dueDate) : '—'}
                      </td>
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr>
                      <td className="p-6 text-center text-slate-400" colSpan={5}>
                        {t(ASSIGNMENT_LABELS.noLearnersFound)}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </Card>
        </div>
      </DetailLayout>
    </div>
  )
}
