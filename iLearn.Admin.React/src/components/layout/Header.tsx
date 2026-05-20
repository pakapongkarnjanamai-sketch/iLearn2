import { Bell, Menu, RefreshCw, Search } from 'lucide-react'
import { AppButton } from '../ui/AppButton'
import { StatusText } from '../ui/StatusText'
import type { CurrentAdminUser } from '../../lib/auth'

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
      <div className="admin-topbar-left">
        <button type="button" className="admin-icon-button admin-mobile-menu" onClick={onMenuClick} aria-label="Toggle navigation">
          <Menu aria-hidden="true" />
        </button>
        <label className="admin-search-box">
          <Search aria-hidden="true" />
          <input type="search" placeholder="Search courses, learners, assignments" />
        </label>
      </div>

      <div className="admin-topbar-right">
        <StatusText tone={sessionState === 'ready' ? 'success' : 'warning'}>
          {sessionState === 'ready' ? 'Windows Auth' : 'Session pending'}
        </StatusText>
        <AppButton variant="ghost" icon={RefreshCw} onClick={() => window.location.reload()}>
          Refresh
        </AppButton>
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