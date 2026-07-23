import {
  Bar,
  BarChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { LearningActivityPoint } from './dashboardApi'
import { BRAND, tooltipStyle, axisStyle } from '../../lib/chartTheme'

export function LearningActivityChart({ data }: { data: LearningActivityPoint[] }) {
  if (!data || data.length === 0) {
    return <EmptyChart label="ไม่มีข้อมูลกิจกรรมการเรียนใน 6 เดือนล่าสุด" />
  }
  return (
    <ResponsiveContainer width="100%" height={220}>
      <BarChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
        <XAxis dataKey="month" tickLine={false} axisLine={false} tick={axisStyle} />
        <YAxis tickLine={false} axisLine={false} tick={axisStyle} allowDecimals={false} />
        <Tooltip cursor={{ fill: 'rgba(79,70,229,0.08)' }} contentStyle={tooltipStyle} />
        <Bar dataKey="sessions" fill={BRAND} radius={[4, 4, 0, 0]} maxBarSize={36} />
      </BarChart>
    </ResponsiveContainer>
  )
}

function EmptyChart({ label }: { label: string }) {
  return (
    <div className="flex items-center justify-center h-[220px] text-xs text-slate-400 font-medium">
      {label}
    </div>
  )
}

