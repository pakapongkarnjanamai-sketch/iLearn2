import type { ReactNode } from 'react'
import { Search, X } from 'lucide-react'
import { t, UI_LABELS } from '../../lib/labels'

type ListToolbarProps = {
  count?: number
  countUnit?: string
  searchValue: string
  onSearchChange: (value: string) => void
  searchPlaceholder?: string
  toolbarContent?: ReactNode
  onClearSearch?: () => void
}

export function ListToolbar({
  count,
  countUnit,
  searchValue,
  onSearchChange,
  searchPlaceholder,
  toolbarContent,
  onClearSearch,
}: ListToolbarProps) {
  const hasCount = typeof count === 'number'
  const hasToolbarContent = toolbarContent !== undefined && toolbarContent !== null

  const handleClearSearch = () => {
    if (onClearSearch) {
      onClearSearch()
      return
    }

    onSearchChange('')
  }

  return (
    <div className="flex flex-col gap-3 pb-2 pt-3 lg:flex-row lg:items-center lg:justify-between">
      {(hasCount || hasToolbarContent) && (
        <div className="flex min-w-0 flex-wrap items-center gap-2.5">
          {hasCount && (
            <span className="text-xs font-semibold text-slate-500 select-none">
              {t(UI_LABELS.showing)} <strong className="text-slate-800">{count}</strong> {countUnit ?? t(UI_LABELS.records)}
            </span>
          )}

          {hasToolbarContent && (
            <div className="flex min-w-0 flex-wrap items-center gap-2">
              {toolbarContent}
            </div>
          )}
        </div>
      )}

      <div className="relative w-full sm:max-w-lg lg:w-80 lg:shrink-0">
        <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
        <input
          type="text"
          value={searchValue}
          onChange={(event) => onSearchChange(event.target.value)}
          placeholder={searchPlaceholder ?? t(UI_LABELS.search)}
          className="w-full rounded-lg border border-slate-200 bg-white py-2 pl-9 pr-9 text-xs font-semibold text-slate-700 shadow-3xs transition focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-100"
        />

        {searchValue && (
          <button
            type="button"
            onClick={handleClearSearch}
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