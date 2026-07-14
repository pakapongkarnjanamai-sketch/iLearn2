// Shared chart styling constants — used by DashboardCharts and AssignmentReportCharts.

export const BRAND = '#4f46e5'

// Colors matching statusTone(): Completed=success (emerald), In Progress=info (indigo), Not Started=neutral (slate), Overdue=danger (red), Upcoming=warning (amber)
export const STATUS_COLORS: Record<string, string> = {
  Completed: '#059669',
  'In Progress': '#4f46e5',
  'Not Started': '#94a3b8',
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
