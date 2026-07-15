import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Bell } from 'lucide-react'
import { useNotifications } from '../../lib/notificationContext'
import { IconButton } from '../ui/IconButton'
import { AppButton } from '../ui/AppButton'
import { Badge } from '../ui/Badge'
import { formatNumber, formatRelativeTime } from '../../lib/format'
import type { NotificationDto } from '../../lib/notificationTypes'

export function NotificationBell() {
  const { items, unreadCount, loading, loadList, markRead, markAllRead } = useNotifications()
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const buttonRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()

  const handleToggle = () => {
    if (!isOpen) {
      void loadList()
    }
    setIsOpen(prev => !prev)
  }

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node) &&
        buttonRef.current &&
        !buttonRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener('keydown', handleKeyDown)
    }
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen])

  const handleItemClick = async (item: NotificationDto) => {
    if (!item.isRead) {
      await markRead(item.id)
    }
    setIsOpen(false)
    if (item.linkPath) {
      navigate(item.linkPath)
    }
  }

  const handleMarkAllRead = async () => {
    await markAllRead()
  }

  const levelToneMap = {
    success: 'success',
    error: 'danger',
    info: 'info',
  } as const

  return (
    <div className="relative inline-block text-left" ref={buttonRef}>
      <div className="relative">
        <IconButton
          icon={Bell}
          title="Notifications"
          onClick={handleToggle}
          className="border border-slate-200 bg-white"
        />
        {unreadCount > 0 && (
          <Badge
            tone="danger"
            size="xxs"
            variant="soft"
            className="absolute -top-1 -right-1 z-10 scale-90 border border-white"
          >
            {unreadCount > 99 ? '99+' : formatNumber(unreadCount)}
          </Badge>
        )}
      </div>

      {isOpen && (
        <div
          ref={dropdownRef}
          className="absolute right-0 mt-2 w-80 sm:w-96 rounded-xl border border-slate-200 bg-white/95 backdrop-blur-md shadow-xl z-50 overflow-hidden flex flex-col"
        >
          <div className="flex items-center justify-between px-4 py-2.5 border-b border-slate-100 bg-slate-50/50">
            <span className="font-bold text-slate-800 text-sm">Notifications</span>
            <AppButton
              variant="ghost"
              size="sm"
              onClick={handleMarkAllRead}
              disabled={unreadCount === 0}
              className="text-xs px-2 py-1 min-h-0 animate-none"
            >
              Mark all read
            </AppButton>
          </div>

          <div className="max-h-96 overflow-y-auto custom-scrollbar flex-1 divide-y divide-slate-100">
            {loading && items.length === 0 ? (
              <div className="py-8 text-center text-slate-400 text-xs">
                Loading notifications...
              </div>
            ) : items.length === 0 ? (
              <div className="py-12 text-center text-slate-400 text-xs">
                No notifications yet
              </div>
            ) : (
              items.map(item => (
                <button
                  key={item.id}
                  onClick={() => void handleItemClick(item)}
                  className={`w-full px-4 py-3 text-left transition cursor-pointer flex gap-3 items-start border-none ${
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
                      <h4 className={`text-[13px] text-slate-800 truncate ${item.isRead ? 'font-medium' : 'font-semibold'}`}>
                        {item.title}
                      </h4>
                      <span className="text-[10px] text-slate-400 shrink-0">
                        {formatRelativeTime(item.createdAt)}
                      </span>
                    </div>
                    {item.message && (
                      <p className="text-xs text-slate-500 mt-0.5 line-clamp-2 break-words">
                        {item.message}
                      </p>
                    )}
                  </div>
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  )
}
