// Central UI label dictionary — every user-facing display text in the admin app
// lives here (PLAN-133/134/136). Each entry keeps both Thai and English as a
// LabelPair; call sites resolve at render time via t()/tf(), so switching the
// language re-renders everything (AppLayout remounts on lang change).
// No page should hardcode display text — add a key here instead.
import { useSyncExternalStore } from 'react'
import type { BadgeTone } from '../components/ui/Badge'

export type UiLang = 'th' | 'en'
export type LabelPair = { readonly th: string; readonly en: string }

// ── Language store (persisted per browser via localStorage) ─────────────────
const LANG_STORAGE_KEY = 'ilearn-admin-lang'

function readStoredLang(): UiLang {
  try {
    return window.localStorage.getItem(LANG_STORAGE_KEY) === 'en' ? 'en' : 'th'
  } catch {
    return 'th'
  }
}

let currentLang: UiLang = readStoredLang()
const langListeners = new Set<() => void>()

export function getLang(): UiLang {
  return currentLang
}

export function setLang(lang: UiLang) {
  if (lang === currentLang) return
  currentLang = lang
  try {
    window.localStorage.setItem(LANG_STORAGE_KEY, lang)
  } catch {
    // Private mode / storage disabled — the choice still applies for this session.
  }
  langListeners.forEach((notify) => notify())
}

function subscribeLang(onChange: () => void): () => void {
  langListeners.add(onChange)
  return () => langListeners.delete(onChange)
}

/** Current language as reactive state — AppLayout keys the tree on this. */
export function useLang(): UiLang {
  return useSyncExternalStore(subscribeLang, getLang)
}

/** Resolve a LabelPair to display text in the current language. */
export const t = (pair: LabelPair): string => pair[currentLang]

/** Resolve a LabelPair containing {0}, {1}, … placeholders with values. */
export const tf = (pair: LabelPair, ...values: Array<string | number>): string =>
  values.reduce<string>((text, value, i) => text.replaceAll(`{${i}}`, String(value)), t(pair))

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
  // Extra statuses emitted by DashboardService priority-assignment rows
  Active: { th: 'กำลังดำเนินการ', en: 'Active' },
  'Due Soon': { th: 'ใกล้ถึงกำหนด', en: 'Due Soon' },
  Enrolling: { th: 'กำลังลงทะเบียน', en: 'Enrolling' },
  Unassigned: { th: 'ยังไม่มอบหมาย', en: 'Unassigned' },
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

// ═══ Zone A: Navigation (Sidebar sections/items + breadcrumb segments) ══════
export const NAV_LABELS = {
  dashboard: { th: 'แดชบอร์ด', en: 'Dashboard' },
  learning: { th: 'การเรียนรู้', en: 'Learning' },
  courses: { th: 'คอร์สเรียน', en: 'Courses' },
  assignments: { th: 'งานมอบหมาย', en: 'Assignments' },
  learnerGroups: { th: 'กลุ่มผู้เรียน', en: 'Learner Groups' },
  operations: { th: 'งานปฏิบัติการ', en: 'Operations' },
  contentLibrary: { th: 'คลังคอนเทนต์', en: 'Content Library' },
  learners: { th: 'ผู้เรียน', en: 'Learners' },
  reports: { th: 'รายงาน', en: 'Reports' },
  superAdmin: { th: 'ผู้ดูแลระบบสูงสุด', en: 'Super Admin' },
  enrollments: { th: 'การลงทะเบียน', en: 'Enrollments' },
  learningLogs: { th: 'บันทึกการเรียน', en: 'Learning Logs' },
  masterData: { th: 'ข้อมูลหลัก', en: 'Master Data' },
  divisions: { th: 'สายงาน', en: 'Divisions' },
  categories: { th: 'หมวดหมู่', en: 'Categories' },
  courseTypes: { th: 'ประเภทคอร์ส', en: 'Course Types' },
  roles: { th: 'บทบาท', en: 'Roles' },
  learnerGroupCategories: { th: 'หมวดหมู่กลุ่มผู้เรียน', en: 'Learner Group Categories' },
  adminUsers: { th: 'ผู้ดูแลระบบ', en: 'Admin Users' },
  systemConfig: { th: 'ตั้งค่าระบบ', en: 'System Config' },
  healthCheck: { th: 'ตรวจสอบระบบ', en: 'Health Check' },
  notifications: { th: 'การแจ้งเตือน', en: 'Notifications' },
} satisfies Record<string, LabelPair>

