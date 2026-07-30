type ReportKpiTileTone = 'neutral' | 'info' | 'success' | 'danger'

type ReportKpiTileProps = {
  label: string
  value: string
  tone?: ReportKpiTileTone
}

const toneClassMap: Record<ReportKpiTileTone, string> = {
  neutral: 'text-slate-900',
  info: 'text-indigo-700',
  success: 'text-emerald-700',
  danger: 'text-rose-600',
}

export function ReportKpiTile({ label, value, tone = 'neutral' }: ReportKpiTileProps) {
  return (
    <div className="border-r border-slate-200/70 px-4 py-3 last:border-r-0">
      <div className="text-[10px] font-extrabold uppercase text-slate-400">{label}</div>
      <div className={`mt-1 text-lg font-bold tabular-nums ${toneClassMap[tone]}`}>{value}</div>
    </div>
  )
}
