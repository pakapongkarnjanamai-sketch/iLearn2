import { StatusText } from './StatusText'

export type CourseStatusTone = 'success' | 'warning' | 'danger' | 'neutral'

type CourseStatusBadgeProps = {
  status: string | null | undefined
  statusCode?: number | null
}

const toneClassByTone: Record<CourseStatusTone, string> = {
  success: 'bg-emerald-100 text-emerald-800 border-emerald-200 font-bold',
  warning: 'bg-amber-100 text-amber-800 border-amber-200',
  danger: 'bg-rose-100 text-rose-800 border-rose-200',
  neutral: 'bg-slate-100 text-slate-800 border-slate-200',
}

function normalizeStatus(status: string | null | undefined) {
  return (status || '').trim().toLowerCase()
}

export function getCourseStatusTone(status: string | null | undefined, statusCode?: number | null): CourseStatusTone {
  if (typeof statusCode === 'number') {
    if (statusCode === 1) return 'success'
    if (statusCode === 0) return 'warning'
    if (statusCode === 2) return 'neutral'
  }

  const normalized = normalizeStatus(status)
  if (normalized === 'open' || normalized === 'active') return 'success'
  if (normalized === 'draft') return 'warning'
  if (normalized === 'closed') return 'neutral'
  return 'neutral'
}

export function CourseStatusBadge({ status, statusCode }: CourseStatusBadgeProps) {
  const tone = getCourseStatusTone(status, statusCode)
  const text = status?.trim() || '-'

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xxs font-semibold border ${toneClassByTone[tone]}`}>
      {text}
    </span>
  )
}

type CourseStatusTextProps = {
  status: string | null | undefined
  statusCode?: number | null
}

export function CourseStatusText({ status, statusCode }: CourseStatusTextProps) {
  const text = status?.trim() || '-'
  return (
    <StatusText tone={getCourseStatusTone(status, statusCode)}>
      {text}
    </StatusText>
  )
}
