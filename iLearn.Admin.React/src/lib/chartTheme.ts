// Shared chart styling constants — used by DashboardCharts and AssignmentReportCharts.

export const BRAND = '#4f46e5'

// Colors matching statusTone() in lib/labels.ts — keyed by the canonical status
// keys from STATUS_LABELS (never by translated display text):
// Completed=success (emerald), InProgress=info (indigo), NotStarted=neutral (slate),
// Overdue=danger (red), Upcoming=warning (amber)
export const STATUS_COLORS: Record<string, string> = {
  Completed: '#059669',
  InProgress: '#4f46e5',
  NotStarted: '#94a3b8',
  Overdue: '#dc2626',
  Upcoming: '#d97706',
}

export const tooltipStyle = {
  background: '#0f172a',
  border: 'none',
  borderRadius: 6,
  color: '#fff',
  fontSize: 12,
  padding: '6px 10px',
}

export const axisStyle = { fontSize: 11, fill: '#64748b' }
