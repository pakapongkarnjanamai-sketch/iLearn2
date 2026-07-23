import type { ReactNode } from 'react'
import { Badge, type BadgeTone, type BadgeVariant } from './Badge'
import { READINESS_LABELS, t } from '../../lib/labels'

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
    return { label: t(READINESS_LABELS.pendingUpload), tone: 'info', ready: false }
  }

  const isPublished = item.isPublished ?? item.isActive ?? false
  if (!isPublished) {
    return { label: t(READINESS_LABELS.notReady), tone: 'danger', ready: false }
  }

  if (!item.url) {
    return { label: t(READINESS_LABELS.missingLaunchUrl), tone: 'warning', ready: false }
  }

  return { label: t(READINESS_LABELS.ready), tone: 'success', ready: true }
}

export function ReadinessBadge({ ready = false, label, tone, variant = 'soft', size = 'xs' }: ReadinessBadgeProps) {
  const resolvedTone = tone ?? (ready ? 'info' : 'neutral')
  const resolvedLabel = label ?? (ready ? t(READINESS_LABELS.ready) : t(READINESS_LABELS.notReady))

  return (
    <Badge variant={variant} tone={resolvedTone} size={size}>
      {resolvedLabel}
    </Badge>
  )
}
