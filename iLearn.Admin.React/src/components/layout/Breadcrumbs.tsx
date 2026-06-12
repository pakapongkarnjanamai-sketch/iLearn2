import { Link, useLocation } from 'react-router-dom'
import { ChevronRight, Home } from 'lucide-react'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'

const SEGMENT_MAP: Record<string, string> = {
  'courses': 'Courses',
  'student-groups': 'Learner Groups',
  'learner-groups': 'Learner Groups',
  'assignments': 'Assignments',
  'content-library': 'Content Library',
  'learners': 'Learners',
  'master-data': 'Master Data',
  'student-group-categories': 'Learner Group Categories',
  'learner-group-categories': 'Learner Group Categories',
  'system-config': 'System Config',
  'new': 'Create',
  'edit': 'Modify',
  'version': 'SCORM Version',
  'profile': 'Profile',
  'gantt': 'Schedule',
  'bulk': 'Bulk Assignment'
}

export function Breadcrumbs() {
  const location = useLocation()
  const pathnames = location.pathname.split('/').filter(x => x)
  const { labels, customCrumbs } = useBreadcrumbs()

  if (customCrumbs && customCrumbs.length > 0) {
    return (
      <nav className="flex items-center space-x-1.5 text-slate-500 font-semibold text-xs select-none">
        <Link
          to="/"
          className="flex items-center text-slate-400 hover:text-slate-600 transition p-0.5 rounded"
          title="Dashboard Home"
        >
          <Home className="h-3.5 w-3.5" />
        </Link>

        <ChevronRight className="h-3 w-3 text-slate-300 shrink-0" />

        {customCrumbs.map((crumb, index) => {
          const last = index === customCrumbs.length - 1

          return (
            <div key={`${crumb.to}-${index}`} className="flex items-center space-x-1.5 shrink-0">
              {last ? (
                <span className="text-slate-800 font-extrabold">{crumb.label}</span>
              ) : (
                <Link to={crumb.to} className="hover:text-slate-700 transition">
                  {crumb.label}
                </Link>
              )}
              {!last && <ChevronRight className="h-3 w-3 text-slate-300 shrink-0" />}
            </div>
          )
        })}
      </nav>
    )
  }

  return (
    <nav className="flex items-center space-x-1.5 text-slate-500 font-semibold text-xs select-none">
      {/* Home link */}
      <Link 
        to="/" 
        className="flex items-center text-slate-400 hover:text-slate-600 transition p-0.5 rounded"
        title="Dashboard Home"
      >
        <Home className="h-3.5 w-3.5" />
      </Link>
      
      {pathnames.length > 0 && <ChevronRight className="h-3 w-3 text-slate-300 shrink-0" />}

      {pathnames.map((value, index) => {
        const last = index === pathnames.length - 1
        const to = `/${pathnames.slice(0, index + 1).join('/')}`
        
        // Resolve breadcrumb text segment
        let label = labels[value] || SEGMENT_MAP[value]
        if (!label) {
          // If the segment is an ID (is a number or is a dynamic hash NID like 500124)
          if (/^\d+$/.test(value) || value.length > 10 || (value.startsWith('CR-') || value.startsWith('LG-'))) {
            label = labels[value] || 'Details'
          } else {
            label = value.charAt(0).toUpperCase() + value.slice(1)
          }
        }

        return (
          <div key={to} className="flex items-center space-x-1.5 shrink-0">
            {last ? (
              <span className="text-slate-800 font-extrabold">{label}</span>
            ) : (
              <Link 
                to={to} 
                className="hover:text-slate-700 transition"
              >
                {label}
              </Link>
            )}
            {!last && <ChevronRight className="h-3 w-3 text-slate-300 shrink-0" />}
          </div>
        )
      })}
    </nav>
  )
}
