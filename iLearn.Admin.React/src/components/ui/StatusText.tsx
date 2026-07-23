import type { ReactNode } from 'react'
import { Badge, type BadgeTone, type BadgeVariant } from './Badge'
import { COMMON_LABELS, t } from '../../lib/labels'

type StatusTextProps = {
  children?: ReactNode
  active?: boolean
  tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'info'
  activeLabel?: string
  inactiveLabel?: string
  variant?: BadgeVariant
}

export function StatusText({
  children,
  active,
  tone,
  activeLabel,
  inactiveLabel,
  variant = 'soft',
}: StatusTextProps) {
  let resolvedTone: BadgeTone = tone ?? 'neutral'
  let resolvedChildren: ReactNode = children

  if (typeof active === 'boolean') {
    if (!tone) {
      resolvedTone = active ? 'success' : 'neutral'
    }
    if (!children) {
      resolvedChildren = active
        ? (activeLabel ?? t(COMMON_LABELS.active))
        : (inactiveLabel ?? t(COMMON_LABELS.inactive))
    }
  } else if (tone === 'success' || tone === 'warning' || tone === 'danger' || tone === 'info') {
    resolvedTone = tone
  }

  return (
    <Badge variant={variant} tone={resolvedTone}>
      {resolvedChildren}
    </Badge>
  )
}
