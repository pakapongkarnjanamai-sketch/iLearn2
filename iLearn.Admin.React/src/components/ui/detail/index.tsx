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
  colSpan = 1,
  className,
  labelClassName,
}: FactProps) {
  const spanClass =
    colSpan === 'full' ? 'col-span-full' : colSpan === 2 ? 'sm:col-span-2' : ''

  const containerClass = [spanClass, className].filter(Boolean).join(' ')
  const labelClass = [
    'text-slate-400 font-bold uppercase tracking-wider',
    labelClassName,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div className={containerClass}>
      <dt className={labelClass}>{label}</dt>
      <dd>{children}</dd>
    </div>
  )
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
