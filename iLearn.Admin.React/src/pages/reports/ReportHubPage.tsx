import { Link } from 'react-router-dom'
import { Card } from '../../components/ui/Card'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { ShieldAlert, GraduationCap, BookOpen, Activity } from 'lucide-react'

export function ReportHubPage() {
  const reports = [
    {
      title: 'Compliance & Overdue Report',
      description: 'Monitor division and department compliance rates and view details of overdue assignments.',
      path: '/reports/compliance',
      icon: ShieldAlert,
    },
    {
      title: 'Learner Transcript',
      description: 'Search for any learner to view their complete training history, statuses, and course details.',
      path: '/reports/transcript',
      icon: GraduationCap,
    },
    {
      title: 'Course Summary Report',
      description: 'Analyze average progress, completions, overdue count, and scores across all courses.',
      path: '/reports/courses',
      icon: BookOpen,
    },
    {
      title: 'Training Activity Report',
      description: 'Track completions, active learners, new enrollments, and total play time over recent months.',
      path: '/reports/activity',
      icon: Activity,
    },
  ]

  return (
    <div className="flex flex-col gap-6">
      <SectionHeader>Report Hub</SectionHeader>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {reports.map((report) => (
          <Link to={report.path} key={report.path} className="group block">
            <Card
              className="h-full border-slate-200 transition-all duration-200 hover:border-indigo-300 hover:shadow-md group-hover:-translate-y-0.5"
              bodyClassName="p-6 flex gap-4"
            >
              <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-indigo-50 text-indigo-600 transition-colors group-hover:bg-indigo-100 group-hover:text-indigo-700 shrink-0">
                <report.icon className="h-6 w-6" aria-hidden="true" />
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="text-base font-bold text-slate-800 transition-colors group-hover:text-indigo-800">
                  {report.title}
                </h3>
                <p className="mt-2 text-xs text-slate-500 font-medium leading-relaxed">
                  {report.description}
                </p>
              </div>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  )
}
