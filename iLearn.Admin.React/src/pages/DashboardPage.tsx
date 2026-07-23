import { useCallback, useEffect, useMemo, useState, useRef, Component, type ReactNode } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  AlertTriangle,
  ArrowDownRight,
  ArrowUpRight,
  BookOpen,
  ClipboardList,
  Globe2,
  GraduationCap,
  History,
  Layers,
  Loader2,
  PlusCircle,
  RefreshCw,
  TrendingUp,
  type LucideIcon,
} from 'lucide-react'
import { Card } from '../components/ui/Card'
import { AppButton } from '../components/ui/AppButton'
import { Badge } from '../components/ui/Badge'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { ProgressBar } from '../components/ui/ProgressBar'
import { useSession } from '../lib/sessionContext'
import { useNotifications } from '../lib/notificationContext'
import { toast } from '../lib/toast'
import { formatDate, formatNumber, formatPercent, formatRelativeTime } from '../lib/format'
import { CRUMB_LABELS, DASHBOARD_LABELS, learnerStatusLabel, t, tf } from '../lib/labels'
import {
  fetchDashboardOverview,
  fetchMaintenanceStatus,
  fetchRecentAdminActivities,
  type AdminActivity,
  type DashboardOverview,
  type MaintenanceStatus,
} from './dashboard/dashboardApi'
import { LearningActivityChart } from './dashboard/DashboardCharts'

interface ErrorBoundaryProps {
  children: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
}

class ChartErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  public state: ErrorBoundaryState = {
    hasError: false,
  }

  public static getDerivedStateFromError(_: Error): ErrorBoundaryState {
    return { hasError: true }
  }

  public componentDidCatch(_error: Error, _errorInfo: React.ErrorInfo) {}

  public render() {
    if (this.state.hasError) {
      return (
        <div className="flex flex-col items-center justify-center py-12 text-slate-400 gap-2 text-xs font-medium">
          <AlertTriangle className="h-5 w-5 text-amber-500" aria-hidden="true" />
          <span>{t(DASHBOARD_LABELS.chartError)}</span>
        </div>
      )
    }

    return this.props.children
  }
}

