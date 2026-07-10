type SegmentedToggleOption = {
  value: any
  label: string
}

type SegmentedToggleProps = {
  options: SegmentedToggleOption[]
  value: any
  onChange: (value: any) => void
  variant?: 'segment' | 'filter'
  className?: string
}

export function SegmentedToggle({
  options,
  value,
  onChange,
  variant = 'segment',
  className = '',
}: SegmentedToggleProps) {
  if (variant === 'filter') {
    return (
      <div className={`flex items-center gap-1.5 select-none ${className}`.trim()}>
        {options.map(opt => {
          const isActive = opt.value === value
          return (
            <button
              key={opt.value}
              type="button"
              onClick={() => onChange(opt.value)}
              className={`rounded-lg border px-3 py-1.5 text-xs font-semibold transition-colors shrink-0 cursor-pointer ${
                isActive
                  ? 'border-indigo-500 bg-indigo-600 text-white shadow-3xs font-bold'
                  : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300 hover:bg-slate-50'
              }`}
            >
              {opt.label}
            </button>
          )
        })}
      </div>
    )
  }

  return (
    <div className={`flex items-center gap-0.5 bg-slate-100 p-0.5 rounded-lg select-none shrink-0 border border-slate-200/40 ${className}`.trim()}>
      {options.map(opt => {
        const isActive = opt.value === value
        return (
          <button
            key={opt.value}
            type="button"
            onClick={() => onChange(opt.value)}
            className={`py-1 px-2.5 text-center text-[11px] font-extrabold rounded-md transition cursor-pointer ${
              isActive ? 'bg-white text-indigo-700 shadow-3xs' : 'text-slate-500 hover:text-slate-700'
            }`}
          >
            {opt.label}
          </button>
        )
      })}
    </div>
  )
}
