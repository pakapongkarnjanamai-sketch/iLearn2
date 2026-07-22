import { Link } from 'react-router-dom'
import { Card } from '../../components/ui/Card'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { Badge } from '../../components/ui/Badge'
import { ShieldAlert, GraduationCap, BookOpen, Activity, ArrowRight, BarChart3 } from 'lucide-react'

export function ReportHubPage() {
  const reports = [
    {
      title: 'Compliance & Overdue Report',
      description: 'Monitor division and department compliance rates, target completion goals, and view granular overdue enrollment details.',
      path: '/reports/compliance',
      icon: ShieldAlert,
      tag: 'Executive Overview',
      tone: 'danger' as const,
      accentBg: 'bg-rose-50 text-rose-600 group-hover:bg-rose-100 group-hover:text-rose-700',
    },
    {
      title: 'Learner Transcript',
      description: 'Search for any learner to view their complete training history, active enrollments, course statuses, score results, and print-ready transcripts.',
      path: '/reports/transcript',
      icon: GraduationCap,
      tag: 'Learner Audit',
      tone: 'info' as const,
      accentBg: 'bg-indigo-50 text-indigo-600 group-hover:bg-indigo-100 group-hover:text-indigo-700',
    },
    {
      title: 'Course Summary Report',
      description: 'Analyze average learner progress, completion rates, total enrollments, overdue counts, and test scores across all active courses.',
      path: '/reports/courses',
      icon: BookOpen,
      tag: 'Course Analytics',
      tone: 'success' as const,
      accentBg: 'bg-emerald-50 text-emerald-600 group-hover:bg-emerald-100 group-hover:text-emerald-700',
    },
    {
      title: 'Training Activity Report',
      description: 'Track monthly completion trends, active learner volume, new course enrollments, and cumulative play time over custom period ranges.',
      path: '/reports/activity',
      icon: Activity,
      tag: 'Monthly Trends',
      tone: 'warning' as const,
      accentBg: 'bg-amber-50 text-amber-600 group-hover:bg-amber-100 group-hover:text-amber-700',
    },
  ]

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <SectionHeader icon={BarChart3}>Report Hub</SectionHeader>
        <p className="text-xs text-slate-500 font-medium">
          Comprehensive analytics, compliance statistics, course metrics, and learner transcript records.
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
                        {report.tag}
                      </Badge>
                    </div>
                    <h3 className="text-base font-bold text-slate-800 transition-colors group-hover:text-indigo-600">
                      {report.title}
                    </h3>
                    <p className="mt-1.5 text-xs text-slate-500 font-medium leading-relaxed">
                      {report.description}
                    </p>
                  </div>
                </div>

                <div className="flex items-center justify-end gap-1 text-xs font-bold text-slate-400 transition-colors group-hover:text-indigo-600 pt-2 border-t border-slate-100/80">
                  <span>View Report</span>
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
