const trimTrailingSlash = (value: string) => value.replace(/\/+$/, '')

const env = import.meta.env

const getEnv = (...keys: string[]) => {
  for (const key of keys) {
    const value = env[key as keyof ImportMetaEnv]
    if (typeof value === 'string' && value.trim()) {
      return value.trim()
    }
  }

  return undefined
}

const normalizeBasePath = (value: string | undefined) => {
  if (!value || value === '/') {
    return '/'
  }

  const withLeadingSlash = value.startsWith('/') ? value : `/${value}`
  return trimTrailingSlash(withLeadingSlash)
}

const normalizeAbsoluteOrRelativeUrl = (value: string | undefined) => {
  if (!value) {
    return undefined
  }

  if (value.startsWith('/')) {
    return trimTrailingSlash(value)
  }

  return trimTrailingSlash(value)
}

const isRelativeUrl = (value: string) => value.startsWith('/')

const shouldUseLocalhostFallback = () => {
  if (typeof window === 'undefined') {
    return false
  }

  const host = window.location.hostname.toLowerCase()
  return host === 'localhost' || host === '127.0.0.1'
}

const resolveApiBaseUrl = () => {
  const configured = normalizeAbsoluteOrRelativeUrl(
    getEnv('VITE_ILEARN_ADMIN_API_BASE_URL', 'VITE_API_BASE_URL'),
  )

  if (!configured) {
    return 'https://localhost:7128/api'
  }

  if (isRelativeUrl(configured) && shouldUseLocalhostFallback()) {
    return 'https://localhost:7128/api'
  }

  return configured
}

const resolveSignalRBaseUrl = () => {
  const configured = normalizeAbsoluteOrRelativeUrl(
    getEnv('VITE_ILEARN_ADMIN_SIGNALR_BASE_URL', 'VITE_SIGNALR_BASE_URL'),
  )

  if (!configured) {
    return trimTrailingSlash(resolveApiBaseUrl().replace(/\/api$/i, ''))
  }

  if (isRelativeUrl(configured) && shouldUseLocalhostFallback()) {
    return trimTrailingSlash(resolveApiBaseUrl().replace(/\/api$/i, ''))
  }

  return configured
}

export const appConfig = {
  appName: getEnv('VITE_ILEARN_ADMIN_APP_NAME', 'VITE_APP_NAME') || 'iLearn Admin',
  appBasePath: normalizeBasePath(
    getEnv('VITE_ILEARN_ADMIN_APP_BASE_PATH', 'VITE_APP_BASE_PATH'),
  ),
  apiBaseUrl: resolveApiBaseUrl(),
  signalRBaseUrl: resolveSignalRBaseUrl(),
  enableSignalR:
    getEnv('VITE_ILEARN_ADMIN_ENABLE_SIGNALR', 'VITE_ENABLE_SIGNALR') === 'true',
  enableSessionBootstrap:
    getEnv('VITE_ILEARN_ADMIN_ENABLE_SESSION_BOOTSTRAP', 'VITE_ENABLE_SESSION_BOOTSTRAP') !== 'false',
  legacyAdminUrl: (() => {
    const base = normalizeBasePath(
      getEnv('VITE_ILEARN_ADMIN_APP_BASE_PATH', 'VITE_APP_BASE_PATH'),
    )
    const match = base.match(/\/admin-react\/?$/i)
    return match ? base.replace(/\/admin-react\/?$/i, '/admin') : ''
  })(),
} as const