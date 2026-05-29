import type { ReactNode } from 'react'

type PageHeaderProps = {
  actions?: ReactNode
}

export function PageHeader({ actions }: PageHeaderProps) {
  if (!actions) return null
  return (
    <div className="flex justify-end mb-3.5 shrink-0 select-none">
      <div className="flex items-center gap-2">{actions}</div>
    </div>
  )
}