export const CRUMB_LABELS = {
  create: { th: 'สร้างใหม่', en: 'Create' },
  modify: { th: 'แก้ไข', en: 'Modify' },
  version: { th: 'เวอร์ชัน', en: 'Version' },
  profile: { th: 'โปรไฟล์', en: 'Profile' },
  schedule: { th: 'ตารางเวลา', en: 'Schedule' },
  assignCourses: { th: 'มอบหมายคอร์ส', en: 'Assign Courses' },
  details: { th: 'รายละเอียด', en: 'Details' },
  dashboardHome: { th: 'หน้าแดชบอร์ด', en: 'Dashboard Home' },
} satisfies Record<string, LabelPair>

// ═══ Zone A: Layout chrome (Header) ═════════════════════════════════════════
export const LAYOUT_LABELS = {
  classicAdmin: { th: 'แอดมินรุ่นเดิม', en: 'Classic Admin' },
  loadingUser: { th: 'กำลังโหลดผู้ใช้', en: 'Loading user' },
  adminConsole: { th: 'คอนโซลผู้ดูแล', en: 'Admin console' },
} satisfies Record<string, LabelPair>

// ═══ Zone A: Shared UI component defaults ═══════════════════════════════════
export const UI_LABELS = {
  search: { th: 'ค้นหา...', en: 'Search...' },
  records: { th: 'รายการ', en: 'records' },
  showing: { th: 'แสดง', en: 'Showing' },
  noData: { th: 'ไม่พบข้อมูล', en: 'No data records found' },
  loadingMore: { th: 'กำลังโหลดเพิ่ม...', en: 'Loading more...' },
  scrollToLoadMore: { th: 'เลื่อนลงเพื่อโหลดเพิ่ม', en: 'Scroll down to load more' },
  allRecordsLoaded: { th: 'โหลดครบทุกรายการแล้ว', en: 'All records loaded' },
  loadingDirectory: { th: 'กำลังโหลดโฟลเดอร์...', en: 'Loading directory...' },
  confirm: { th: 'ยืนยัน', en: 'Confirm' },
  cancel: { th: 'ยกเลิก', en: 'Cancel' },
  previous: { th: 'ย้อนกลับ', en: 'Previous' },
  continue: { th: 'ถัดไป', en: 'Continue' },
  openDetails: { th: 'เปิดดูรายละเอียด', en: 'Open Details' },
  createEntity: { th: 'สร้าง{0}', en: 'Create {0}' },
} satisfies Record<string, LabelPair>

