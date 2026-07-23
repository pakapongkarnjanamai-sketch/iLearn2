import { useEffect, useMemo, useState } from 'react'
import {
  Download,
  ArrowUpDown,
} from 'lucide-react'
import { Card } from '../../components/ui/Card'
import { LoadingState } from '../../components/ui/LoadingState'
import { Badge } from '../../components/ui/Badge'
import { AppButton } from '../../components/ui/AppButton'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatNumber } from '../../lib/format'
import { exportRowsAsCsv } from '../../lib/csvExport'
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
      .catch(() => toast.error('Failed to load course summary report'))
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

  const handleExportCsv = () => {
    if (!data || data.rows.length === 0) {
      toast.info('No course records to export')
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
    <div className="h-full flex flex-col min-h-0">
      {/* Courses Performance List with Scroller Grid */}
      <Card
        title="Course Completion Summary"
        className="flex-1 flex flex-col min-h-0"
        bodyClassName="flex-1 flex flex-col min-h-0"
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
        <div className="border-b border-slate-100 bg-slate-50/20 px-5 shrink-0">
          <ListToolbar
            searchValue={search}
            onSearchChange={setSearch}
            searchPlaceholder="Search course code, title, division or category..."
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
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('divisionName')}
                >
                  Division{renderSortIndicator('divisionName')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('courseTypeName')}
                >
                  Type{renderSortIndicator('courseTypeName')}
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
                      {row.courseTypeName || 'General'}
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
                    No course records found
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Footer showing row count & infinite scroll status */}
        {filteredRows.length > 0 && (
          <div className="border-t border-slate-100 bg-slate-50/50 px-5 py-2.5 text-xs text-slate-500 font-medium flex items-center justify-between shrink-0">
            <span>
              Showing <strong className="text-slate-800 tabular-nums">{visibleCourseRows.length}</strong> of{' '}
              <strong className="text-slate-800 tabular-nums">{filteredRows.length}</strong> courses
            </span>
            {visibleCourseRows.length < filteredRows.length && (
              <span className="text-xxs text-indigo-600 font-semibold flex items-center gap-1">
                Scroll down to load more
              </span>
            )}
          </div>
        )}
      </Card>
    </div>
  )
}


