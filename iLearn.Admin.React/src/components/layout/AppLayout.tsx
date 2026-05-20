import { useEffect, useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Header } from './Header'
import { Sidebar } from './Sidebar'
import { loadCurrentAdminUser } from '../../lib/auth'
import type { CurrentAdminUser } from '../../lib/auth'

type SessionState = 'loading' | 'ready' | 'fallback'

export function AppLayout() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const [sessionState, setSessionState] = useState<SessionState>('loading')
  const [currentUser, setCurrentUser] = useState<CurrentAdminUser | null>(null)

  useEffect(() => {
    let isMounted = true

    loadCurrentAdminUser().then((user) => {
      if (!isMounted) {
        return
      }

      setCurrentUser(user)
      setSessionState(user.isFallback ? 'fallback' : 'ready')
    })

    return () => {
      isMounted = false
    }
  }, [])

  return (
    <div className="admin-app-shell">
      <Sidebar isOpen={isSidebarOpen} onNavigate={() => setIsSidebarOpen(false)} />
      <main className="admin-main-shell">
        <Header
          currentUser={currentUser}
          sessionState={sessionState}
          onMenuClick={() => setIsSidebarOpen((value) => !value)}
        />
        <div className="admin-page">
          <Outlet />
        </div>
      </main>
    </div>
  )
}