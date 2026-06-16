import type { ReactNode } from 'react'
import { Badge, type BadgeTone } from './Badge'

type StatusTone = BadgeTone

/** Maps common API status strings to a badge tone. */
export function statusTone(status: string | null | undefined): StatusTone {
  switch (status) {
    case 'Completed':
      return 'success'
    case 'In Progress':
    case 'InProgress':
    case 'Active':
    case 'Enrolling':
      return 'info'
    case 'Overdue':
    case 'Expired':
      return 'danger'
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
