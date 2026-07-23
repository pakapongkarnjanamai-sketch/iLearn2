import { useCallback, useEffect, useState } from 'react'
import { Activity, GraduationCap, RefreshCw, Server } from 'lucide-react'
import { buildApiUrl, fetchWithAccessControl } from '../../lib/apiClient'
import { formatDateTime, formatNumber } from '../../lib/format'
import { AppButton } from '../../components/ui/AppButton'
import { Badge } from '../../components/ui/Badge'
import { ADMIN_LABELS, HEALTH_LABELS, t, tf, type LabelPair } from '../../lib/labels'
import { Card } from '../../components/ui/Card'
import { LoadingState } from '../../components/ui/LoadingState'

// Mirrors HealthController smoke response
// (iLearn.API/Controllers/HealthController.cs and iLearn.User/Controllers/HealthController.cs)
type HealthCheckItem = {
  name: string
  status: 'pass' | 'fail'
  detail: string
  durationMs: number
}

type HealthReport = {
  status: 'pass' | 'fail'
  service: string
  timestamp: string
  checks: HealthCheckItem[]
}

/** Result of probing one service: a parsed report, or unreachable with a reason. */
type ProbeResult =
  | { kind: 'report'; report: HealthReport }
  | { kind: 'unreachable'; reason: string }

// Mirrors the fileSettings part of SystemConfigController.Get (iLearn.API/Controllers/SystemConfigController.cs)
type SystemConfigFileSettings = {
  fileSettings: {
    hostUrl: string
  }
}

/**
 * Health endpoints intentionally return 503 with a JSON body when a check fails,
 * so parse the body on both 200 and 503 instead of treating non-OK as fatal.
 */
async function probeHealth(url: string): Promise<ProbeResult> {
  try {
    const response = await fetch(url, {
      credentials: 'include',
      headers: { Accept: 'application/json' },
    })
    const body = await response.text()
    try {
      return { kind: 'report', report: JSON.parse(body) as HealthReport }
    } catch {
      return {
        kind: 'unreachable',
        reason: tf(ADMIN_LABELS.unexpectedResponse, response.status, url),
      }
    }
  } catch {
    return {
      kind: 'unreachable',
      reason: tf(ADMIN_LABELS.unreachableReason, url),
    }
  }
}

const CHECK_LABELS: Record<string, LabelPair> = {
  database: ADMIN_LABELS.databaseConnection,
  courseFileShare: ADMIN_LABELS.courseFileShare,
  courseContentFolder: ADMIN_LABELS.courseContentFolder,
  courseIndexFile: ADMIN_LABELS.courseIndexFile,
  api: ADMIN_LABELS.apiReachability,
}

function overallBadge(result: ProbeResult | null) {
  if (!result) {
    return <Badge tone="neutral" variant="outline">{t(HEALTH_LABELS.checking)}</Badge>
  }
  if (result.kind === 'unreachable') {
    return <Badge tone="warning" variant="outline">{t(HEALTH_LABELS.unreachable)}</Badge>
  }
  return result.report.status === 'pass'
    ? <Badge tone="success" variant="outline">{t(HEALTH_LABELS.operational)}</Badge>
    : <Badge tone="danger" variant="outline">{t(HEALTH_LABELS.degraded)}</Badge>
}

function checkLabel(name: string): string {
  const pair = CHECK_LABELS[name]
  return pair ? t(pair) : name
}

function ProbeResultBody({ result }: { result: ProbeResult | null }) {
  if (!result) {
    return <p className="p-4 text-sm text-slate-500">{t(ADMIN_LABELS.waitingForResult)}</p>
  }

  if (result.kind === 'unreachable') {
    return <p className="p-4 text-sm text-amber-700">{result.reason}</p>
  }

  return (
    <div>
      <ul className="divide-y divide-slate-100">
        {result.report.checks.map((check) => (
          <li key={check.name} className="flex items-start justify-between gap-4 px-4 py-3">
            <div className="min-w-0">
              <p className="text-sm font-semibold text-slate-800">
                {checkLabel(check.name)}
              </p>
              <p className="mt-0.5 break-all font-mono text-xs text-slate-500">{check.detail}</p>
            </div>
            <div className="flex shrink-0 flex-col items-end gap-1">
              <Badge tone={check.status === 'pass' ? 'success' : 'danger'}>
                {t(check.status === 'pass' ? HEALTH_LABELS.pass : HEALTH_LABELS.fail)}
              </Badge>
              <span className="text-xs text-slate-400">{formatNumber(check.durationMs)} ms</span>
            </div>
          </li>
        ))}
      </ul>
      <p className="border-t border-slate-100 px-4 py-2 text-xs text-slate-400">
        {tf(ADMIN_LABELS.checkedAt, formatDateTime(result.report.timestamp))}
      </p>
    </div>
  )
}

