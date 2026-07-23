import { useCallback, useEffect, useState } from 'react'
import { Activity, GraduationCap, RefreshCw, Server } from 'lucide-react'
import { buildApiUrl, fetchWithAccessControl } from '../../lib/apiClient'
import { formatDateTime, formatNumber } from '../../lib/format'
import { AppButton } from '../../components/ui/AppButton'
import { Badge } from '../../components/ui/Badge'
import { HEALTH_LABELS, t } from '../../lib/labels'
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
        reason: `Unexpected response (HTTP ${response.status}) from ${url}`,
      }
    }
  } catch {
    return {
      kind: 'unreachable',
      reason: `Could not reach ${url} from this browser (service down, or blocked by CORS when running the dev server)`,
    }
  }
}

const CHECK_LABELS: Record<string, string> = {
  database: 'Database connection',
  courseFileShare: 'Course file share (UNC)',
  courseContentFolder: 'Course content folder',
  courseIndexFile: 'Course entry file (res/index.html)',
  api: 'API reachability',
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

function ProbeResultBody({ result }: { result: ProbeResult | null }) {
  if (!result) {
    return <p className="p-4 text-sm text-slate-500">Waiting for result…</p>
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
                {CHECK_LABELS[check.name] ?? check.name}
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
        Checked at {formatDateTime(result.report.timestamp)}
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
          reason: 'Learner site URL is not available (FileSettings.HostUrl could not be read from the API)',
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
    return <LoadingState label="Running system health checks..." />
  }

  return (
    <div className="space-y-6">
      <Card
        title="System Health Check"
        icon={Activity}
        actions={
          <AppButton
            type="button"
            icon={RefreshCw}
            loading={refreshing}
            onClick={() => runChecks(courseId)}
          >
            Re-run checks
          </AppButton>
        }
        bodyClassName="p-4"
      >
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <label className="flex-1">
            <span className="mb-1 block text-xs font-semibold uppercase text-slate-500">
              Course ID (optional)
            </span>
            <input
              type="text"
              value={courseId}
              onChange={(event) => setCourseId(event.target.value)}
              placeholder="e.g. e57bcaf3-f64b-4d18-bf28-a2c5c0b75f7b"
              className="w-full rounded-md border border-slate-300 px-3 py-2 font-mono text-sm text-slate-800 placeholder:text-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            />
          </label>
          <p className="text-xs text-slate-500 sm:max-w-xs sm:pb-2.5">
            Verifies that the SCORM entry file <span className="font-mono">res/index.html</span> for
            this course exists on the learner site before re-running.
          </p>
        </div>
      </Card>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card
          title="iLearn API"
          icon={Server}
          actions={overallBadge(apiResult)}
        >
          <ProbeResultBody result={apiResult} />
        </Card>

        <Card
          title="Learner Site (iLearn.User)"
          icon={GraduationCap}
          actions={overallBadge(userResult)}
        >
          <ProbeResultBody result={userResult} />
        </Card>
      </div>
    </div>
  )
}
