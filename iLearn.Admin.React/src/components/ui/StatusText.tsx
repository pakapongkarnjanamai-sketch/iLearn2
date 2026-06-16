import type { ReactNode } from 'react'
import { Badge, type BadgeTone } from './Badge'

type StatusTextProps = {
  children: ReactNode
  tone?: 'neutral' | 'success' | 'warning' | 'danger'
}

export function StatusText({ children, tone = 'neutral' }: StatusTextProps) {
  const resolvedTone: BadgeTone = tone === 'success' || tone === 'warning' || tone === 'danger'
    ? tone
    : 'neutral'

  return (
    <Badge variant="outline" tone={resolvedTone}>
      {children}
    </Badge>
  )
}