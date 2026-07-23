// Central UI label vocabulary — every status/badge display text in the admin app
// lives here (PLAN-133). Each entry keeps both Thai and English; the app renders
// Thai today via t(). The future two-language phase only needs to make
// `currentLang` dynamic (user setting) — no page should hardcode status text.
import type { BadgeTone } from '../components/ui/Badge'

export type UiLang = 'th' | 'en'
export type LabelPair = { readonly th: string; readonly en: string }

// Fixed to Thai for now — the bilingual phase turns this into a user setting.
const currentLang: UiLang = 'th'

/** Resolve a LabelPair to display text in the current language. */
export const t = (pair: LabelPair): string => pair[currentLang]

// ── Learner / batch status ──────────────────────────────────────────────────
// Keys mirror AssignmentStatusKeys (iLearn.Application/Common/AssignmentStatusKeys.cs).
// Backend sends the raw keys (no spaces); UI renders them via learnerStatusLabel().
export const LEARNER_STATUS_KEYS = [
  'Completed',
  'InProgress',
  'NotStarted',
  'Overdue',
  'Upcoming',
] as const

export type LearnerStatusKey = (typeof LEARNER_STATUS_KEYS)[number]

export const STATUS_LABELS: Record<string, LabelPair> = {
  Completed: { th: 'เรียนจบแล้ว', en: 'Completed' },
  InProgress: { th: 'กำลังเรียน', en: 'In Progress' },
  NotStarted: { th: 'ยังไม่เริ่ม', en: 'Not Started' },
  Overdue: { th: 'เกินกำหนด', en: 'Overdue' },
  Upcoming: { th: 'ใกล้กำหนด', en: 'Upcoming' },
  Expired: { th: 'หมดอายุ', en: 'Expired' },
}

/** Display label for a learner/batch status key coming from the API. */
export const learnerStatusLabel = (status: string | null | undefined): string => {
  if (!status) return '—'
  const pair = STATUS_LABELS[status]
  return pair ? t(pair) : status
}

// ── Status → badge tone ─────────────────────────────────────────────────────
const STATUS_TONES: Record<string, BadgeTone> = {
  Completed: 'success',
  InProgress: 'info',
  Active: 'info',
  Enrolling: 'info',
  Overdue: 'danger',
  Expired: 'danger',
  Upcoming: 'warning',
  'Due Soon': 'warning',
  NotStarted: 'warning',
  Unassigned: 'neutral',
}

/**
 * Maps a status to a badge tone. Accepts the canonical key or its display text
 * in either language (pages often pass already-translated text into StatusBadge
 * children); display text is resolved back through STATUS_LABELS so the tone
 * map can never drift from the label map.
 */
export function statusTone(status: string | null | undefined): BadgeTone {
  if (!status) return 'neutral'
  const direct = STATUS_TONES[status]
  if (direct) return direct
  for (const [key, pair] of Object.entries(STATUS_LABELS)) {
    if (pair.th === status || pair.en === status) {
      return STATUS_TONES[key] ?? 'neutral'
    }
  }
  return 'neutral'
}

// ── Course status ───────────────────────────────────────────────────────────
// statusCode mirrors CourseStatus enum (0=Draft, 1=Open, 2=Closed).
export const COURSE_STATUS_LABELS: Record<'open' | 'draft' | 'closed', LabelPair> = {
  open: { th: 'เปิดใช้งาน', en: 'Open' },
  draft: { th: 'ฉบับร่าง', en: 'Draft' },
  closed: { th: 'ปิดใช้งาน', en: 'Closed' },
}

function normalizeCourseStatus(status: string | null | undefined) {
  return (status || '').trim().toLowerCase()
}

export function getCourseStatusTone(status: string | null | undefined, statusCode?: number | null): BadgeTone {
  if (typeof statusCode === 'number') {
    if (statusCode === 1) return 'success'
    if (statusCode === 0) return 'warning'
    if (statusCode === 2) return 'neutral'
  }

  const normalized = normalizeCourseStatus(status)
  if (normalized === 'open' || normalized === 'active') return 'success'
  if (normalized === 'draft') return 'warning'
  if (normalized === 'closed') return 'neutral'
  return 'neutral'
}

