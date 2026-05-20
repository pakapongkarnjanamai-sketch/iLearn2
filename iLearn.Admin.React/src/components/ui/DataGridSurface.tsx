import type { ReactNode } from 'react'

type DataGridSurfaceProps = {
  title: string
  note?: string
  actions?: ReactNode
  children: ReactNode
}

export function DataGridSurface({ title, note, actions, children }: DataGridSurfaceProps) {
  return (
    <section className="admin-grid-surface">
      <div className="admin-grid-surface-head">
        <div>
          <h2 className="admin-grid-title">{title}</h2>
          {note ? <p className="admin-grid-note">{note}</p> : null}
        </div>
        {actions ? <div className="admin-page-actions">{actions}</div> : null}
      </div>
      <div className="admin-grid-body">{children}</div>
    </section>
  )
}