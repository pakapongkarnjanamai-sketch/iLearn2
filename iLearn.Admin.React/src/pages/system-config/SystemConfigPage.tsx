import { useState, useEffect } from 'react'
import { 
  Database, 
  FolderSync, 
  ShieldCheck, 
  Terminal, 
  Trash2,
  Server
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { AppButton } from '../../components/ui/AppButton'
import { Badge } from '../../components/ui/Badge'
import { ADMIN_LABELS, HEALTH_LABELS, t } from '../../lib/labels'
import { LoadingState } from '../../components/ui/LoadingState'

type DbConfigInfo = {
  dataSource: string
  databaseName: string
  userId: string
  trustCert: string
}

type FileSettingsInfo = {
  hostUrl: string
  hostUnc: string
  courseFolder: string
  fileUrl: string
  fileUnc: string
}

type ApiRuntimeInfo = {
  dotNetVersion: string
  machineName: string
  osDescription: string
  osArchitecture: string
  serverTime: string
  appVersion: string
}

type EmployeeServiceInfo = {
  baseLearnerLookupUrl: string
  baseLearnerUrl: string
}

type SystemConfigResponse = {
  environment: string
  database: DbConfigInfo
  fileSettings: FileSettingsInfo
  employeeService: EmployeeServiceInfo
  allowedHosts: string
  logging: Record<string, string>
  runtime: ApiRuntimeInfo
}

export function SystemConfigPage() {
  const [loading, setLoading] = useState(true)
  const [clearingCache, setClearingCache] = useState(false)
  const [config, setConfig] = useState<SystemConfigResponse | null>(null)

  const loadConfig = async () => {
    setLoading(true)
    try {
      const data = await fetchWithAccessControl<SystemConfigResponse>('admin/SystemConfig')
      setConfig(data)
    } catch (err) {
      console.error('Failed to load system configuration', err)
      toast.error(t(ADMIN_LABELS.configLoadFailed))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadConfig()
  }, [])

  const handleClearCache = async () => {
    setClearingCache(true)
    try {
      await fetchWithAccessControl('admin/Cache/clear-all', { method: 'POST' })
      toast.success(t(ADMIN_LABELS.cacheCleared))
    } catch (err) {
      console.error('Failed to clear cache', err)
      toast.error(t(ADMIN_LABELS.cacheClearFailed))
    } finally {
      setClearingCache(false)
    }
  }

  if (loading) {
    return <LoadingState label={t(ADMIN_LABELS.loadingSystemConfig)} />
  }

  return (
    <>
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Main Columns */}
        <div className="lg:col-span-2 space-y-6">
          
          {/* Database Details */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <Database className="h-5 w-5 text-indigo-500" />
              <h2 className="text-base font-bold text-slate-800">{t(ADMIN_LABELS.databaseContext)}</h2>
            </div>
            {config ? (
              <dl className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">{t(ADMIN_LABELS.dataSourceServer)}</dt><dd className="mt-1 font-mono text-sm text-slate-800 break-all">{config.database.dataSource || t(ADMIN_LABELS.notSet)}</dd>
                </div>
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">{t(ADMIN_LABELS.catalogDatabase)}</dt><dd className="mt-1 font-semibold text-sm text-slate-800">{config.database.databaseName || t(ADMIN_LABELS.notSet)}</dd>
                </div>
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">{t(ADMIN_LABELS.userId)}</dt><dd className="mt-1 font-mono text-sm text-slate-800">{config.database.userId || t(ADMIN_LABELS.notSet)}</dd>
                </div>
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">{t(ADMIN_LABELS.trustServerCertificate)}</dt>
                  <dd className="mt-1 text-sm">
                    <Badge tone={config.database.trustCert === 'true' ? 'warning' : 'success'}>
                      {t(config.database.trustCert === 'true' ? HEALTH_LABELS.enabled : HEALTH_LABELS.disabledSecure)}
                    </Badge>
                  </dd>
                </div>
              </dl>
            ) : (
              <p className="text-sm text-slate-500">{t(ADMIN_LABELS.noDatabaseConfig)}</p>
            )}
          </section>

          {/* File Storage Details */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <FolderSync className="h-5 w-5 text-indigo-500" />
              <h2 className="text-base font-bold text-slate-800">{t(ADMIN_LABELS.contentStorage)}</h2>
            </div>
            {config ? (
              <div className="space-y-4">
                <div className="bg-slate-50 p-3 rounded">
                  <h3 className="text-xs font-semibold text-slate-500 uppercase mb-2">{t(ADMIN_LABELS.webContentTarget)}</h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
                    <div>
                      <span className="block text-xs text-slate-400">{t(ADMIN_LABELS.hostUrl)}:</span>
                      <span className="font-mono text-slate-700 break-all">{config.fileSettings.hostUrl || '—'}</span>
                    </div>
                    <div>
                      <span className="block text-xs text-slate-400">{t(ADMIN_LABELS.combinedContentUrl)}:</span>
                      <span className="font-mono text-slate-700 break-all">{config.fileSettings.fileUrl || '—'}</span>
                    </div>
                  </div>
                </div>

                <div className="bg-slate-50 p-3 rounded">
                  <h3 className="text-xs font-semibold text-slate-500 uppercase mb-2">{t(ADMIN_LABELS.networkServerTarget)}</h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
                    <div>
                      <span className="block text-xs text-slate-400">{t(ADMIN_LABELS.hostUncShare)}:</span>
                      <span className="font-mono text-slate-700 break-all">{config.fileSettings.hostUnc || '—'}</span>
                    </div>
                    <div>
                      <span className="block text-xs text-slate-400">{t(ADMIN_LABELS.combinedContentUnc)}:</span>
                      <span className="font-mono text-slate-700 break-all">{config.fileSettings.fileUnc || '—'}</span>
                    </div>
                  </div>
                </div>

                <div className="bg-slate-50 p-3 rounded">
                  <span className="block text-xs font-semibold text-slate-500 uppercase">{t(ADMIN_LABELS.subfolderName)}</span>
                  <span className="mt-1 block font-mono text-sm text-slate-800">{config.fileSettings.courseFolder || '—'}</span>
                </div>
              </div>
            ) : (
              <p className="text-sm text-slate-500">{t(ADMIN_LABELS.noContentStorageConfig)}</p>
            )}
          </section>

          {/* HR / Employee Sync Details */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <ShieldCheck className="h-5 w-5 text-indigo-500" />
              <h2 className="text-base font-bold text-slate-800">{t(ADMIN_LABELS.hrServices)}</h2>
            </div>
            {config ? (
              <dl className="grid grid-cols-1 gap-4">
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">{t(ADMIN_LABELS.employeeLookupUrl)}</dt>
                  <dd className="mt-1 font-mono text-sm text-slate-800 break-all">{config.employeeService.baseLearnerLookupUrl || '—'}</dd>
                </div>
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">{t(ADMIN_LABELS.employeeDetailUrl)}</dt>
                  <dd className="mt-1 font-mono text-sm text-slate-800 break-all">{config.employeeService.baseLearnerUrl || '—'}</dd>
                </div>
              </dl>
            ) : (
              <p className="text-sm text-slate-500">{t(ADMIN_LABELS.noHrSettings)}</p>
            )}
          </section>

        </div>

        {/* Sidebar Actions Column */}
        <div className="space-y-6">
          
          {/* Operations & Cache Panel */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <Trash2 className="h-5 w-5 text-red-500" />
              <h2 className="text-base font-bold text-slate-800">{t(ADMIN_LABELS.adminMaintenance)}</h2>
            </div>
            <div className="space-y-4">
              <p className="text-sm text-slate-500 leading-relaxed">{t(ADMIN_LABELS.clearCacheDescription)}</p>
              
              <div className="pt-2">
                <AppButton
                  type="button"
                  onClick={handleClearCache}
                  variant="danger"
                  icon={Trash2}
                  loading={clearingCache}
                  className="w-full px-4 py-2.5 text-sm font-semibold shadow"
                >
                  {t(ADMIN_LABELS.clearSystemCache)}
                </AppButton>
              </div>
            </div>
          </section>

          {/* Web Service Environment Details */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <Server className="h-5 w-5 text-slate-700" />
              <h2 className="text-base font-bold text-slate-800">{t(ADMIN_LABELS.apiRuntimeStats)}</h2>
            </div>
            {config ? (
              <dl className="space-y-3 text-sm">
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">{t(ADMIN_LABELS.environment)}</dt><dd className="font-semibold text-slate-800">{config.environment || t(ADMIN_LABELS.production)}</dd>
                </div>
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">{t(ADMIN_LABELS.machineName)}</dt>
                  <dd className="font-mono text-slate-700">{config.runtime.machineName}</dd>
                </div>
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">{t(ADMIN_LABELS.applicationVersion)}</dt>
                  <dd className="font-semibold text-slate-800">{config.runtime.appVersion}</dd>
                </div>
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">{t(ADMIN_LABELS.framework)}</dt>
                  <dd className="text-slate-700">{config.runtime.dotNetVersion}</dd>
                </div>
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">{t(ADMIN_LABELS.operatingSystem)}</dt>
                  <dd className="text-slate-700 max-w-37.5 text-right truncate" title={config.runtime.osDescription}>{config.runtime.osDescription}</dd>
                </div>
                <div className="flex justify-between py-1.5">
                  <dt className="text-slate-500 font-medium">{t(ADMIN_LABELS.serverTime)}</dt>
                  <dd className="font-mono text-slate-700">{config.runtime.serverTime}</dd>
                </div>
              </dl>
            ) : (
              <p className="text-sm text-slate-500">{t(ADMIN_LABELS.noRuntimeMetadata)}</p>
            )}
          </section>

          {/* Logging Configuration */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <Terminal className="h-5 w-5 text-slate-700" />
              <h2 className="text-base font-bold text-slate-800">{t(ADMIN_LABELS.loggingThresholds)}</h2>
            </div>
            {config ? (
              <dl className="space-y-2 text-xs font-mono">
                {Object.entries(config.logging).map(([key, val]) => (
                  <div key={key} className="flex justify-between py-1 border-b border-slate-50">
                    <dt className="text-slate-500 font-medium break-all mr-2">{key}</dt>
                    <dd className="font-semibold text-slate-800 shrink-0">{val}</dd>
                  </div>
                ))}
              </dl>
            ) : (
              <p className="text-sm text-slate-500">{t(ADMIN_LABELS.noLogConfig)}</p>
            )}
          </section>

        </div>
      </div>
    </>
  )
}
