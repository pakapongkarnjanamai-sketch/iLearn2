import { Bell, Menu } from 'lucide-react'
import type { CurrentAdminUser } from '../../lib/auth'
import { Breadcrumbs } from './Breadcrumbs'

type HeaderProps = {
  currentUser: CurrentAdminUser | null
  sessionState: 'loading' | 'ready' | 'fallback' | 'unauthenticated'
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

export function Header({ currentUser, sessionState: _sessionState, onMenuClick }: HeaderProps) {
  const displayName = currentUser?.displayName ?? 'Loading user'
  const divisionName = currentUser?.divisionName ?? 'Admin console'

  return (
    <header className="sticky top-0 z-10 flex min-h-[64px] items-center justify-between gap-4 border-b border-slate-200 bg-white/96 px-6">
      <div className="flex min-w-0 items-center gap-2.5">
        <button type="button" className="inline-grid h-[34px] w-[34px] place-items-center rounded-md border border-slate-200 bg-white text-slate-500 cursor-pointer [&_svg]:w-[17px] [&_svg]:h-[17px] mr-2" onClick={onMenuClick} aria-label="Toggle navigation">
          <Menu aria-hidden="true" />
        </button>
        <div className="h-4 w-px bg-slate-200 mx-3 hidden md:block" />
        <Breadcrumbs />
      </div>

      <div className="flex min-w-0 items-center gap-3">
        <button type="button" className="inline-grid h-[34px] w-[34px] place-items-center rounded-md border border-slate-200 bg-white text-slate-500 cursor-pointer [&_svg]:w-[17px] [&_svg]:h-[17px]" aria-label="Notifications">
          <Bell aria-hidden="true" />
        </button>
        <div className="flex items-center gap-2.5 min-w-0">
          <div className="grid h-8 w-8 place-items-center rounded-full bg-indigo-100 text-indigo-600 font-bold" aria-hidden="true">
            {getInitials(displayName)}
          </div>
          <div className="flex min-w-0 flex-col">
            <strong className="max-w-[180px] overflow-hidden text-ellipsis whitespace-nowrap text-[13px] font-bold">{displayName}</strong>
            <span className="max-w-[180px] overflow-hidden text-ellipsis whitespace-nowrap text-xs text-slate-500">{divisionName}</span>
          </div>
        </div>
      </div>
    </header>
  )
}