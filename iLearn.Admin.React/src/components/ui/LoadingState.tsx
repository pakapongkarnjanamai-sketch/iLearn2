import { Loader2 } from 'lucide-react'

type LoadingStateProps = {
  /** 'page' for full-page loads, 'section' for in-panel loads. */
  size?: 'page' | 'section'
  /** Optional caption shown under the spinner. */
  label?: string
  /** Optional extra classes for the outer wrapper. */
  className?: string
}

export function LoadingState({ size = 'page', label, className = '' }: LoadingStateProps) {
  if (size === 'section') {
    return (
      <div className={`flex h-32 items-center justify-center ${className}`.trim()}>
        <div className="flex flex-col items-center gap-2 select-none">
          <Loader2 className="h-6 w-6 animate-spin text-slate-400" aria-label="Loading" />
          {label && <span className="mt-2 text-xs font-semibold text-slate-400">{label}</span>}
        </div>
      </div>
    )
  }

  return (
    <div className={`flex h-96 items-center justify-center ${className}`.trim()}>
      <div className="flex flex-col items-center gap-3 select-none">
        <Loader2 className="h-8 w-8 animate-spin text-indigo-500" aria-label="Loading" />
        {label && <span className="text-sm font-semibold text-slate-400">{label}</span>}
      </div>
    </div>
  )
}
