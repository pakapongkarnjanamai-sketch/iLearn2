import { useEffect, useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Header } from './Header'
import { Sidebar } from './Sidebar'
import { SessionProvider, useSession } from '../../lib/sessionContext'
import { BreadcrumbProvider } from '../../lib/breadcrumbContext'

function AppLayoutInner() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(() => window.innerWidth > 1120)
  const { user, state } = useSession()

  useEffect(() => {
    const handleResize = () => {
      setIsSidebarOpen(window.innerWidth > 1120)
    }
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  const headerState: 'loading' | 'ready' | 'fallback' | 'unauthenticated' =
    state === 'ready' ? 'ready' : state === 'unauthenticated' ? 'unauthenticated' : state === 'fallback' ? 'fallback' : 'loading'

  return (
    <div className="flex min-h-screen bg-slate-50/60">
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
          className="fixed inset-0 z-20 block min-[1121px]:hidden bg-slate-900/40 backdrop-blur-xs transition-opacity duration-200" 
          onClick={() => setIsSidebarOpen(false)} 
        />
      )}
      <main className="flex flex-1 min-w-0 flex-col min-h-screen">
        <Header
          currentUser={user}
          sessionState={headerState}
          onMenuClick={() => setIsSidebarOpen((value) => !value)}
        />
        <div className="flex h-[calc(100vh-56px)] min-h-0 flex-col gap-4 overflow-auto p-4 px-5 pb-5 bg-slate-50/60 has-[.wizard-surface]:overflow-hidden has-[.wizard-surface]:bg-slate-50/60 print:h-auto print:overflow-visible print:bg-white">
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