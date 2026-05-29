import { NavLink, useLocation } from 'react-router-dom'
import { appConfig } from '../../config/appConfig'
import { navigationItems, type NavigationItem } from '../../config/navigation'
import { useSession } from '../../lib/sessionContext'

type SidebarProps = {
  isOpen: boolean
  onNavigate: () => void
}

export function Sidebar({ isOpen, onNavigate }: SidebarProps) {
  const { hasAnyRole, isSuperAdmin } = useSession()
  const location = useLocation()

  const isVisible = (item: NavigationItem): boolean => {
    if (item.superAdminOnly && !isSuperAdmin) return false
    if (item.requiredRoles && item.requiredRoles.length > 0) {
      return hasAnyRole(item.requiredRoles)
    }
    return true
  }

  const visibleItems = navigationItems.filter(isVisible)

  const isParentActive = (item: NavigationItem) => {
    if (!item.children?.length) return false
    return item.children.some(
      (child) =>
        location.pathname === child.path ||
        (child.path !== '/' && location.pathname.startsWith(child.path + '/')),
    )
  }

  return (
    <aside className={`sticky top-0 flex h-screen w-[252px] min-w-0 flex-col overflow-hidden border-r border-[#1d3554] bg-slate-900 text-blue-50 transition-[transform,width,visibility] duration-[220ms] ease-[cubic-bezier(0.4,0,0.2,1)] max-[1120px]:fixed max-[1120px]:z-30 max-[1120px]:w-[252px] ${isOpen ? 'max-[1120px]:translate-x-0' : 'max-[1120px]:translate-x-full min-[1121px]:w-0 min-[1121px]:-translate-x-full min-[1121px]:invisible min-[1121px]:border-r-0'}`} aria-label="Admin navigation">
      <div className="flex min-h-[64px] items-center gap-2.5 border-b border-[#1d3554] px-5">
        <div className="grid h-[34px] w-[34px] place-items-center rounded-md bg-indigo-600 text-white text-[15px] font-bold" aria-hidden="true">
          iL
        </div>
        <div className="flex flex-col gap-px">
          <strong className="text-white text-[15px] font-bold">{appConfig.appName}</strong>
          <span className="text-slate-400 text-xs">Enterprise LMS</span>
        </div>
      </div>

      <nav className="flex flex-1 flex-col gap-1 overflow-y-auto py-3.5 px-2.5">
        {visibleItems.map((item) => {
          const Icon = item.icon
          const visibleChildren = item.children?.filter(isVisible) ?? []
          const showChildren = visibleChildren.length > 0 && isParentActive(item)

          return (
            <div key={item.path}>
              <NavLink
                to={item.path}
                end={item.path === '/'}
                className={({ isActive }) =>
                  `flex items-center gap-2.5 min-h-[38px] rounded-md px-2.5 font-medium [&_svg]:w-[17px] [&_svg]:h-[17px] focus-visible:outline-none ${
                    isActive
                      ? 'bg-white text-slate-800 [&_svg]:text-indigo-600'
                      : 'text-blue-100 [&_svg]:text-slate-400 hover:bg-[#18304d] hover:text-white focus-visible:bg-[#18304d] focus-visible:text-white'
                  }`
                }
                onClick={onNavigate}
              >
                <Icon aria-hidden="true" />
                <span>{item.label}</span>
              </NavLink>
              {showChildren && (
                <div className="ml-7 mt-0.5 mb-1 flex flex-col gap-px border-l border-slate-700/40 pl-2">
                  {visibleChildren.map((child) => (
                    <NavLink
                      key={child.path}
                      to={child.path}
                      end={child.path === item.path}
                      className={({ isActive }) =>
                        `block rounded px-2 py-1 text-xs font-semibold transition-colors ${
                          isActive
                            ? 'bg-slate-700/40 text-white'
                            : 'text-slate-300 hover:bg-slate-700/20 hover:text-white'
                        }`
                      }
                      onClick={onNavigate}
                    >
                      {child.label}
                    </NavLink>
                  ))}
                </div>
              )}
            </div>
          )
        })}
      </nav>

      <div className="border-t border-[#1d3554] px-5 py-3 text-slate-400 text-xs">React console running side by side</div>
    </aside>
  )
}
