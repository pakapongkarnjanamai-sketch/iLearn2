import type { ReactNode } from 'react'

type PageHeaderProps = {
  title: string
  eyebrow?: string
  description?: string
  actions?: ReactNode
}

export function PageHeader({ actions }: PageHeaderProps) {
  if (!actions) return null
  return (
    <div className="flex justify-end mb-3.5 shrink-0 select-none">
      <div className="admin-page-actions">{actions}</div>
    </div>
  )
}