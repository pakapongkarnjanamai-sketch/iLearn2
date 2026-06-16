import type { ReactNode } from 'react'
import { LoadingState } from '../LoadingState'

export type ExplorerColumn<TItem> = {
  key: string
  title: string
  headerClassName?: string
  cellClassName?: string
  render: (item: TItem) => ReactNode
}

type ExplorerTableProps<TItem> = {
  loading: boolean
  loadingLabel?: string
  emptyText: string
  columns: ExplorerColumn<TItem>[]
  items: TItem[]
  getRowKey: (item: TItem) => string
  onRowDoubleClick?: (item: TItem) => void
  rowClassName?: string
}

export function ExplorerTable<TItem>({
  loading,
  loadingLabel = 'Loading directory...',
  emptyText,
  columns,
  items,
  getRowKey,
  onRowDoubleClick,
  rowClassName = 'cursor-pointer transition hover:bg-slate-50/70',
}: ExplorerTableProps<TItem>) {
  return (
    <div className="min-h-0 flex-1 overflow-hidden rounded-lg border border-slate-200/80 bg-white shadow-3xs">
      {loading ? (
        <LoadingState size="section" label={loadingLabel} className="h-full" />
      ) : (
        <div className="custom-scrollbar h-full overflow-auto">
          <table className="min-w-full divide-y divide-slate-100 text-left text-xs">
            <thead className="sticky top-0 z-10 border-b border-slate-200 bg-slate-50/90 text-xxs font-extrabold uppercase tracking-wider text-slate-500">
              <tr>
                {columns.map(column => (
                  <th key={column.key} className={`px-4 py-2.5 ${column.headerClassName ?? ''}`.trim()}>
                    {column.title}
                  </th>
                ))}
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100 bg-white">
              {items.length === 0 ? (
                <tr>
                  <td colSpan={columns.length} className="px-4 py-12 text-center text-xs font-semibold text-slate-400">
                    {emptyText}
                  </td>
                </tr>
              ) : (
                items.map(item => (
                  <tr
                    key={getRowKey(item)}
                    className={rowClassName}
                    onDoubleClick={onRowDoubleClick ? () => onRowDoubleClick(item) : undefined}
                  >
                    {columns.map(column => (
                      <td key={column.key} className={`px-4 py-2.5 ${column.cellClassName ?? ''}`.trim()}>
                        {column.render(item)}
                      </td>
                    ))}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
