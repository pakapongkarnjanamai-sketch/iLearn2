import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Bell } from 'lucide-react'
import { useNotifications } from '../../lib/notificationContext'
import { IconButton } from '../ui/IconButton'
import { AppButton } from '../ui/AppButton'
import { Badge } from '../ui/Badge'
import { formatNumber } from '../../lib/format'
import { NotificationRow } from '../shared/NotificationRow'
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

  const handleViewAll = () => {
    setIsOpen(false)
    navigate('/notifications')
  }

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
                <NotificationRow
                  key={item.id}
                  item={item}
                  onClick={handleItemClick}
                  compact
                />
              ))
            )}
          </div>

          {/* §3: Footer — View all notifications */}
          <div className="border-t border-slate-100">
            <AppButton
              variant="ghost"
              size="sm"
              onClick={handleViewAll}
              className="w-full text-xs py-2.5 min-h-0 rounded-none animate-none"
            >
              View all notifications
            </AppButton>
          </div>
        </div>
      )}
    </div>
  )
}