// ═══ Zone B: Dashboard ══════════════════════════════════════════════════════
export const DASHBOARD_LABELS = {
  pageTitle: { th: 'ภาพรวมการดำเนินงาน', en: 'Operational summary' },
  loading: { th: 'กำลังโหลดแดชบอร์ด...', en: 'Loading dashboard...' },
  loadFailed: { th: 'ไม่สามารถโหลดแดชบอร์ดได้', en: 'Unable to load dashboard' },
  retry: { th: 'ลองใหม่', en: 'Try again' },
  chartError: { th: 'ไม่สามารถแสดงกราฟได้', en: 'Unable to render chart' },
  allDivisions: { th: 'ทุกสังกัด', en: 'All divisions' },
  updated: { th: 'อัปเดต {0}', en: 'Updated {0}' },
  newCourse: { th: 'สร้างคอร์สใหม่', en: 'New Course' },
  newAssignment: { th: 'มอบหมายงานใหม่', en: 'New Assignment' },
  maintenanceInProgress: { th: 'กำลังดำเนินการปรับปรุงระบบ', en: 'Maintenance in progress' },
  // KPI strip
  overdueTasks: { th: 'งานเกินกำหนด', en: 'Overdue tasks' },
  fromPrefix: { th: 'จาก', en: 'of' },
  totalTasksSuffix: { th: 'งานทั้งหมด', en: 'total tasks' },
  dueSoon: { th: 'ใกล้ถึงกำหนด', en: 'Due soon' },
  within7Days: { th: 'ภายใน 7 วัน', en: 'within 7 days' },
  completionRate: { th: 'อัตราเรียนสำเร็จ', en: 'Completion rate' },
  learnersUnit: { th: 'ผู้เรียน', en: 'learners' },
  tasksUnit: { th: 'งาน', en: 'tasks' },
  learningActivity30: { th: 'กิจกรรมการเรียน 30 วัน', en: 'Learning activity (30 days)' },
  previous30Days: { th: '30 วันก่อนหน้า', en: 'previous 30 days' },
  steady: { th: 'คงที่', en: 'steady' },
  // Priority assignments table
  priorityAssignments: { th: 'งานมอบหมายที่ต้องจัดการ', en: 'Assignments needing action' },
  viewAll: { th: 'ดูทั้งหมด', en: 'View all' },
  noPriorityAssignments: { th: 'ไม่มีงานมอบหมายที่ต้องจัดการในขณะนี้', en: 'No assignments need action right now' },
  colAssignment: { th: 'งานมอบหมาย', en: 'Assignment' },
  colStatus: { th: 'สถานะ', en: 'Status' },
  colLearners: { th: 'ผู้เรียน', en: 'Learners' },
  colDueDate: { th: 'กำหนดส่ง', en: 'Due Date' },
  colCompletion: { th: 'ความคืบหน้า', en: 'Completion' },
  colActions: { th: 'การดำเนินการ', en: 'Actions' },
  // Course attention table
  courseAttention: { th: 'คอร์สที่ต้องติดตาม', en: 'Courses needing attention' },
  allCoursesOnTrack: { th: 'ทุกคอร์สเป็นไปตามแผน', en: 'All courses are on track' },
  colCourse: { th: 'คอร์สเรียน', en: 'Course' },
  colTasks: { th: 'งานเรียน', en: 'Tasks' },
  // Bottom grid
  activityTrend: { th: 'แนวโน้มกิจกรรมการเรียน', en: 'Learning activity trend' },
  last6Months: { th: '6 เดือนล่าสุด', en: 'Last 6 months' },
  noActivityData: { th: 'ไม่มีข้อมูลกิจกรรมการเรียนใน 6 เดือนล่าสุด', en: 'No learning activity in the last 6 months' },
  recentAdminActivity: { th: 'กิจกรรมล่าสุดของผู้ดูแล', en: 'Recent admin activity' },
  realtime: { th: 'เรียลไทม์', en: 'Real-time' },
  autoRefresh: { th: 'รีเฟรชอัตโนมัติ', en: 'Auto-refresh' },
  noRecentActivity: { th: 'ยังไม่มีกิจกรรมล่าสุด', en: 'No recent activity yet' },
} satisfies Record<string, LabelPair>

