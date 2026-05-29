import { useState, useEffect } from 'react'
import { 
  Database, 
  FolderSync, 
  ShieldCheck, 
  Terminal, 
  Trash2, 
  RefreshCw,
  Server
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

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
      toast.error('Could not load system configuration from API')
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
      toast.success('All Admin and API cached data cleared successfully.')
    } catch (err) {
      console.error('Failed to clear cache', err)
      toast.error('Failed to clear API system cache.')
    } finally {
      setClearingCache(false)
    }
  }

  if (loading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <RefreshCw className="h-8 w-8 animate-spin text-indigo-500" />
          <span className="text-sm text-gray-500 font-medium">Loading system configuration...</span>
        </div>
      </div>
    )
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
              <h2 className="text-base font-bold text-slate-800">Database Context</h2>
            </div>
            {config ? (
              <dl className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">Data Source / Server</dt>
                  <dd className="mt-1 font-mono text-sm text-slate-800 break-all">{config.database.dataSource || '(not set)'}</dd>
                </div>
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">Catalog / Database</dt>
                  <dd className="mt-1 font-semibold text-sm text-slate-800">{config.database.databaseName || '(not set)'}</dd>
                </div>
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">User ID</dt>
                  <dd className="mt-1 font-mono text-sm text-slate-800">{config.database.userId || '(not set)'}</dd>
                </div>
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">Trust Server Certificate</dt>
                  <dd className="mt-1 text-sm">
                    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold ${
                      config.database.trustCert === 'true' 
                        ? 'bg-amber-100 text-amber-800' 
                        : 'bg-emerald-100 text-emerald-800'
                    }`}>
                      {config.database.trustCert === 'true' ? 'Enabled' : 'Disabled (Secure)'}
                    </span>
                  </dd>
                </div>
              </dl>
            ) : (
              <p className="text-sm text-slate-500">No database configuration details available.</p>
            )}
          </section>

          {/* File Storage Details */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <FolderSync className="h-5 w-5 text-indigo-500" />
              <h2 className="text-base font-bold text-slate-800">Content Storage & Directories</h2>
            </div>
            {config ? (
              <div className="space-y-4">
                <div className="bg-slate-50 p-3 rounded">
                  <h3 className="text-xs font-semibold text-slate-500 uppercase mb-2">Web Content Target (URLs)</h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
                    <div>
                      <span className="block text-xs text-slate-400">Host URL:</span>
                      <span className="font-mono text-slate-700 break-all">{config.fileSettings.hostUrl || '—'}</span>
                    </div>
                    <div>
                      <span className="block text-xs text-slate-400">Combined Content URL:</span>
                      <span className="font-mono text-slate-700 break-all">{config.fileSettings.fileUrl || '—'}</span>
                    </div>
                  </div>
                </div>

                <div className="bg-slate-50 p-3 rounded">
                  <h3 className="text-xs font-semibold text-slate-500 uppercase mb-2">Network Server Target (UNC Share Paths)</h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
                    <div>
                      <span className="block text-xs text-slate-400">Host UNC Share:</span>
                      <span className="font-mono text-slate-700 break-all">{config.fileSettings.hostUnc || '—'}</span>
                    </div>
                    <div>
                      <span className="block text-xs text-slate-400">Combined Content UNC:</span>
                      <span className="font-mono text-slate-700 break-all">{config.fileSettings.fileUnc || '—'}</span>
                    </div>
                  </div>
                </div>

                <div className="bg-slate-50 p-3 rounded">
                  <span className="block text-xs font-semibold text-slate-500 uppercase">Subfolder Name</span>
                  <span className="mt-1 block font-mono text-sm text-slate-800">{config.fileSettings.courseFolder || '—'}</span>
                </div>
              </div>
            ) : (
              <p className="text-sm text-slate-500">No content storage configuration details available.</p>
            )}
          </section>

          {/* HR / Employee Sync Details */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <ShieldCheck className="h-5 w-5 text-indigo-500" />
              <h2 className="text-base font-bold text-slate-800">HR Services & Authentication</h2>
            </div>
            {config ? (
              <dl className="grid grid-cols-1 gap-4">
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">Employee Lookup URL (Active Directory Sync)</dt>
                  <dd className="mt-1 font-mono text-sm text-slate-800 break-all">{config.employeeService.baseLearnerLookupUrl || '—'}</dd>
                </div>
                <div className="bg-slate-50 p-3 rounded">
                  <dt className="text-xs font-semibold text-slate-500 uppercase">Employee Detail URL</dt>
                  <dd className="mt-1 font-mono text-sm text-slate-800 break-all">{config.employeeService.baseLearnerUrl || '—'}</dd>
                </div>
              </dl>
            ) : (
              <p className="text-sm text-slate-500">No HR integration settings found.</p>
            )}
          </section>

        </div>

        {/* Sidebar Actions Column */}
        <div className="space-y-6">
          
          {/* Operations & Cache Panel */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <Trash2 className="h-5 w-5 text-red-500" />
              <h2 className="text-base font-bold text-slate-800">Admin Maintenance</h2>
            </div>
            <div className="space-y-4">
              <p className="text-sm text-slate-500 leading-relaxed">Clears cached dropdowns, trees, and reports.</p>
              
              <div className="pt-2">
                <button
                  type="button"
                  disabled={clearingCache}
                  onClick={handleClearCache}
                  className="w-full inline-flex justify-center items-center gap-2 px-4 py-2.5 bg-red-600 hover:bg-red-700 text-white rounded text-sm font-semibold transition disabled:opacity-55 disabled:cursor-not-allowed shadow"
                >
                  {clearingCache ? (
                    <>
                      <RefreshCw className="h-4 w-4 animate-spin" />
                      <span>Clearing Cache...</span>
                    </>
                  ) : (
                    <>
                      <Trash2 className="h-4 w-4" />
                      <span>Clear System Cache</span>
                    </>
                  )}
                </button>
              </div>
            </div>
          </section>

          {/* Web Service Environment Details */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <Server className="h-5 w-5 text-slate-700" />
              <h2 className="text-base font-bold text-slate-800">API Runtime Stats</h2>
            </div>
            {config ? (
              <dl className="space-y-3 text-sm">
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">Environment</dt>
                  <dd className="font-semibold text-slate-800">{config.environment || 'Production'}</dd>
                </div>
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">Machine Name</dt>
                  <dd className="font-mono text-slate-700">{config.runtime.machineName}</dd>
                </div>
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">Application Version</dt>
                  <dd className="font-semibold text-slate-800">{config.runtime.appVersion}</dd>
                </div>
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">Framework</dt>
                  <dd className="text-slate-700">{config.runtime.dotNetVersion}</dd>
                </div>
                <div className="flex justify-between py-1.5 border-b border-slate-100">
                  <dt className="text-slate-500 font-medium">Operating System</dt>
                  <dd className="text-slate-700 max-w-37.5 text-right truncate" title={config.runtime.osDescription}>{config.runtime.osDescription}</dd>
                </div>
                <div className="flex justify-between py-1.5">
                  <dt className="text-slate-500 font-medium">Server Time</dt>
                  <dd className="font-mono text-slate-700">{config.runtime.serverTime}</dd>
                </div>
              </dl>
            ) : (
              <p className="text-sm text-slate-500">No runtime metadata loaded.</p>
            )}
          </section>

          {/* Logging Configuration */}
          <section className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
              <Terminal className="h-5 w-5 text-slate-700" />
              <h2 className="text-base font-bold text-slate-800">Logging Thresholds</h2>
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
              <p className="text-sm text-slate-500">No log configuration loaded.</p>
            )}
          </section>

        </div>
      </div>
    </>
  )
}
