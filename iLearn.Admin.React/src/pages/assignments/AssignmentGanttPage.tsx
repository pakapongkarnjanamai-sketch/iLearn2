import { useEffect, useMemo, useState } from 'react'
import { CalendarClock } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { LoadingState } from '../../components/ui/LoadingState'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { ASSIGNMENT_LABELS, COMMON_LABELS, learnerStatusLabel, t, tf } from '../../lib/labels'

type GanttTask = {
  id: number
  parentId: number
  assignmentNo: string
  title: string
  startDate: string
  dueDate: string
  progress: number
  color: string
  status: string
}

const STATUS_FILTERS = ['All', 'InProgress', 'Upcoming', 'Completed', 'Expired'] as const


const DAY_PX = 18
const ROW_PX = 32

const parseDate = (s: string) => {
  const d = new Date(s)
  d.setHours(0, 0, 0, 0)
  return d
}

const diffDays = (a: Date, b: Date) =>
  Math.round((b.getTime() - a.getTime()) / 86_400_000)

const monthFmt = new Intl.DateTimeFormat('en-GB', { month: 'short', year: '2-digit' })
const dayFmt = new Intl.DateTimeFormat('en-GB', { day: 'numeric' })

export function AssignmentGanttPage() {
  const [loading, setLoading] = useState(true)
  const [tasks, setTasks] = useState<GanttTask[]>([])
  const [statusFilter, setStatusFilter] =
    useState<(typeof STATUS_FILTERS)[number]>('All')

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    // gantt endpoint returns flat array, not wrapped envelope
    fetchWithAccessControl<GanttTask[]>('Assignments/gantt')
      .then((data) => {
        if (cancelled) return
        setTasks(Array.isArray(data) ? data : [])
      })
      .catch(() => toast.error(t(ASSIGNMENT_LABELS.failedToLoadGantt)))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [])

  const filtered = useMemo(
    () => (statusFilter === 'All' ? tasks : tasks.filter((t) => t.status === statusFilter)),
    [tasks, statusFilter],
  )

  const { rangeStart, totalDays, today } = useMemo(() => {
    if (filtered.length === 0) {
      const now = new Date()
      now.setHours(0, 0, 0, 0)
      return { rangeStart: now, totalDays: 30, today: now }
    }
    let min = parseDate(filtered[0]!.startDate)
    let max = parseDate(filtered[0]!.dueDate)
    filtered.forEach((t) => {
      const s = parseDate(t.startDate)
      const d = parseDate(t.dueDate)
      if (s < min) min = s
      if (d > max) max = d
    })
    // pad
    min.setDate(min.getDate() - 3)
    max.setDate(max.getDate() + 3)
    const t = new Date()
    t.setHours(0, 0, 0, 0)
    return { rangeStart: min, totalDays: Math.max(diffDays(min, max) + 1, 14), today: t }
  }, [filtered])

  const monthHeaders = useMemo(() => {
    const result: { label: string; days: number }[] = []
    const cur = new Date(rangeStart)
    let remaining = totalDays
    while (remaining > 0) {
      const monthEnd = new Date(cur.getFullYear(), cur.getMonth() + 1, 0)
      const daysInThisMonth = Math.min(remaining, diffDays(cur, monthEnd) + 1)
      result.push({ label: monthFmt.format(cur), days: daysInThisMonth })
      cur.setDate(cur.getDate() + daysInThisMonth)
      remaining -= daysInThisMonth
    }
    return result
  }, [rangeStart, totalDays])

  const scrollToToday = () => {
    const el = document.getElementById('gantt-today-marker')
    el?.scrollIntoView({ inline: 'center', behavior: 'smooth', block: 'nearest' })
  }

  const counts = useMemo(() => {
    const map: Record<string, number> = { All: tasks.length }
    STATUS_FILTERS.slice(1).forEach((s) => {
      map[s] = tasks.filter((t) => t.status === s).length
    })
    return map
  }, [tasks])

  return (
    <DataGridSurface
      title={t(ASSIGNMENT_LABELS.assignmentSchedule)}
      note={t(ASSIGNMENT_LABELS.ganttNote)}
      actions={
        <AppButton variant="secondary" icon={CalendarClock} onClick={scrollToToday}>
          {t(ASSIGNMENT_LABELS.today)}
        </AppButton>
      }
    >
      <div className="flex min-h-0 flex-1 flex-col gap-2 pt-2">
        <div className="flex flex-wrap items-center gap-2">
          {STATUS_FILTERS.map((s) => (
            <button
              key={s}
              type="button"
              onClick={() => setStatusFilter(s)}
              className={`rounded border px-2.5 py-1 text-xs font-semibold transition-colors ${
                statusFilter === s
                  ? 'border-indigo-600 bg-indigo-50 text-indigo-700'
                  : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300'
              }`}
            >
              {s === 'All' ? t(COMMON_LABELS.all) : learnerStatusLabel(s)} <span className="ml-1 text-slate-400">{counts[s] ?? 0}</span>
            </button>
          ))}
        </div>

        {loading ? (
          <LoadingState size="section" />
        ) : filtered.length === 0 ? (
          <div className="flex flex-1 items-center justify-center rounded-lg border border-slate-200 bg-slate-50/40 p-8 text-[13px] font-medium text-slate-400">
            {t(ASSIGNMENT_LABELS.noAssignments)}
          </div>
        ) : (
          <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg border border-slate-200/80 bg-white">
            <div className="flex items-center justify-between border-b border-slate-200 px-3 py-1.5">
              <span className="text-xs font-semibold text-slate-500">
                {tf(ASSIGNMENT_LABELS.showingBatches, filtered.length, tasks.length)}
              </span>
            </div>

            <div className="min-h-0 flex-1 overflow-auto custom-scrollbar">
              <div className="flex min-w-full">
                <div className="w-72 shrink-0 border-r border-slate-200/70 bg-white">
                  <div className="h-14 border-b border-slate-200/70 px-3 py-1.5 text-xxs font-extrabold uppercase text-slate-500">
                    <div className="leading-tight">{t(ASSIGNMENT_LABELS.assignment)}</div>
                    <div className="text-slate-400">{t(ASSIGNMENT_LABELS.batch)}</div>
                  </div>
                  {filtered.map((t) => (
                    <div
                      key={t.id}
                      className="flex items-center border-b border-slate-100/70 px-3"
                      style={{ height: ROW_PX }}
                    >
                      <div className="truncate">
                        <span className="font-mono text-xs text-slate-500">{t.assignmentNo}</span>
                        <span className="ml-2 text-xs font-semibold text-slate-700">{t.title}</span>
                      </div>
                    </div>
                  ))}
                </div>

                <div className="flex-1 overflow-x-auto overflow-y-hidden">
                  <div style={{ width: totalDays * DAY_PX, minWidth: '100%' }}>
                    <div className="flex border-b border-slate-200/70">
                      {monthHeaders.map((m, i) => (
                        <div
                          key={i}
                          className="border-r border-slate-200/70 px-2 py-1 text-xxs font-bold uppercase text-slate-500"
                          style={{ width: m.days * DAY_PX }}
                        >
                          {m.label}
                        </div>
                      ))}
                    </div>

                    <div className="flex border-b border-slate-200/70" style={{ height: 28 }}>
                      {Array.from({ length: totalDays }).map((_, i) => {
                        const d = new Date(rangeStart)
                        d.setDate(d.getDate() + i)
                        const isWeekend = d.getDay() === 0 || d.getDay() === 6
                        const isToday = d.getTime() === today.getTime()
                        return (
                          <div
                            key={i}
                            className={`text-center text-xxs leading-7 ${
                              isWeekend ? 'bg-slate-50 text-slate-400' : 'text-slate-500'
                            } ${isToday ? 'bg-indigo-50 font-bold text-indigo-700' : ''}`}
                            style={{ width: DAY_PX }}
                          >
                            {dayFmt.format(d)}
                          </div>
                        )
                      })}
                    </div>

                    <div className="relative">
                      {diffDays(rangeStart, today) >= 0 && diffDays(rangeStart, today) < totalDays && (
                        <div
                          id="gantt-today-marker"
                          className="pointer-events-none absolute top-0 z-10 h-full border-l-2 border-indigo-500/60"
                          style={{ left: diffDays(rangeStart, today) * DAY_PX + DAY_PX / 2 }}
                        />
                      )}

                      {filtered.map((t) => {
                        const start = parseDate(t.startDate)
                        const end = parseDate(t.dueDate)
                        const left = Math.max(0, diffDays(rangeStart, start)) * DAY_PX
                        const width = Math.max(DAY_PX, (diffDays(start, end) + 1) * DAY_PX)
                        return (
                          <div
                            key={t.id}
                            className="relative border-b border-slate-100/70"
                            style={{ height: ROW_PX }}
                          >
                            <div
                              className="absolute top-1.5 flex items-center overflow-hidden rounded text-xxs font-bold text-white shadow-sm"
                              style={{
                                left,
                                width,
                                height: ROW_PX - 12,
                                background: t.color,
                              }}
                              title={`${t.title} — ${t.status} (${t.progress}%)`}
                            >
                              <div
                                className="h-full bg-white/25"
                                style={{ width: `${Math.min(100, Math.max(0, t.progress))}%` }}
                              />
                              <span className="absolute inset-0 flex items-center justify-center px-1.5">
                                {t.progress}%
                              </span>
                            </div>
                          </div>
                        )
                      })}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </DataGridSurface>
  )
}
