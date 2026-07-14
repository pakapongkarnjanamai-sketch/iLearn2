import {
  Bar,
  BarChart,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { CategoryMixPoint, LearningActivityPoint, TaskStatusPoint } from './dashboardApi'
import { formatPercent } from '../../lib/format'
import { BRAND, STATUS_COLORS, tooltipStyle, axisStyle } from '../../lib/chartTheme'

export function LearningActivityChart({ data }: { data: LearningActivityPoint[] }) {
  if (!data || data.length === 0) {
    return <EmptyChart label="No learning activity in the last 6 months" />
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

export function TaskStatusPie({ data }: { data: TaskStatusPoint[] }) {
  const filtered = (data ?? []).filter((d) => d.count > 0)
  if (filtered.length === 0) {
    return <EmptyChart label="No learning tasks yet" />
  }
  return (
    <ResponsiveContainer width="100%" height={220}>
      <PieChart>
        <Tooltip contentStyle={tooltipStyle} />
        <Pie
          data={filtered}
          dataKey="count"
          nameKey="status"
          cx="50%"
          cy="50%"
          innerRadius={56}
          outerRadius={84}
          paddingAngle={2}
          stroke="#fff"
          strokeWidth={2}
        >
          {filtered.map((entry) => (
            <Cell key={entry.status} fill={STATUS_COLORS[entry.status] ?? '#64748b'} />
          ))}
        </Pie>
      </PieChart>
    </ResponsiveContainer>
  )
}

export function CategoryMixChart({
  data,
  onSelect,
}: {
  data: CategoryMixPoint[]
  onSelect?: (categoryId: number | null) => void
}) {
  if (!data || data.length === 0) {
    return <EmptyChart label="No category data" />
  }
  return (
    <ResponsiveContainer width="100%" height={Math.max(160, data.length * 32 + 24)}>
      <BarChart data={data} layout="vertical" margin={{ top: 4, right: 16, left: 8, bottom: 0 }}>
        <XAxis type="number" tickLine={false} axisLine={false} tick={axisStyle} allowDecimals={false} />
        <YAxis
          type="category"
          dataKey="categoryName"
          tickLine={false}
          axisLine={false}
          tick={axisStyle}
          width={120}
        />
        <Tooltip cursor={{ fill: 'rgba(79,70,229,0.08)' }} contentStyle={tooltipStyle} />
        <Bar
          dataKey="courseCount"
          fill={BRAND}
          radius={[0, 4, 4, 0]}
          maxBarSize={20}
          onClick={(payload) => {
            const entry = (payload as { payload?: CategoryMixPoint })?.payload
            onSelect?.(entry?.categoryId ?? null)
          }}
          cursor={onSelect ? 'pointer' : undefined}
        />
      </BarChart>
    </ResponsiveContainer>
  )
}

function EmptyChart({ label }: { label: string }) {
  return (
    <div className="flex items-center justify-center h-[220px] text-xs text-slate-400">
      {label}
    </div>
  )
}

export function TaskStatusLegend({ data }: { data: TaskStatusPoint[] }) {
  const total = (data ?? []).reduce((s, d) => s + d.count, 0)
  return (
    <ul className="flex flex-wrap gap-x-4 gap-y-1.5 text-xs">
      {(data ?? []).map((d) => {
        const pctVal = total > 0 ? (d.count / total) * 100 : 0
        const pctStr = formatPercent(pctVal)
        return (
          <li key={d.status} className="flex items-center gap-1.5">
            <span
              className="inline-block h-2.5 w-2.5 rounded-sm shrink-0"
              style={{ background: STATUS_COLORS[d.status] ?? '#64748b' }}
            />
            <span className="text-slate-600">{d.status}</span>
            <span className="font-bold text-slate-800 tabular-nums">{d.count}</span>
            <span className="text-slate-400 tabular-nums">({pctStr})</span>
          </li>
        )
      })}
    </ul>
  )
}
