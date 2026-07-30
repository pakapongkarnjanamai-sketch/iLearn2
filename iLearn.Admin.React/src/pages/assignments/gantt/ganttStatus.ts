// Solid fills for Gantt bars and their legend swatches — one source of truth so the
// two can never drift. Tones follow STATUS_TONES (lib/labels): info / warning /
// success / danger / neutral. Badge's own classes are soft (bg-*-50 + text-*-700)
// and unreadable as a bar fill, hence the separate solid map rather than reusing it.
const STATUS_BAR_CLASS: Record<string, string> = {
  InProgress: 'bg-indigo-600',
  Upcoming: 'bg-amber-600',
  Completed: 'bg-emerald-600',
  Expired: 'bg-red-600',
  NotStarted: 'bg-slate-400',
}

const FALLBACK_BAR_CLASS = 'bg-slate-500'

export const ganttStatusBarClass = (status: string) =>
  STATUS_BAR_CLASS[status] ?? FALLBACK_BAR_CLASS
