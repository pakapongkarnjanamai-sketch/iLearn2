import type { IScaleConfig, ITask } from '@svar-ui/react-gantt'
import { formatDayOfMonth, formatMonthShort, formatMonthYear, formatYear } from '../../../lib/format'
import { ganttStatusHex } from './ganttStatus'

export type GanttZoom = 'day' | 'week' | 'month'

// Mirrors AssignmentGanttTaskDto (iLearn.Application/DTOs/AssignmentApiResponseDtos.cs).
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

const DAY_MS = 86_400_000

const parseLocalDate = (value: string) => {
  const parsed = new Date(value)
  return new Date(parsed.getFullYear(), parsed.getMonth(), parsed.getDate())
}

const diffDays = (from: Date, to: Date) =>
  Math.round((to.getTime() - from.getTime()) / DAY_MS)

export const svarScales: Record<GanttZoom, IScaleConfig[]> = {
  day: [
    { unit: 'month', step: 1, format: formatMonthYear },
    // Day number only: a 28px cell fits ~2 characters, and appending a weekday name
    // overflowed 19 of 30 visible cells (measured). Weekend shading covers orientation.
    { unit: 'day', step: 1, format: formatDayOfMonth },
  ],
  week: [
    { unit: 'month', step: 1, format: formatMonthYear },
    // `next` is absent on the last cell — fall back to a full 7-day span rather than
    // subtracting a day from the cell's own start, which prints the previous month.
    { unit: 'week', step: 1, format: (date, next) => `${formatDayOfMonth(date)}-${formatDayOfMonth(new Date((next?.getTime() ?? date.getTime() + 7 * DAY_MS) - DAY_MS))}` },
  ],
  month: [
    { unit: 'year', step: 1, format: formatYear },
    { unit: 'month', step: 1, format: formatMonthShort },
  ],
}

export const getSvarTaskColor = (status: string) => ganttStatusHex(status)

export function mapAssignmentToSvarTask(task: GanttTask): ITask {
  const start = parseLocalDate(task.startDate)
  const end = parseLocalDate(task.dueDate)
  const description = task.title.trim() === task.assignmentNo ? '' : task.title.trim()
  const displayLabel = description || task.assignmentNo

  return {
    id: task.id,
    text: displayLabel,
    start,
    end,
    duration: Math.max(1, diffDays(start, end) + 1),
    type: 'task',
    assignmentNo: task.assignmentNo,
    description,
    status: task.status,
    dueDate: task.dueDate,
    title: task.title,
    color: getSvarTaskColor(task.status),
  }
}

export function mapAssignmentsToSvarTasks(tasks: GanttTask[]) {
  return tasks.map(mapAssignmentToSvarTask)
}

export function getSvarDateRange(tasks: GanttTask[]) {
  if (tasks.length === 0) return undefined

  const dates = tasks.flatMap((task) => [parseLocalDate(task.startDate), parseLocalDate(task.dueDate)])
  const minDate = new Date(Math.min(...dates.map((date) => date.getTime())))
  const maxDate = new Date(Math.max(...dates.map((date) => date.getTime())))

  minDate.setDate(minDate.getDate() - 3)
  maxDate.setDate(maxDate.getDate() + 3)

  return { start: minDate, end: maxDate }
}

export function getDefaultZoom(tasks: GanttTask[]): GanttZoom {
  if (tasks.length === 0) return 'week'

  const dates = tasks.flatMap((task) => [parseLocalDate(task.startDate), parseLocalDate(task.dueDate)])
  const minTime = Math.min(...dates.map((date) => date.getTime()))
  const maxTime = Math.max(...dates.map((date) => date.getTime()))
  const totalDays = Math.max(14, Math.round((maxTime - minTime) / DAY_MS) + 7)
  return totalDays <= 60 ? 'day' : 'week'
}

