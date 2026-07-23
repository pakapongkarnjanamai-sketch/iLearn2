import type { ReactNode } from 'react'
import { Badge, type BadgeTone, type BadgeVariant } from './Badge'

type ContentReadinessInput = {
  source: 'upload' | 'library'
  isPublished?: boolean | undefined
  isActive?: boolean | undefined
  url?: string | null | undefined
}

export type ReadinessBadgeModel = {
  label: string
  tone: BadgeTone
  ready: boolean
}

type ReadinessBadgeProps = {
  ready?: boolean
  label?: ReactNode
  tone?: BadgeTone
  variant?: BadgeVariant
  size?: 'xs' | 'xxs'
}

/** Maps content state to a consistent readiness label and badge tone. */
export function getContentReadinessBadgeModel(item: ContentReadinessInput): ReadinessBadgeModel {
  if (item.source === 'upload') {
    return { label: 'รออัปโหลด', tone: 'info', ready: false }
  }

  const isPublished = item.isPublished ?? item.isActive ?? false
  if (!isPublished) {
    return { label: 'ยังไม่พร้อม', tone: 'danger', ready: false }
  }

  if (!item.url) {
    return { label: 'ไม่มีลิงก์เปิดเรียน', tone: 'warning', ready: false }
  }

  return { label: 'พร้อมใช้งาน', tone: 'success', ready: true }
}

export function ReadinessBadge({ ready = false, label, tone, variant = 'soft', size = 'xs' }: ReadinessBadgeProps) {
  const resolvedTone = tone ?? (ready ? 'info' : 'neutral')
  const resolvedLabel = label ?? (ready ? 'พร้อมใช้งาน' : 'ยังไม่พร้อม')

  return (
    <Badge variant={variant} tone={resolvedTone} size={size}>
      {resolvedLabel}
    </Badge>
  )
}