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
export const HEADER_TOTAL_H = HEADER_MONTH_H + HEADER_TICK_H

export const ZOOM_LEVELS: Record<GanttZoom, number> = {
  day: 22,
  week: 8,
  month: 3,
}

type TimelineMonth = {
  key: string
  label: string
  days: number
  widthPx: number
}

type TimelineTick = {
  key: string
  label: string
  leftPx: number
  widthPx: number
  isToday?: boolean
  isWeekend?: boolean
}

export type TimelineModel = {
  rangeStart: Date
  totalDays: number
  pxPerDay: number
  widthPx: number
  months: TimelineMonth[]
  ticks: TimelineTick[]
  /** Raw offset — negative or >= totalDays when every batch sits on one side of today. */
  todayOffsetDays: number
  isTodayInRange: boolean
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

function buildMonthHeaders(rangeStart: Date, totalDays: number, pxPerDay: number): TimelineMonth[] {
  const months: TimelineMonth[] = []
  const cursor = new Date(rangeStart)
  let remaining = totalDays

  while (remaining > 0) {
    const monthEnd = new Date(cursor.getFullYear(), cursor.getMonth() + 1, 0)
    const daysInSegment = Math.min(remaining, diffDays(cursor, monthEnd) + 1)
    months.push({
      key: `${cursor.getFullYear()}-${cursor.getMonth()}`,
      label: monthFmt.format(cursor),
      days: daysInSegment,
      widthPx: daysInSegment * pxPerDay,
    })
    cursor.setDate(cursor.getDate() + daysInSegment)
    remaining -= daysInSegment
  }

  return months
}

function buildDayTicks(rangeStart: Date, totalDays: number, pxPerDay: number, today: Date): TimelineTick[] {
  return Array.from({ length: totalDays }, (_, offset) => {
    const date = addDays(rangeStart, offset)
    return {
      key: `day-${offset}`,
      label: dayNumberFmt.format(date),
      leftPx: offset * pxPerDay,
      widthPx: pxPerDay,
      isToday: date.getTime() === today.getTime(),
      isWeekend: date.getDay() === 0 || date.getDay() === 6,
    }
  })
}

function buildWeekTicks(rangeStart: Date, totalDays: number, pxPerDay: number): TimelineTick[] {
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
      leftPx: offset * pxPerDay,
      widthPx: widthDays * pxPerDay,
      isToday: false,
      isWeekend: false,
    })

    offset += widthDays
  }

  return ticks
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
  const widthPx = totalDays * pxPerDay

  const months = buildMonthHeaders(paddedStart, totalDays, pxPerDay)
  const ticks = zoom === 'day'
    ? buildDayTicks(paddedStart, totalDays, pxPerDay, today)
    : zoom === 'week'
      ? buildWeekTicks(paddedStart, totalDays, pxPerDay)
      : []

  const todayOffsetDays = diffDays(paddedStart, today)

  return {
    rangeStart: paddedStart,
    totalDays,
    pxPerDay,
    widthPx,
    months,
    ticks,
    todayOffsetDays,
    isTodayInRange: todayOffsetDays >= 0 && todayOffsetDays < totalDays,
  }
}

export function getTaskLayout(task: GanttTask, rangeStart: Date, pxPerDay: number) {
  const start = parseDate(task.startDate)
  const due = parseDate(task.dueDate)
  const durationDays = Math.max(1, diffDays(start, due) + 1)
  const startOffsetDays = Math.max(0, diffDays(rangeStart, start))

  return {
    start,
    due,
    durationDays,
    startOffsetDays,
    leftPx: startOffsetDays * pxPerDay,
    widthPx: Math.max(pxPerDay, durationDays * pxPerDay),
  }
}
