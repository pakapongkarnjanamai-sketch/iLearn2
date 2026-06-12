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
  const hasMore = totalCount > 0 && loadedCount < totalCount

  return (
    <footer className="flex items-center justify-end border-t border-slate-200 bg-slate-50/80 px-4 py-2.5 text-xxs font-semibold text-slate-500">
      {loading && loadedCount > 0 ? (
        <div className="flex items-center gap-1.5 text-indigo-500 uppercase font-bold tracking-wider animate-pulse">
          <Loader2 className="h-3 w-3 animate-spin" />
          <span>Loading more...</span>
        </div>
      ) : hasMore ? (
        <span className="text-slate-400">Scroll down to load more</span>
      ) : totalCount > 0 ? (
        <span className="text-emerald-600 font-bold">All records loaded</span>
      ) : null}
    </footer>
  )
}
