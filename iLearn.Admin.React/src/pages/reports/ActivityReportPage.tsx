import { useEffect, useMemo, useState } from 'react'
import {
  Activity,
  CheckCircle2,
  BookOpen,
  Users,
  Clock,
} from 'lucide-react'
import {
  Bar,
  BarChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
  Legend,
} from 'recharts'
import { Card } from '../../components/ui/Card'
import { ExportMenu } from '../../components/ui/ExportMenu'
import { LoadingState } from '../../components/ui/LoadingState'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatNumber } from '../../lib/format'
import { REPORT_LABELS, t, tf } from '../../lib/labels'
import { exportRows, type ExportFormat } from '../../lib/tableExport'
import { toast } from '../../lib/toast'
import { BRAND, tooltipStyle, axisStyle } from '../../lib/chartTheme'
import type { ActivityReportDto } from './reportTypes'

export function ActivityReportPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<ActivityReportDto | null>(null)
  const [months, setMonths] = useState<number>(12)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    fetchWithAccessControl<{ success: boolean; data: ActivityReportDto }>(`Reports/activity?months=${months}`)
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
  }, [months])

  const periodStats = useMemo(() => {
    if (!data || data.months.length === 0) {
      return {
        totalCompletions: 0,
        totalNewEnrollments: 0,
        avgActiveLearners: 0,
        totalHoursPlayed: 0,
      }
    }
    const totalCompletions = data.months.reduce((acc, r) => acc + r.completions, 0)
    const totalNewEnrollments = data.months.reduce((acc, r) => acc + r.newEnrollments, 0)
    const avgActiveLearners = Math.round(
      data.months.reduce((acc, r) => acc + r.activeLearners, 0) / data.months.length
    )
    const totalHoursPlayed = data.months.reduce((acc, r) => acc + r.totalHoursPlayed, 0)

    return {
      totalCompletions,
      totalNewEnrollments,
      avgActiveLearners,
      totalHoursPlayed,
    }
  }, [data])

  const handleExport = async (format: ExportFormat) => {
    if (!data || data.months.length === 0) {
      toast.info(t(REPORT_LABELS.noRowsToExport))
      return
    }
    const header = [
      'Month',
      'Completions',
      'New Enrollments',
      'Active Learners',
      'Total Hours Played',
    ]
    const body = data.months.map((r) => [
      r.month,
      r.completions,
      r.newEnrollments,
      r.activeLearners,
      formatNumber(r.totalHoursPlayed, 1),
    ])
    const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    await exportRows(format, `training-activity-report-${stamp}`, header, body)
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
      {/* KPI Summary Grid */}
      <section className="grid grid-cols-2 lg:grid-cols-4 gap-4 shrink-0">
        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              {t(REPORT_LABELS.actTotalCompletions)}
            </span>
            <CheckCircle2 className="h-4 w-4 text-emerald-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-emerald-600 tabular-nums leading-tight mt-1">
            {formatNumber(periodStats.totalCompletions)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">{t(REPORT_LABELS.actTotalCompletionsSub)}</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              {t(REPORT_LABELS.actNewEnrollments)}
            </span>
            <BookOpen className="h-4 w-4 text-indigo-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-indigo-600 tabular-nums leading-tight mt-1">
            {formatNumber(periodStats.totalNewEnrollments)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">{t(REPORT_LABELS.actNewEnrollmentsSub)}</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              {t(REPORT_LABELS.actActiveLearners)}
            </span>
            <Users className="h-4 w-4 text-amber-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-amber-600 tabular-nums leading-tight mt-1">
            {formatNumber(periodStats.avgActiveLearners)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">{t(REPORT_LABELS.actActiveLearnersSub)}</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              {t(REPORT_LABELS.actTotalHours)}
            </span>
            <Clock className="h-4 w-4 text-blue-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight mt-1">
            {formatNumber(periodStats.totalHoursPlayed, 1)} {t(REPORT_LABELS.hoursUnitShort)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">{t(REPORT_LABELS.actTotalHoursSub)}</span>
        </Card>
      </section>

      {/* Graphs */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 items-start shrink-0">
        {/* completions & newEnrollments */}
        <Card title={t(REPORT_LABELS.actTrendTitle)} icon={Activity}>
          <div className="p-4">
            {data.months.length === 0 ? (
              <div className="text-center py-12 text-slate-400 text-xs font-medium">
                {t(REPORT_LABELS.actNoData)}
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={data.months} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                  <XAxis dataKey="month" tickLine={false} axisLine={false} tick={axisStyle} />
                  <YAxis tickLine={false} axisLine={false} tick={axisStyle} allowDecimals={false} />
                  <Tooltip cursor={{ fill: 'rgba(79,70,229,0.05)' }} contentStyle={tooltipStyle} />
                  <Legend verticalAlign="top" height={36} wrapperStyle={{ fontSize: 11 }} />
                  <Bar name={t(REPORT_LABELS.actLegendCompletions)} dataKey="completions" fill={BRAND} radius={[4, 4, 0, 0]} maxBarSize={24} />
                  <Bar name={t(REPORT_LABELS.actLegendNewEnrollments)} dataKey="newEnrollments" fill="#10b981" radius={[4, 4, 0, 0]} maxBarSize={24} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </Card>

        {/* Active Learners */}
        <Card title={t(REPORT_LABELS.actActiveMonthlyTitle)} icon={Activity}>
          <div className="p-4">
            {data.months.length === 0 ? (
              <div className="text-center py-12 text-slate-400 text-xs font-medium">
                {t(REPORT_LABELS.actNoData)}
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={data.months} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                  <XAxis dataKey="month" tickLine={false} axisLine={false} tick={axisStyle} />
                  <YAxis tickLine={false} axisLine={false} tick={axisStyle} allowDecimals={false} />
                  <Tooltip cursor={{ fill: 'rgba(245,158,11,0.05)' }} contentStyle={tooltipStyle} />
                  <Legend verticalAlign="top" height={36} wrapperStyle={{ fontSize: 11 }} />
                  <Bar name={t(REPORT_LABELS.actLegendActive)} dataKey="activeLearners" fill="#f59e0b" radius={[4, 4, 0, 0]} maxBarSize={28} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </Card>
      </div>

      {/* Monthly Activity Table */}
      <Card
        title={t(REPORT_LABELS.actMonthlyDetails)}
        className="flex-1 flex flex-col min-h-64"
        bodyClassName="flex-1 flex flex-col min-h-0"
        actions={
          <div className="flex items-center gap-3">
            <SegmentedToggle
              options={[
                { value: 6, label: tf(REPORT_LABELS.lastNMonths, 6) },
                { value: 12, label: tf(REPORT_LABELS.lastNMonths, 12) },
                { value: 24, label: tf(REPORT_LABELS.lastNMonths, 24) },
              ]}
              value={months}
              onChange={(val) => setMonths(Number(val))}
              variant="segment"
            />
            <ExportMenu hasRows={data.months.length > 0} onExport={handleExport} />
          </div>
        }
      >
        <div className="flex-1 overflow-x-auto overflow-y-auto min-h-0 custom-scrollbar">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none sticky top-0 z-10 shadow-xs">
                <th className="p-3 pl-5">{t(REPORT_LABELS.actColMonth)}</th>
                <th className="p-3 text-center">{t(REPORT_LABELS.actLegendCompletions)}</th>
                <th className="p-3 text-center">{t(REPORT_LABELS.actLegendNewEnrollments)}</th>
                <th className="p-3 text-center">{t(REPORT_LABELS.actLegendActive)}</th>
                <th className="p-3 pr-5 text-right">{t(REPORT_LABELS.actColTotalHours)}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {[...data.months].reverse().map((row) => (
                <tr key={row.month} className="hover:bg-slate-50/50 transition duration-100">
                  <td className="p-3 pl-5 text-xs font-bold text-slate-800">
                    {row.month}
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {formatNumber(row.completions)}
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {formatNumber(row.newEnrollments)}
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {formatNumber(row.activeLearners)}
                  </td>
                  <td className="p-3 pr-5 text-right text-xs font-semibold tabular-nums">
                    {formatNumber(row.totalHoursPlayed, 1)} {t(REPORT_LABELS.hoursUnitShort)}
                  </td>
                </tr>
              ))}
              {data.months.length === 0 && (
                <tr>
                  <td colSpan={5} className="p-6 text-center text-slate-400 text-xs font-medium">
                    {t(REPORT_LABELS.actNoMonthlyData)}
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

