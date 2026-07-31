import { forwardRef, useImperativeHandle, useMemo, useRef } from 'react'
import { Gantt, Willow, type IApi, type ITask } from '@svar-ui/react-gantt'
import '@svar-ui/react-gantt/style.css'
import { useNavigate } from 'react-router-dom'
import { formatDate } from '../../../lib/format'
import { ASSIGNMENT_LABELS, learnerStatusLabel, t, tf } from '../../../lib/labels'
import type { GanttTask, GanttZoom } from './svarGanttMapping'
import { getSvarDateRange, getSvarTaskColor, mapAssignmentsToSvarTasks, svarScales } from './svarGanttMapping'
import { ganttStatusBorderHex } from './ganttStatus'

type AssignmentSvarGanttChartProps = {
  tasks: GanttTask[]
  zoom: GanttZoom
  onReady?: (api: IApi) => void
}

export type AssignmentSvarGanttChartHandle = {
  scrollToToday: () => void
}

const CELL_WIDTH: Record<GanttZoom, number> = { day: 28, week: 84, month: 140 }
const DAYS_PER_CELL: Record<GanttZoom, number> = { day: 1, week: 7, month: 30.4375 }

// Below this the bar is too narrow to hold readable text, so the label moves outside.
const SHORT_BAR_PX = 64
const DAY_MS = 86_400_000
const GRID_WIDTH = 301

const startOfDay = (value: Date) =>
  new Date(value.getFullYear(), value.getMonth(), value.getDate())

/**
 * `scroll-chart` puts the given date at the left edge, which would clip the start of
 * every bar already running today. Back the target off by a third of the visible span
 * so today lands inside the viewport instead of against its edge.
 */
function getChartDate(
  today: Date,
  dateRange: ReturnType<typeof getSvarDateRange>,
  zoom: GanttZoom,
  chartWidthPx: number,
) {
  const visibleDays = chartWidthPx > 0
    ? (chartWidthPx / CELL_WIDTH[zoom]) * DAYS_PER_CELL[zoom]
    : 0
  const target = new Date(today.getTime() - Math.round(visibleDays / 3) * DAY_MS)

  if (!dateRange) return target
  if (target < dateRange.start) return dateRange.start
  if (target > dateRange.end) return dateRange.end
  return target
}

/**
 * SVAR calls this per cell of the bottom scale row and renders the returned class
 * as an absolutely positioned band it positions itself (`left = from + index * width`).
 *
 * That formula assumes every cell is the same width, which only holds for day and
 * week cells — month cells are 28-31 days wide, so any band would drift across the
 * chart. Hence no highlight at all on month zoom.
 *
 * This is also the only way to draw a "today" band: `markers` is typed in the SVAR
 * API but the community build hard-clears `markers`/`_markers` in gantt-store, so
 * that prop can never render (see PLAN-180 Reviewer Notes).
 */
const buildHighlightTime = (zoom: GanttZoom, today: Date) => (date: Date, unit: string) => {
  if (zoom === 'month') return ''

  const classes: string[] = []
  const isWeekend = date.getDay() === 0 || date.getDay() === 6

  if (unit === 'day' && isWeekend) classes.push('gantt-weekend-cell')

  const cellDays = unit === 'week' ? 7 : 1
  const cellStart = startOfDay(date).getTime()
  const offset = today.getTime() - cellStart
  if (offset >= 0 && offset < cellDays * DAY_MS) classes.push('gantt-today-cell')

  return classes.join(' ')
}

// SVAR normalises `end` to an exclusive bound (start + duration), so it is a day past
// the real due date — always read the due date off the field the mapping carried over.
const scheduleText = (data: ITask) => `${formatDate(data.start)} - ${formatDate(String(data.dueDate ?? ''))}`

function AssignmentCell({ row }: { row: Record<string, unknown> }) {
  const data = row as ITask
  const assignmentNo = String(data.assignmentNo ?? '')
  const description = String(data.description ?? '')
  const secondary = description || scheduleText(data)
  const color = getSvarTaskColor(String(data.status ?? ''))

  return (
    <div className="gantt-assignment-cell" title={`${assignmentNo}\n${secondary}`}>
      <span className="gantt-assignment-rail" style={{ backgroundColor: color }} />
      <div className="gantt-assignment-copy">
        <span className="gantt-assignment-no">{assignmentNo}</span>
        <span className="gantt-assignment-description">{secondary}</span>
      </div>
    </div>
  )
}

