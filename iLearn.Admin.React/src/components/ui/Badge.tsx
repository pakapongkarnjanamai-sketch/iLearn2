import type { ReactNode } from 'react'

export type BadgeTone = 'success' | 'info' | 'warning' | 'danger' | 'neutral'

export type BadgeVariant = 'soft' | 'outline' | 'tag'

type BadgeSize = 'xs' | 'xxs'

type BadgeProps = {
  children: ReactNode
  tone?: BadgeTone
  variant?: BadgeVariant
  size?: BadgeSize
  className?: string
}

const sizeClasses: Record<BadgeSize, string> = {
  xs: 'text-xs',
  xxs: 'text-xxs',
}

const variantBaseClasses: Record<BadgeVariant, string> = {
  soft: 'inline-flex items-center rounded px-2 py-0.5 font-bold',
  outline: 'inline-flex min-h-[24px] items-center rounded-full border px-2.5 py-0.5 font-bold',
  tag: 'inline-flex items-center rounded border px-2 py-0.5 font-extrabold uppercase',
}

const variantToneClasses: Record<BadgeVariant, Record<BadgeTone, string>> = {
  soft: {
    success: 'bg-emerald-100 text-emerald-800',
    info: 'bg-blue-100 text-blue-800',
    warning: 'bg-amber-100 text-amber-800',
    danger: 'bg-red-100 text-red-700',
    neutral: 'bg-slate-100 text-slate-700',
  },
  outline: {
    success: 'border-emerald-300 bg-emerald-50 text-emerald-700',
    info: 'border-blue-200 bg-blue-50 text-blue-700',
    warning: 'border-amber-300 bg-amber-50 text-amber-700',
    danger: 'border-red-300 bg-red-50 text-red-600',
    neutral: 'border-slate-200 bg-white text-slate-500',
  },
  tag: {
    success: 'border-emerald-300 bg-emerald-50 text-emerald-700',
    info: 'border-blue-200 bg-blue-50 text-blue-700',
    warning: 'border-amber-300 bg-amber-50 text-amber-700',
    danger: 'border-red-300 bg-red-50 text-red-600',
    neutral: 'border-slate-200 bg-white text-slate-500',
  },
}

function joinClasses(...classes: Array<string | undefined>) {
  return classes.filter(Boolean).join(' ')
}

export function Badge({ children, tone = 'neutral', variant = 'soft', size, className }: BadgeProps) {
  const resolvedSize = size ?? (variant === 'tag' ? 'xxs' : 'xs')

  return (
    <span
      className={joinClasses(
        variantBaseClasses[variant],
        sizeClasses[resolvedSize],
        variantToneClasses[variant][tone],
        className,
      )}
    >
      {children}
    </span>
  )
}