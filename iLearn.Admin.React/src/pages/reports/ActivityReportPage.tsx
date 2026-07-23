import { useEffect, useMemo, useState } from 'react'
import {
  Activity,
  Download,
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
    <div className="h-full flex flex-col min-h-0 gap-5 overflow-auto custom-scrollbar">
      {/* KPI Summary Grid */}
      <section className="grid grid-cols-2 lg:grid-cols-4 gap-4 shrink-0">
        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              เรียนสำเร็จทั้งหมด
            </span>
            <CheckCircle2 className="h-4 w-4 text-emerald-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-emerald-600 tabular-nums leading-tight mt-1">
            {formatNumber(periodStats.totalCompletions)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">จำนวนครั้งที่เรียนจบในรอบช่วงเวลา</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              ลงทะเบียนคอร์สใหม่
            </span>
            <BookOpen className="h-4 w-4 text-indigo-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-indigo-600 tabular-nums leading-tight mt-1">
            {formatNumber(periodStats.totalNewEnrollments)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">จำนวนการมอบหมายคอร์สใหม่</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              ผู้เรียนแอคทีฟเฉลี่ย
            </span>
            <Users className="h-4 w-4 text-amber-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-amber-600 tabular-nums leading-tight mt-1">
            {formatNumber(periodStats.avgActiveLearners)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">ผู้เรียนที่มีกิจกรรมเข้าเรียน / เดือน</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              เวลาเรียนสะสม
            </span>
            <Clock className="h-4 w-4 text-blue-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight mt-1">
            {formatNumber(periodStats.totalHoursPlayed, 1)} ชม.
          </div>
          <span className="text-xxs text-slate-400 font-medium">ชั่วโมงการเรียนรวมทั้งหมด</span>
        </Card>
      </section>

      {/* Graphs */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 items-start shrink-0">
        {/* completions & newEnrollments */}
        <Card title="Completions & New Enrollments / สถิติการเรียนจบและการลงทะเบียนใหม่" icon={Activity}>
          <div className="p-4">
            {data.months.length === 0 ? (
              <div className="text-center py-12 text-slate-400 text-xs font-medium">
                ไม่มีข้อมูลกิจกรรม
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={data.months} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                  <XAxis dataKey="month" tickLine={false} axisLine={false} tick={axisStyle} />
                  <YAxis tickLine={false} axisLine={false} tick={axisStyle} allowDecimals={false} />
                  <Tooltip cursor={{ fill: 'rgba(79,70,229,0.05)' }} contentStyle={tooltipStyle} />
                  <Legend verticalAlign="top" height={36} wrapperStyle={{ fontSize: 11 }} />
                  <Bar name="เรียนสำเร็จ (Completions)" dataKey="completions" fill={BRAND} radius={[4, 4, 0, 0]} maxBarSize={24} />
                  <Bar name="ลงทะเบียนใหม่ (New Enrollments)" dataKey="newEnrollments" fill="#10b981" radius={[4, 4, 0, 0]} maxBarSize={24} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </Card>

        {/* Active Learners */}
        <Card title="Monthly Active Learners / จำนวนผู้เรียนที่แอคทีฟรายเดือน" icon={Activity}>
          <div className="p-4">
            {data.months.length === 0 ? (
              <div className="text-center py-12 text-slate-400 text-xs font-medium">
                ไม่มีข้อมูลกิจกรรม
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={data.months} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                  <XAxis dataKey="month" tickLine={false} axisLine={false} tick={axisStyle} />
                  <YAxis tickLine={false} axisLine={false} tick={axisStyle} allowDecimals={false} />
                  <Tooltip cursor={{ fill: 'rgba(245,158,11,0.05)' }} contentStyle={tooltipStyle} />
                  <Legend verticalAlign="top" height={36} wrapperStyle={{ fontSize: 11 }} />
                  <Bar name="ผู้เรียนแอคทีฟ (Active Learners)" dataKey="activeLearners" fill="#f59e0b" radius={[4, 4, 0, 0]} maxBarSize={28} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </Card>
      </div>

      {/* Monthly Activity Table */}
      <Card
        title="Training Activity Breakdown / รายละเอียดสถิติการเรียนรู้รายเดือน"
        className="flex-1 flex flex-col min-h-64"
        bodyClassName="flex-1 flex flex-col min-h-0"
        actions={
          <div className="flex items-center gap-3">
            <SegmentedToggle
              options={[
                { value: 6, label: '6 เดือนล่าสุด' },
                { value: 12, label: '12 เดือนล่าสุด' },
                { value: 24, label: '24 เดือนล่าสุด' },
              ]}
              value={months}
              onChange={(val) => setMonths(Number(val))}
              variant="segment"
            />
            {data.months.length > 0 && (
              <AppButton
                onClick={handleExportCsv}
                icon={Download}
                variant="secondary"
                size="sm"
              >
                Export CSV
              </AppButton>
            )}
          </div>
        }
      >
        <div className="flex-1 overflow-x-auto overflow-y-auto min-h-0 custom-scrollbar">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none sticky top-0 z-10 shadow-xs">
                <th className="p-3 pl-5">เดือน (Month)</th>
                <th className="p-3 text-center">เรียนสำเร็จ (Completions)</th>
                <th className="p-3 text-center">ลงทะเบียนใหม่ (New Enrollments)</th>
                <th className="p-3 text-center">ผู้เรียนแอคทีฟ (Active Learners)</th>
                <th className="p-3 pr-5 text-right">เวลาเรียนสะสม (Total Hours)</th>
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
                    {formatNumber(row.totalHoursPlayed, 1)} ชม.
                  </td>
                </tr>
              ))}
              {data.months.length === 0 && (
                <tr>
                  <td colSpan={5} className="p-6 text-center text-slate-400 text-xs font-medium">
                    ไม่พบข้อมูลกิจกรรมรายเดือน
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Footer showing row count */}
        {data.months.length > 0 && (
          <div className="border-t border-slate-100 bg-slate-50/50 px-5 py-2.5 text-xs text-slate-500 font-medium flex items-center justify-between shrink-0">
            <span>
              แสดงข้อมูลทั้งหมด <strong className="text-slate-800 tabular-nums">{data.months.length}</strong> เดือน
            </span>
          </div>
        )}
      </Card>
    </div>
  )
}

