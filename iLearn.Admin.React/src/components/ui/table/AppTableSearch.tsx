import type { ReactNode } from 'react'
import { Search } from 'lucide-react'

type AppTableSearchProps = {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  toolbarContent?: ReactNode
}

export function AppTableSearch({
  value,
  onChange,
  placeholder = 'Search...',
  toolbarContent
}: AppTableSearchProps) {
  return (
    <div className="flex flex-col gap-3 border-b border-slate-100/50 bg-slate-50/30 p-3 lg:flex-row lg:items-center">
      <div className="relative w-full sm:max-w-lg lg:max-w-xl">
        <Search className="absolute left-3 top-2 h-4 w-4 text-slate-400" />
        <input
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          className="w-full rounded-md border border-slate-200 bg-white py-1.5 pl-9 pr-4 text-xs font-semibold text-slate-700 shadow-xs transition focus:border-indigo-500 focus:outline-none"
        />
      </div>

      {toolbarContent && (
        <div className="flex min-w-0 flex-1 flex-wrap items-center gap-2">
          {toolbarContent}
        </div>
      )}
    </div>
  )
}
