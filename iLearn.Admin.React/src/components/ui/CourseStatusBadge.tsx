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

export function courseStatusLabel(status: string | null | undefined, statusCode?: number | null): string {
  if (typeof statusCode === 'number') {
    if (statusCode === 1) return 'เปิดใช้งาน'
    if (statusCode === 0) return 'ฉบับร่าง'
    if (statusCode === 2) return 'ปิดใช้งาน'
  }

  const normalized = normalizeStatus(status)
  if (normalized === 'open' || normalized === 'active') return 'เปิดใช้งาน'
  if (normalized === 'draft') return 'ฉบับร่าง'
  if (normalized === 'closed') return 'ปิดใช้งาน'
  return status?.trim() || '—'
}

export function CourseStatusBadge({ status, statusCode }: CourseStatusBadgeProps) {
  const tone = getCourseStatusTone(status, statusCode)
  const text = courseStatusLabel(status, statusCode)

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
  return (
    <Badge variant={variant} tone={getCourseStatusTone(status, statusCode)}>
      {courseStatusLabel(status, statusCode)}
    </Badge>
  )
}
