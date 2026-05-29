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

  const headerState: 'loading' | 'ready' | 'fallback' | 'unauthenticated' =
    state === 'ready' ? 'ready' : state === 'unauthenticated' ? 'unauthenticated' : state === 'fallback' ? 'fallback' : 'loading'

  return (
    <div className={`grid min-h-screen transition-[grid-template-columns] duration-[220ms] ease-[cubic-bezier(0.4,0,0.2,1)] ${isSidebarOpen ? 'grid-cols-[252px_minmax(0,1fr)]' : 'grid-cols-[0px_minmax(0,1fr)]'}`}>
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
          className="fixed inset-0 z-25 hidden max-[1120px]:block bg-slate-900/40 backdrop-blur-xs animate-[fade-in-backdrop_0.2s_ease-out_forwards]" 
          onClick={() => setIsSidebarOpen(false)} 
        />
      )}
      <main className="flex min-w-0 min-h-screen flex-col">
        <Header
          currentUser={user}
          sessionState={headerState}
          onMenuClick={() => setIsSidebarOpen((value) => !value)}
        />
        <div className="flex h-[calc(100vh-64px)] min-h-0 flex-col gap-4 overflow-auto p-5 px-6 pb-6 has-[.wizard-surface]:overflow-hidden has-[.wizard-surface]:p-0 has-[.wizard-surface]:gap-0">
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