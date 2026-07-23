import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Users,
  BookOpen,
  CheckCircle2,
  AlertTriangle,
  Percent,
  Download,
  ChevronRight,
  TrendingUp,
} from 'lucide-react'
import {
  Bar,
  BarChart,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { Card } from '../../components/ui/Card'
import { LoadingState } from '../../components/ui/LoadingState'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { AppButton } from '../../components/ui/AppButton'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatDate, formatPercent, formatNumber } from '../../lib/format'
import { DASHBOARD_LABELS, REPORT_LABELS, UI_LABELS, learnerStatusLabel, t, tf } from '../../lib/labels'
import { exportRowsAsCsv } from '../../lib/csvExport'
import { DETAIL_TABLE_CHUNK_SIZE } from '../../lib/tableStandards'
import { BRAND, tooltipStyle, axisStyle } from '../../lib/chartTheme'
import { toast } from '../../lib/toast'
import type { ComplianceReportDto, ComplianceGroupRow } from './reportTypes'

export function ComplianceReportPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<ComplianceReportDto | null>(null)
  const [activeTab, setActiveTab] = useState<'division' | 'department'>('division')
  const [search, setSearch] = useState('')
  const [visibleRows, setVisibleRows] = useState(DETAIL_TABLE_CHUNK_SIZE)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    fetchWithAccessControl<{ success: boolean; data: ComplianceReportDto }>('Reports/compliance')
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
  }, [search])

  const chartData = useMemo(() => {
    if (!data) return []
    return [...data.byDivision].sort((a, b) => a.completionRate - b.completionRate)
  }, [data])

  const filteredOverdueRows = useMemo(() => {
    if (!data) return []
    const q = search.trim().toLowerCase()
    if (!q) return data.overdueRows
    return data.overdueRows.filter((r) =>
      [
        r.learnerCode,
        r.learnerName,
        r.division,
        r.department,
        r.courseCode,
        r.courseTitle,
        r.assignmentNo,
      ]
        .filter(Boolean)
        .some((val) => val!.toLowerCase().includes(q))
    )
  }, [data, search])

  const visibleOverdueRows = useMemo(
    () => filteredOverdueRows.slice(0, visibleRows),
    [filteredOverdueRows, visibleRows]
  )

  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const target = e.currentTarget
    const threshold = target.scrollHeight - target.scrollTop - target.clientHeight
    if (threshold <= 60 && visibleRows < filteredOverdueRows.length) {
      setVisibleRows((prev) => prev + DETAIL_TABLE_CHUNK_SIZE)
    }
  }

  const groupRows = useMemo(() => {
    if (!data) return []
    return activeTab === 'division' ? data.byDivision : data.byDepartment
  }, [data, activeTab])

  const handleExportCsv = () => {
    if (!data || data.overdueRows.length === 0) {
      toast.info(t(REPORT_LABELS.noRowsToExport))
      return
    }
    const header = [
      'Learner Code',
      'Name',
      'Division',
      'Department',
      'Course Code',
      'Course Title',
      'Assignment No',
      'Due Date',
      'Days Overdue',
      'Progress %',
    ]
    const body = data.overdueRows.map((r) => [
      r.learnerCode,
      r.learnerName ?? r.learnerCode,
      r.division ?? '',
      r.department ?? '',
      r.courseCode ?? '',
      r.courseTitle ?? '',
      r.assignmentNo ?? '',
      r.dueDate ? formatDate(r.dueDate) : '',
      r.daysOverdue,
      formatPercent(r.progress).replace('%', ''),
    ])
    const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    exportRowsAsCsv(`compliance-overdue-report-${stamp}.csv`, header, body)
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
    <div className="h-full flex flex-col min-h-0 gap-5 overflow-auto custom-scrollbar">
      {/* KPI Tiles */}
      <section className="grid grid-cols-2 lg:grid-cols-5 gap-4 shrink-0">
        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              {t(REPORT_LABELS.compTotalLearners)}
            </span>
            <Users className="h-4 w-4 text-slate-400" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight mt-1">
            {formatNumber(data.totalLearners)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">{t(REPORT_LABELS.compTotalLearnersSub)}</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              {t(REPORT_LABELS.compOpenTasks)}
            </span>
            <BookOpen className="h-4 w-4 text-blue-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight mt-1">
            {formatNumber(data.openEnrollments)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">{t(REPORT_LABELS.compOpenTasksSub)}</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              {t(REPORT_LABELS.compCompleted)}
            </span>
            <CheckCircle2 className="h-4 w-4 text-emerald-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-emerald-600 tabular-nums leading-tight mt-1">
            {formatNumber(data.completedEnrollments)}
          </div>
          <span className="text-xxs text-emerald-600 font-medium">{t(REPORT_LABELS.compCompletedSub)}</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              {t(REPORT_LABELS.compOverdueTitle)}
            </span>
            <AlertTriangle
              className={`h-4 w-4 ${data.overdueEnrollments > 0 ? 'text-rose-500' : 'text-slate-400'}`}
              aria-hidden="true"
            />
          </div>
          <div
            className={`text-2xl font-extrabold tabular-nums leading-tight mt-1 ${
              data.overdueEnrollments > 0 ? 'text-rose-600' : 'text-slate-800'
            }`}
          >
            {formatNumber(data.overdueEnrollments)}
          </div>
          <span className="text-xxs text-rose-600 font-semibold">
            {data.overdueLearners > 0 ? tf(REPORT_LABELS.compOverdueLearners, formatNumber(data.overdueLearners)) : t(REPORT_LABELS.compNoOverdue)}
          </span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              {t(DASHBOARD_LABELS.completionRate)}
            </span>
            <Percent className="h-4 w-4 text-indigo-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-indigo-600 tabular-nums leading-tight mt-1">
            {formatPercent(data.complianceRate)}
          </div>
          <span className="text-xxs text-indigo-600 font-medium">{t(REPORT_LABELS.compRateSub)}</span>
        </Card>
      </section>

      {/* Charts & Group breakdown */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 items-start shrink-0">
        {/* Division Completion Rate Chart */}
        <Card title={t(REPORT_LABELS.compChartTitle)} icon={TrendingUp}>
          <div className="p-4">
            {chartData.length === 0 ? (
              <div className="text-center py-8 text-slate-400 text-xs font-medium">
                {t(REPORT_LABELS.compNoDivisionData)}
              </div>
            ) : (
              <>
                <ResponsiveContainer width="100%" height={Math.max(180, chartData.length * 36 + 24)}>
                  <BarChart data={chartData} layout="vertical" margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
                    <XAxis type="number" domain={[0, 100]} tickLine={false} axisLine={false} tick={axisStyle} unit="%" />
                    <YAxis
                      type="category"
                      dataKey="groupName"
                      tickLine={false}
                      axisLine={false}
                      tick={axisStyle}
                      width={120}
                    />
                    <Tooltip
                      cursor={{ fill: 'rgba(79,70,229,0.05)' }}
                      contentStyle={tooltipStyle}
                      formatter={(_value, _name, props) => {
                        const entry = props.payload as ComplianceGroupRow
                        return [
                          `${formatPercent(entry.completionRate)} (${entry.completed}/${entry.enrollments})`,
                          entry.groupName,
                        ]
                      }}
                      labelFormatter={() => ''}
                    />
                    <Bar dataKey="completionRate" radius={[0, 4, 4, 0]} maxBarSize={18}>
                      {chartData.map((entry) => (
                        <Cell
                          key={entry.groupName}
                          fill={BRAND}
                        />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              </>
            )}
          </div>
        </Card>

        {/* Division/Department Summary Table */}
        <Card
          title={
            <div className="flex items-center gap-3">
              <span>{t(REPORT_LABELS.compOverviewRates)}</span>
              <SegmentedToggle
                options={[
                  { value: 'division', label: t(REPORT_LABELS.byDivision) },
                  { value: 'department', label: t(REPORT_LABELS.byDepartment) },
                ]}
                value={activeTab}
                onChange={setActiveTab}
                variant="segment"
              />
            </div>
          }
        >
          <div className="overflow-x-auto custom-scrollbar max-h-80">
            <table className="w-full text-left text-sm border-collapse">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs sticky top-0 z-10 shadow-3xs">
                  <th className="p-3 pl-5">{t(activeTab === 'division' ? REPORT_LABELS.colDivision : REPORT_LABELS.colDepartment)}</th>
                  <th className="p-3 text-center">{t(REPORT_LABELS.colLearner)}</th>
                  <th className="p-3 text-center">{t(REPORT_LABELS.colAssigned)}</th>
                  <th className="p-3 text-center">{t(REPORT_LABELS.colCompleted)}</th>
                  <th className="p-3 text-center">{learnerStatusLabel('Overdue')}</th>
                  <th className="p-3 pr-5">{t(REPORT_LABELS.colCompletionShare)}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-700">
                {groupRows.map((row) => (
                  <tr key={row.groupName} className="hover:bg-slate-50/50 transition duration-100">
                    <td className="p-3 pl-5 text-xs">
                      <div className="font-bold text-slate-800">{row.groupName}</div>
                      {row.parentDivision && (
                        <div className="text-xxs text-slate-400 mt-0.5">{row.parentDivision}</div>
                      )}
                    </td>
                    <td className="p-3 text-center text-xs font-semibold tabular-nums">{formatNumber(row.learners)}</td>
                    <td className="p-3 text-center text-xs font-semibold tabular-nums">{formatNumber(row.enrollments)}</td>
                    <td className="p-3 text-center text-xs font-semibold tabular-nums">{formatNumber(row.completed)}</td>
                    <td className="p-3 text-center text-xs font-bold tabular-nums">
                      <span className={row.overdue > 0 ? 'text-rose-600' : 'text-slate-400'}>
                        {row.overdue}
                      </span>
                    </td>
                    <td className="p-3 pr-5">
                      <div className="flex items-center gap-3">
                        <ProgressBar value={row.completionRate} completed={row.completionRate >= 100} maxWidthClass="max-w-24" />
                        <span className="text-xxs font-bold text-slate-500 tabular-nums">
                          {formatPercent(row.completionRate)}
                        </span>
                      </div>
                    </td>
                  </tr>
                ))}
                {groupRows.length === 0 && (
                  <tr>
                    <td colSpan={6} className="p-6 text-center text-slate-400 text-xs font-medium">
                      {t(REPORT_LABELS.notFound)}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>
      </div>

      {/* Overdue Enrollments Detailed Table */}
      <Card
        title={t(REPORT_LABELS.compDetailsTitle)}
        className="flex-1 flex flex-col min-h-64"
        bodyClassName="flex-1 flex flex-col min-h-0"
        actions={
          data.overdueRows.length > 0 && (
            <AppButton
              onClick={handleExportCsv}
              icon={Download}
              variant="secondary"
              size="sm"
            >
              {t(REPORT_LABELS.exportCsv)}
            </AppButton>
          )
        }
      >
        <div className="border-b border-slate-100 bg-slate-50/20 px-5 shrink-0">
          <ListToolbar
            searchValue={search}
            onSearchChange={setSearch}
            searchPlaceholder={t(REPORT_LABELS.compSearchPlaceholder)}
          />
        </div>

        <div
          onScroll={handleScroll}
          className="flex-1 overflow-x-auto overflow-y-auto min-h-0 custom-scrollbar"
        >
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs sticky top-0 z-10 shadow-xs">
                <th className="p-3 pl-5">{t(REPORT_LABELS.colLearner)}</th>
                <th className="p-3">{t(DASHBOARD_LABELS.colCourse)}</th>
                <th className="p-3 text-center">{t(REPORT_LABELS.colDaysOverdue)}</th>
                <th className="p-3">{t(REPORT_LABELS.colProgress)}</th>
                <th className="p-3 pr-5">{t(DASHBOARD_LABELS.colDueDate)}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {visibleOverdueRows.map((row, idx) => (
                <tr key={`${row.learnerCode}-${row.courseCode}-${idx}`} className="hover:bg-slate-50/50 transition duration-100">
                  <td className="p-3 pl-5">
                    {row.learnerName ? (
                      <Link
                        to={`/reports/transcript?code=${row.learnerCode}`}
                        className="font-bold text-indigo-600 hover:text-indigo-800 text-xs sm:text-[13px] inline-flex items-center gap-1 group/link"
                      >
                        <span>{row.learnerName}</span>
                        <ChevronRight className="h-3 w-3 transition-transform group-hover/link:translate-x-0.5" />
                      </Link>
                    ) : (
                      <Link
                        to={`/reports/transcript?code=${row.learnerCode}`}
                        className="font-bold text-indigo-600 hover:text-indigo-800 text-xs sm:text-[13px] inline-flex items-center gap-1 group/link"
                      >
                        <span>{row.learnerCode}</span>
                        <ChevronRight className="h-3 w-3 transition-transform group-hover/link:translate-x-0.5" />
                      </Link>
                    )}
                    <div className="text-xxs font-mono text-slate-400 mt-0.5">{row.learnerCode}</div>
                    {(row.division || row.department) && (
                      <div className="text-xxs text-slate-400 mt-0.5">
                        {[row.division, row.department].filter(Boolean).join(' · ')}
                      </div>
                    )}
                  </td>
                  <td className="p-3 select-all">
                    <div className="font-bold text-slate-700 text-xs">{row.courseTitle || '—'}</div>
                    <div className="text-xxs font-mono text-slate-400 mt-0.5">
                      {[row.courseCode, row.assignmentNo ? `Assign: ${row.assignmentNo}` : null]
                        .filter(Boolean)
                        .join(' · ')}
                    </div>
                  </td>
                  <td className="p-3 text-center">
                    <StatusBadge tone="danger" size="xxs">
                      {tf(REPORT_LABELS.daysCount, row.daysOverdue)}
                    </StatusBadge>
                  </td>
                  <td className="p-3">
                    <ProgressBar value={row.progress} completed={false} />
                  </td>
                  <td className="p-3 pr-5 text-slate-500 text-xxs font-semibold">
                    {row.dueDate ? tf(REPORT_LABELS.duePrefix, formatDate(row.dueDate)) : '—'}
                  </td>
                </tr>
              ))}
              {filteredOverdueRows.length === 0 && (
                <tr>
                  <td colSpan={5} className="p-6 text-center text-slate-400 text-xs font-medium">
                    {t(REPORT_LABELS.compNoOverdueRows)}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Footer showing row count & infinite scroll status */}
        {filteredOverdueRows.length > 0 && (
          <div className="border-t border-slate-100 bg-slate-50/50 px-5 py-2.5 text-xs text-slate-500 font-medium flex items-center justify-between shrink-0">
            <span>
              {t(REPORT_LABELS.rowsShowing)} <strong className="text-slate-800 tabular-nums">{visibleOverdueRows.length}</strong> {t(REPORT_LABELS.rowsOf)}{' '}
              <strong className="text-slate-800 tabular-nums">{filteredOverdueRows.length}</strong> {t(REPORT_LABELS.rowsUnit)}
            </span>
            {visibleOverdueRows.length < filteredOverdueRows.length && (
              <span className="text-xxs text-indigo-600 font-semibold flex items-center gap-1">
                {t(UI_LABELS.scrollToLoadMore)}
              </span>
            )}
          </div>
        )}
      </Card>
    </div>
  )
}

