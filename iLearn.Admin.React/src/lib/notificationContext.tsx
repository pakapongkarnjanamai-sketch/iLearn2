import { createContext, useContext, useState, useEffect, useRef, type ReactNode } from 'react'
import { fetchWithAccessControl } from './apiClient'
import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { appConfig } from '../config/appConfig'
import { toast } from './toast'
import type { NotificationDto, NotificationListDto } from './notificationTypes'
import { useSession } from './sessionContext'

type ApiResponse<T> = { success: boolean; data: T; message?: string }

interface NotificationContextType {
  items: NotificationDto[]
  unreadCount: number
  loading: boolean
  loadList: () => Promise<void>
  markRead: (id: number) => Promise<void>
  markAllRead: () => Promise<void>
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined)

export function NotificationProvider({ children }: { children: ReactNode }) {
  const { state: sessionState } = useSession()
  const [items, setItems] = useState<NotificationDto[]>([])
  const [unreadCount, setUnreadCount] = useState<number>(0)
  const [loading, setLoading] = useState<boolean>(false)
  
  const connectionRef = useRef<HubConnection | null>(null)
  // Ids already pushed over SignalR this session — guards against duplicate redelivery.
  const seenIdsRef = useRef<Set<number>>(new Set())

  const loadList = async () => {
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<ApiResponse<NotificationListDto>>('Notifications?take=20')
      if (resp && resp.success) {
        setItems(resp.data.items)
        setUnreadCount(resp.data.unreadCount)
      }
    } catch (err) {
      console.error('Failed to load notifications list:', err)
    } finally {
      setLoading(false)
    }
  }

  const markRead = async (id: number) => {
    try {
      const resp = await fetchWithAccessControl<ApiResponse<{ unreadCount: number }>>(`Notifications/${id}/read`, {
        method: 'POST',
      })
      if (resp && resp.success) {
        setUnreadCount(resp.data.unreadCount)
        setItems(prev =>
          prev.map(item => (item.id === id ? { ...item, isRead: true } : item))
        )
      }
    } catch (err) {
      console.error(`Failed to mark notification ${id} as read:`, err)
    }
  }

  const markAllRead = async () => {
    try {
      const resp = await fetchWithAccessControl<ApiResponse<{ unreadCount: number }>>('Notifications/read-all', {
        method: 'POST',
      })
      if (resp && resp.success) {
        setUnreadCount(resp.data.unreadCount)
        setItems(prev => prev.map(item => ({ ...item, isRead: true })))
      }
    } catch (err) {
      console.error('Failed to mark all notifications as read:', err)
    }
  }

  useEffect(() => {
    if (sessionState !== 'ready') {
      setItems([])
      setUnreadCount(0)
      return
    }

    // Load initial unread count
    fetchWithAccessControl<ApiResponse<{ unreadCount: number }>>('Notifications/unread-count')
      .then(resp => {
        if (resp && resp.success) {
          setUnreadCount(resp.data.unreadCount)
        }
      })
      .catch(err => console.error('Failed to fetch initial unread count:', err))

    if (!appConfig.enableSignalR) return

    const hubUrl = `${appConfig.signalRBaseUrl.replace(/\/$/, '')}/hubs/admin-activity`
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connectionRef.current = connection

    connection.on('NotificationCreated', (dto: NotificationDto) => {
      // The hub can redeliver the same event around a reconnect. Gate on a ref, not on a
      // state updater: updaters run at re-render time, too late for the count/toast below.
      if (seenIdsRef.current.has(dto.id)) return
      seenIdsRef.current.add(dto.id)

      setItems(prev => [dto, ...prev].slice(0, 20))
      setUnreadCount(prev => prev + 1)

      if (dto.level === 'error') {
        toast.error(dto.message || dto.title)
      } else if (dto.level === 'success') {
        toast.success(dto.title)
      } else {
        toast.info(dto.title)
      }
    })

    connection.start().catch(err => console.error('SignalR notifications connection failed:', err))

    return () => {
      if (connectionRef.current) {
        const conn = connectionRef.current
        if (conn.state !== HubConnectionState.Disconnected) {
          conn.stop().catch(() => undefined)
        }
        connectionRef.current = null
      }
    }
  }, [sessionState])

  return (
    <NotificationContext.Provider
      value={{
        items,
        unreadCount,
        loading,
        loadList,
        markRead,
        markAllRead,
      }}
    >
      {children}
    </NotificationContext.Provider>
  )
}

export function useNotifications() {
  const context = useContext(NotificationContext)
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider')
  }
  return context
}
