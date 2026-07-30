import type { MutableRefObject } from 'react'
import { ASSIGNMENT_LABELS, t } from '../../../lib/labels'
import { GanttBar } from './GanttBar'
import { getTaskLayout, HEADER_MONTH_H, HEADER_TICK_H, NAME_COL_W, ROW_H, weekPhaseDays, type GanttTask, type GanttZoom, type TimelineModel } from './ganttScale'

type GanttChartProps = {
  tasks: GanttTask[]
  timeline: TimelineModel
  zoom: GanttZoom
  scrollerRef: MutableRefObject<HTMLDivElement | null>
}

const GUIDE_LINE = 'rgba(148, 163, 184, 0.22)'
const WEEKEND_BAND = 'rgba(148, 163, 184, 0.10)'

function buildRowsBackground(timeline: TimelineModel, zoom: GanttZoom) {
  // The month view draws its guides as one percentage-positioned overlay instead: a px
  // gradient cannot line up with a column that stretches, and at 3px per day a per-day
  // line is a hatch rather than a grid.
  if (zoom === 'month') return undefined

  const { pxPerDay, rangeStart } = timeline
  const weekPx = 7 * pxPerDay

  if (zoom === 'week') {
    // Phase-shifted so the lines land on the same boundaries as the week tick cells.
    const phasePx = weekPhaseDays(rangeStart) * pxPerDay
    return {
      backgroundImage: `repeating-linear-gradient(to right, ${GUIDE_LINE} 0, ${GUIDE_LINE} 1px, transparent 1px, transparent ${weekPx}px)`,
      backgroundPosition: `${phasePx - 1}px 0`,
    }
  }

  const weekendStartPx = ((6 - rangeStart.getDay() + 7) % 7) * pxPerDay
  const weekendEndPx = weekendStartPx + 2 * pxPerDay
  return {
    backgroundImage: [
      `repeating-linear-gradient(to right, transparent 0, transparent ${pxPerDay - 1}px, ${GUIDE_LINE} ${pxPerDay - 1}px, ${GUIDE_LINE} ${pxPerDay}px)`,
      `repeating-linear-gradient(to right, transparent 0, transparent ${weekendStartPx}px, ${WEEKEND_BAND} ${weekendStartPx}px, ${WEEKEND_BAND} ${weekendEndPx}px, transparent ${weekendEndPx}px, transparent ${weekPx}px)`,
    ].join(','),
  }
}

