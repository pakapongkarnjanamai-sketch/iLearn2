type ProgressBarProps = {
  /** Percentage 0–100. */
  value: number
  /** Green when complete, blue while in progress. */
  completed?: boolean
  /** Tailwind max-width class constraining the bar inside table cells. */
  maxWidthClass?: string
}

/** Slim progress bar with a percentage label, used in learner/enrollment tables. */
export function ProgressBar({ value, completed = false, maxWidthClass = 'max-w-24' }: ProgressBarProps) {
  return (
    <div className={`flex items-center gap-2 ${maxWidthClass}`}>
      <div className="w-full bg-slate-100 rounded-full h-1.5">
        <div
          className={`h-1.5 rounded-full ${completed ? 'bg-emerald-500' : 'bg-blue-600'}`}
          style={{ width: `${Math.min(100, Math.max(0, value))}%` }}
        />
      </div>
      <span className="font-bold text-xxs text-slate-500 shrink-0">{Math.round(value)}%</span>
    </div>
  )
}
