import { useState, useEffect, useCallback, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Bell } from 'lucide-react'
import { Card } from '../../components/ui/Card'
import { AppButton } from '../../components/ui/AppButton'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotificationRow } from '../../components/shared/NotificationRow'
import { useNotifications } from '../../lib/notificationContext'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatNumber } from '../../lib/format'
import type { NotificationDto, NotificationListDto } from '../../lib/notificationTypes'
import { ADMIN_LABELS, t, tf } from '../../lib/labels'

type ApiResponse<T> = { success: boolean; data: T; message?: string }

const PAGE_SIZE = 20

export function NotificationsPage() {
  const navigate = useNavigate()
  const { markRead, markAllRead, unreadCount, subscribeHubEvent } = useNotifications()

  const [items, setItems] = useState<NotificationDto[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)
  const [filter, setFilter] = useState<'all' | 'unread'>('all')
  const [skip, setSkip] = useState(0)
  const filterOptions = [
    { value: 'all', label: t(ADMIN_LABELS.all) },
    { value: 'unread', label: t(ADMIN_LABELS.unread) },
  ]

  // Track ids already in the local list for realtime dedupe
  const seenIdsRef = useRef<Set<number>>(new Set())

  const fetchPage = useCallback(async (currentSkip: number, unreadOnly: boolean, append: boolean) => {
    if (append) {
      setLoadingMore(true)
    } else {
      setLoading(true)
    }

    try {
      const params = new URLSearchParams({
        take: String(PAGE_SIZE),
        skip: String(currentSkip),
      })
      if (unreadOnly) {
        params.set('unreadOnly', 'true')
      }

      const resp = await fetchWithAccessControl<ApiResponse<NotificationListDto>>(`Notifications?${params}`)
      if (resp && resp.success) {
        if (append) {
          setItems(prev => {
            const combined = [...prev, ...resp.data.items]
            // Update seen ids
            const ids = new Set(seenIdsRef.current)
            for (const it of resp.data.items) ids.add(it.id)
            seenIdsRef.current = ids
            return combined
          })
        } else {
          setItems(resp.data.items)
          const ids = new Set<number>()
          for (const it of resp.data.items) ids.add(it.id)
          seenIdsRef.current = ids
        }
        setTotalCount(resp.data.totalCount)
      }
    } catch (err) {
      console.error('Failed to fetch notifications page:', err)
    } finally {
      setLoading(false)
      setLoadingMore(false)
    }
  }, [])

  // Initial load + reset when filter changes
  useEffect(() => {
    setSkip(0)
    void fetchPage(0, filter === 'unread', false)
  }, [filter, fetchPage])

  // Subscribe to realtime NotificationCreated events — prepend new items
  useEffect(() => {
    const unsubscribe = subscribeHubEvent('NotificationCreated', (...args: unknown[]) => {
      const dto = args[0] as NotificationDto
      if (seenIdsRef.current.has(dto.id)) return
      seenIdsRef.current.add(dto.id)

      // In unread filter, always prepend (new items are unread).
      // In all filter, always prepend too.
      setItems(prev => [dto, ...prev])
      setTotalCount(prev => prev + 1)
    })
    return unsubscribe
  }, [subscribeHubEvent])

  const handleLoadMore = () => {
    const nextSkip = skip + PAGE_SIZE
    setSkip(nextSkip)
    void fetchPage(nextSkip, filter === 'unread', true)
  }

  const handleItemClick = async (item: NotificationDto) => {
    if (!item.isRead) {
      await markRead(item.id)
      // Update local list to reflect read state
      setItems(prev =>
        prev.map(it => (it.id === item.id ? { ...it, isRead: true } : it))
      )
    }
    if (item.linkPath) {
      navigate(item.linkPath)
    }
  }

  const handleMarkAllRead = async () => {
    await markAllRead()
    // Sync local list
    setItems(prev => prev.map(it => ({ ...it, isRead: true })))
  }

  const handleFilterChange = (value: 'all' | 'unread') => {
    setFilter(value)
  }

  const hasMore = items.length < totalCount

  return (
    <div className="flex flex-col gap-4 max-w-4xl mx-auto w-full">
      <Card
        title={t(ADMIN_LABELS.notifications)}
        icon={Bell}
        actions={
          <div className="flex items-center gap-3">
            <SegmentedToggle
              options={filterOptions}
              value={filter}
              onChange={handleFilterChange}
              variant="segment"
            />
            <AppButton
              variant="secondary"
              size="sm"
              onClick={handleMarkAllRead}
              disabled={unreadCount === 0}
            >
              {t(ADMIN_LABELS.markAllRead)}
            </AppButton>
          </div>
        }
      >
        {loading ? (
          <div className="py-12">
            <LoadingState size="section" label={t(ADMIN_LABELS.loadingNotifications)} />
          </div>
        ) : items.length === 0 ? (
          <div className="py-16 text-center text-slate-400 text-sm">
            {t(ADMIN_LABELS.noNotifications)}
          </div>
        ) : (
          <>
            <div className="divide-y divide-slate-100">
              {items.map(item => (
                <NotificationRow
                  key={item.id}
                  item={item}
                  onClick={handleItemClick}
                />
              ))}
            </div>

            {/* Showing X of Y + Load more */}
            <div className="flex items-center justify-between px-5 py-3 border-t border-slate-100 bg-slate-50/50 text-xs text-slate-500">
              <span>
                {tf(ADMIN_LABELS.showingOf, formatNumber(items.length), formatNumber(totalCount))}
              </span>
              {hasMore && (
                <AppButton
                  variant="ghost"
                  size="sm"
                  onClick={handleLoadMore}
                  loading={loadingMore}
                >
                  {t(ADMIN_LABELS.loadMore)}
                </AppButton>
              )}
            </div>
          </>
        )}
      </Card>
    </div>
  )
}
