// Mirrors NotificationDto (iLearn.Application/DTOs/NotificationDtos.cs)
export interface NotificationDto {
  id: number
  type: string
  level: string          // 'success' | 'error' | 'info'
  title: string
  message?: string | null
  linkPath?: string | null
  entityType?: string | null
  entityId?: number | null
  isRead: boolean
  createdAt: string
}

// Mirrors NotificationListDto (iLearn.Application/DTOs/NotificationDtos.cs) — PLAN-090 added totalCount
export interface NotificationListDto {
  unreadCount: number
  totalCount: number
  items: NotificationDto[]
}
