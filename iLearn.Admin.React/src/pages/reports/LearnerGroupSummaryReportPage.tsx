import { useEffect, useMemo, useState, type UIEvent } from 'react'
import { Link } from 'react-router-dom'
import { ArrowUpDown, ExternalLink } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { Card } from '../../components/ui/Card'
import { ExportMenu } from '../../components/ui/ExportMenu'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { LoadingState } from '../../components/ui/LoadingState'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { buildApiUrl, fetchWithAccessControl } from '../../lib/apiClient'
import { downloadBlob, filenameFromContentDisposition } from '../../lib/downloadBlob'
import { formatDate, formatNumber, formatPercent } from '../../lib/format'
import { DASHBOARD_LABELS, REPORT_LABELS, getLang, learnerStatusLabel, t } from '../../lib/labels'
import { exportRows } from '../../lib/tableExport'
import { DETAIL_TABLE_CHUNK_SIZE, shouldLoadMoreOnScroll } from '../../lib/tableStandards'
import { toast } from '../../lib/toast'
import type { LearnerGroupSummaryReportDto } from './reportTypes'

type SortKey =
  | 'name'
  | 'description'
  | 'divisionName'
  | 'categoryName'
  | 'createdAt'
  | 'memberCount'
  | 'assignmentCount'
  | 'courseCount'
  | 'enrollmentCount'
  | 'completedCount'
  | 'overdueCount'
  | 'avgProgress'
  | 'completionRate'

function compareNullableText(a: string | null | undefined, b: string | null | undefined) {
  return (a ?? '').localeCompare(b ?? '')
}

function compareNullableDate(a: string | null | undefined, b: string | null | undefined) {
  const aTime = a ? new Date(a).getTime() : 0
  const bTime = b ? new Date(b).getTime() : 0
  return aTime - bTime
}

function isDueDateInRange(dueDate: string | null | undefined, from: string, to: string) {
  if (!from && !to) return true
  if (!dueDate) return false

  const date = dueDate.slice(0, 10)
  return (!from || date >= from) && (!to || date <= to)
}

function KpiTile({ label, value, tone = 'slate' }: { label: string; value: string; tone?: 'slate' | 'indigo' | 'emerald' | 'rose' }) {
  const toneClass = {
    slate: 'text-slate-900',
    indigo: 'text-indigo-700',
    emerald: 'text-emerald-700',
    rose: 'text-rose-600',
  }[tone]

  return (
    <div className="border-r border-slate-200/70 px-4 py-3 last:border-r-0">
      <div className="text-[10px] font-extrabold uppercase text-slate-400">{label}</div>
      <div className={`mt-1 text-lg font-bold tabular-nums ${toneClass}`}>{value}</div>
    </div>
  )
}

export function LearnerGroupSummaryReportPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<LearnerGroupSummaryReportDto | null>(null)
  const [search, setSearch] = useState('')
  const [sortKey, setSortKey] = useState<SortKey>('name')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')
  const [visibleRows, setVisibleRows] = useState(DETAIL_TABLE_CHUNK_SIZE)
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [exportingExcel, setExportingExcel] = useState(false)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    fetchWithAccessControl<{ success: boolean; data: LearnerGroupSummaryReportDto }>('Reports/learner-groups')
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
  }, [search, sortKey, sortOrder, fromDate, toDate])

  const handleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortKey(key)
      setSortOrder(key === 'name' ? 'asc' : 'desc')
    }
  }

  const renderSortIndicator = (key: SortKey) => {
    if (sortKey !== key) {
      return <ArrowUpDown className="ml-1 inline-block h-3 w-3 text-slate-300" aria-hidden="true" />
    }
    return sortOrder === 'asc' ? ' ▲' : ' ▼'
  }

  const sortedRows = useMemo(() => {
    if (!data) return []
    return [...data.rows].sort((a, b) => {
      let result: number
      switch (sortKey) {
        case 'name':
          result = compareNullableText(a.name, b.name)
          break
        case 'description':
          result = compareNullableText(a.description, b.description)
          break
        case 'divisionName':
          result = compareNullableText(a.divisionName, b.divisionName)
          break
        case 'categoryName':
          result = compareNullableText(a.categoryName, b.categoryName)
          break
        case 'createdAt':
          result = compareNullableDate(a.createdAt, b.createdAt)
          break
        default:
          result = (a[sortKey] as number) - (b[sortKey] as number)
      }
      return sortOrder === 'asc' ? result : -result
    })
  }, [data, sortKey, sortOrder])

  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase()
    return sortedRows.filter((row) =>
      isDueDateInRange(row.dueDate, fromDate, toDate)
      && (!q || [row.name, row.description, row.divisionName, row.categoryName]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(q))),
    )
  }, [fromDate, search, sortedRows, toDate])

  const visibleGroupRows = useMemo(
    () => filteredRows.slice(0, visibleRows),
    [filteredRows, visibleRows],
  )

  const handleScroll = (event: UIEvent<HTMLDivElement>) => {
    if (visibleRows < filteredRows.length && shouldLoadMoreOnScroll(event.currentTarget)) {
      setVisibleRows((prev) => prev + DETAIL_TABLE_CHUNK_SIZE)
    }
  }

  const handleExportCsv = async () => {
    if (!data || filteredRows.length === 0) {
      toast.info(t(REPORT_LABELS.noRowsToExport))
      return
    }

    const header = [
      '#',
      'Learner Group',
      'Description',
      'Division',
      'Category',
      'Created',
      'Members',
      'Assignments',
      'Courses',
      'Enrollments',
      'Completed',
      'Overdue',
      'Average Progress %',
      'Completion %',
    ]
    const body = filteredRows.map((row, index) => [
      index + 1,
      row.name,
      row.description ?? '',
      row.divisionName ?? '',
      row.categoryName ?? '',
      formatDate(row.createdAt),
      row.memberCount,
      row.assignmentCount,
      row.courseCount,
      row.enrollmentCount,
      row.completedCount,
      row.overdueCount,
      formatPercent(row.avgProgress).replace('%', ''),
      formatPercent(row.completionRate).replace('%', ''),
    ])
    const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    await exportRows('csv', `learner-group-summary-report-${stamp}`, header, body)
  }

  const handleExportExcel = async () => {
    setExportingExcel(true)
    try {
      const params = new URLSearchParams()
      if (fromDate) params.set('from', fromDate)
      if (toDate) params.set('to', toDate)
      params.set('lang', getLang())
      const response = await fetch(buildApiUrl(`Reports/learner-groups/export?${params.toString()}`), {
        credentials: 'include',
        headers: { Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' },
      })
      if (!response.ok) throw new Error(response.statusText || t(REPORT_LABELS.exportExcelFailed))

      const blob = await response.blob()
      downloadBlob(blob, filenameFromContentDisposition(response.headers.get('content-disposition'), 'learner-group-report.xlsx'))
    } catch {
      toast.error(t(REPORT_LABELS.exportExcelFailed))
    } finally {
      setExportingExcel(false)
    }
  }

  const clearDateFilter = () => {
    setFromDate('')
    setToDate('')
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
    <div className="flex h-full min-h-0 flex-col gap-4">
      <div className="grid grid-cols-2 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs sm:grid-cols-3 xl:grid-cols-6">
        <KpiTile label={t(REPORT_LABELS.lgTotalGroups)} value={formatNumber(data.totalGroups)} />
        <KpiTile label={t(REPORT_LABELS.lgTotalMembers)} value={formatNumber(data.totalMembers)} tone="indigo" />
        <KpiTile label={t(REPORT_LABELS.lgGroupsWithAssignments)} value={formatNumber(data.groupsWithAssignments)} />
        <KpiTile label={t(REPORT_LABELS.lgTotalAssignments)} value={formatNumber(data.totalAssignments)} />
        <KpiTile label={t(REPORT_LABELS.lgTotalEnrollments)} value={formatNumber(data.totalEnrollments)} tone="emerald" />
        <KpiTile label={t(REPORT_LABELS.lgOverallCompletion)} value={formatPercent(data.completionRate)} />
      </div>

      <Card
        title={t(REPORT_LABELS.lgTitle)}
        className="flex min-h-0 flex-1 flex-col"
        bodyClassName="flex min-h-0 flex-1 flex-col"
        actions={
          <ExportMenu
            hasRows={data.rows.length > 0}
            csv={{ onClick: handleExportCsv }}
            xlsx={{
              label: t(REPORT_LABELS.exportExcelDetail),
              loadingLabel: t(REPORT_LABELS.exportingExcel),
              onClick: handleExportExcel,
              loading: exportingExcel,
            }}
          />
        }
      >
        <div className="shrink-0 border-b border-slate-100 bg-slate-50/20 px-5 py-3">
          <div className="flex flex-col gap-3 xl:flex-row xl:items-end xl:justify-between">
            <ListToolbar
              searchValue={search}
              onSearchChange={setSearch}
              searchPlaceholder={t(REPORT_LABELS.lgSearchPlaceholder)}
            />
            <div className="flex flex-wrap items-end gap-2">
              <label className="flex flex-col gap-1 text-xxs font-bold uppercase text-slate-400">
                {t(REPORT_LABELS.filterFromDate)}
                <input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} className="h-8 rounded-md border border-slate-200 bg-white px-2 text-xs font-semibold text-slate-700" />
              </label>
              <label className="flex flex-col gap-1 text-xxs font-bold uppercase text-slate-400">
                {t(REPORT_LABELS.filterToDate)}
                <input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} className="h-8 rounded-md border border-slate-200 bg-white px-2 text-xs font-semibold text-slate-700" />
              </label>
              {(fromDate || toDate) && (
                <AppButton onClick={clearDateFilter} variant="ghost" size="sm">
                  {t(REPORT_LABELS.clearDateFilter)}
                </AppButton>
              )}
            </div>
          </div>
        </div>

        <div onScroll={handleScroll} className="min-h-0 flex-1 overflow-auto custom-scrollbar">
          <table className="w-full border-collapse text-left text-sm">
            <thead>
              <tr className="sticky top-0 z-10 select-none border-b border-slate-200 bg-slate-50 text-xxs font-bold uppercase text-slate-500 shadow-xs">
                <th className="w-12 p-3 pl-5 text-center">#</th>
                <th className="min-w-56 cursor-pointer p-3 transition duration-100 hover:bg-slate-100/70" onClick={() => handleSort('name')}>
                  {t(REPORT_LABELS.lgColGroup)}{renderSortIndicator('name')}
                </th>
                <th className="min-w-36 cursor-pointer p-3 transition duration-100 hover:bg-slate-100/70" onClick={() => handleSort('divisionName')}>
                  {t(REPORT_LABELS.colDivision)}{renderSortIndicator('divisionName')}
                </th>
                <th className="min-w-40 cursor-pointer p-3 transition duration-100 hover:bg-slate-100/70" onClick={() => handleSort('categoryName')}>
                  {t(REPORT_LABELS.lgColCategory)}{renderSortIndicator('categoryName')}
                </th>
                <th className="w-24 cursor-pointer p-3 text-center transition duration-100 hover:bg-slate-100/70" onClick={() => handleSort('memberCount')}>
                  {t(REPORT_LABELS.lgColMembers)}{renderSortIndicator('memberCount')}
                </th>
                <th className="w-24 cursor-pointer p-3 text-center transition duration-100 hover:bg-slate-100/70" onClick={() => handleSort('assignmentCount')}>
                  {t(REPORT_LABELS.lgColAssignments)}{renderSortIndicator('assignmentCount')}
                </th>
                <th className="w-24 cursor-pointer p-3 text-center transition duration-100 hover:bg-slate-100/70" onClick={() => handleSort('courseCount')}>
                  {t(REPORT_LABELS.lgColCourses)}{renderSortIndicator('courseCount')}
                </th>
                <th className="w-28 cursor-pointer p-3 text-center transition duration-100 hover:bg-slate-100/70" onClick={() => handleSort('enrollmentCount')}>
                  {t(REPORT_LABELS.lgColEnrollments)}{renderSortIndicator('enrollmentCount')}
                </th>
                <th className="min-w-36 cursor-pointer p-3 transition duration-100 hover:bg-slate-100/70" onClick={() => handleSort('avgProgress')}>
                  {t(REPORT_LABELS.lgColAvgProgress)}{renderSortIndicator('avgProgress')}
                </th>
                <th className="min-w-36 cursor-pointer p-3 transition duration-100 hover:bg-slate-100/70" onClick={() => handleSort('completionRate')}>
                  {t(REPORT_LABELS.colCompletionShare)}{renderSortIndicator('completionRate')}
                </th>
                <th className="w-24 p-3 pr-5 text-right">{t(DASHBOARD_LABELS.colActions)}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {visibleGroupRows.map((row, index) => (
                <tr key={row.learnerGroupId} className="transition duration-100 hover:bg-slate-50/50">
                  <td className="p-3 pl-5 text-center text-xs font-semibold tabular-nums text-slate-400">
                    {index + 1}
                  </td>
                  <td className="p-3 text-xs font-semibold text-slate-700">
                    <div className="max-w-sm truncate font-bold text-slate-800" title={row.name}>{row.name}</div>
                    <div className="mt-0.5 max-w-sm truncate text-xxs font-semibold text-slate-400" title={row.description ?? undefined}>
                      {row.description || '-'}
                    </div>
                    <div className="mt-0.5 text-xxs font-semibold text-slate-400">
                      {t(REPORT_LABELS.asgColCreated)} {formatDate(row.createdAt)}
                    </div>
                    <div className="mt-0.5 text-xxs font-semibold text-slate-400">
                      {t(REPORT_LABELS.filterToDate)} {formatDate(row.dueDate)}
                    </div>
                  </td>
                  <td className="p-3 text-xs font-semibold text-slate-500">{row.divisionName || '-'}</td>
                  <td className="p-3 text-xs font-semibold text-slate-500">{row.categoryName || '-'}</td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">{formatNumber(row.memberCount)}</td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">{formatNumber(row.assignmentCount)}</td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">{formatNumber(row.courseCount)}</td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    <div>{formatNumber(row.enrollmentCount)}</div>
                    {row.overdueCount > 0 && <div className="text-xxs font-bold text-rose-600">{formatNumber(row.overdueCount)} {learnerStatusLabel('Overdue')}</div>}
                  </td>
                  <td className="p-3 text-xs">
                    <ProgressBar value={row.avgProgress} maxWidthClass="max-w-36" />
                    <div className="mt-1 text-xxs font-semibold text-slate-400">{formatPercent(row.avgProgress)}</div>
                  </td>
                  <td className="p-3 text-xs">
                    <ProgressBar value={row.completionRate} completed={row.completionRate >= 100} maxWidthClass="max-w-36" />
                    <div className="mt-1 text-xxs font-semibold text-slate-400">
                      {formatNumber(row.completedCount)} / {formatNumber(row.enrollmentCount)}
                    </div>
                  </td>
                  <td className="p-3 pr-5 text-right text-xs font-bold text-indigo-700">
                    <Link to={`/learner-groups/${row.learnerGroupId}`} className="inline-flex items-center justify-end gap-1 hover:text-indigo-900">
                      {t(REPORT_LABELS.lgOpenGroup)}
                      <ExternalLink className="h-3 w-3" aria-hidden="true" />
                    </Link>
                  </td>
                </tr>
              ))}
              {filteredRows.length === 0 && (
                <tr>
                  <td colSpan={11} className="p-6 text-center text-xs font-medium text-slate-400">
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