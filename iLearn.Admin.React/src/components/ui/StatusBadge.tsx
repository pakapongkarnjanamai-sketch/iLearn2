import type { ReactNode } from 'react'
import { Badge, type BadgeTone } from './Badge'
import { statusTone } from '../../lib/labels'

// Tone mapping lives in lib/labels.ts (single source of truth for status
// vocabulary); re-exported here so existing importers keep working.
export { statusTone }

type StatusBadgeProps = {
  children: ReactNode
  /** Explicit tone; omit to derive from the status text via statusTone(). */
  tone?: BadgeTone
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
