const trimTrailingSlash = (value: string) => value.replace(/\/+$/, '')

const normalizeBasePath = (value: string | undefined) => {
  if (!value || value === '/') {
    return '/'
  }

  const withLeadingSlash = value.startsWith('/') ? value : `/${value}`
  return trimTrailingSlash(withLeadingSlash)
}

export const appConfig = {
  appName: import.meta.env.VITE_APP_NAME || 'iLearn Admin',
  appBasePath: normalizeBasePath(import.meta.env.VITE_APP_BASE_PATH),
  apiBaseUrl: trimTrailingSlash(
    import.meta.env.VITE_API_BASE_URL || 'https://ap-ntc2138-qawb/iLearnNew/Service/api',
  ),
  signalRBaseUrl: trimTrailingSlash(
    import.meta.env.VITE_SIGNALR_BASE_URL || 'https://ap-ntc2138-qawb/iLearnNew/Service',
  ),
  enableSignalR: import.meta.env.VITE_ENABLE_SIGNALR === 'true',
  enableSessionBootstrap: import.meta.env.VITE_ENABLE_SESSION_BOOTSTRAP === 'true',
  devExtremeLicenseKey: import.meta.env.VITE_DEVEXTREME_LICENSE_KEY || '',
} as const