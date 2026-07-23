import { Link } from 'react-router-dom'
import type { LucideIcon } from 'lucide-react'
import { Card } from '../../components/ui/Card'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { Badge, type BadgeTone } from '../../components/ui/Badge'
import { ShieldAlert, GraduationCap, BookOpen, Activity, ArrowRight, BarChart3 } from 'lucide-react'
import { REPORT_LABELS, getLang, t, type LabelPair, type UiLang } from '../../lib/labels'

type ReportTone = Exclude<BadgeTone, 'neutral'>

type ReportDefinition = {
  title: LabelPair
  description: LabelPair
  tag: LabelPair
  path: string
  icon: LucideIcon
  tone: ReportTone
}

const reports = [
  {
    title: REPORT_LABELS.complianceTitle,
    description: REPORT_LABELS.complianceDesc,
    path: '/reports/compliance',
    icon: ShieldAlert,
    tag: REPORT_LABELS.complianceTag,
    tone: 'danger',
  },
  {
    title: REPORT_LABELS.transcriptTitle,
    description: REPORT_LABELS.transcriptDesc,
    path: '/reports/transcript',
    icon: GraduationCap,
    tag: REPORT_LABELS.transcriptTag,
    tone: 'info',
  },
  {
    title: REPORT_LABELS.coursesTitle,
    description: REPORT_LABELS.coursesDesc,
    path: '/reports/courses',
    icon: BookOpen,
    tag: REPORT_LABELS.coursesTag,
    tone: 'success',
  },
  {
    title: REPORT_LABELS.activityTitle,
    description: REPORT_LABELS.activityDesc,
    path: '/reports/activity',
    icon: Activity,
    tag: REPORT_LABELS.activityTag,
    tone: 'warning',
  },
] satisfies readonly ReportDefinition[]

const reportToneClasses: Record<ReportTone, string> = {
  danger: 'bg-rose-50 text-rose-600 group-hover:bg-rose-100 group-hover:text-rose-700',
  info: 'bg-indigo-50 text-indigo-600 group-hover:bg-indigo-100 group-hover:text-indigo-700',
  success: 'bg-emerald-50 text-emerald-600 group-hover:bg-emerald-100 group-hover:text-emerald-700',
  warning: 'bg-amber-50 text-amber-600 group-hover:bg-amber-100 group-hover:text-amber-700',
}

function getSecondaryLang(lang: UiLang): UiLang {
  return lang === 'th' ? 'en' : 'th'
}

function ReportTile({ report, secondaryLang }: { report: ReportDefinition; secondaryLang: UiLang }) {
  const Icon = report.icon

  return (
    <Link
      to={report.path}
      className="group block h-full focus-visible:outline-none"
      aria-label={`${t(REPORT_LABELS.openReport)}: ${t(report.title)}`}
    >
      <Card
        className="h-full border-slate-200/90 transition-colors group-hover:border-slate-300 group-focus-visible:border-indigo-300 group-focus-visible:ring-2 group-focus-visible:ring-indigo-100"
        bodyClassName="h-full"
      >
        <div className="flex h-full flex-col p-5 sm:p-6">
          <div className="flex items-start gap-4">
            <div
              className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-lg transition-colors ${reportToneClasses[report.tone]}`}
            >
              <Icon className="h-5 w-5" aria-hidden="true" />
            </div>
            <div className="min-w-0 flex-1">
              <Badge tone={report.tone} variant="tag" size="xxs">
                {t(report.tag)}
              </Badge>
              <h3 className="mt-2 text-[15px] font-bold leading-snug text-slate-900 transition-colors group-hover:text-indigo-700">
                <span>{t(report.title)}</span>
                <span className="mt-0.5 block text-xs font-semibold text-slate-400 sm:ml-1 sm:inline">
                  ({report.title[secondaryLang]})
                </span>
              </h3>
              <p className="mt-2 max-w-3xl text-xs font-medium leading-relaxed text-slate-500">
                {t(report.description)}
              </p>
            </div>
          </div>

          <div className="mt-auto flex items-center justify-end gap-1 border-t border-slate-100/80 pt-4 text-xs font-bold text-slate-400 transition-colors group-hover:text-indigo-700">
            <span>{t(REPORT_LABELS.openReport)}</span>
            <ArrowRight className="h-3.5 w-3.5 transition-transform group-hover:translate-x-0.5" aria-hidden="true" />
          </div>
        </div>
      </Card>
    </Link>
  )
}

export function ReportHubPage() {
  const secondaryLang = getSecondaryLang(getLang())

  return (
    <div className="flex flex-col gap-5">
      <div className="flex max-w-5xl flex-col gap-1">
        <SectionHeader icon={BarChart3}>{t(REPORT_LABELS.hubTitle)}</SectionHeader>
        <p className="text-xs font-medium leading-relaxed text-slate-500">
          {t(REPORT_LABELS.hubIntro)}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
        {reports.map((report) => (
          <ReportTile key={report.path} report={report} secondaryLang={secondaryLang} />
        ))}
      </div>
    </div>
  )
}
