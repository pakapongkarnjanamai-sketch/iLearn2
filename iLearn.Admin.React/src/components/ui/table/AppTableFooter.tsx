import { Loader2 } from 'lucide-react'

type AppTableFooterProps = {
  loadedCount: number
  totalCount: number
  loading: boolean
}

export function AppTableFooter({
  loadedCount,
  totalCount,
  loading
}: AppTableFooterProps) {
  return (
    <footer className="flex flex-col sm:flex-row items-center sm:justify-between p-4 gap-3 border-t border-slate-100 bg-slate-50/50 text-xs font-semibold text-slate-500">
      <div className="select-none">
        {totalCount > 0 ? (
          <span>
            Showing <strong className="text-slate-800">{loadedCount}</strong> of{' '}
            <strong className="text-slate-800">{totalCount}</strong> records
            {loadedCount < totalCount ? (
              <span className="text-slate-400 font-normal">
                {' '}
                (Scroll down to load more)
              </span>
            ) : (
              <span className="text-emerald-600 font-bold">
                {' '}
                (All records loaded)
              </span>
            )}
          </span>
        ) : (
          <span>No records found</span>
        )}
      </div>

      {loading && loadedCount > 0 && (
        <div className="flex items-center gap-1.5 text-indigo-500 text-xxs uppercase font-bold tracking-wider animate-pulse">
          <Loader2 className="h-3 w-3 animate-spin" />
          <span>Loading more...</span>
        </div>
      )}
    </footer>
  )
}
