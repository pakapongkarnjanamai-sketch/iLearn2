export type GanttZoom = 'day' | 'week' | 'month'

// Mirrors AssignmentGanttTaskDto (iLearn.Application/DTOs/AssignmentApiResponseDtos.cs).
// Color is kept for contract parity with the API, but the UI maps status to local tones.
export type GanttTask = {
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

export const ROW_H = 34
export const HEADER_MONTH_H = 26
export const HEADER_TICK_H = 28
export const NAME_COL_W = 340

export const ZOOM_LEVELS: Record<GanttZoom, number> = {
  day: 22,
  week: 8,
  month: 3,
}

type TimelineMonth = {
  key: string
  label: string
  days: number
  widthPct: number
}

type TimelineTick = {
  key: string
  label: string
  widthPct: number
  isToday: boolean
  isWeekend: boolean
}

export type TimelineModel = {
  rangeStart: Date
  totalDays: number
  pxPerDay: number
  widthPx: number
  /**
   * Month view scales to the card instead of scrolling: a fixed px-per-day leaves the
   * chart far narrower than the space it sits in. Every horizontal position is therefore
   * a percentage of the timeline column, which resolves to the same pixels in the
   * px-width zooms and stretches to fit in this one.
   */
  fitsWidth: boolean
  /** Month view drops the tick row, so the header is not a fixed height. */
  headerH: number
  months: TimelineMonth[]
  ticks: TimelineTick[]
  /** Month starts as percentages, for the body guide lines (skips 0). */
  monthBoundaryPcts: number[]
  /** Raw offset — negative or >= totalDays when every batch sits on one side of today. */
  todayOffsetDays: number
  isTodayInRange: boolean
  todayLeftPct: number
}

const DAY_MS = 86_400_000
const PAD_DAYS = 3
const MIN_DAYS = 14
const monthFmt = new Intl.DateTimeFormat('en-GB', { month: 'short', year: '2-digit' })
const dayNumberFmt = new Intl.DateTimeFormat('en-GB', { day: 'numeric' })
const weekStartFmt = new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short' })

const parseDate = (value: string) => {
  const date = new Date(value)
  date.setHours(0, 0, 0, 0)
  return date
}

const startOfDay = (value: Date) => {
  const date = new Date(value)
  date.setHours(0, 0, 0, 0)
  return date
}

const addDays = (value: Date, days: number) => {
  const next = new Date(value)
  next.setDate(next.getDate() + days)
  return next
}

const diffDays = (from: Date, to: Date) =>
  Math.round((to.getTime() - from.getTime()) / DAY_MS)

export const headerHeight = (zoom: GanttZoom) =>
  zoom === 'month' ? HEADER_MONTH_H : HEADER_MONTH_H + HEADER_TICK_H

function buildMonthHeaders(rangeStart: Date, totalDays: number) {
  const months: TimelineMonth[] = []
  const boundaryPcts: number[] = []
  const cursor = new Date(rangeStart)
  let remaining = totalDays
  let consumed = 0

  while (remaining > 0) {
    const monthEnd = new Date(cursor.getFullYear(), cursor.getMonth() + 1, 0)
    const daysInSegment = Math.min(remaining, diffDays(cursor, monthEnd) + 1)
    if (consumed > 0) {
      boundaryPcts.push((consumed / totalDays) * 100)
    }
    months.push({
      key: `${cursor.getFullYear()}-${cursor.getMonth()}`,
      label: monthFmt.format(cursor),
      days: daysInSegment,
      widthPct: (daysInSegment / totalDays) * 100,
    })
    cursor.setDate(cursor.getDate() + daysInSegment)
    consumed += daysInSegment
    remaining -= daysInSegment
  }

  return { months, boundaryPcts }
}

function buildDayTicks(rangeStart: Date, totalDays: number, today: Date): TimelineTick[] {
  return Array.from({ length: totalDays }, (_, offset) => {
    const date = addDays(rangeStart, offset)
    return {
      key: `day-${offset}`,
      label: dayNumberFmt.format(date),
      widthPct: (1 / totalDays) * 100,
      isToday: date.getTime() === today.getTime(),
      isWeekend: date.getDay() === 0 || date.getDay() === 6,
    }
  })
}

function buildWeekTicks(rangeStart: Date, totalDays: number): TimelineTick[] {
  const ticks: TimelineTick[] = []
  let offset = 0

  while (offset < totalDays) {
    const date = addDays(rangeStart, offset)
    const day = date.getDay()
    const daysUntilNextWeek = day === 0 ? 1 : 8 - day
    const widthDays = Math.min(daysUntilNextWeek, totalDays - offset)

    ticks.push({
      key: `week-${offset}`,
      label: weekStartFmt.format(date),
      widthPct: (widthDays / totalDays) * 100,
      isToday: false,
      isWeekend: false,
    })

    offset += widthDays
  }

  return ticks
}

/** Days from rangeStart to the first week boundary — the phase for the body guide lines. */
export const weekPhaseDays = (rangeStart: Date) => {
  const day = rangeStart.getDay()
  return day === 0 ? 1 : 8 - day
}

export function getDefaultZoom(tasks: GanttTask[]): GanttZoom {
  const timeline = buildTimeline(tasks, 'day')
  return timeline.totalDays <= 60 ? 'day' : 'week'
}

export function buildTimeline(tasks: GanttTask[], zoom: GanttZoom): TimelineModel {
  const today = startOfDay(new Date())

  let minDate = today
  let maxDate = today

  if (tasks.length > 0) {
    minDate = parseDate(tasks[0]!.startDate)
    maxDate = parseDate(tasks[0]!.dueDate)

    tasks.forEach((task) => {
      const start = parseDate(task.startDate)
      const due = parseDate(task.dueDate)
      if (start < minDate) minDate = start
      if (due > maxDate) maxDate = due
    })
  }

  const paddedStart = addDays(minDate, -PAD_DAYS)
  let paddedEnd = addDays(maxDate, PAD_DAYS)
  const minEnd = addDays(paddedStart, MIN_DAYS - 1)
  if (paddedEnd < minEnd) paddedEnd = minEnd

  const totalDays = Math.max(MIN_DAYS, diffDays(paddedStart, paddedEnd) + 1)
  const pxPerDay = ZOOM_LEVELS[zoom]
  const { months, boundaryPcts } = buildMonthHeaders(paddedStart, totalDays)
  const ticks = zoom === 'day'
    ? buildDayTicks(paddedStart, totalDays, today)
    : zoom === 'week'
      ? buildWeekTicks(paddedStart, totalDays)
      : []

  const todayOffsetDays = diffDays(paddedStart, today)

  return {
    rangeStart: paddedStart,
    totalDays,
    pxPerDay,
    widthPx: totalDays * pxPerDay,
    fitsWidth: zoom === 'month',
    headerH: headerHeight(zoom),
    months,
    ticks,
    monthBoundaryPcts: boundaryPcts,
    todayOffsetDays,
    isTodayInRange: todayOffsetDays >= 0 && todayOffsetDays < totalDays,
    todayLeftPct: ((todayOffsetDays + 0.5) / totalDays) * 100,
  }
}

export function getTaskLayout(task: GanttTask, rangeStart: Date, totalDays: number) {
  const start = parseDate(task.startDate)
  const due = parseDate(task.dueDate)
  const durationDays = Math.max(1, diffDays(start, due) + 1)
  const startOffsetDays = Math.max(0, diffDays(rangeStart, start))
  const visibleDays = Math.max(1, Math.min(durationDays, totalDays - startOffsetDays))

  return {
    start,
    due,
    durationDays,
    leftPct: (startOffsetDays / totalDays) * 100,
    widthPct: (visibleDays / totalDays) * 100,
  }
}
