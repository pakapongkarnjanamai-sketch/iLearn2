import type { ReactNode } from 'react'

type DetailLayoutProps = {
  sidebar: ReactNode
  children: ReactNode
}

/** Shared two-column detail layout: content left + controls sidebar right. */
export function DetailLayout({ sidebar, children }: DetailLayoutProps) {
  return (
    <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start">
      <div className="min-w-0">{children}</div>
      {sidebar}
    </div>
  )
}

type DetailCardProps = {
  children: ReactNode
  className?: string
}

/** Standard bordered white card container for detail sections. */
export function DetailCard({ children, className }: DetailCardProps) {
  const classes = ['rounded-lg border border-slate-200 bg-white p-5 space-y-5', className]
    .filter(Boolean)
    .join(' ')

  return <section className={classes}>{children}</section>
}

type FactGridProps = {
  cols?: 2 | 3
  className?: string
  children: ReactNode
}

/** Grid wrapper for label-value facts in detail pages. */
export function FactGrid({ cols = 3, className, children }: FactGridProps) {
  const colClass = cols === 2 ? 'grid-cols-1 sm:grid-cols-2' : 'grid-cols-2 sm:grid-cols-3'
  const classes = ['grid gap-x-6 gap-y-5 text-xs', colClass, className].filter(Boolean).join(' ')

  return <dl className={classes}>{children}</dl>
}

type FactProps = {
  label: string
  children: ReactNode
  mono?: boolean
  colSpan?: 1 | 2 | 'full'
  className?: string
  labelClassName?: string
  valueClassName?: string
}

/** Reusable single fact item (dt/dd). */
export function Fact({
  label,
  children,
  mono = false,
  colSpan = 1,
  className,
  labelClassName,
  valueClassName,
}: FactProps) {
  const spanClass =
    colSpan === 'full' ? 'col-span-full' : colSpan === 2 ? 'sm:col-span-2' : ''

  const containerClass = ['space-y-1', spanClass, className].filter(Boolean).join(' ')
  const labelClass = [
    'text-slate-400 font-bold uppercase tracking-wider',
    labelClassName,
  ]
    .filter(Boolean)
    .join(' ')
  const valueClass = [mono ? 'font-mono' : '', valueClassName].filter(Boolean).join(' ')

  return (
    <div className={containerClass}>
      <dt className={labelClass}>{label}</dt>
      <dd className={valueClass || undefined}>{children}</dd>
    </div>
  )
}

type StatTileProps = {
  label: ReactNode
  children: ReactNode
  className?: string
}

/** Standard KPI tile component inside detail overview cards. */
export function StatTile({ label, children, className }: StatTileProps) {
  const containerClass = [
    'rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div className={containerClass}>
      <div className="text-[10px] font-extrabold uppercase text-slate-400">{label}</div>
      <div className="mt-0.5 flex items-center justify-center text-lg font-bold text-slate-800 tabular-nums">
        {children}
      </div>
    </div>
  )
}

type StatTileRowProps = {
  cols?: 2 | 3 | 4
  className?: string
  children: ReactNode
}

/** Horizontal row container for StatTile items. */
export function StatTileRow({ cols = 3, className, children }: StatTileRowProps) {
  const colClass =
    cols === 2
      ? 'grid-cols-2'
      : cols === 4
      ? 'grid-cols-2 sm:grid-cols-4'
      : 'grid-cols-3'

  const classes = ['grid gap-3', colClass, className].filter(Boolean).join(' ')

  return <div className={classes}>{children}</div>
}

type DetailSubSectionProps = {
  title: string
  children: ReactNode
}

/** Divider + mini heading block used to separate groups inside a detail card. */
export function DetailSubSection({ title, children }: DetailSubSectionProps) {
  return (
    <section className="space-y-2">
      <hr className="border-slate-100" />
      <div className="text-xxs font-extrabold uppercase text-slate-400">{title}</div>
      {children}
    </section>
  )
}
