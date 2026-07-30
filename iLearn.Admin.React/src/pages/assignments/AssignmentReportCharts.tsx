import {
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
} from 'recharts'
import { STATUS_COLORS } from '../../lib/chartTheme'
import { formatPercent } from '../../lib/format'
import { ASSIGNMENT_LABELS, LEARNER_STATUS_KEYS, learnerStatusLabel, t } from '../../lib/labels'

type StatusEntry = { status: string; label: string; count: number }

const assignmentReportTooltipStyle = {
  background: '#ffffff',
  border: '1px solid #cbd5e1',
  borderRadius: 6,
  boxShadow: '0 10px 20px rgba(15, 23, 42, 0.12)',
  color: '#0f172a',
  fontSize: 12,
  padding: '8px 10px',
}

const assignmentReportTooltipLabelStyle = {
  color: '#334155',
  fontWeight: 700,
}

const assignmentReportTooltipItemStyle = {
  color: '#475569',
  fontWeight: 600,
}

type StatusDonutProps = {
  data: StatusEntry[]
  completionRate?: number
  activeStatus?: string
}

export function StatusDonut({ data, completionRate, activeStatus }: StatusDonutProps) {
  const filtered = data.filter(d => d.count > 0)
  if (filtered.length === 0) {
    return <EmptyChart label={t(ASSIGNMENT_LABELS.noEnrollments)} />
  }
  const total = filtered.reduce((s, d) => s + d.count, 0)
  const hasActive = activeStatus && activeStatus !== 'All'

  return (
    <div className="flex flex-col items-center gap-3 [&_.recharts-surface_*:focus]:outline-none [&_.recharts-surface:focus]:outline-none [&_.recharts-wrapper:focus]:outline-none">
      <div className="relative w-full">
        <ResponsiveContainer width="100%" height={200}>
          <PieChart accessibilityLayer={false}>
            <Tooltip
              contentStyle={assignmentReportTooltipStyle}
              cursor={false}
              isAnimationActive={false}
              itemStyle={assignmentReportTooltipItemStyle}
              labelStyle={assignmentReportTooltipLabelStyle}
              formatter={(value, name) => [`${value} (${formatPercent(total > 0 ? (Number(value) / total) * 100 : 0)})`, name]}
            />
            <Pie
              data={filtered}
              dataKey="count"
              nameKey="label"
              cx="50%"
              cy="50%"
              innerRadius={56}
              outerRadius={84}
              paddingAngle={2}
              stroke="#fff"
              strokeWidth={2}
              isAnimationActive={false}
              rootTabIndex={-1}
            >
              {filtered.map((entry) => (
                <Cell
                  key={entry.status}
                  fill={STATUS_COLORS[entry.status] ?? '#64748b'}
                  fillOpacity={hasActive && entry.status !== activeStatus ? 0.35 : 1}
                />
              ))}
            </Pie>
          </PieChart>
        </ResponsiveContainer>
        {/* Center label */}
        <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
          {completionRate !== undefined ? (
            <>
              <span className="text-xl font-bold text-slate-800 tabular-nums leading-none">{formatPercent(completionRate)}</span>
              <span className="text-xxs font-semibold text-slate-400 mt-0.5">{t(ASSIGNMENT_LABELS.completion)}</span>
            </>
          ) : (
            <>
              <span className="text-xl font-bold text-slate-800 tabular-nums leading-none">{total}</span>
              <span className="text-xxs font-semibold text-slate-400 mt-0.5">{t(ASSIGNMENT_LABELS.enrollments)}</span>
            </>
          )}
        </div>
      </div>
      {/* Legend */}
      <ul className="flex flex-wrap gap-x-4 gap-y-1.5 text-xs justify-center">
        {filtered.map((d) => {
          const pctVal = total > 0 ? (d.count / total) * 100 : 0
          return (
            <li key={d.status} className="flex items-center gap-1.5">
              <span
                className="inline-block h-2.5 w-2.5 rounded-sm shrink-0"
                style={{ background: STATUS_COLORS[d.status] ?? '#64748b' }}
              />
              <span className="text-slate-600">{d.label}</span>
              <span className="font-bold text-slate-800 tabular-nums">{d.count}</span>
              <span className="text-slate-400 tabular-nums">({formatPercent(pctVal)})</span>
            </li>
          )
        })}
      </ul>
    </div>
  )
}

/** Build StatusDonut data from raw learner rows */
export function buildStatusData(learners: Array<{ status: string }>): StatusEntry[] {
  const counts = new Map<string, number>()
  learners.forEach(l => {
    counts.set(l.status, (counts.get(l.status) ?? 0) + 1)
  })
  return LEARNER_STATUS_KEYS.map(key => ({
    status: key,
    label: learnerStatusLabel(key),
    count: counts.get(key) ?? 0,
  }))
}

function EmptyChart({ label }: { label: string }) {
  return (
    <div className="flex h-50 items-center justify-center text-xs text-slate-400">
      {label}
    </div>
  )
}
