import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import { App } from './App.tsx'
import { appConfig } from './config/appConfig'

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
