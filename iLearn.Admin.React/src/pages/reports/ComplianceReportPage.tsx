import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Users,
  BookOpen,
  CheckCircle,
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
import { SectionHeader } from '../../components/ui/SectionHeader'
import { LoadingState } from '../../components/ui/LoadingState'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { AppButton } from '../../components/ui/AppButton'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatDate, formatPercent, formatNumber } from '../../lib/format'
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
      .catch(() => toast.error('Failed to load compliance report'))
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
    // Sort division completion rate worst first (ascending)
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

  const groupRows = useMemo(() => {
    if (!data) return []
    return activeTab === 'division' ? data.byDivision : data.byDepartment
  }, [data, activeTab])

  const handleExportCsv = () => {
    if (!data || data.overdueRows.length === 0) {
      toast.info('No overdue rows to export')
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
    return <LoadingState label="Loading compliance report..." />
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
      <SectionHeader icon={TrendingUp}>Compliance & Overdue Report</SectionHeader>

      {/* KPI Tiles */}
      <section className="grid grid-cols-2 lg:grid-cols-5 border border-slate-200 rounded-lg bg-white shadow-xs divide-y lg:divide-y-0 lg:divide-x divide-slate-100 overflow-hidden">
        <div className="p-4 flex flex-col gap-1">
          <div className="flex items-center gap-1.5 text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
            <Users className="h-3.5 w-3.5 text-slate-400" aria-hidden="true" />
            Total Learners
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight">
            {formatNumber(data.totalLearners)}
          </div>
        </div>

        <div className="p-4 flex flex-col gap-1">
          <div className="flex items-center gap-1.5 text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
            <BookOpen className="h-3.5 w-3.5 text-slate-400" aria-hidden="true" />
            Open Enrollments
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight">
            {formatNumber(data.openEnrollments)}
          </div>
        </div>

        <div className="p-4 flex flex-col gap-1">
          <div className="flex items-center gap-1.5 text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
            <CheckCircle className="h-3.5 w-3.5 text-emerald-500" aria-hidden="true" />
            Completed
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight">
            {formatNumber(data.completedEnrollments)}
          </div>
        </div>

        <div className="p-4 flex flex-col gap-1">
          <div className="flex items-center gap-1.5 text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
            <AlertTriangle
              className={`h-3.5 w-3.5 ${data.overdueEnrollments > 0 ? 'text-rose-500' : 'text-slate-400'}`}
              aria-hidden="true"
            />
            Overdue
          </div>
          <div
            className={`text-2xl font-extrabold tabular-nums leading-tight ${
              data.overdueEnrollments > 0 ? 'text-red-600' : 'text-slate-800'
            }`}
          >
            {formatNumber(data.overdueEnrollments)}
          </div>
          {data.overdueLearners > 0 && (
            <div className="text-xxs text-rose-600 font-semibold mt-0.5">
              Across {formatNumber(data.overdueLearners)} learners
            </div>
          )}
        </div>

        <div className="p-4 flex flex-col gap-1">
          <div className="flex items-center gap-1.5 text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
            <Percent className="h-3.5 w-3.5 text-indigo-500" aria-hidden="true" />
            Compliance Rate
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight">
            {formatPercent(data.complianceRate)}
          </div>
        </div>
      </section>

      {/* Charts & Group breakdown */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 items-start">
        {/* Division Completion Rate Chart */}
        <Card title="Completion by Division" icon={TrendingUp}>
          <div className="p-4">
            {chartData.length === 0 ? (
              <div className="text-center py-8 text-slate-400 text-xs font-medium">
                No division data available
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
                <p className="text-xxs text-slate-400 text-center mt-2">
                  Sorted worst-first compliance rate
                </p>
              </>
            )}
          </div>
        </Card>

        {/* Division/Department Summary Table */}
        <Card
          title={
            <div className="flex items-center gap-3">
              <span>Overview Rates</span>
              <SegmentedToggle
                options={[
                  { value: 'division', label: 'By Division' },
                  { value: 'department', label: 'By Department' },
                ]}
                value={activeTab}
                onChange={setActiveTab}
                variant="segment"
              />
            </div>
          }
        >
          <div className="overflow-x-auto custom-scrollbar">
            <table className="w-full text-left text-sm border-collapse">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                  <th className="p-3 pl-5">{activeTab === 'division' ? 'Division' : 'Department'}</th>
                  <th className="p-3 text-center">Learners</th>
                  <th className="p-3 text-center">Enrollments</th>
                  <th className="p-3 text-center">Completed</th>
                  <th className="p-3 text-center">Overdue</th>
                  <th className="p-3 pr-5">Completion</th>
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
                      No records found
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
        title="Overdue Enrollments Details"
        actions={
          data.overdueRows.length > 0 && (
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
            searchPlaceholder="Search overdue learner name, code, division, course..."
          />
        </div>

        <div className="overflow-x-auto custom-scrollbar">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                <th className="p-3 pl-5">Learner</th>
                <th className="p-3">Course Code & Title</th>
                <th className="p-3 text-center">Days Overdue</th>
                <th className="p-3">Progress</th>
                <th className="p-3 pr-5">Timeline</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {visibleOverdueRows.map((row, idx) => (
                <tr key={`${row.learnerCode}-${row.courseCode}-${idx}`} className="hover:bg-slate-50/50 transition duration-100">
                  <td className="p-3 pl-5">
                    {row.learnerName ? (
                      <Link
                        to={`/reports/transcript?code=${row.learnerCode}`}
                        className="font-bold text-indigo-600 hover:text-indigo-800 text-xs sm:text-[13px] inline-flex items-center gap-1"
                      >
                        {row.learnerName}
                        <ChevronRight className="h-3 w-3" />
                      </Link>
                    ) : (
                      <div className="font-bold text-slate-800 text-xs sm:text-[13px]">
                        {row.learnerCode}
                      </div>
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
                      {row.daysOverdue} days
                    </StatusBadge>
                  </td>
                  <td className="p-3">
                    <ProgressBar value={row.progress} completed={false} />
                  </td>
                  <td className="p-3 pr-5 text-slate-500 text-xxs font-semibold">
                    {row.dueDate ? `Due: ${formatDate(row.dueDate)}` : '—'}
                  </td>
                </tr>
              ))}
              {filteredOverdueRows.length === 0 && (
                <tr>
                  <td colSpan={5} className="p-6 text-center text-slate-400 text-xs font-medium">
                    No overdue enrollments found
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {filteredOverdueRows.length > visibleOverdueRows.length && (
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
