import type { ReactNode } from 'react'
import { Badge, type BadgeTone } from './Badge'

type StatusTextProps = {
  children?: ReactNode
  active?: boolean
  tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'info'
  activeLabel?: string
  inactiveLabel?: string
}

export function StatusText({
  children,
  active,
  tone,
  activeLabel = 'ใช้งานอยู่',
  inactiveLabel = 'ปิดใช้งาน',
}: StatusTextProps) {
  let resolvedTone: BadgeTone = tone ?? 'neutral'
  let resolvedChildren: ReactNode = children

  if (typeof active === 'boolean') {
    if (!tone) {
      resolvedTone = active ? 'success' : 'neutral'
    }
    if (!children) {
      resolvedChildren = active ? activeLabel : inactiveLabel
    }
  } else if (tone === 'success' || tone === 'warning' || tone === 'danger' || tone === 'info') {
    resolvedTone = tone
  }

  return (
    <Badge variant="outline" tone={resolvedTone}>
      {resolvedChildren}
    </Badge>
  )
}