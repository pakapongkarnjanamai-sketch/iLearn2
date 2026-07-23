import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import { App } from './App.tsx'
import { appConfig } from './config/appConfig'

const redirectIfCanonicalHostNeeded = (): boolean => {
  if (typeof window === 'undefined' || !appConfig.canonicalDomain) {
    return false
  }

  const hostname = window.location.hostname.toLowerCase()
  const isLocalhost =
    hostname === 'localhost' ||
    hostname === '127.0.0.1' ||
    hostname === '::1' ||
    hostname === '[::1]' ||
    hostname.startsWith('127.')

  const isShortName = !hostname.includes('.')

  if (isShortName && !isLocalhost) {
    const domain = appConfig.canonicalDomain.replace(/^\.+/, '')
    const targetUrl = `${window.location.protocol}//${window.location.hostname}.${domain}${window.location.pathname}${window.location.search}${window.location.hash}`
    window.location.replace(targetUrl)
    return true
  }

  return false
}

if (!redirectIfCanonicalHostNeeded()) {
  if (!appConfig.isProd) {
    document.title = `${appConfig.appName} (${appConfig.environmentName})`
    const link = document.querySelector("link[rel~='icon']") as HTMLLinkElement | null
    if (link) {
      const basePath = appConfig.appBasePath === '/' ? '' : appConfig.appBasePath
      link.href = `${basePath}/favicon-qa.svg`
    }
  }

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <BrowserRouter basename={appConfig.appBasePath}>
        <App />
      </BrowserRouter>
    </StrictMode>,
  )
}

