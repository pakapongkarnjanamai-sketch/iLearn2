import type { ReactNode } from 'react'

type DataGridSurfaceProps = {
  title: string
  note?: string
  actions?: ReactNode
  children: ReactNode
}

export function DataGridSurface({ title, note, actions, children }: DataGridSurfaceProps) {
  return (
    <section className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg border border-slate-200 bg-white pt-5 px-6 shadow-xs">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 bg-white pb-3.5">
        <div className="min-w-0">
          <h2 className="text-sm font-bold">{title}</h2>
          {note && <p className="mt-0.5 text-xs text-slate-500">{note}</p>}
        </div>
        {actions && <div className="flex shrink-0 items-center gap-2 justify-end">{actions}</div>}
      </div>
      <div className="flex min-h-0 flex-1 [&>*]:min-h-0 [&>*]:flex-1">{children}</div>
    </section>
  )
}