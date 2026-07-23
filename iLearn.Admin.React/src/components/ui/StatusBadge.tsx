import type { ReactNode } from 'react'
import { Badge, type BadgeTone } from './Badge'

type StatusTone = BadgeTone

/** Maps common API status strings to a badge tone. */
export function statusTone(status: string | null | undefined): StatusTone {
  switch (status) {
    case 'Completed':
    case 'เรียนจบแล้ว':
      return 'success'
    case 'In Progress':
    case 'InProgress':
    case 'กำลังเรียน':
    case 'Active':
    case 'Enrolling':
      return 'info'
    case 'Overdue':
    case 'เกินกำหนด':
    case 'Expired':
    case 'หมดอายุ':
      return 'danger'
    case 'Upcoming':
    case 'ใกล้กำหนด':
    case 'Due Soon':
    case 'NotStarted':
    case 'Not Started':
    case 'ยังไม่เริ่ม':
      return 'warning'
    case 'Unassigned':
      return 'neutral'
    default:
      return 'neutral'
  }
}

type StatusBadgeProps = {
  children: ReactNode
  /** Explicit tone; omit to derive from the status text via statusTone(). */
  tone?: StatusTone
  size?: 'xs' | 'xxs'
}

/** Solid soft-background status pill used in tables and KPI strips. */
export function StatusBadge({ children, tone, size = 'xs' }: StatusBadgeProps) {
  const resolved = tone ?? statusTone(typeof children === 'string' ? children : undefined)
  return (
    <Badge variant="soft" tone={resolved} size={size}>
      {children}
    </Badge>
  )
}