export function courseStatusLabel(status: string | null | undefined, statusCode?: number | null): string {
  if (typeof statusCode === 'number') {
    if (statusCode === 1) return t(COURSE_STATUS_LABELS.open)
    if (statusCode === 0) return t(COURSE_STATUS_LABELS.draft)
    if (statusCode === 2) return t(COURSE_STATUS_LABELS.closed)
  }

  const normalized = normalizeCourseStatus(status)
  if (normalized === 'open' || normalized === 'active') return t(COURSE_STATUS_LABELS.open)
  if (normalized === 'draft') return t(COURSE_STATUS_LABELS.draft)
  if (normalized === 'closed') return t(COURSE_STATUS_LABELS.closed)
  return status?.trim() || '—'
}

// ── Content readiness (ReadinessBadge) ──────────────────────────────────────
export const READINESS_LABELS: Record<'ready' | 'notReady' | 'pendingUpload' | 'missingLaunchUrl', LabelPair> = {
  ready: { th: 'พร้อมใช้งาน', en: 'Ready' },
  notReady: { th: 'ยังไม่พร้อม', en: 'Not ready' },
  pendingUpload: { th: 'รออัปโหลด', en: 'Pending upload' },
  missingLaunchUrl: { th: 'ไม่มีลิงก์เปิดเรียน', en: 'No launch link' },
}

// ── Content type (ContentItem.typeId: 1=Learn, 2=Exam) ─────────────────────
export const CONTENT_TYPE_LABELS: Record<'learn' | 'exam', LabelPair> = {
  learn: { th: 'บทเรียน', en: 'Learn' },
  exam: { th: 'แบบทดสอบ', en: 'Exam' },
}

/** Display label for ContentItem.typeId (1=Learn, 2=Exam). */
export const contentTypeLabel = (typeId: number | null | undefined): string => {
  if (typeId === 1) return t(CONTENT_TYPE_LABELS.learn)
  if (typeId === 2) return t(CONTENT_TYPE_LABELS.exam)
  return typeId == null ? '—' : `Type ${typeId}`
}

// ── System health / config diagnostics (HealthCheckPage, SystemConfigPage) ──
export const HEALTH_LABELS = {
  checking: { th: 'กำลังตรวจสอบ…', en: 'Checking…' },
  unreachable: { th: 'เชื่อมต่อไม่ได้', en: 'Unreachable' },
  operational: { th: 'ระบบปกติ', en: 'Operational' },
  degraded: { th: 'มีปัญหา', en: 'Degraded' },
  pass: { th: 'ผ่าน', en: 'Pass' },
  fail: { th: 'ไม่ผ่าน', en: 'Fail' },
  enabled: { th: 'เปิดอยู่', en: 'Enabled' },
  disabledSecure: { th: 'ปิดอยู่ (ปลอดภัย)', en: 'Disabled (Secure)' },
} satisfies Record<string, LabelPair>

// ── Common on/off + misc badge labels ───────────────────────────────────────
export const COMMON_LABELS = {
  active: { th: 'ใช้งานอยู่', en: 'Active' },
  inactive: { th: 'ปิดใช้งาน', en: 'Inactive' },
  published: { th: 'เผยแพร่แล้ว', en: 'Published' },
  draft: { th: 'ฉบับร่าง', en: 'Draft' },
  assignable: { th: 'มอบหมายได้', en: 'Assignable' },
  notAssignable: { th: 'ไม่อนุญาต', en: 'Not allowed' },
  all: { th: 'ทั้งหมด', en: 'All' },
  // Course version status (CourseDetailPage / VersionDetailPage)
  activeVersion: { th: 'เวอร์ชันที่ใช้งาน', en: 'Active Version' },
  inactiveVersion: { th: 'ไม่ได้ใช้งาน', en: 'Inactive' },
  // Learner enrollment status (LearnerProfilePage)
  passed: { th: 'ผ่านแล้ว', en: 'Passed' },
  cancelled: { th: 'ยกเลิกแล้ว', en: 'Cancelled' },
  assigned: { th: 'ได้รับมอบหมาย', en: 'Assigned' },
  selfEnroll: { th: 'ลงทะเบียนเอง', en: 'Self-Enroll' },
  // Learner directory lookup (AssignmentDetailPage)
  notFoundInDirectory: { th: 'ไม่พบข้อมูลพนักงาน', en: 'Not found in directory' },
  // Learner group tree tags (LearnerGroupListPage)
  folder: { th: 'โฟลเดอร์', en: 'Folder' },
  group: { th: 'กลุ่ม', en: 'Group' },
} satisfies Record<string, LabelPair>
