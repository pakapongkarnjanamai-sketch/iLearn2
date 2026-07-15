import { useEffect, useMemo, useState } from 'react'
import {
  BookOpen,
  Download,
  ArrowUpDown,
} from 'lucide-react'
import { Card } from '../../components/ui/Card'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { LoadingState } from '../../components/ui/LoadingState'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { AppButton } from '../../components/ui/AppButton'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatPercent, formatNumber } from '../../lib/format'
import { exportRowsAsCsv } from '../../lib/csvExport'
import { DETAIL_TABLE_CHUNK_SIZE } from '../../lib/tableStandards'
import { toast } from '../../lib/toast'
import type { CourseSummaryReportDto } from './reportTypes'

type SortKey =
  | 'code'
  | 'title'
  | 'categoryName'
  | 'assignmentCount'
  | 'enrolledLearners'
  | 'completedCount'
  | 'overdueCount'
  | 'avgProgress'
  | 'completionRate'
  | 'avgScore'

export function CourseSummaryReportPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<CourseSummaryReportDto | null>(null)
  const [search, setSearch] = useState('')
  const [sortKey, setSortKey] = useState<SortKey>('completionRate')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')
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
      .catch(() => toast.error('Failed to load course summary report'))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    setVisibleRows(DETAIL_TABLE_CHUNK_SIZE)
  }, [search])

  const handleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortKey(key)
      setSortOrder('asc')
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
      [r.code, r.title, r.categoryName]
        .filter(Boolean)
        .some((val) => val!.toLowerCase().includes(q))
    )
  }, [sortedRows, search])

  const visibleCourseRows = useMemo(
    () => filteredRows.slice(0, visibleRows),
    [filteredRows, visibleRows]
  )

  const handleExportCsv = () => {
    if (!data || data.rows.length === 0) {
      toast.info('No course records to export')
      return
    }
    const header = [
      'Course Code',
      'Course Title',
      'Category',
      'Assignments',
      'Enrolled Learners',
      'Completed Count',
      'Overdue Count',
      'Avg Progress %',
      'Completion Rate %',
      'Avg Score',
    ]
    const body = data.rows.map((r) => [
      r.code ?? '',
      r.title ?? '',
      r.categoryName ?? '',
      r.assignmentCount,
      r.enrolledLearners,
      r.completedCount,
      r.overdueCount,
      formatPercent(r.avgProgress).replace('%', ''),
      formatPercent(r.completionRate).replace('%', ''),
      r.avgScore !== null && r.avgScore !== undefined ? formatNumber(r.avgScore) : '',
    ])
    const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    exportRowsAsCsv(`course-summary-report-${stamp}.csv`, header, body)
  }

  if (loading) {
    return <LoadingState label="Loading course summary report..." />
  }

  if (!data) {
    return (
      <div className="py-12 text-center text-slate-500 font-semibold">
        No report data available.
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <SectionHeader icon={BookOpen}>Course Completion Summary</SectionHeader>

      <Card
        title="Courses Performance List"
        actions={
          data.rows.length > 0 && (
            <AppButton
              onClick={handleExportCsv}
              icon={Download}
              variant="secondary"
              size="sm"
            >
              Export CSV
            </AppButton>
          )
        }
      >
        <div className="border-b border-slate-100 bg-slate-50/20 px-5">
          <ListToolbar
            searchValue={search}
            onSearchChange={setSearch}
            searchPlaceholder="Search course code, title or category..."
          />
        </div>

        <div className="overflow-x-auto custom-scrollbar">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none">
                <th
                  className="p-3 pl-5 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('code')}
                >
                  Code{renderSortIndicator('code')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('title')}
                >
                  Title{renderSortIndicator('title')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('categoryName')}
                >
                  Category{renderSortIndicator('categoryName')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('assignmentCount')}
                >
                  Assignments{renderSortIndicator('assignmentCount')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('enrolledLearners')}
                >
                  Learners{renderSortIndicator('enrolledLearners')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('completedCount')}
                >
                  Completed{renderSortIndicator('completedCount')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('overdueCount')}
                >
                  Overdue{renderSortIndicator('overdueCount')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('avgProgress')}
                >
                  Avg Progress{renderSortIndicator('avgProgress')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('completionRate')}
                >
                  Completion Rate{renderSortIndicator('completionRate')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('avgScore')}
                >
                  Avg Score{renderSortIndicator('avgScore')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {visibleCourseRows.map((row) => (
                <tr key={row.courseId} className="hover:bg-slate-50/50 transition duration-100">
                  <td className="p-3 pl-5 text-xs font-mono font-bold text-slate-700 select-all">
                    {row.code || '—'}
                  </td>
                  <td className="p-3 text-xs font-bold text-slate-800">
                    {row.title || '—'}
                  </td>
                  <td className="p-3 text-xs font-semibold text-slate-500">
                    {row.categoryName || '—'}
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
                  <td className="p-3">
                    <div className="flex items-center gap-2">
                      <ProgressBar value={row.avgProgress} completed={row.completionRate >= 100} />
                    </div>
                  </td>
                  <td className="p-3">
                    <div className="flex items-center gap-3">
                      <ProgressBar value={row.completionRate} completed={row.completionRate >= 100} maxWidthClass="max-w-20" />
                      <span className="text-xxs font-bold text-slate-500 tabular-nums">
                        {formatPercent(row.completionRate)}
                      </span>
                    </div>
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {row.avgScore !== null && row.avgScore !== undefined ? formatNumber(row.avgScore) : '—'}
                  </td>
                </tr>
              ))}
              {filteredRows.length === 0 && (
                <tr>
                  <td colSpan={10} className="p-6 text-center text-slate-400 text-xs font-medium">
                    No course records found
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {filteredRows.length > visibleCourseRows.length && (
          <div className="border-t border-slate-100 p-3 text-center">
            <AppButton
              variant="secondary"
              size="sm"
              onClick={() => setVisibleRows((v) => v + DETAIL_TABLE_CHUNK_SIZE)}
            >
              Load more
            </AppButton>
          </div>
        )}
      </Card>
    </div>
  )
}
