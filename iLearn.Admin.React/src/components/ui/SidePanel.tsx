import type { ReactNode } from 'react'

type SidePanelProps = {
  title: string
  note?: string
  children: ReactNode
}

export function SidePanel({ title, note, children }: SidePanelProps) {
  return (
    <aside className="admin-side-panel">
      <div className="admin-side-panel-head">
        <div>
          <h2 className="admin-side-panel-title">{title}</h2>
          {note ? <p className="admin-side-panel-note">{note}</p> : null}
        </div>
      </div>
      <div className="admin-side-panel-body">{children}</div>
    </aside>
  )
}