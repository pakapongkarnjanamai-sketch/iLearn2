import { Bell, Menu } from 'lucide-react'
import { StatusText } from '../ui/StatusText'
import type { CurrentAdminUser } from '../../lib/auth'
import { Breadcrumbs } from './Breadcrumbs'

type HeaderProps = {
  currentUser: CurrentAdminUser | null
  sessionState: 'loading' | 'ready' | 'fallback'
  onMenuClick: () => void
}

const getInitials = (name: string | undefined) => {
  if (!name) {
    return 'AD'
  }

  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('')
}

export function Header({ currentUser, sessionState, onMenuClick }: HeaderProps) {
  const displayName = currentUser?.displayName ?? 'Loading user'
  const divisionName = currentUser?.divisionName ?? 'Admin console'

  return (
    <header className="admin-topbar">
      <div className="admin-topbar-left flex items-center">
        <button type="button" className="admin-icon-button admin-mobile-menu mr-2" onClick={onMenuClick} aria-label="Toggle navigation">
          <Menu aria-hidden="true" />
        </button>
        <div className="h-4 w-px bg-slate-200 mx-3 hidden md:block" />
        <Breadcrumbs />
      </div>

      <div className="admin-topbar-right flex items-center gap-3">
        <div className="flex items-center gap-1.5">
          {sessionState === 'ready' && (
            <span className="h-2 w-2 rounded-full bg-emerald-500 neon-glow-dot shrink-0" title="Real-time session active" />
          )}
          <StatusText tone={sessionState === 'ready' ? 'success' : 'warning'}>
            {sessionState === 'ready' ? 'Windows Auth' : 'Session pending'}
          </StatusText>
        </div>
        <button type="button" className="admin-icon-button" aria-label="Notifications">
          <Bell aria-hidden="true" />
        </button>
        <div className="admin-user-block">
          <div className="admin-user-avatar" aria-hidden="true">
            {getInitials(displayName)}
          </div>
          <div className="admin-user-copy">
            <strong>{displayName}</strong>
            <span>{divisionName}</span>
          </div>
        </div>
      </div>
    </header>
  )
}