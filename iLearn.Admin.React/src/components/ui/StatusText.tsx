import type { ReactNode } from 'react'

type StatusTextProps = {
  children: ReactNode
  tone?: 'neutral' | 'success' | 'warning' | 'danger'
}

export function StatusText({ children, tone = 'neutral' }: StatusTextProps) {
  return <span className={`admin-status-text admin-status-text--${tone}`}>{children}</span>
}