export function DashboardPage() {
  const navigate = useNavigate()
  const { user } = useSession()
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
      const message = err instanceof Error ? err.message : t(DASHBOARD_LABELS.loadFailed)
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

  // Live admin activity feed via SignalR — uses central connection from NotificationProvider
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
    if (overview.scope.isGlobal) return t(DASHBOARD_LABELS.allDivisions)
    return overview.scope.divisionName ?? user?.divisionName ?? '—'
  }, [overview, user])

  if (isLoading && !overview) {
    return <LoadingState label={t(DASHBOARD_LABELS.loading)} />
  }

  if (!overview) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-slate-500 gap-3">
        <AlertTriangle className="h-6 w-6 text-amber-500" aria-hidden="true" />
        <p className="text-sm font-semibold">{t(DASHBOARD_LABELS.loadFailed)}</p>
        <AppButton variant="secondary" icon={RefreshCw} onClick={() => void loadAll()}>
          {t(DASHBOARD_LABELS.retry)}
        </AppButton>
      </div>
    )
  }

  const { kpi, learningActivity, priorityAssignments, courseAttention } = overview

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <header className="flex flex-wrap items-end justify-between gap-3 pb-4 border-b border-slate-200/60">
        <div>
          <div className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider mb-1">
            iLearn Admin
          </div>
          <h1 className="text-2xl font-extrabold text-slate-800">{t(DASHBOARD_LABELS.pageTitle)}</h1>
          <div className="flex items-center gap-2 mt-1.5 text-xs text-slate-500 font-medium">
            {overview.scope.isGlobal ? (
              <Globe2 className="h-3.5 w-3.5" aria-hidden="true" />
            ) : (
              <Layers className="h-3.5 w-3.5" aria-hidden="true" />
            )}
            <span className="font-bold text-slate-700">{scopeLabel}</span>
            <span className="text-slate-300">•</span>
            <span>{tf(DASHBOARD_LABELS.updated, formatRelativeTime(overview.generatedAt))}</span>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <AppButton variant="secondary" icon={PlusCircle} onClick={() => navigate('/courses/new')}>
            {t(DASHBOARD_LABELS.newCourse)}
          </AppButton>
          <AppButton
            variant="primary"
            icon={ClipboardList}
            onClick={() => navigate('/assignments/bulk')}
          >
            {t(DASHBOARD_LABELS.newAssignment)}
          </AppButton>
        </div>
      </header>

      {/* Maintenance Banner */}
      {maintenance?.hasActiveMaintenance && maintenance.operations.length > 0 && (
        <div className="border border-amber-200 rounded-lg bg-amber-50/60 shadow-xs p-4 flex flex-col gap-1.5">
          <div className="flex items-center gap-2 text-xs font-bold text-amber-900">
            <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden="true" />
            {t(DASHBOARD_LABELS.maintenanceInProgress)}
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

      {/* Action-First KPI Strip */}
      <Card bodyClassName="p-0 grid auto-cols-fr grid-flow-col divide-x divide-slate-200">
        <KpiTile
          icon={AlertTriangle}
          label={t(DASHBOARD_LABELS.overdueTasks)}
          value={formatNumber(kpi.overdueTasks)}
          valueTone="rose"
          to="/assignments"
          meta={
            <>
              {t(DASHBOARD_LABELS.fromPrefix)} <span className="font-bold text-slate-700">{formatNumber(kpi.totalLearningTasks)}</span> {t(DASHBOARD_LABELS.totalTasksSuffix)}
            </>
          }
        />
        <KpiTile
          icon={History}
          label={t(DASHBOARD_LABELS.dueSoon)}
          value={formatNumber(kpi.dueSoonTasks)}
          valueTone="amber"
          to="/assignments"
          meta={t(DASHBOARD_LABELS.within7Days)}
        />
        <KpiTile
          icon={GraduationCap}
          label={t(DASHBOARD_LABELS.completionRate)}
          value={formatPercent(kpi.completionRate, Number.isInteger(kpi.completionRate) ? 0 : 1)}
          valueTone="indigo"
          to="/reports/courses"
          meta={
            <>
              <span className="font-bold text-slate-700">{formatNumber(kpi.assignedLearners)}</span> {t(DASHBOARD_LABELS.learnersUnit)} ·{' '}
              <span className="font-bold text-slate-700">{formatNumber(kpi.completedLearningTasks)}</span>/
              <span className="font-bold text-slate-700">{formatNumber(kpi.totalLearningTasks)}</span> {t(DASHBOARD_LABELS.tasksUnit)}
            </>
          }
        />
        <KpiTile
          icon={BookOpen}
          label={t(DASHBOARD_LABELS.learningActivity30)}
          value={formatNumber(kpi.learningSessionsLast30)}
          meta={
            <span className="inline-flex items-center gap-1.5">
              <span>{t(DASHBOARD_LABELS.previous30Days)}</span>
              <span className="text-slate-300">·</span>
              <DeltaTag delta={kpi.learningSessionDelta} />
            </span>
          }
        />
      </Card>

      {/* Priority Assignments Table */}
      <Card
        title={t(DASHBOARD_LABELS.priorityAssignments)}
        icon={ClipboardList}
        actions={
          <Link
            to="/assignments"
            className="text-xs font-bold text-indigo-600 hover:text-indigo-800 transition-colors"
          >
            {t(DASHBOARD_LABELS.viewAll)} →
          </Link>
        }
      >
        {priorityAssignments.length === 0 ? (
          <EmptyRow label={t(DASHBOARD_LABELS.noPriorityAssignments)} />
        ) : (
          <div className="overflow-x-auto custom-scrollbar">
            <table className="w-full text-xs text-left border-collapse">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none">
                  <Th>{t(DASHBOARD_LABELS.colAssignment)}</Th>
                  <Th>{t(DASHBOARD_LABELS.colStatus)}</Th>
                  <Th align="right">{t(DASHBOARD_LABELS.colLearners)}</Th>
                  <Th>{t(DASHBOARD_LABELS.colDueDate)}</Th>
                  <Th>{t(DASHBOARD_LABELS.colCompletion)}</Th>
                  <Th align="right">{t(DASHBOARD_LABELS.colActions)}</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-700">
                {priorityAssignments.map((a) => (
                  <tr key={a.assignmentId} className="hover:bg-slate-50/50 transition-colors">
                    <Td>
                      <div className="font-bold text-slate-800">{a.assignmentNo}</div>
                      {a.description && (
                        <div className="text-slate-500 truncate max-w-md mt-0.5">{a.description}</div>
                      )}
                    </Td>
                    <Td>
                      <StatusBadge size="xxs">{learnerStatusLabel(a.status)}</StatusBadge>
                    </Td>
                    <Td align="right">
                      <span className="tabular-nums font-bold text-slate-700">
                        {formatNumber(a.learnerCount)}
                      </span>
                    </Td>
                    <Td>
                      <span className="text-slate-600 font-semibold">{formatDate(a.dueDate)}</span>
                    </Td>
                    <Td>
                      <ProgressBar value={a.completionRate} completed={a.completionRate >= 100} />
                    </Td>
                    <Td align="right">
                      <Link
                        to={`/assignments/${a.assignmentId}`}
                        className="text-indigo-600 font-bold hover:text-indigo-800"
                      >
                        {t(CRUMB_LABELS.details)} →
                      </Link>
                    </Td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Courses Needing Attention Table */}
      <Card title={t(DASHBOARD_LABELS.courseAttention)} icon={BookOpen}>
        {courseAttention.length === 0 ? (
          <EmptyRow label={t(DASHBOARD_LABELS.allCoursesOnTrack)} />
        ) : (
          <div className="overflow-x-auto custom-scrollbar">
            <table className="w-full text-xs text-left border-collapse">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none">
                  <Th>{t(DASHBOARD_LABELS.colCourse)}</Th>
                  <Th align="right">{t(DASHBOARD_LABELS.colTasks)}</Th>
                  <Th align="right">{learnerStatusLabel('Overdue')}</Th>
                  <Th>{t(DASHBOARD_LABELS.colCompletion)}</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-700">
                {courseAttention.map((c) => (
                  <tr key={c.courseId} className="hover:bg-slate-50/50 transition-colors">
                    <Td>
                      <Link
                        to={`/courses/${c.courseId}`}
                        className="font-bold text-slate-800 hover:text-indigo-700"
                      >
                        {c.courseCode} — {c.courseTitle}
                      </Link>
                      {c.categoryName && (
                        <div className="text-slate-500 mt-0.5">{c.categoryName}</div>
                      )}
                    </Td>
                    <Td align="right">
                      <span className="tabular-nums font-semibold">{formatNumber(c.learnerTasks)}</span>
                    </Td>
                    <Td align="right">
                      <span className="tabular-nums font-bold text-rose-600">
                        {formatNumber(c.overdueTasks)}
                      </span>
                    </Td>
                    <Td>
                      <ProgressBar value={c.completionRate} completed={c.completionRate >= 100} />
                    </Td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Bottom Grid: Trends & Recent Activity */}
      <section className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card
          title={t(DASHBOARD_LABELS.activityTrend)}
          icon={TrendingUp}
          className="lg:col-span-2"
          actions={<span className="text-xxs font-medium text-slate-400">{t(DASHBOARD_LABELS.last6Months)}</span>}
        >
          <div className="p-4">
            <ChartErrorBoundary>
              <LearningActivityChart data={learningActivity} />
            </ChartErrorBoundary>
          </div>
        </Card>

        <Card
          title={t(DASHBOARD_LABELS.recentAdminActivity)}
          icon={History}
          actions={
            <Badge tone={isSignalRConnected ? 'success' : 'neutral'} variant="soft" size="xxs">
              <span
                className={`mr-1.5 h-1.5 w-1.5 rounded-full ${
                  isSignalRConnected ? 'bg-emerald-500 shadow-[0_0_0_2px_rgb(209_250_229)]' : 'bg-slate-400'
                }`}
                aria-hidden="true"
              />
              {t(isSignalRConnected ? DASHBOARD_LABELS.realtime : DASHBOARD_LABELS.autoRefresh)}
            </Badge>
          }
        >
          <div className="p-4">
            {activities.length === 0 ? (
              <EmptyRow label={t(DASHBOARD_LABELS.noRecentActivity)} />
            ) : (
              <ul className="flex flex-col divide-y divide-slate-100">
                {activities.map((act) => (
                  <li key={act.id} className="py-2.5 first:pt-0 last:pb-0 flex items-start gap-2.5">
                    <div className="h-2 w-2 rounded-full bg-indigo-500 mt-1.5 shrink-0" aria-hidden="true" />
                    <div className="flex-1 min-w-0">
                      <div className="text-xs font-bold text-slate-800 truncate">{act.title}</div>
                      <div className="text-xxs text-slate-500 flex items-center gap-1.5 mt-0.5 font-medium">
                        <span>{act.actionType}</span>
                        <span className="text-slate-300">·</span>
                        <span>{formatRelativeTime(act.createdAt)}</span>
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
        </Card>
      </section>
    </div>
  )
}

function KpiTile({
  icon: Icon,
  label,
  value,
  valueTone,
  meta,
  to,
}: {
  icon: LucideIcon
  label: string
  value: string
  valueTone?: 'rose' | 'amber' | 'indigo' | 'default'
  meta?: ReactNode
  to?: string
}) {
  const textColor =
    valueTone === 'rose'
      ? 'text-rose-600'
      : valueTone === 'amber'
      ? 'text-amber-600'
      : valueTone === 'indigo'
      ? 'text-indigo-600'
      : 'text-slate-800'

  const content = (
    <div className="p-4 flex flex-col gap-1">
      <div className="flex items-center gap-1.5 text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
        <Icon className="h-3.5 w-3.5" aria-hidden="true" />
        {label}
      </div>
      <div className={`text-2xl font-extrabold ${textColor} tabular-nums leading-tight`}>
        {value}
      </div>
      {meta && <div className="text-xs text-slate-500 font-medium">{meta}</div>}
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
  if (delta === 0) return <span className="text-slate-500 font-medium">{t(DASHBOARD_LABELS.steady)}</span>
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

function Th({ children, align = 'left' }: { children: ReactNode; align?: 'left' | 'right' }) {
  return (
    <th
      className={`px-4 py-3 text-xxs font-extrabold uppercase text-slate-500 ${
        align === 'right' ? 'text-right' : 'text-left'
      }`}
    >
      {children}
    </th>
  )
}

function Td({ children, align = 'left' }: { children: ReactNode; align?: 'left' | 'right' }) {
  return (
    <td
      className={`px-4 py-3 align-middle text-slate-700 font-semibold ${
        align === 'right' ? 'text-right' : ''
      }`}
    >
      {children}
    </td>
  )
}

function EmptyRow({ label }: { label: string }) {
  return <div className="py-8 text-center text-xs text-slate-400 font-medium">{label}</div>
}

export default DashboardPage
