import type { ReactNode } from 'react'
import { Search, X } from 'lucide-react'

type AppTableSearchProps = {
  value: string
  onChange: (value: string) => void
  totalCount: number
  placeholder?: string
  toolbarContent?: ReactNode
}

export function AppTableSearch({
  value,
  onChange,
  totalCount,
  placeholder = 'Search...',
  toolbarContent
}: AppTableSearchProps) {
  return (
    <div className="flex flex-col gap-3 pb-2 pt-3 lg:flex-row lg:items-center lg:justify-between">
      <div className="flex min-w-0 flex-wrap items-center gap-2.5">
        <span className="text-xs font-semibold text-slate-500 select-none">
          Showing <strong className="text-slate-800">{totalCount}</strong> records
        </span>

        {toolbarContent && (
          <div className="flex min-w-0 flex-wrap items-center gap-2">
            {toolbarContent}
          </div>
        )}
      </div>

      <div className="relative w-full sm:max-w-lg lg:w-80 lg:shrink-0">
        <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
        <input
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          className="w-full rounded-lg border border-slate-200 bg-white py-2 pl-9 pr-9 text-xs font-semibold text-slate-700 shadow-3xs transition focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-100"
        />

        {value && (
          <button
            type="button"
            onClick={() => onChange('')}
            className="absolute right-2.5 top-2 rounded-full p-0.5 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
            aria-label="Clear search"
          >
            <X className="h-3 w-3" />
          </button>
        )}
      </div>
    </div>
  )
}
