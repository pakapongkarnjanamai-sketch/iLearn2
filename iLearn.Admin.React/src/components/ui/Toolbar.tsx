import type { ReactNode } from 'react'

type ToolbarProps = {
  children: ReactNode
  align?: 'start' | 'end' | 'between'
}

export function Toolbar({ children, align = 'start' }: ToolbarProps) {
  const alignmentClass = align === 'between' ? 'justify-between' : align === 'end' ? 'justify-end' : 'justify-start'

  return <div className={`admin-toolbar ${alignmentClass}`}>{children}</div>
}