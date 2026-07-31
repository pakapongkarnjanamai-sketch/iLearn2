// Solid fills for Gantt bars and their legend swatches. The hex values are also
// consumed by SVAR's inline task template, so status color cannot drift by renderer.
const STATUS_BAR_STYLE: Record<string, { className: string; hex: string; borderHex: string }> = {
  InProgress: { className: 'bg-indigo-600', hex: '#4f46e5', borderHex: '#4338ca' },
  Upcoming: { className: 'bg-amber-600', hex: '#d97706', borderHex: '#b45309' },
  Completed: { className: 'bg-emerald-600', hex: '#059669', borderHex: '#047857' },
  Expired: { className: 'bg-red-600', hex: '#dc2626', borderHex: '#b91c1c' },
  NotStarted: { className: 'bg-slate-400', hex: '#94a3b8', borderHex: '#64748b' },
}

const FALLBACK_BAR_STYLE = { className: 'bg-slate-500', hex: '#64748b', borderHex: '#475569' }

export const ganttStatusBarClass = (status: string) =>
  (STATUS_BAR_STYLE[status] ?? FALLBACK_BAR_STYLE).className

export const ganttStatusHex = (status: string) =>
  (STATUS_BAR_STYLE[status] ?? FALLBACK_BAR_STYLE).hex

export const ganttStatusBorderHex = (status: string) =>
  (STATUS_BAR_STYLE[status] ?? FALLBACK_BAR_STYLE).borderHex
