import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import 'devextreme/dist/css/dx.light.css'
import './devextreme-license'
import './index.css'
import App from './App.tsx'
import { appConfig } from './config/appConfig'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter basename={appConfig.appBasePath}>
      <App />
    </BrowserRouter>
  </StrictMode>,
)
