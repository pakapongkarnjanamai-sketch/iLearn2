import { useCallback, useEffect, useMemo, useState, useRef, Component, type ReactNode } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  AlertTriangle,
  ArrowDownRight,
  ArrowUpRight,
  BookOpen,
  CalendarRange,
  ClipboardList,
  Database,
  FileBarChart,
  FolderTree,
  Globe2,
  GraduationCap,
  History,
  Layers,
  Loader2,
  PlusCircle,
  RefreshCw,
  Users,
  type LucideIcon,
} from 'lucide-react'
import { AppButton } from '../components/ui/AppButton'
import { Badge } from '../components/ui/Badge'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useSession } from '../lib/sessionContext'
import { useNotifications } from '../lib/notificationContext'
import { toast } from '../lib/toast'
import { formatDateTime, formatNumber, formatPercent } from '../lib/format'
import {
  fetchDashboardOverview,
  fetchMaintenanceStatus,
  fetchRecentAdminActivities,
  type AdminActivity,
  type DashboardOverview,
  type MaintenanceStatus,
} from './dashboard/dashboardApi'
import {
  CategoryMixChart,
  LearningActivityChart,
  TaskStatusLegend,
  TaskStatusPie,
} from './dashboard/DashboardCharts'



const formatDateShort = (value: string | null) => {
  if (!value) return '—'
  try {
    return new Intl.DateTimeFormat('en-GB', {
      day: '2-digit',
      month: 'short',
    }).format(new Date(value))
  } catch {
    return '—'
  }
}

const relativeTime = (iso: string) => {
  try {
    const date = new Date(iso)
    const diffMs = Date.now() - date.getTime()
    const mins = Math.floor(diffMs / 60000)
    if (mins < 1) return 'just now'
    if (mins < 60) return `${mins}m ago`
    const hours = Math.floor(mins / 60)
    if (hours < 24) return `${hours}h ago`
    const days = Math.floor(hours / 24)
    if (days < 7) return `${days}d ago`
    return formatDateTime(iso)
  } catch {
    return '—'
  }
}

interface ErrorBoundaryProps {
  children: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
}

class ChartErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  public state: ErrorBoundaryState = {
    hasError: false
  }

  public static getDerivedStateFromError(_: Error): ErrorBoundaryState {
    return { hasError: true }
  }

  public componentDidCatch(_error: Error, _errorInfo: React.ErrorInfo) {
  }

  public render() {
    if (this.state.hasError) {
      return (
        <div className="flex flex-col items-center justify-center py-12 text-slate-400 gap-2 text-xs">
          <AlertTriangle className="h-5 w-5 text-amber-500" aria-hidden="true" />
          <span>Chart display error</span>
        </div>
      )
    }

    return this.props.children
  }
}

