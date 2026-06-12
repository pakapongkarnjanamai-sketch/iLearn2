import type { ReactNode } from 'react'

type StatusTone = 'success' | 'info' | 'warning' | 'danger' | 'neutral'

const toneStyles: Record<StatusTone, string> = {
  success: 'bg-emerald-100 text-emerald-800',
  info: 'bg-blue-100 text-blue-800',
  warning: 'bg-amber-100 text-amber-800',
  danger: 'bg-red-100 text-red-700',
  neutral: 'bg-slate-100 text-slate-700',
}

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
    <span className={`inline-flex px-2 py-0.5 rounded font-bold ${size === 'xxs' ? 'text-xxs' : 'text-xs'} ${toneStyles[resolved]}`}>
      {children}
    </span>
  )
}
