import { Badge, type BadgeVariant } from './Badge'

export type CourseStatusTone = 'success' | 'warning' | 'danger' | 'neutral'

type CourseStatusBadgeProps = {
  status: string | null | undefined
  statusCode?: number | null
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
    <Badge variant="soft" tone={tone} size="xxs">
      {text}
    </Badge>
  )
}

type CourseStatusTextProps = {
  status: string | null | undefined
  statusCode?: number | null
  variant?: BadgeVariant
}

export function CourseStatusText({ status, statusCode, variant = 'soft' }: CourseStatusTextProps) {
  const text = status?.trim() || '-'
  return (
    <Badge variant={variant} tone={getCourseStatusTone(status, statusCode)}>
      {text}
    </Badge>
  )
}