function SvarTaskTemplate({ data, zoom }: { data: ITask; zoom: GanttZoom }) {
  const color = getSvarTaskColor(String(data.status ?? ''))
  const duration = Number(data.duration ?? 1)
  const barWidth = duration * CELL_WIDTH[zoom] / DAYS_PER_CELL[zoom]
  const isShort = barWidth < SHORT_BAR_PX
  const label = String(data.text ?? '')
  const assignmentNo = String(data.assignmentNo ?? '')
  const title = String(data.title ?? label)
  const status = String(data.status ?? '')
  const tooltip = [
    assignmentNo,
    title,
    scheduleText(data),
    tf(ASSIGNMENT_LABELS.durationDays, duration),
    learnerStatusLabel(status),
  ].join(' · ')

  return (
    <div
      className="gantt-task-content"
      style={{ backgroundColor: color, borderLeftColor: ganttStatusBorderHex(status) }}
      title={tooltip}
    >
      {/* One label or the other — never both, or a narrow bar prints its text twice. */}
      {isShort
        ? <span className="gantt-task-short-label">{label}</span>
        : <span className="gantt-task-label">{label}</span>}
    </div>
  )
}

export const AssignmentSvarGanttChart = forwardRef<AssignmentSvarGanttChartHandle, AssignmentSvarGanttChartProps>(function AssignmentSvarGanttChart({ tasks, zoom, onReady }, ref) {
  const navigate = useNavigate()
  const apiRef = useRef<IApi | null>(null)
  const hostRef = useRef<HTMLDivElement | null>(null)
  const svarTasks = useMemo(() => mapAssignmentsToSvarTasks(tasks), [tasks])
  const dateRange = useMemo(() => getSvarDateRange(tasks), [tasks])
  const today = useMemo(() => startOfDay(new Date()), [])

  // Stable identities: a new component/array literal per render makes SVAR tear down
  // and rebuild every bar (and the grid column) on any parent state change.
  const taskTemplate = useMemo(
    () => (props: { data: ITask }) => <SvarTaskTemplate {...props} zoom={zoom} />,
    [zoom],
  )
  const highlightTime = useMemo(() => buildHighlightTime(zoom, today), [zoom, today])
  const columns = useMemo(
    () => [{ id: 'assignmentNo', header: t(ASSIGNMENT_LABELS.assignment), width: 300, resize: false, cell: AssignmentCell }],
    [],
  )

  useImperativeHandle(ref, () => ({
    scrollToToday: () => {
      if (!apiRef.current) return
      const chartWidth = Math.max(0, (hostRef.current?.clientWidth ?? 0) - GRID_WIDTH)
      void apiRef.current.exec('scroll-chart', { date: getChartDate(today, dateRange, zoom, chartWidth) })
    },
  }), [dateRange, today, zoom])

  const init = (api: IApi) => {
    apiRef.current = api
    onReady?.(api)
  }

  const handleSelectTask = (event: { id: string | number }) => {
    const id = Number(event.id)
    if (Number.isFinite(id)) navigate(`/assignments/${id}`)
  }

  return (
    <div ref={hostRef} className="svar-assignment-gantt h-full min-h-0 min-w-0 overflow-hidden">
      <Willow fonts={false}>
        <Gantt
          ref={apiRef}
          tasks={svarTasks}
          scales={svarScales[zoom]}
          {...(dateRange ? { start: dateRange.start, end: dateRange.end } : {})}
          autoScale={false}
          cellHeight={40}
          scaleHeight={52}
          cellWidth={CELL_WIDTH[zoom]}
          gridWidth={GRID_WIDTH}
          readonly
          columns={columns}
          taskTemplate={taskTemplate}
          highlightTime={highlightTime}
          init={init}
          onSelectTask={handleSelectTask}
        />
      </Willow>
    </div>
  )
})
