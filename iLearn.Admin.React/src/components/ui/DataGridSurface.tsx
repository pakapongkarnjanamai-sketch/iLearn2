import type { ReactNode } from 'react'

type DataGridSurfaceProps = {
  title: string
  note?: string
  actions?: ReactNode
  children: ReactNode
}

export function DataGridSurface({ title, note, actions, children }: DataGridSurfaceProps) {
  return (
    <section className="admin-card admin-grid-surface">
      <div className="admin-grid-surface-head flex-wrap">
        <div className="min-w-0">
          <h2 className="admin-grid-title">{title}</h2>
          {note && <p className="admin-grid-note">{note}</p>}
        </div>
        {actions && <div className="admin-toolbar shrink-0 justify-end">{actions}</div>}
      </div>
      <div className="admin-grid-body">{children}</div>
    </section>
  )
}