export function DashboardPage() {
  const navigate = useNavigate()
  const { user, isSuperAdmin } = useSession()
  const { isConnected: isSignalRConnected, subscribeHubEvent } = useNotifications()
  const [overview, setOverview] = useState<DashboardOverview | null>(null)
  const [maintenance, setMaintenance] = useState<MaintenanceStatus | null>(null)
  const [activities, setActivities] = useState<AdminActivity[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const isSignalRConnectedRef = useRef(false)

  useEffect(() => {
    isSignalRConnectedRef.current = isSignalRConnected
  }, [isSignalRConnected])

  const loadAll = useCallback(async (silent = false) => {
    if (!silent) setIsLoading(true)
    try {
      const [ov, mt, acts] = await Promise.all([
        fetchDashboardOverview(),
        fetchMaintenanceStatus().catch(() => null),
        fetchRecentAdminActivities(10).catch(() => [] as AdminActivity[]),
      ])
      setOverview(ov)
      setMaintenance(mt)
      setActivities(acts)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load dashboard'
      toast.error(message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadAll()
  }, [loadAll])

  useEffect(() => {
    const mt = window.setInterval(() => {
      fetchMaintenanceStatus()
        .then(setMaintenance)
        .catch(() => undefined)
    }, 15000)
    const acts = window.setInterval(() => {
      if (isSignalRConnectedRef.current) return
      fetchRecentAdminActivities(10)
        .then(setActivities)
        .catch(() => undefined)
    }, 60000)
    return () => {
      window.clearInterval(mt)
      window.clearInterval(acts)
    }
  }, [])

  // Live admin activity feed via SignalR — uses the central connection from NotificationProvider
  // instead of opening a second connection to the same hub (tech debt fix from PLAN-089).
  useEffect(() => {
    const unsubscribe = subscribeHubEvent('AdminActivityCreated', () => {
      fetchRecentAdminActivities(10)
        .then(setActivities)
        .catch(() => undefined)
    })
    return unsubscribe
  }, [subscribeHubEvent])

  const scopeLabel = useMemo(() => {
    if (!overview) return user?.divisionName ?? '—'
    if (overview.scope.isGlobal) return 'All divisions'
    return overview.scope.divisionName ?? user?.divisionName ?? '—'
  }, [overview, user])

  if (isLoading && !overview) {
    return <LoadingState label="Loading dashboard..." />
  }

  if (!overview) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-slate-500 gap-3">
        <AlertTriangle className="h-6 w-6 text-amber-500" aria-hidden="true" />
        <p className="text-sm">Dashboard could not be loaded.</p>
        <AppButton variant="secondary" icon={RefreshCw} onClick={() => void loadAll()}>
          Retry
        </AppButton>
      </div>
    )
  }

  const { kpi, taskStatus, learningActivity, categoryMix, priorityAssignments, courseAttention } =
    overview

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-3 pb-4 border-b border-slate-200/60">
        <div>
          <div className="text-xxs font-extrabold text-slate-400 uppercase mb-1">iLearn Admin</div>
          <h1 className="text-2xl font-extrabold text-slate-800">
            Operational summary
          </h1>
          <div className="flex items-center gap-2 mt-2 text-xs text-slate-500">
            {overview.scope.isGlobal ? (
              <Globe2 className="h-3.5 w-3.5" aria-hidden="true" />
            ) : (
              <Layers className="h-3.5 w-3.5" aria-hidden="true" />
            )}
            <span className="font-bold text-slate-700">{scopeLabel}</span>
            <span className="text-slate-300">•</span>
            <span>Generated {relativeTime(overview.generatedAt)}</span>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <AppButton variant="secondary" icon={PlusCircle} onClick={() => navigate('/courses/new')}>
            New Course
          </AppButton>
          <AppButton
            variant="primary"
            icon={ClipboardList}
            onClick={() => navigate('/assignments/bulk')}
          >
            New Assignment
          </AppButton>
        </div>
      </header>

      {maintenance?.hasActiveMaintenance && maintenance.operations.length > 0 && (
        <div className="border border-amber-200 rounded-lg bg-amber-50/60 shadow-xs p-4 flex flex-col gap-1.5">
          <div className="flex items-center gap-2 text-xs font-bold text-amber-900">
            <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden="true" />
            Maintenance in progress
          </div>
          {maintenance.operations.map((op) => (
            <div key={op.operationId} className="text-xs text-amber-800 pl-5">
              <span className="font-bold">{op.operationName}</span>
              {op.currentStep && <span className="text-amber-700"> — {op.currentStep}</span>}
              {typeof op.progress === 'number' && (
                <span className="ml-2 tabular-nums">{op.progress}%</span>
              )}
            </div>
          ))}
        </div>
      )}

      <section
        aria-label="Key performance indicators"
        className="grid auto-cols-fr grid-flow-col gap-0 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs"
      >
        <KpiTile
          icon={BookOpen}
          label="Course Portfolio"
          value={formatNumber(kpi.activeCourses)}
          to="/courses"
          meta={
            <>
              <span className="font-bold text-slate-700">{formatNumber(kpi.draftCourses)}</span> draft
              <span className="text-slate-300 mx-1.5">·</span>
              <span className="font-bold text-slate-700">{formatNumber(kpi.contentItemCount)}</span>{' '}
              content items
            </>
          }
        />
        <KpiTile
          icon={ClipboardList}
          label="Active Assignments"
          value={formatNumber(kpi.activeAssignmentBatches)}
          to="/assignments"
          meta={
            <>
              <span className="font-bold text-amber-700">{formatNumber(kpi.dueSoonTasks)}</span> due
              soon
              <span className="text-slate-300 mx-1.5">·</span>
              <span className="font-bold text-rose-700">{formatNumber(kpi.overdueTasks)}</span>{' '}
              overdue
            </>
          }
        />
        <KpiTile
          icon={GraduationCap}
          label="Learner Progress"
          value={formatNumber(kpi.assignedLearners)}
          to={isSuperAdmin ? '/learners' : '/assignments'}
          meta={
            <>
              <span className="font-bold text-emerald-700">
                {formatPercent(kpi.completionRate, Number.isInteger(kpi.completionRate) ? 0 : 1)}
              </span>{' '}
              completion
              <span className="text-slate-300 mx-1.5">·</span>
              <span className="font-bold text-slate-700">
                {formatNumber(kpi.totalLearningTasks)}
              </span>{' '}
              tasks
            </>
          }
        />
        <KpiTile
          icon={History}
          label="Learning Activity"
          value={formatNumber(kpi.learningSessionsLast30)}
          meta={
            <span className="inline-flex items-center gap-1">
              last 30 days
              <span className="text-slate-300 mx-1">·</span>
              <DeltaTag delta={kpi.learningSessionDelta} />
            </span>
          }
        />
      </section>

      <section className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4 lg:col-span-2">
          <SectionHeader title="Learning Activity Trends" subtitle="Last 6 months" />
          <ChartErrorBoundary>
            <LearningActivityChart data={learningActivity} />
          </ChartErrorBoundary>
        </div>
        <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
          <SectionHeader
            title="Task Status"
            subtitle={`${formatNumber(kpi.totalLearningTasks)} total tasks`}
          />
          <ChartErrorBoundary>
            <TaskStatusPie data={taskStatus} />
            <div className="mt-2">
              <TaskStatusLegend data={taskStatus} />
            </div>
          </ChartErrorBoundary>
        </div>
      </section>

      <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
        <div className="p-4 pb-0">
          <SectionHeader
            title="Priority Assignment Reports"
            trailing={
              <Link
                to="/assignments"
                className="text-xs font-bold text-indigo-600 hover:text-indigo-800"
              >
                View all →
              </Link>
            }
          />
        </div>
        {priorityAssignments.length === 0 ? (
          <EmptyRow label="No assignments need attention right now." />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="text-left text-slate-500 border-b border-slate-200/60">
                  <Th>Assignment</Th>
                  <Th>Status</Th>
                  <Th align="right">Learners</Th>
                  <Th>Due</Th>
                  <Th>Completion</Th>
                  <Th align="right">Open</Th>
                </tr>
              </thead>
              <tbody>
                {priorityAssignments.map((a) => (
                  <tr
                    key={a.assignmentId}
                    className="border-b border-slate-100/70 hover:bg-slate-50/50 transition-colors"
                  >
                    <Td>
                      <div className="font-bold text-slate-800">{a.assignmentNo}</div>
                      {a.description && (
                        <div className="text-slate-500 truncate max-w-md">{a.description}</div>
                      )}
                    </Td>
                    <Td>
                      <StatusBadge>{a.status}</StatusBadge>
                    </Td>
                    <Td align="right">
                      <span className="tabular-nums font-bold text-slate-700">
                        {formatNumber(a.learnerCount)}
                      </span>
                    </Td>
                    <Td>
                      <span className="text-slate-600">{formatDateShort(a.dueDate)}</span>
                    </Td>
                    <Td>
                      <CompletionBar value={a.completionRate} />
                    </Td>
                    <Td align="right">
                      <Link
                        to={`/assignments/${a.assignmentId}`}
                        className="text-indigo-600 font-bold hover:text-indigo-800"
                      >
                        Detail →
                      </Link>
                    </Td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs lg:col-span-2">
          <div className="p-4 pb-0">
            <SectionHeader
              title="Courses Needing Attention"
            />
          </div>
          {courseAttention.length === 0 ? (
            <EmptyRow label="All courses are on track." />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr className="text-left text-slate-500 border-b border-slate-200/60">
                    <Th>Course</Th>
                    <Th align="right">Tasks</Th>
                    <Th align="right">Overdue</Th>
                    <Th>Completion</Th>
                  </tr>
                </thead>
                <tbody>
                  {courseAttention.map((c) => (
                    <tr
                      key={c.courseId}
                      className="border-b border-slate-100/70 hover:bg-slate-50/50"
                    >
                      <Td>
                        <Link
                          to={`/courses/${c.courseId}`}
                          className="font-bold text-slate-800 hover:text-indigo-700"
                        >
                          {c.courseCode} — {c.courseTitle}
                        </Link>
                        {c.categoryName && (
                          <div className="text-slate-500">{c.categoryName}</div>
                        )}
                      </Td>
                      <Td align="right">
                        <span className="tabular-nums">{formatNumber(c.learnerTasks)}</span>
                      </Td>
                      <Td align="right">
                        <span className="tabular-nums font-bold text-rose-700">
                          {formatNumber(c.overdueTasks)}
                        </span>
                      </Td>
                      <Td>
                        <CompletionBar value={c.completionRate} />
                      </Td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
        <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
          <SectionHeader title="Course Categories" subtitle="Top 6 by course count" />
          <ChartErrorBoundary>
            <CategoryMixChart data={categoryMix} />
          </ChartErrorBoundary>
        </div>
      </section>

      <section className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4 lg:col-span-2">
          <SectionHeader title="Report Hub" />
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
            <ReportLink
              to="/assignments"
              icon={ClipboardList}
              title="Assignment Reports"
            />
            <ReportLink
              to="/courses"
              icon={BookOpen}
              title="Course Catalog"
            />
            <ReportLink
              to="/content-library"
              icon={FolderTree}
              title="Content Library"
            />
            {isSuperAdmin && (
              <ReportLink
                to="/learners"
                icon={Users}
                title="Learner Directory"
              />
            )}
            <ReportLink
              to="/learner-groups"
              icon={Users}
              title="Learner Groups"
            />
            {isSuperAdmin && (
              <ReportLink
                to="/master-data"
                icon={Database}
                title="Master Data"
              />
            )}
            <ReportLink
              to="/assignments/bulk"
              icon={CalendarRange}
              title="Bulk Assign"
            />
            <ReportLink
              to="/reports"
              icon={FileBarChart}
              title="Report Hub"
            />
          </div>
        </div>
        <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
          <SectionHeader
            title="Recent Admin Activity"
            subtitle={`${activities.length} item(s)`}
            trailing={
              <Badge tone={isSignalRConnected ? 'success' : 'neutral'} variant="soft" size="xxs">
                <span
                  className={`mr-1.5 h-1.5 w-1.5 rounded-full ${
                    isSignalRConnected ? 'bg-emerald-500 shadow-[0_0_0_2px_rgb(209_250_229)]' : 'bg-slate-400'
                  }`}
                  aria-hidden="true"
                />
                {isSignalRConnected ? 'Live' : 'Polling'}
              </Badge>
            }
          />
          {activities.length === 0 ? (
            <EmptyRow label="No recent activity." />
          ) : (
            <ul className="flex flex-col">
              {activities.map((act) => (
                <li
                  key={act.id}
                  className="py-2 border-b border-slate-100/70 last:border-b-0 flex items-start gap-2"
                >
                  <FileBarChart
                    className="h-3.5 w-3.5 text-slate-400 mt-0.5 shrink-0"
                    aria-hidden="true"
                  />
                  <div className="flex-1 min-w-0">
                    <div className="text-xs font-bold text-slate-800 truncate">{act.title}</div>
                    <div className="text-xxs text-slate-500 flex items-center gap-1.5">
                      <span>{act.actionType}</span>
                      <span className="text-slate-300">·</span>
                      <span>{relativeTime(act.createdAt)}</span>
                      {act.createdBy && (
                        <>
                          <span className="text-slate-300">·</span>
                          <span>{act.createdBy}</span>
                        </>
                      )}
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>
    </div>
  )
}

function KpiTile({
  icon: Icon,
  label,
  value,
  meta,
  to,
}: {
  icon: LucideIcon
  label: string
  value: string
  meta?: ReactNode
  to?: string
}) {
  const content = (
    <div className="min-w-0 border-r border-slate-200 p-4 last:border-r-0 flex flex-col gap-1">
      <div className="flex items-center gap-1.5 text-xxs font-extrabold text-slate-400 uppercase">
        <Icon className="h-3.5 w-3.5" aria-hidden="true" />
        {label}
      </div>
      <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight">
        {value}
      </div>
      {meta && <div className="text-xs text-slate-500">{meta}</div>}
    </div>
  )
  return to ? (
    <Link to={to} className="block hover:bg-slate-50/60 transition-colors">
      {content}
    </Link>
  ) : (
    content
  )
}

function DeltaTag({ delta }: { delta: number }) {
  if (delta === 0) return <span className="text-slate-500">flat</span>
  const isUp = delta > 0
  const Icon = isUp ? ArrowUpRight : ArrowDownRight
  const color = isUp ? 'text-emerald-700' : 'text-rose-700'
  return (
    <span className={`inline-flex items-center gap-0.5 font-bold ${color}`}>
      <Icon className="h-3 w-3" aria-hidden="true" />
      {isUp ? '+' : ''}
      {delta}
    </span>
  )
}

function SectionHeader({
  title,
  subtitle,
  trailing,
}: {
  title: string
  subtitle?: string
  trailing?: ReactNode
}) {
  return (
    <div className="flex items-end justify-between gap-2 mb-3 pb-2 border-b border-slate-200/60">
      <div>
        <h2 className="text-sm font-extrabold text-slate-700">{title}</h2>
        {subtitle && <div className="text-xxs text-slate-400 mt-0.5">{subtitle}</div>}
      </div>
      {trailing}
    </div>
  )
}

function Th({ children, align = 'left' }: { children: ReactNode; align?: 'left' | 'right' }) {
  return (
    <th
      className={`text-xxs font-extrabold uppercase px-4 py-3 ${
        align === 'right' ? 'text-right' : 'text-left'
      }`}
    >
      {children}
    </th>
  )
}

function Td({ children, align = 'left' }: { children: ReactNode; align?: 'left' | 'right' }) {
  return (
    <td className={`px-4 py-3 align-middle text-slate-700 font-semibold ${align === 'right' ? 'text-right' : ''}`}>{children}</td>
  )
}

function CompletionBar({ value }: { value: number }) {
  const pct = Math.max(0, Math.min(100, value ?? 0))
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-1.5 bg-slate-100 rounded-full overflow-hidden min-w-20">
        <div className="h-full bg-emerald-500 rounded-full" style={{ width: `${pct}%` }} />
      </div>
      <span className="text-xxs font-bold text-slate-600 tabular-nums w-10 text-right">
        {formatPercent(pct)}
      </span>
    </div>
  )
}

function EmptyRow({ label }: { label: string }) {
  return <div className="py-6 text-center text-xs text-slate-400">{label}</div>
}

function ReportLink({
  to,
  icon: Icon,
  title,
}: {
  to: string
  icon: LucideIcon
  title: string
}) {
  return (
    <Link
      to={to}
      className="group flex items-start gap-2.5 p-2.5 rounded-md border border-slate-200/60 hover:border-indigo-300 hover:bg-indigo-50/40 transition-colors"
    >
      <div className="h-7 w-7 rounded bg-slate-100 group-hover:bg-indigo-100 flex items-center justify-center shrink-0">
        <Icon
          className="h-3.5 w-3.5 text-slate-600 group-hover:text-indigo-700"
          aria-hidden="true"
        />
      </div>
      <div className="flex-1 min-w-0">
        <div className="text-xs font-bold text-slate-800 group-hover:text-indigo-800">{title}</div>
      </div>
    </Link>
  )
}

export default DashboardPage
