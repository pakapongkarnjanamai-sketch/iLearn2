import type { ReactNode } from 'react'
import { Badge, type BadgeTone } from './Badge'

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
  size?: 'xs' | 'xxs'
}

/** Maps content state to a consistent readiness label and badge tone. */
export function getContentReadinessBadgeModel(item: ContentReadinessInput): ReadinessBadgeModel {
  if (item.source === 'upload') {
    return { label: 'Queued Upload', tone: 'info', ready: false }
  }

  const isPublished = item.isPublished ?? item.isActive ?? false
  if (!isPublished) {
    return { label: 'Not Ready', tone: 'danger', ready: false }
  }

  if (!item.url) {
    return { label: 'Missing Launch', tone: 'warning', ready: false }
  }

  return { label: 'Published', tone: 'success', ready: true }
}

export function ReadinessBadge({ ready = false, label, tone, size = 'xs' }: ReadinessBadgeProps) {
  const resolvedTone = tone ?? (ready ? 'info' : 'neutral')
  const resolvedLabel = label ?? (ready ? 'Ready' : 'Not Ready')

  return (
    <Badge variant="outline" tone={resolvedTone} size={size}>
      {resolvedLabel}
    </Badge>
  )
}