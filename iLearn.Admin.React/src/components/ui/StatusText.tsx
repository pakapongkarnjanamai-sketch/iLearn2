import type { ReactNode } from 'react'

type StatusTextProps = {
  children: ReactNode
  tone?: 'neutral' | 'success' | 'warning' | 'danger'
}

const toneStyles: Record<string, string> = {
  neutral: 'border-slate-200 bg-white text-slate-500',
  success: 'border-emerald-300 bg-emerald-50 text-emerald-600',
  warning: 'border-amber-300 bg-amber-50 text-amber-600',
  danger: 'border-red-300 bg-red-50 text-red-600',
}

export function StatusText({ children, tone = 'neutral' }: StatusTextProps) {
  return (
    <span className={`inline-flex min-h-[24px] items-center rounded-full border px-2.5 text-xs font-bold ${toneStyles[tone] ?? toneStyles.neutral}`}>
      {children}
    </span>
  )
}