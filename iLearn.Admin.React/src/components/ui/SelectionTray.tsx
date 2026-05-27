import { X, RotateCcw } from 'lucide-react'

type SelectionTrayProps<T> = {
  selectedItems: T[]
  getId: (item: T) => number | string
  getLabel: (item: T) => string
  onRemove: (id: number | string) => void
  onClear: () => void
  maxChipsToShow?: number
  title?: string
}

export function SelectionTray<T>({
  selectedItems,
  getId,
  getLabel,
  onRemove,
  onClear,
  maxChipsToShow = 12,
  title = 'Selected'
}: SelectionTrayProps<T>) {
  if (selectedItems.length === 0) {
    return (
      <div className="flex items-center justify-between premium-glass-card p-2.5 text-slate-400 text-xs">
        <span className="font-medium">No items selected yet.</span>
      </div>
    )
  }

  const displayedItems = selectedItems.slice(0, maxChipsToShow)
  const overflowCount = Math.max(0, selectedItems.length - maxChipsToShow)

  return (
    <div className="flex flex-col gap-1.5 premium-glass-card p-2.5">
      <div className="flex items-center justify-between pb-1 border-b border-slate-100">
        <span className="font-bold text-slate-700">
          {title} ({selectedItems.length})
        </span>
        <button
          type="button"
          onClick={onClear}
          className="text-slate-400 hover:text-red-600 flex items-center gap-1 cursor-pointer transition text-xxs font-bold uppercase"
        >
          <RotateCcw className="h-3 w-3" />
          Clear All
        </button>
      </div>
      <div className="flex flex-wrap gap-1 max-h-20 overflow-y-auto pr-1 custom-scrollbar">
        {displayedItems.map((item) => {
          const id = getId(item)
          return (
            <span
              key={id}
              className="inline-flex items-center gap-1.5 bg-linear-to-r from-indigo-50 to-indigo-100/50 border border-indigo-100 text-indigo-900 text-xs font-semibold px-2 py-0.5 rounded-md hover:scale-102 transition-transform duration-100"
            >
              <span className="truncate max-w-37.5">{getLabel(item)}</span>
              <button
                type="button"
                onClick={() => onRemove(id)}
                className="hover:text-red-600 transition focus:outline-none cursor-pointer p-0.5 rounded-full hover:bg-indigo-200/50"
                aria-label="Remove item"
              >
                <X className="h-3 w-3" />
              </button>
            </span>
          )
        })}
        {overflowCount > 0 && (
          <span className="inline-flex items-center bg-slate-100 border border-slate-200 text-slate-600 text-xs px-2 py-0.5 rounded font-bold">
            +{overflowCount} more
          </span>
        )}
      </div>
    </div>
  )
}
