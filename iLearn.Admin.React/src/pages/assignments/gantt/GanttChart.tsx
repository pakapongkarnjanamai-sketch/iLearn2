import type { MutableRefObject } from 'react'
import { ASSIGNMENT_LABELS, t } from '../../../lib/labels'
import { GanttBar } from './GanttBar'
import { getTaskLayout, HEADER_MONTH_H, HEADER_TICK_H, HEADER_TOTAL_H, NAME_COL_W, ROW_H, type GanttTask, type GanttZoom, type TimelineModel } from './ganttScale'

type GanttChartProps = {
  tasks: GanttTask[]
  timeline: TimelineModel
  zoom: GanttZoom
  scrollerRef: MutableRefObject<HTMLDivElement | null>
}

function buildRowsBackground(rangeStart: Date, zoom: GanttZoom, pxPerDay: number) {
  if (zoom !== 'day') {
    return {
      backgroundImage: `repeating-linear-gradient(to right, transparent 0, transparent ${pxPerDay - 1}px, rgba(148, 163, 184, 0.18) ${pxPerDay - 1}px, rgba(148, 163, 184, 0.18) ${pxPerDay}px)`,
    }
  }

  const day = rangeStart.getDay()
  const daysUntilSaturday = (6 - day + 7) % 7
  const weekendStartPx = daysUntilSaturday * pxPerDay
  const weekWidthPx = 7 * pxPerDay

  return {
    backgroundImage: [
      `repeating-linear-gradient(to right, transparent 0, transparent ${pxPerDay - 1}px, rgba(148, 163, 184, 0.22) ${pxPerDay - 1}px, rgba(148, 163, 184, 0.22) ${pxPerDay}px)`,
      `repeating-linear-gradient(to right, transparent 0, transparent ${weekendStartPx}px, rgba(148, 163, 184, 0.1) ${weekendStartPx}px, rgba(148, 163, 184, 0.1) ${weekendStartPx + 2 * pxPerDay}px, transparent ${weekendStartPx + 2 * pxPerDay}px, transparent ${weekWidthPx}px)`,
    ].join(','),
  }
}

export function GanttChart({ tasks, timeline, zoom, scrollerRef }: GanttChartProps) {
  const tableWidth = NAME_COL_W + timeline.widthPx
  const rowsBackground = buildRowsBackground(timeline.rangeStart, zoom, timeline.pxPerDay)

  return (
    <div ref={scrollerRef} className="min-h-0 flex-1 overflow-auto custom-scrollbar">
      <div className="relative min-h-full" style={{ width: `${tableWidth}px`, minWidth: '100%' }}>
        {/*
          Every cell pins its own `gridRow`: with auto-placement, a column-1 item that
          follows a column-2 item starts a new row, which silently drops each name cell
          one row below its bar. Pinned rows also free the DOM order to encode the
          freeze-pane paint order (equal z-index ⇒ later sibling wins), so the four
          layers below stay at z-10 and never need to climb the app's z-ladder:
          name cells → bar rows → timeline header → corner.
        */}
        <div
          className="grid"
          style={{
            gridTemplateColumns: `${NAME_COL_W}px ${timeline.widthPx}px`,
          }}
        >
          {tasks.map((task, index) => (
            <div
              key={`name-${task.id}`}
              className="sticky left-0 z-10 col-start-1 col-end-2 flex items-center gap-2 border-r border-b border-slate-100 bg-white px-3"
              style={{ gridRow: index + 2, height: `${ROW_H}px` }}
            >
              <span className="shrink-0 font-mono text-xs text-slate-500">{task.assignmentNo}</span>
              {task.title !== task.assignmentNo && (
                <>
                  <span className="text-slate-300">-</span>
                  <span className="truncate text-xs font-semibold text-slate-700" title={task.title}>
                    {task.title}
                  </span>
                </>
              )}
            </div>
          ))}

          {tasks.map((task, index) => {
            const layout = getTaskLayout(task, timeline.rangeStart, timeline.pxPerDay)
            return (
              <div
                key={`bar-${task.id}`}
                className="relative col-start-2 col-end-3 border-b border-slate-100"
                style={{ ...rowsBackground, gridRow: index + 2, height: `${ROW_H}px` }}
              >
                <GanttBar
                  task={task}
                  leftPx={layout.leftPx}
                  widthPx={layout.widthPx}
                  durationDays={layout.durationDays}
                  rowHeight={ROW_H}
                  timelineWidth={timeline.widthPx}
                  startDate={layout.start}
                  dueDate={layout.due}
                  flipHoverCardUp={tasks.length > 3 && index >= tasks.length - 2}
                />
              </div>
            )
          })}

          <div
            className="sticky top-0 z-10 col-start-2 col-end-3 bg-white"
            style={{ gridRow: 1, height: `${HEADER_TOTAL_H}px` }}
          >
            <div className="flex border-b border-slate-200" style={{ height: `${HEADER_MONTH_H}px` }}>
              {timeline.months.map((month) => (
                <div
                  key={month.key}
                  className="overflow-hidden border-r border-slate-200 px-2 text-xxs font-bold uppercase leading-6.5 text-slate-500"
                  style={{ width: `${month.widthPx}px` }}
                >
                  {month.label}
                </div>
              ))}
            </div>
            <div className="flex border-b border-slate-200" style={{ height: `${HEADER_TICK_H}px` }}>
              {timeline.ticks.length === 0 ? (
                <div className="w-full border-r border-slate-200" />
              ) : (
                timeline.ticks.map((tick) => (
                  <div
                    key={tick.key}
                    className={`overflow-hidden border-r border-slate-200 px-1 text-center text-xxs leading-7 ${tick.isToday ? 'bg-indigo-50 font-bold text-indigo-700' : tick.isWeekend ? 'bg-slate-50 text-slate-400' : 'text-slate-500'}`}
                    style={{ width: `${tick.widthPx}px` }}
                  >
                    {tick.label}
                  </div>
                ))
              )}
            </div>
          </div>

          <div
            className="sticky top-0 left-0 z-10 col-start-1 col-end-2 flex flex-col justify-center border-r border-b border-slate-200 bg-white px-3"
            style={{ gridRow: 1, height: `${HEADER_TOTAL_H}px` }}
          >
            <div className="text-xxs font-extrabold uppercase leading-tight text-slate-500">
              {t(ASSIGNMENT_LABELS.assignment)}
            </div>
            <div className="text-xxs font-extrabold uppercase leading-tight text-slate-400">
              {t(ASSIGNMENT_LABELS.batch)}
            </div>
          </div>
        </div>

        {/* No z-index: the line has to sit above the bars (later sibling, both auto)
            but below the sticky header and name column (z-10). */}
        {timeline.isTodayInRange && (
          <div
            className="pointer-events-none absolute top-0 border-l-2 border-indigo-500/60"
            style={{
              left: `${NAME_COL_W + timeline.todayOffsetDays * timeline.pxPerDay + timeline.pxPerDay / 2}px`,
              height: `${HEADER_TOTAL_H + tasks.length * ROW_H}px`,
            }}
          />
        )}
      </div>
    </div>
  )
}
