import { Badge, type BadgeVariant } from './Badge'
import { courseStatusLabel, getCourseStatusTone } from '../../lib/labels'

// Label + tone mapping lives in lib/labels.ts (single source of truth for
// status vocabulary); re-exported here so existing importers keep working.
export { courseStatusLabel, getCourseStatusTone }

type CourseStatusBadgeProps = {
  status: string | null | undefined
  statusCode?: number | null
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
