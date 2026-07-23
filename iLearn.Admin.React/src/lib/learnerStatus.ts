// Mirrors AssignmentStatusKeys.Learner (iLearn.Application/Common/AssignmentStatusKeys.cs)
// Backend sends the raw keys (no spaces); UI renders them via learnerStatusLabel().
export const LEARNER_STATUS_KEYS = [
  'Completed',
  'InProgress',
  'NotStarted',
  'Overdue',
  'Upcoming',
] as const

export type LearnerStatusKey = (typeof LEARNER_STATUS_KEYS)[number]

const STATUS_LABELS: Record<string, string> = {
  Completed: 'เรียนจบแล้ว',
  InProgress: 'กำลังเรียน',
  NotStarted: 'ยังไม่เริ่ม',
  Overdue: 'เกินกำหนด',
  Upcoming: 'ใกล้กำหนด',
  Expired: 'หมดอายุ',
}

/** Display label for a learner/batch status key coming from the API. */
export const learnerStatusLabel = (status: string | null | undefined) =>
  status ? (STATUS_LABELS[status] ?? status) : '—'
