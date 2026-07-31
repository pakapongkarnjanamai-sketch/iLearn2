import { lazy, Suspense, useEffect, useMemo, useRef, useState } from 'react'
import { CalendarClock } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { LoadingState } from '../../components/ui/LoadingState'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatNumber } from '../../lib/format'
import { toast } from '../../lib/toast'
import { ASSIGNMENT_LABELS, COMMON_LABELS, learnerStatusLabel, t, tf } from '../../lib/labels'
import { getDefaultZoom, type GanttTask, type GanttZoom } from './gantt/svarGanttMapping'
import { ganttStatusBarClass } from './gantt/ganttStatus'
import type { AssignmentSvarGanttChartHandle } from './gantt/AssignmentSvarGanttChart'

const AssignmentSvarGanttChart = lazy(() =>
  import('./gantt/AssignmentSvarGanttChart').then((module) => ({ default: module.AssignmentSvarGanttChart })),
)

const STATUS_FILTERS = ['All', 'InProgress', 'Upcoming', 'Completed', 'Expired'] as const
type StatusFilter = (typeof STATUS_FILTERS)[number]
type LegendStatus = Exclude<StatusFilter, 'All'>

const buildCounts = (items: GanttTask[]) => items.reduce<Record<StatusFilter, number>>(
  (acc, task) => {
    acc.All += 1
    if (task.status === 'InProgress') acc.InProgress += 1
    if (task.status === 'Upcoming') acc.Upcoming += 1
    if (task.status === 'Completed') acc.Completed += 1
    if (task.status === 'Expired') acc.Expired += 1
    return acc
  },
  { All: 0, InProgress: 0, Upcoming: 0, Completed: 0, Expired: 0 },
)

export function AssignmentGanttPage() {
  const [loading, setLoading] = useState(true)
  const [tasks, setTasks] = useState<GanttTask[]>([])
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('All')
  const [zoom, setZoom] = useState<GanttZoom>('week')
  const svarChartRef = useRef<AssignmentSvarGanttChartHandle | null>(null)
  const hasCenteredTodayRef = useRef(false)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    fetchWithAccessControl<GanttTask[]>('Assignments/gantt')
      .then((data) => {
        if (cancelled) return
        const nextTasks = Array.isArray(data) ? data : []
        setTasks(nextTasks)
        setZoom(getDefaultZoom(nextTasks))
        hasCenteredTodayRef.current = false
      })
      .catch(() => toast.error(t(ASSIGNMENT_LABELS.failedToLoadGantt)))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [])

  const handleSvarReady = () => {
    if (!hasCenteredTodayRef.current && tasks.length > 0) {
      svarChartRef.current?.scrollToToday()
      hasCenteredTodayRef.current = true
    }
  }

  const counts = useMemo(() => buildCounts(tasks), [tasks])

  // Resolved per render, not at module scope: AppLayout remounts the tree on a
  // language switch, which re-runs component bodies but never module initialisers.
  const zoomOptions: Array<{ value: GanttZoom; label: string }> = [
    { value: 'day', label: t(ASSIGNMENT_LABELS.zoomDay) },
    { value: 'week', label: t(ASSIGNMENT_LABELS.zoomWeek) },
    { value: 'month', label: t(ASSIGNMENT_LABELS.zoomMonth) },
  ]

  const legendStatuses = useMemo(
    () => STATUS_FILTERS.filter((status): status is LegendStatus => status !== 'All' && counts[status] > 0),
    [counts],
  )

  const filterOptions = useMemo(
    () => STATUS_FILTERS.map((status) => ({
      value: status,
      label: status === 'All'
        ? `${t(COMMON_LABELS.all)} (${formatNumber(counts.All)})`
        : `${learnerStatusLabel(status)} (${formatNumber(counts[status])})`,
    })),
    [counts],
  )

  const filtered = useMemo(
    () => (statusFilter === 'All' ? tasks : tasks.filter((task) => task.status === statusFilter)),
    [tasks, statusFilter],
  )

  return (
    <DataGridSurface
      title={t(ASSIGNMENT_LABELS.assignmentSchedule)}
      note={t(ASSIGNMENT_LABELS.ganttNote)}
      actions={
        <div className="flex items-center gap-2">
          <SegmentedToggle
            variant="segment"
            value={zoom}
            onChange={setZoom}
            options={zoomOptions}
          />
          <AppButton variant="secondary" icon={CalendarClock} onClick={() => svarChartRef.current?.scrollToToday()}>
            {t(ASSIGNMENT_LABELS.today)}
          </AppButton>
        </div>
      }
    >
      <div className="flex min-h-0 min-w-0 flex-1 flex-col gap-2 pt-2">
        <SegmentedToggle
          variant="filter"
          value={statusFilter}
          onChange={(value) => setStatusFilter(value as StatusFilter)}
          options={filterOptions}
          className="flex-wrap"
        />

        {loading ? (
          <LoadingState size="section" />
        ) : tasks.length === 0 ? (
          <div className="flex flex-1 items-center justify-center rounded-lg border border-slate-200 bg-slate-50/40 p-8 text-[13px] font-medium text-slate-400">
            {t(ASSIGNMENT_LABELS.noAssignments)}
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-1 flex-col items-center justify-center rounded-lg border border-slate-200 bg-slate-50/40 p-8 text-center">
            <p className="text-[13px] font-semibold text-slate-600">{t(ASSIGNMENT_LABELS.noAssignmentsMatchFilter)}</p>
            <p className="mt-1 text-xs text-slate-500">{t(ASSIGNMENT_LABELS.tryAnotherStatusFilter)}</p>
          </div>
        ) : (
          <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg border border-slate-200/80 bg-white">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-200 px-3 py-1.5">
              <span className="text-xs font-semibold text-slate-500">
                {tf(ASSIGNMENT_LABELS.showingBatches, filtered.length, tasks.length)}
              </span>
              {legendStatuses.length > 0 && (
                <div className="flex flex-wrap items-center gap-3">
                  {legendStatuses.map((status) => (
                    <span key={status} className="flex items-center gap-1.5 text-xxs font-semibold text-slate-500">
                      <span className={`size-2 rounded-xs ${ganttStatusBarClass(status)}`} />
                      {learnerStatusLabel(status)}
                    </span>
                  ))}
                </div>
              )}
            </div>
            <Suspense fallback={<LoadingState size="section" />}>
              <AssignmentSvarGanttChart ref={svarChartRef} tasks={filtered} zoom={zoom} onReady={handleSvarReady} />
            </Suspense>
          </div>
        )}
      </div>
    </DataGridSurface>
  )
}