export function HealthCheckPage() {
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [apiResult, setApiResult] = useState<ProbeResult | null>(null)
  const [userResult, setUserResult] = useState<ProbeResult | null>(null)
  const [userSiteBaseUrl, setUserSiteBaseUrl] = useState<string | null>(null)
  const [courseId, setCourseId] = useState('')

  const runChecks = useCallback(async (courseIdToCheck: string) => {
    setRefreshing(true)
    setApiResult(null)
    setUserResult(null)
    try {
      // Resolve the learner site base URL from API config once; the API smoke
      // check runs in parallel and does not depend on it.
      const apiProbe = probeHealth(buildApiUrl('health/smoke'))

      let hostUrl = userSiteBaseUrl
      if (hostUrl === null) {
        try {
          const config = await fetchWithAccessControl<SystemConfigFileSettings>('admin/SystemConfig')
          hostUrl = config.fileSettings.hostUrl.replace(/\/+$/, '')
        } catch {
          hostUrl = ''
        }
        setUserSiteBaseUrl(hostUrl)
      }

      setApiResult(await apiProbe)

      if (hostUrl) {
        const query = courseIdToCheck.trim()
          ? `?courseId=${encodeURIComponent(courseIdToCheck.trim())}`
          : ''
        setUserResult(await probeHealth(`${hostUrl}/health/smoke${query}`))
      } else {
        setUserResult({
          kind: 'unreachable',
          reason: t(ADMIN_LABELS.learnerSiteUnavailable),
        })
      }
    } finally {
      setLoading(false)
      setRefreshing(false)
    }
  }, [userSiteBaseUrl])

  useEffect(() => {
    runChecks('')
    // Run once on mount; refreshes are user-triggered.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  if (loading) {
    return <LoadingState label={t(ADMIN_LABELS.runningHealthChecks)} />
  }

  return (
    <div className="space-y-6">
      <Card
        title={t(ADMIN_LABELS.healthCheck)}
        icon={Activity}
        actions={
          <AppButton
            type="button"
            icon={RefreshCw}
            loading={refreshing}
            onClick={() => runChecks(courseId)}
          >
            {t(ADMIN_LABELS.rerunChecks)}
          </AppButton>
        }
        bodyClassName="p-4"
      >
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <label className="flex-1">
            <span className="mb-1 block text-xs font-semibold uppercase text-slate-500">
              {t(ADMIN_LABELS.courseIdOptional)}
            </span>
            <input
              type="text"
              value={courseId}
              onChange={(event) => setCourseId(event.target.value)}
              placeholder={t(ADMIN_LABELS.courseIdExample)}
              className="w-full rounded-md border border-slate-300 px-3 py-2 font-mono text-sm text-slate-800 placeholder:text-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            />
          </label>
          <p className="text-xs text-slate-500 sm:max-w-xs sm:pb-2.5">
            {t(ADMIN_LABELS.healthCheckHelp)}
          </p>
        </div>
      </Card>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card
          title={t(ADMIN_LABELS.apiService)}
          icon={Server}
          actions={overallBadge(apiResult)}
        >
          <ProbeResultBody result={apiResult} />
        </Card>

        <Card
          title={t(ADMIN_LABELS.learnerSite)}
          icon={GraduationCap}
          actions={overallBadge(userResult)}
        >
          <ProbeResultBody result={userResult} />
        </Card>
      </div>
    </div>
  )
}