export function GanttChart({ tasks, timeline, zoom, scrollerRef }: GanttChartProps) {
  const rowsBackground = buildRowsBackground(timeline, zoom)
  const bodyHeight = tasks.length * ROW_H
  // Spans exactly the timeline column in both modes: the inner box is the table width
  // when scrolling, and 100% when the month view stretches to fit.
  const timelineArea = { left: `${NAME_COL_W}px`, right: 0 }

  return (
    // Sized to its content instead of flex-1: a stretched scroller parks the horizontal
    // scrollbar at the bottom of the card, far below the last row, where nobody finds it.
    <div ref={scrollerRef} className="min-h-0 overflow-auto custom-scrollbar">
      <div
        className="relative"
        style={
          timeline.fitsWidth
            ? { minWidth: '100%' }
            : { width: `${NAME_COL_W + timeline.widthPx}px`, minWidth: '100%' }
        }
      >
        {/* Month guides sit behind the bars by preceding the grid in the DOM. */}
        {timeline.fitsWidth && (
          <div
            className="pointer-events-none absolute"
            style={{ ...timelineArea, top: `${timeline.headerH}px`, height: `${bodyHeight}px` }}
          >
            {timeline.monthBoundaryPcts.map((pct) => (
              <div
                key={pct}
                className="absolute top-0 h-full border-l border-slate-200/70"
                style={{ left: `${pct}%` }}
              />
            ))}
          </div>
        )}

        {/*
          Every cell pins its own `gridRow`: with auto-placement, a column-1 item that
          follows a column-2 item starts a new row, which silently drops each name cell
          one row below its bar. Pinned rows also free the DOM order to encode the
          freeze-pane paint order (equal z-index ⇒ later sibling wins), so every layer
          stays at z-10 and never climbs the app's z-ladder:
          bar rows (z-auto) → hover cards (z-10, inside a bar) → name cells → header → corner.
        */}
        <div
          className="grid"
          style={{
            gridTemplateColumns: timeline.fitsWidth
              ? `${NAME_COL_W}px minmax(0, 1fr)`
              : `${NAME_COL_W}px ${timeline.widthPx}px`,
          }}
        >
          {tasks.map((task, index) => {
            const layout = getTaskLayout(task, timeline.rangeStart, timeline.totalDays)
            return (
              <div
                key={`bar-${task.id}`}
                className="relative col-start-2 col-end-3 border-b border-slate-100"
                style={{ ...rowsBackground, gridRow: index + 2, height: `${ROW_H}px` }}
              >
                <GanttBar
                  task={task}
                  leftPct={layout.leftPct}
                  widthPct={layout.widthPct}
                  durationDays={layout.durationDays}
                  rowHeight={ROW_H}
                  startDate={layout.start}
                  dueDate={layout.due}
                  flipHoverCardUp={tasks.length > 3 && index >= tasks.length - 2}
                />
              </div>
            )
          })}

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

          <div
            className="sticky top-0 z-10 col-start-2 col-end-3 bg-white"
            style={{ gridRow: 1, height: `${timeline.headerH}px` }}
          >
            <div className="flex border-b border-slate-200" style={{ height: `${HEADER_MONTH_H}px` }}>
              {timeline.months.map((month) => (
                <div
                  key={month.key}
                  // The padding lives on the label, not the cell: a border-box cell can
                  // never be narrower than its own padding + border, so a short edge
                  // month would be clamped wider than its share and the shrink pass
                  // would shave every other cell, drifting labels off the body guides.
                  className="min-w-0 shrink-0 overflow-hidden border-r border-slate-200 text-xxs font-bold uppercase leading-6.5 text-slate-500"
                  style={{ width: `${month.widthPct}%` }}
                >
                  <span className="block truncate px-2">{month.label}</span>
                </div>
              ))}
            </div>
            {timeline.ticks.length > 0 && (
              <div className="flex border-b border-slate-200" style={{ height: `${HEADER_TICK_H}px` }}>
                {timeline.ticks.map((tick) => (
                  <div
                    key={tick.key}
                    className={`min-w-0 shrink-0 overflow-hidden border-r border-slate-200 text-center text-xxs leading-7 ${tick.isToday ? 'bg-indigo-50 font-bold text-indigo-700' : tick.isWeekend ? 'bg-slate-50 text-slate-400' : 'text-slate-500'}`}
                    style={{ width: `${tick.widthPct}%` }}
                  >
                    <span className="block truncate px-1">{tick.label}</span>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div
            className="sticky top-0 left-0 z-10 col-start-1 col-end-2 flex flex-col justify-center border-r border-b border-slate-200 bg-white px-3"
            style={{ gridRow: 1, height: `${timeline.headerH}px` }}
          >
            <div className="text-xxs font-extrabold uppercase leading-tight text-slate-500">
              {t(ASSIGNMENT_LABELS.assignment)}
            </div>
            {timeline.ticks.length > 0 && (
              <div className="text-xxs font-extrabold uppercase leading-tight text-slate-400">
                {t(ASSIGNMENT_LABELS.batch)}
              </div>
            )}
          </div>
        </div>

        {/* No z-index: the line sits above the bars (later sibling, both auto) and below
            the sticky header and name column (z-10). */}
        {timeline.isTodayInRange && (
          <div
            className="pointer-events-none absolute top-0"
            style={{ ...timelineArea, height: `${timeline.headerH + bodyHeight}px` }}
          >
            <div
              className="absolute top-0 h-full border-l-2 border-indigo-500/60"
              style={{ left: `${timeline.todayLeftPct}%` }}
            />
          </div>
        )}
      </div>
    </div>
  )
}
