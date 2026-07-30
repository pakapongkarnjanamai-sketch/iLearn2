import { Link } from 'react-router-dom'
import { formatDate, formatNumber } from '../../../lib/format'
import { ASSIGNMENT_LABELS, learnerStatusLabel, tf } from '../../../lib/labels'
import type { GanttTask } from './ganttScale'
import { ganttStatusBarClass } from './ganttStatus'

type GanttBarProps = {
  task: GanttTask
  leftPx: number
  widthPx: number
  durationDays: number
  rowHeight: number
  timelineWidth: number
  startDate: Date
  dueDate: Date
  /** Bottom rows open the hover card upward — the timeline scroller clips it otherwise. */
  flipHoverCardUp: boolean
}

export function GanttBar({
  task,
  leftPx,
  widthPx,
  durationDays,
  rowHeight,
  timelineWidth,
  startDate,
  dueDate,
  flipHoverCardUp,
}: GanttBarProps) {
  const hoverAlignRight = leftPx + 200 > timelineWidth
  const dateRange = `${formatDate(startDate)} - ${formatDate(dueDate)}`
  const statusText = learnerStatusLabel(task.status)
  const durationText = tf(ASSIGNMENT_LABELS.durationDays, formatNumber(durationDays))
  // z-10 lifts the card over the bars of the rows below — those are later siblings at
  // z-auto, so they paint straight over an unlayered card. The name column and header
  // are also z-10 but emitted after the bars, so they still cover it.
  const hoverCardClass = [
    'pointer-events-none absolute z-10 hidden w-52 rounded-md border border-slate-200 bg-white p-2',
    'text-xxs text-slate-700 shadow-sm group-hover:block group-focus-visible:block',
    flipHoverCardUp ? 'bottom-full mb-1' : 'top-full mt-1',
    hoverAlignRight ? 'right-0' : 'left-0',
  ].join(' ')

  return (
    <div
      className="absolute top-1"
      style={{
        left: `${leftPx}px`,
        width: `${widthPx}px`,
        height: `${rowHeight - 8}px`,
      }}
    >
      <Link
        to={`/assignments/${task.id}`}
        aria-label={`${task.title}, ${statusText}, ${dateRange}`}
        className={`group relative flex h-full items-center rounded-sm px-2 text-xxs font-bold text-white shadow-[inset_0_0_0_1px_rgba(255,255,255,0.2)] outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-1 ${ganttStatusBarClass(task.status)}`}
      >
        <span className="truncate">{task.title}</span>

        <div className={hoverCardClass}>
          <p className="truncate font-semibold text-slate-800">{task.title}</p>
          <p className="mt-1 text-slate-500">{statusText}</p>
          <p className="mt-1">{dateRange}</p>
          <p className="mt-1 text-slate-500">{durationText}</p>
        </div>
      </Link>
    </div>
  )
}
