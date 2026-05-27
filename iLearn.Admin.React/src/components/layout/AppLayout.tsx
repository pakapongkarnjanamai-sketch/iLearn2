import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Header } from './Header'
import { Sidebar } from './Sidebar'
import { SessionProvider, useSession } from '../../lib/sessionContext'
import { BreadcrumbProvider } from '../../lib/breadcrumbContext'

function AppLayoutInner() {
  // Desktop has sidebar open by default (width > 1120px)
  const [isSidebarOpen, setIsSidebarOpen] = useState(() => window.innerWidth > 1120)
  const { user, state } = useSession()

  const headerState: 'loading' | 'ready' | 'fallback' =
    state === 'ready' ? 'ready' : state === 'fallback' ? 'fallback' : 'loading'

  return (
    <div className={`admin-app-shell ${!isSidebarOpen ? 'sidebar-closed' : ''}`}>
      <Sidebar 
        isOpen={isSidebarOpen} 
        onNavigate={() => {
          if (window.innerWidth <= 1120) {
            setIsSidebarOpen(false)
          }
        }} 
      />
      {isSidebarOpen && (
        <div 
          className="admin-sidebar-backdrop" 
          onClick={() => setIsSidebarOpen(false)} 
        />
      )}
      <main className="admin-main-shell">
        <Header
          currentUser={user}
          sessionState={headerState}
          onMenuClick={() => setIsSidebarOpen((value) => !value)}
        />
        <div className="admin-page">
          <Outlet />
        </div>
      </main>
    </div>
  )
}

export function AppLayout() {
  return (
    <SessionProvider>
      <BreadcrumbProvider>
        <AppLayoutInner />
      </BreadcrumbProvider>
    </SessionProvider>
  )
}