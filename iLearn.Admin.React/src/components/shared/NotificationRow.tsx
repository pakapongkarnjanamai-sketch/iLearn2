import { Badge } from '../ui/Badge'
import { formatRelativeTime } from '../../lib/format'
import type { NotificationDto } from '../../lib/notificationTypes'

const levelToneMap = {
  success: 'success',
  error: 'danger',
  info: 'info',
} as const

type NotificationRowProps = {
  item: NotificationDto
  onClick: (item: NotificationDto) => void
  /** compact = dropdown style (tighter padding, smaller text); full = page style */
  compact?: boolean
}

/**
 * Shared notification row used in both the bell dropdown and the full
 * /notifications page.  Rendered as a full-width <button> for accessibility
 * (pattern accepted in PLAN-089 Finding 3).
 */
export function NotificationRow({ item, onClick, compact = false }: NotificationRowProps) {
  return (
    <button
      onClick={() => onClick(item)}
      className={`w-full text-left transition cursor-pointer flex gap-3 items-start border-none ${
        compact ? 'px-4 py-3' : 'px-5 py-4'
      } ${
        item.isRead ? 'bg-white hover:bg-slate-50/80' : 'bg-indigo-50/20 hover:bg-indigo-50/40'
      }`}
    >
      <div className="mt-1 shrink-0 flex items-center">
        {!item.isRead && (
          <span className="w-1.5 h-1.5 rounded-full bg-indigo-600 mr-1.5 shrink-0" title="Unread" />
        )}
        <Badge
          tone={levelToneMap[item.level as keyof typeof levelToneMap] || 'neutral'}
          size="xxs"
          variant="soft"
          className="uppercase font-bold"
        >
          {item.level}
        </Badge>
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-baseline justify-between gap-2">
          <h4
            className={`${compact ? 'text-[13px]' : 'text-sm'} text-slate-800 truncate ${
              item.isRead ? 'font-medium' : 'font-semibold'
            }`}
          >
            {item.title}
          </h4>
          <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-slate-400 shrink-0`}>
            {formatRelativeTime(item.createdAt)}
          </span>
        </div>
        {item.message && (
          <p
            className={`text-slate-500 mt-0.5 line-clamp-2 break-words ${
              compact ? 'text-xs' : 'text-[13px]'
            }`}
          >
            {item.message}
          </p>
        )}
      </div>
    </button>
  )
}
