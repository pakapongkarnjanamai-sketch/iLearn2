import { Link } from 'react-router-dom'
import { Card } from '../../components/ui/Card'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { Badge } from '../../components/ui/Badge'
import { ShieldAlert, GraduationCap, BookOpen, Activity, ArrowRight, BarChart3 } from 'lucide-react'
import { REPORT_LABELS, getLang, t, type LabelPair } from '../../lib/labels'

export function ReportHubPage() {
  // Secondary title shows the other language so both names stay discoverable.
  const otherLang = getLang() === 'th' ? 'en' : 'th'

  const reports: Array<{
    title: LabelPair
    description: LabelPair
    tag: LabelPair
    path: string
    icon: typeof ShieldAlert
    tone: 'danger' | 'info' | 'success' | 'warning'
    accentBg: string
  }> = [
    {
      title: REPORT_LABELS.complianceTitle,
      description: REPORT_LABELS.complianceDesc,
      path: '/reports/compliance',
      icon: ShieldAlert,
      tag: REPORT_LABELS.complianceTag,
      tone: 'danger',
      accentBg: 'bg-rose-50 text-rose-600 group-hover:bg-rose-100 group-hover:text-rose-700',
    },
    {
      title: REPORT_LABELS.transcriptTitle,
      description: REPORT_LABELS.transcriptDesc,
      path: '/reports/transcript',
      icon: GraduationCap,
      tag: REPORT_LABELS.transcriptTag,
      tone: 'info',
      accentBg: 'bg-indigo-50 text-indigo-600 group-hover:bg-indigo-100 group-hover:text-indigo-700',
    },
    {
      title: REPORT_LABELS.coursesTitle,
      description: REPORT_LABELS.coursesDesc,
      path: '/reports/courses',
      icon: BookOpen,
      tag: REPORT_LABELS.coursesTag,
      tone: 'success',
      accentBg: 'bg-emerald-50 text-emerald-600 group-hover:bg-emerald-100 group-hover:text-emerald-700',
    },
    {
      title: REPORT_LABELS.activityTitle,
      description: REPORT_LABELS.activityDesc,
      path: '/reports/activity',
      icon: Activity,
      tag: REPORT_LABELS.activityTag,
      tone: 'warning',
      accentBg: 'bg-amber-50 text-amber-600 group-hover:bg-amber-100 group-hover:text-amber-700',
    },
  ]

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <SectionHeader icon={BarChart3}>{t(REPORT_LABELS.hubTitle)}</SectionHeader>
        <p className="text-xs text-slate-500 font-medium">
          {t(REPORT_LABELS.hubIntro)}
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {reports.map((report) => {
          const Icon = report.icon
          return (
            <Link to={report.path} key={report.path} className="group block">
              <Card
                className="h-full border-slate-200 transition-all duration-200 hover:border-slate-300 hover:shadow-md group-hover:-translate-y-0.5"
                bodyClassName="p-6 flex flex-col justify-between h-full gap-4"
              >
                <div className="flex items-start gap-4">
                  <div
                    className={`flex h-12 w-12 items-center justify-center rounded-xl transition-colors shrink-0 ${report.accentBg}`}
                  >
                    <Icon className="h-6 w-6" aria-hidden="true" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2 mb-1">
                      <Badge tone={report.tone} variant="tag" size="xxs">
                        {t(report.tag)}
                      </Badge>
                    </div>
                    <h3 className="text-base font-bold text-slate-800 transition-colors group-hover:text-indigo-600">
                      {t(report.title)}{' '}
                      <span className="text-xs font-semibold text-slate-400 block sm:inline">
                        ({report.title[otherLang]})
                      </span>
                    </h3>
                    <p className="mt-1.5 text-xs text-slate-500 font-medium leading-relaxed">
                      {t(report.description)}
                    </p>
                  </div>
                </div>

                <div className="flex items-center justify-end gap-1 text-xs font-bold text-slate-400 transition-colors group-hover:text-indigo-600 pt-2 border-t border-slate-100/80">
                  <span>{t(REPORT_LABELS.openReport)}</span>
                  <ArrowRight className="h-3.5 w-3.5 transition-transform group-hover:translate-x-1" />
                </div>
              </Card>
            </Link>
          )
        })}
      </div>
    </div>
  )
}
