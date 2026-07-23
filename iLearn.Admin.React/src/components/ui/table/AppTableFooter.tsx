import { Loader2 } from 'lucide-react'
import { t, UI_LABELS } from '../../../lib/labels'

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
          <span>{t(UI_LABELS.loadingMore)}</span>
        </div>
      ) : hasMore ? (
        <span className="text-slate-400">{t(UI_LABELS.scrollToLoadMore)}</span>
      ) : totalCount > 0 ? (
        <span className="text-emerald-600 font-bold">{t(UI_LABELS.allRecordsLoaded)}</span>
      ) : null}
    </footer>
  )
}
