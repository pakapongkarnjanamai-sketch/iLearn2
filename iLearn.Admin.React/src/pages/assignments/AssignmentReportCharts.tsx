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
import { STATUS_COLORS, BRAND, tooltipStyle, axisStyle } from '../../lib/chartTheme'
import { formatPercent } from '../../lib/format'
import { LEARNER_STATUS_KEYS, learnerStatusLabel } from '../../lib/labels'

type StatusEntry = { status: string; label: string; count: number }

type StatusDonutProps = {
  data: StatusEntry[]
  completionRate: number
  activeStatus?: string
}

export function StatusDonut({ data, completionRate, activeStatus }: StatusDonutProps) {
  const filtered = data.filter(d => d.count > 0)
  if (filtered.length === 0) {
    return <EmptyChart label="No enrollments" />
  }
  const total = filtered.reduce((s, d) => s + d.count, 0)
  const hasActive = activeStatus && activeStatus !== 'All'

  return (
    <div className="flex flex-col items-center gap-3">
      <div className="relative w-full">
        <ResponsiveContainer width="100%" height={200}>
          <PieChart>
            <Tooltip
              contentStyle={tooltipStyle}
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
          <span className="text-xl font-bold text-slate-800 tabular-nums leading-none">{formatPercent(completionRate)}</span>
          <span className="text-xxs font-semibold text-slate-400 mt-0.5">Completion</span>
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

type CourseBarEntry = {
  assignmentRuleId: number
  courseCode: string
  courseTitle: string
  pct: number
  completedLearners: number
  totalLearners: number
  isCourseDeleted: boolean
}

type CourseCompletionBarsProps = {
  data: CourseBarEntry[]
  activeCourse?: 'All' | number
}

export function CourseCompletionBars({ data, activeCourse }: CourseCompletionBarsProps) {
  if (!data || data.length === 0) {
    return <EmptyChart label="No course data" />
  }

  const sorted = [...data].sort((a, b) => a.pct - b.pct)
  const hasActive = activeCourse !== undefined && activeCourse !== 'All'

  return (
    <div className="flex flex-col gap-2">
      <ResponsiveContainer width="100%" height={Math.max(160, sorted.length * 32 + 24)}>
        <BarChart data={sorted} layout="vertical" margin={{ top: 4, right: 16, left: 8, bottom: 0 }}>
          <XAxis type="number" domain={[0, 100]} tickLine={false} axisLine={false} tick={axisStyle} unit="%" />
          <YAxis
            type="category"
            dataKey="courseCode"
            tickLine={false}
            axisLine={false}
            tick={axisStyle}
            width={110}
          />
          <Tooltip
            cursor={{ fill: 'rgba(79,70,229,0.08)' }}
            contentStyle={tooltipStyle}
            formatter={(_value, _name, props) => {
              const entry = (props as { payload?: CourseBarEntry })?.payload
              if (!entry) return ['', '']
              return [`${entry.completedLearners}/${entry.totalLearners} Completed`, entry.courseTitle]
            }}
            labelFormatter={() => ''}
          />
          <Bar
            dataKey="pct"
            radius={[0, 4, 4, 0]}
            maxBarSize={20}
          >
            {sorted.map((entry) => (
              <Cell
                key={entry.assignmentRuleId}
                fill={entry.isCourseDeleted ? '#94a3b8' : BRAND}
                fillOpacity={hasActive && entry.assignmentRuleId !== activeCourse ? 0.35 : 1}
              />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
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

/** Build CourseCompletionBars data from course summary rows */
export function buildCourseBarData(courses: Array<{
  assignmentRuleId: number
  courseCode: string
  courseTitle: string
  completedLearners: number
  totalLearners: number
  isCourseDeleted: boolean
}>): CourseBarEntry[] {
  return courses.map(c => ({
    assignmentRuleId: c.assignmentRuleId,
    courseCode: c.courseCode,
    courseTitle: c.courseTitle,
    pct: c.totalLearners === 0 ? 0 : (c.completedLearners / c.totalLearners) * 100,
    completedLearners: c.completedLearners,
    totalLearners: c.totalLearners,
    isCourseDeleted: c.isCourseDeleted,
  }))
}

function EmptyChart({ label }: { label: string }) {
  return (
    <div className="flex items-center justify-center h-[200px] text-xs text-slate-400">
      {label}
    </div>
  )
}