// ═══ Zone C: Reports (Hub + Compliance + Transcript + Activity + CourseSummary) ═══
export const REPORT_LABELS = {
  // Shared
  loadingReport: { th: 'กำลังโหลดรายงาน...', en: 'Loading report...' },
  loadReportFailed: { th: 'โหลดรายงานไม่สำเร็จ', en: 'Failed to load report' },
  noReportData: { th: 'ไม่มีข้อมูลรายงาน', en: 'No report data available' },
  exportCsv: { th: 'ส่งออก CSV', en: 'Export CSV' },
  noRowsToExport: { th: 'ไม่มีรายการให้ส่งออก', en: 'No rows to export' },
  rowsShowing: { th: 'แสดงข้อมูล', en: 'Showing' },
  rowsOf: { th: 'จาก', en: 'of' },
  rowsUnit: { th: 'รายการ', en: 'records' },
  notFound: { th: 'ไม่พบข้อมูล', en: 'No data found' },
  colLearner: { th: 'ผู้เรียน', en: 'Learner' },
  colDivision: { th: 'สายงาน', en: 'Division' },
  colDepartment: { th: 'ฝ่าย', en: 'Department' },
  colProgress: { th: 'ความก้าวหน้า', en: 'Progress' },
  colCompleted: { th: 'สำเร็จ', en: 'Completed' },
  daysCount: { th: '{0} วัน', en: '{0} days' },
  duePrefix: { th: 'กำหนดส่ง: {0}', en: 'Due: {0}' },
  // Report Hub
  hubTitle: { th: 'ศูนย์รวมรายงาน', en: 'Report Hub' },
  hubIntro: { th: 'ศูนย์รวมข้อมูลสถิติ วิเคราะห์ผลการเรียนรู้ ติดตามการเข้าเรียนเกินกำหนด สรุปผลรายคอร์ส และประวัติการเรียนของผู้เรียน', en: 'Central hub for learning statistics — outcomes, overdue tracking, per-course summaries, and learner history.' },
  openReport: { th: 'เปิดดูรายงาน', en: 'Open report' },
  complianceTitle: { th: 'รายงานความก้าวหน้าและคอร์สเกินกำหนด', en: 'Compliance & Overdue Report' },
  complianceTag: { th: 'ภาพรวมผู้บริหาร', en: 'Executive overview' },
  complianceDesc: { th: 'ตรวจสอบอัตราความก้าวหน้าและการเรียนตามเกณฑ์แยกตามสายงานและฝ่าย ติดตามเป้าหมาย และดูรายละเอียดการเรียนเกินกำหนดของผู้เรียนแบบเจาะลึก', en: 'Track completion and compliance rates by division and department, monitor targets, and drill into each learner’s overdue items.' },
  transcriptTitle: { th: 'ประวัติการเรียนรายบุคคล', en: 'Learner Transcript' },
  transcriptTag: { th: 'ประวัติรายบุคคล', en: 'Individual history' },
  transcriptDesc: { th: 'ค้นหารายชื่อผู้เรียนเพื่อตรวจสอบประวัติการฝึกอบรมทั้งหมด รายวิชาที่กำลังเรียน สถานะการเรียน ผลคะแนนสอบ และพิมพ์ใบทรานสคริปต์', en: 'Search for a learner to review their full training history, active courses, learning status, exam scores, and print a transcript.' },
  coursesTitle: { th: 'รายงานสรุปผลการเรียนรายคอร์ส', en: 'Course Summary Report' },
  coursesTag: { th: 'สถิติรายคอร์ส', en: 'Per-course statistics' },
  coursesDesc: { th: 'วิเคราะห์อัตราการเรียนสำเร็จ (Completion Rate) จำนวนผู้เรียนที่ลงทะเบียน จำนวนผู้เรียนเกินกำหนด และคะแนนสอบเฉลี่ยของทุกคอร์สในแคตตาล็อก', en: 'Analyze completion rates, enrolled learners, overdue counts, and average exam scores across every course in the catalog.' },
  activityTitle: { th: 'รายงานสถิติและกิจกรรมการเรียนรู้', en: 'Training Activity Report' },
  activityTag: { th: 'แนวโน้มรายเดือน', en: 'Monthly trends' },
  activityDesc: { th: 'ติดตามแนวโน้มการเรียนจบรายเดือน ปริมาณผู้เรียนที่แอคทีฟ การลงทะเบียนคอร์สใหม่ และเวลาเรียนสะสมตามช่วงเวลาที่เลือก', en: 'Follow monthly completion trends, active learner volume, new enrollments, and accumulated learning hours over the selected period.' },
  // Compliance report
  compTotalLearners: { th: 'ผู้เรียนทั้งหมด', en: 'Total learners' },
  compTotalLearnersSub: { th: 'ผู้เรียนที่ลงทะเบียนในระบบ', en: 'learners enrolled in the system' },
  compOpenTasks: { th: 'งานที่รอดำเนินการ', en: 'Open tasks' },
  compOpenTasksSub: { th: 'จำนวนคอร์สที่มอบหมาย', en: 'assigned course enrollments' },
  compCompleted: { th: 'เรียนสำเร็จแล้ว', en: 'Completed' },
  compCompletedSub: { th: 'คอร์สที่เรียนจบแล้ว', en: 'courses finished' },
  compOverdueTitle: { th: 'เกินกำหนดส่ง', en: 'Overdue' },
  compOverdueLearners: { th: 'ผู้เรียนเกินกำหนด {0} คน', en: '{0} learners overdue' },
  compNoOverdue: { th: 'ไม่มีงานเกินกำหนด', en: 'No overdue tasks' },
  compRateSub: { th: 'สัดส่วนตามเป้าหมาย', en: 'against target' },
  compChartTitle: { th: 'อัตราการเรียนจบแยกตามสายงาน', en: 'Compliance by Division' },
  compNoDivisionData: { th: 'ไม่มีข้อมูลสายงาน', en: 'No division data' },
  compOverviewRates: { th: 'สถิติตามสังกัด', en: 'Overview Rates' },
  byDivision: { th: 'แยกตามสายงาน', en: 'By Division' },
  byDepartment: { th: 'แยกตามฝ่าย', en: 'By Department' },
  colAssigned: { th: 'มอบหมาย', en: 'Assigned' },
  colCompletionShare: { th: 'สัดส่วนเรียนสำเร็จ', en: 'Completion rate' },
  compDetailsTitle: { th: 'รายละเอียดรายการเกินกำหนด', en: 'Compliance & Overdue Details' },
  compSearchPlaceholder: { th: 'ค้นหารายชื่อผู้เรียน, รหัส, สายงาน, ฝ่าย หรือคอร์สเรียน...', en: 'Search learner name, code, division, department, or course...' },
  colDaysOverdue: { th: 'เกินกำหนด (วัน)', en: 'Days Overdue' },
  compNoOverdueRows: { th: 'ไม่พบรายการผู้เรียนเกินกำหนด', en: 'No overdue learners found' },
  // Transcript report
  trEnterCode: { th: 'กรุณากรอกรหัสพนักงานเพื่อค้นหา', en: 'Enter a learner code to search' },
  trSearchTitle: { th: 'ค้นหาประวัติการเรียนรายบุคคล', en: 'Search learner transcript' },
  trSearchPlaceholder: { th: 'กรอกรหัสพนักงาน (EId)...', en: 'Enter learner code (EId)...' },
  trClear: { th: 'ล้างคำค้นหา', en: 'Clear search' },
  trSearch: { th: 'ค้นหา', en: 'Search' },
  trIntroTitle: { th: 'ค้นหาและพิมพ์ประวัติการเรียน (Transcript)', en: 'Search & print learner transcript' },
  trIntroBody: { th: 'กรุณากรอกรหัสพนักงาน (เช่น EId) ในช่องค้นหาด้านบน เพื่อดึงและตรวจสอบประวัติการฝึกอบรมทั้งหมดของผู้เรียน', en: 'Enter a learner code (EId) in the search box above to load and review the learner’s full training history.' },
  trSearching: { th: 'กำลังค้นหาประวัติการเรียน...', en: 'Searching transcript...' },
  trNotFoundTitle: { th: 'ไม่พบข้อมูลผู้เรียน', en: 'Learner not found' },
  trNotFoundBody: { th: 'ไม่พบประวัติการเรียนสำหรับรหัส {0} กรุณาตรวจสอบรหัสพนักงานอีกครั้ง', en: 'No training history found for code {0} — please check the learner code.' },
  trBackToHub: { th: 'กลับไปยังศูนย์รวมรายงาน', en: 'Back to Report Hub' },
  trLearnerInfo: { th: 'ข้อมูลผู้เรียน', en: 'Learner information' },
  trPrint: { th: 'พิมพ์ใบทรานสคริปต์', en: 'Print transcript' },
  trName: { th: 'ชื่อผู้เรียน', en: 'Learner Name' },
  trCode: { th: 'รหัสพนักงาน', en: 'Learner Code' },
  trCompletedOf: { th: 'เรียนสำเร็จ {0} จาก {1} คอร์ส', en: 'Completed {0} of {1} courses' },
  trHistoryTitle: { th: 'ประวัติการเรียนรู้', en: 'Learning history' },
  trFilterPlaceholder: { th: 'กรองประวัติการเรียนด้วยรหัสคอร์ส, ชื่อคอร์ส หรือสถานะ...', en: 'Filter by course code, title, or status...' },
  trColScore: { th: 'คะแนน', en: 'Score' },
  trColTimeSpent: { th: 'เวลาที่ใช้', en: 'Time Spent' },
  trColTimeline: { th: 'ช่วงเวลา', en: 'Timeline' },
  trStarted: { th: 'เริ่มเรียน: {0}', en: 'Started: {0}' },
  trCompletedOn: { th: 'สำเร็จเมื่อ: {0}', en: 'Completed: {0}' },
  trNoHistory: { th: 'ไม่พบประวัติการเรียนสำหรับผู้เรียนรายนี้', en: 'No training history for this learner' },
  // Training activity report
  actTotalCompletions: { th: 'เรียนสำเร็จทั้งหมด', en: 'Total completions' },
  actTotalCompletionsSub: { th: 'จำนวนครั้งที่เรียนจบในรอบช่วงเวลา', en: 'completions within the selected period' },
  actNewEnrollments: { th: 'ลงทะเบียนคอร์สใหม่', en: 'New enrollments' },
  actNewEnrollmentsSub: { th: 'จำนวนการมอบหมายคอร์สใหม่', en: 'new course assignments' },
  actActiveLearners: { th: 'ผู้เรียนแอคทีฟเฉลี่ย', en: 'Avg. active learners' },
  actActiveLearnersSub: { th: 'ผู้เรียนที่มีกิจกรรมเข้าเรียน / เดือน', en: 'learners active per month' },
  actTotalHours: { th: 'เวลาเรียนสะสม', en: 'Total learning hours' },
  hoursUnitShort: { th: 'ชม.', en: 'hrs' },
  actTotalHoursSub: { th: 'ชั่วโมงการเรียนรวมทั้งหมด', en: 'accumulated learning hours' },
  actTrendTitle: { th: 'สถิติการเรียนจบและการลงทะเบียนใหม่', en: 'Completions & new enrollments' },
  actNoData: { th: 'ไม่มีข้อมูลกิจกรรม', en: 'No activity data' },
  actLegendCompletions: { th: 'เรียนสำเร็จ', en: 'Completions' },
  actLegendNewEnrollments: { th: 'ลงทะเบียนใหม่', en: 'New Enrollments' },
  actActiveMonthlyTitle: { th: 'จำนวนผู้เรียนที่แอคทีฟรายเดือน', en: 'Active learners by month' },
  actLegendActive: { th: 'ผู้เรียนแอคทีฟ', en: 'Active Learners' },
  actMonthlyDetails: { th: 'รายละเอียดสถิติการเรียนรู้รายเดือน', en: 'Monthly learning statistics' },
  lastNMonths: { th: '{0} เดือนล่าสุด', en: 'Last {0} months' },
  actColMonth: { th: 'เดือน', en: 'Month' },
  actColTotalHours: { th: 'เวลาเรียนสะสม (ชม.)', en: 'Total Hours' },
  actNoMonthlyData: { th: 'ไม่พบข้อมูลกิจกรรมรายเดือน', en: 'No monthly activity data' },
  actShowingMonths: { th: 'แสดงข้อมูลทั้งหมด {0} เดือน', en: 'Showing all {0} months' },
  // Course summary report
  csTitle: { th: 'สรุปผลการเรียนรายคอร์ส', en: 'Course Completion Summary' },
  csSearchPlaceholder: { th: 'ค้นหารหัสคอร์ส, ชื่อคอร์ส, สายงาน หรือหมวดหมู่...', en: 'Search course code, title, division or category...' },
  csColCode: { th: 'รหัส', en: 'Code' },
  csColTitle: { th: 'ชื่อคอร์ส', en: 'Title' },
  csColCategory: { th: 'หมวดหมู่', en: 'Category' },
  csColType: { th: 'ประเภท', en: 'Type' },
  csColAssignments: { th: 'ชุดงานมอบหมาย', en: 'Assignments' },
  csGeneralType: { th: 'ทั่วไป', en: 'General' },
} satisfies Record<string, LabelPair>
