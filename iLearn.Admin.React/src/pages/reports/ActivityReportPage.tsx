import { useEffect, useState } from 'react'
import {
  Activity,
  Download,
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
import { SectionHeader } from '../../components/ui/SectionHeader'
import { LoadingState } from '../../components/ui/LoadingState'
import { AppButton } from '../../components/ui/AppButton'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatNumber } from '../../lib/format'
import { exportRowsAsCsv } from '../../lib/csvExport'
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
      .catch(() => toast.error('Failed to load training activity report'))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [months])

  const handleExportCsv = () => {
    if (!data || data.months.length === 0) {
      toast.info('No activity records to export')
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
    exportRowsAsCsv(`training-activity-report-${stamp}.csv`, header, body)
  }

  if (loading) {
    return <LoadingState label="Loading training activity report..." />
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
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <SectionHeader icon={Activity}>Training Activity Report</SectionHeader>

        <SegmentedToggle
          options={[
            { value: 6, label: 'Last 6 Months' },
            { value: 12, label: 'Last 12 Months' },
            { value: 24, label: 'Last 24 Months' },
          ]}
          value={months}
          onChange={(val) => setMonths(Number(val))}
          variant="segment"
        />
      </div>

      {/* Graphs */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 items-start">
        {/* completions & newEnrollments */}
        <Card title="Completions & New Enrollments" icon={Activity}>
          <div className="p-4">
            {data.months.length === 0 ? (
              <div className="text-center py-12 text-slate-400 text-xs font-medium">
                No activity data available
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={data.months} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                  <XAxis dataKey="month" tickLine={false} axisLine={false} tick={axisStyle} />
                  <YAxis tickLine={false} axisLine={false} tick={axisStyle} allowDecimals={false} />
                  <Tooltip cursor={{ fill: 'rgba(79,70,229,0.05)' }} contentStyle={tooltipStyle} />
                  <Legend verticalAlign="top" height={36} wrapperStyle={{ fontSize: 11 }} />
                  <Bar name="Completions" dataKey="completions" fill={BRAND} radius={[4, 4, 0, 0]} maxBarSize={24} />
                  <Bar name="New Enrollments" dataKey="newEnrollments" fill="#10b981" radius={[4, 4, 0, 0]} maxBarSize={24} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </Card>

        {/* Active Learners */}
        <Card title="Monthly Active Learners" icon={Activity}>
          <div className="p-4">
            {data.months.length === 0 ? (
              <div className="text-center py-12 text-slate-400 text-xs font-medium">
                No activity data available
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={data.months} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                  <XAxis dataKey="month" tickLine={false} axisLine={false} tick={axisStyle} />
                  <YAxis tickLine={false} axisLine={false} tick={axisStyle} allowDecimals={false} />
                  <Tooltip cursor={{ fill: 'rgba(245,158,11,0.05)' }} contentStyle={tooltipStyle} />
                  <Legend verticalAlign="top" height={36} wrapperStyle={{ fontSize: 11 }} />
                  <Bar name="Active Learners" dataKey="activeLearners" fill="#f59e0b" radius={[4, 4, 0, 0]} maxBarSize={28} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </Card>
      </div>

      {/* Monthly Activity Table */}
      <Card
        title="Monthly Breakdown"
        actions={
          data.months.length > 0 && (
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
        <div className="overflow-x-auto custom-scrollbar">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none">
                <th className="p-3 pl-5">Month</th>
                <th className="p-3 text-center">Completions</th>
                <th className="p-3 text-center">New Enrollments</th>
                <th className="p-3 text-center">Active Learners</th>
                <th className="p-3 pr-5 text-right">Total Hours Played</th>
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
                    {formatNumber(row.totalHoursPlayed, 1)} h
                  </td>
                </tr>
              ))}
              {data.months.length === 0 && (
                <tr>
                  <td colSpan={5} className="p-6 text-center text-slate-400 text-xs font-medium">
                    No monthly breakdown data found
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
