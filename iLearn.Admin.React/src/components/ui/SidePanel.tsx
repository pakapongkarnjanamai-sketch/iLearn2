import type { ReactNode } from 'react'

type SidePanelProps = {
  title: string
  note?: string
  children: ReactNode
}

export function SidePanel({ title, note, children }: SidePanelProps) {
  return (
    <aside className="rounded-lg border border-slate-200 bg-white shadow-xs">
      <div className="flex items-center justify-between gap-3 border-b border-slate-200 bg-white p-3.5">
        <div>
          <h2 className="text-sm font-bold">{title}</h2>
          {note ? <p className="mt-0.5 text-xs text-slate-500">{note}</p> : null}
        </div>
      </div>
      <div className="p-3.5">{children}</div>
    </aside>
  )
}