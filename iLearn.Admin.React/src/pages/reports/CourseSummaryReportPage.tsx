import { useEffect, useMemo, useState } from 'react'
import {
  ArrowUpDown,
} from 'lucide-react'
import { Card } from '../../components/ui/Card'
import { ExportMenu } from '../../components/ui/ExportMenu'
import { LoadingState } from '../../components/ui/LoadingState'
import { Badge } from '../../components/ui/Badge'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatNumber } from '../../lib/format'
import { DASHBOARD_LABELS, REPORT_LABELS, learnerStatusLabel, t } from '../../lib/labels'
import { exportRows, type ExportFormat } from '../../lib/tableExport'
import { DETAIL_TABLE_CHUNK_SIZE } from '../../lib/tableStandards'
import { toast } from '../../lib/toast'
import type { CourseSummaryReportDto } from './reportTypes'

type SortKey =
  | 'code'
  | 'title'
  | 'categoryName'
  | 'divisionName'
  | 'courseTypeName'
  | 'assignmentCount'
  | 'enrolledLearners'
  | 'completedCount'
  | 'overdueCount'

export function CourseSummaryReportPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<CourseSummaryReportDto | null>(null)
  const [search, setSearch] = useState('')
  const [sortKey, setSortKey] = useState<SortKey>('enrolledLearners')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc')
  const [visibleRows, setVisibleRows] = useState(DETAIL_TABLE_CHUNK_SIZE)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    fetchWithAccessControl<{ success: boolean; data: CourseSummaryReportDto }>('Reports/course-summary')
      .then((resp) => {
        if (cancelled) return
        if (resp.success) {
          setData(resp.data)
        }
      })
      .catch(() => toast.error(t(REPORT_LABELS.loadReportFailed)))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    setVisibleRows(DETAIL_TABLE_CHUNK_SIZE)
  }, [search, sortKey, sortOrder])

  const handleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortKey(key)
      setSortOrder('desc')
    }
  }

  const renderSortIndicator = (key: SortKey) => {
    if (sortKey !== key) {
      return <ArrowUpDown className="inline-block ml-1 h-3 w-3 text-slate-300" />
    }
    return sortOrder === 'asc' ? ' ▲' : ' ▼'
  }

  const sortedRows = useMemo(() => {
    if (!data) return []
    return [...data.rows].sort((a, b) => {
      const aVal = a[sortKey]
      const bVal = b[sortKey]

      if (aVal === null || aVal === undefined) return sortOrder === 'asc' ? 1 : -1
      if (bVal === null || bVal === undefined) return sortOrder === 'asc' ? -1 : 1

      if (typeof aVal === 'string' && typeof bVal === 'string') {
        return sortOrder === 'asc' ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal)
      }

      // numeric sorting
      return sortOrder === 'asc' ? (aVal as number) - (bVal as number) : (bVal as number) - (aVal as number)
    })
  }, [data, sortKey, sortOrder])

  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return sortedRows
    return sortedRows.filter((r) =>
      [r.code, r.title, r.categoryName, r.divisionName, r.courseTypeName]
        .filter(Boolean)
        .some((val) => val!.toLowerCase().includes(q))
    )
  }, [sortedRows, search])

  const visibleCourseRows = useMemo(
    () => filteredRows.slice(0, visibleRows),
    [filteredRows, visibleRows]
  )

  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const target = e.currentTarget
    const threshold = target.scrollHeight - target.scrollTop - target.clientHeight
    if (threshold <= 60 && visibleRows < filteredRows.length) {
      setVisibleRows((prev) => prev + DETAIL_TABLE_CHUNK_SIZE)
    }
  }

  const handleExport = async (format: ExportFormat) => {
    if (!data || data.rows.length === 0) {
      toast.info(t(REPORT_LABELS.noRowsToExport))
      return
    }
    const header = [
      '#',
      'Course Code',
      'Course Title',
      'Category',
      'Division',
      'Course Type',
      'Assignments',
      'Enrolled Learners',
      'Completed Count',
      'Overdue Count',
    ]
    const body = data.rows.map((r, idx) => [
      idx + 1,
      r.code ?? '',
      r.title ?? '',
      r.categoryName ?? '',
      r.divisionName ?? '',
      r.courseTypeName ?? 'General',
      r.assignmentCount,
      r.enrolledLearners,
      r.completedCount,
      r.overdueCount,
    ])
    const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    await exportRows(format, `course-summary-report-${stamp}`, header, body)
  }

  if (loading) {
    return <LoadingState label={t(REPORT_LABELS.loadingReport)} />
  }

  if (!data) {
    return (
      <div className="py-12 text-center text-slate-500 font-semibold">
        {t(REPORT_LABELS.noReportData)}
      </div>
    )
  }

  return (
    <div className="h-full flex flex-col min-h-0">
      {/* Courses Performance List with Scroller Grid */}
      <Card
        title={t(REPORT_LABELS.csTitle)}
        className="flex-1 flex flex-col min-h-0"
        bodyClassName="flex-1 flex flex-col min-h-0"
        actions={<ExportMenu hasRows={data.rows.length > 0} onExport={handleExport} />}
      >
        <div className="border-b border-slate-100 bg-slate-50/20 px-5 shrink-0">
          <ListToolbar
            searchValue={search}
            onSearchChange={setSearch}
            searchPlaceholder={t(REPORT_LABELS.csSearchPlaceholder)}
          />
        </div>

        {/* Scroller Grid container flexing height to fit screen */}
        <div
          onScroll={handleScroll}
          className="flex-1 overflow-x-auto overflow-y-auto min-h-0 custom-scrollbar"
        >
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none sticky top-0 z-10 shadow-xs">
                <th className="p-3 pl-5 text-center w-12">#</th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('code')}
                >
                  {t(REPORT_LABELS.csColCode)}{renderSortIndicator('code')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('title')}
                >
                  {t(REPORT_LABELS.csColTitle)}{renderSortIndicator('title')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('categoryName')}
                >
                  {t(REPORT_LABELS.csColCategory)}{renderSortIndicator('categoryName')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('divisionName')}
                >
                  {t(REPORT_LABELS.colDivision)}{renderSortIndicator('divisionName')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('courseTypeName')}
                >
                  {t(REPORT_LABELS.csColType)}{renderSortIndicator('courseTypeName')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('assignmentCount')}
                >
                  {t(REPORT_LABELS.csColAssignments)}{renderSortIndicator('assignmentCount')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('enrolledLearners')}
                >
                  {t(DASHBOARD_LABELS.colLearners)}{renderSortIndicator('enrolledLearners')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('completedCount')}
                >
                  {t(REPORT_LABELS.colCompleted)}{renderSortIndicator('completedCount')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('overdueCount')}
                >
                  {learnerStatusLabel('Overdue')}{renderSortIndicator('overdueCount')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {visibleCourseRows.map((row, idx) => (
                <tr key={row.courseId} className="hover:bg-slate-50/50 transition duration-100">
                  <td className="p-3 pl-5 text-center text-xs font-semibold text-slate-400 tabular-nums">
                    {idx + 1}
                  </td>
                  <td className="p-3 text-xs font-mono font-bold text-slate-700 select-all">
                    {row.code || '—'}
                  </td>
                  <td className="p-3 text-xs font-bold text-slate-800">
                    {row.title || '—'}
                  </td>
                  <td className="p-3 text-xs font-semibold text-slate-500">
                    {row.categoryName || '—'}
                  </td>
                  <td className="p-3 text-xs font-semibold text-slate-600">
                    {row.divisionName || '—'}
                  </td>
                  <td className="p-3 text-center text-xs">
                    <Badge
                      tone={(row.courseTypeName || '').toLowerCase().includes('special') ? 'warning' : 'info'}
                      variant="soft"
                      size="xxs"
                    >
                      {row.courseTypeName || t(REPORT_LABELS.csGeneralType)}
                    </Badge>
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {formatNumber(row.assignmentCount)}
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {formatNumber(row.enrolledLearners)}
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {formatNumber(row.completedCount)}
                  </td>
                  <td className="p-3 text-center text-xs font-bold tabular-nums">
                    <span className={row.overdueCount > 0 ? 'text-rose-600' : 'text-slate-400'}>
                      {row.overdueCount}
                    </span>
                  </td>
                </tr>
              ))}
              {filteredRows.length === 0 && (
                <tr>
                  <td colSpan={10} className="p-6 text-center text-slate-400 text-xs font-medium">
                    {t(REPORT_LABELS.notFound)}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  )
}


