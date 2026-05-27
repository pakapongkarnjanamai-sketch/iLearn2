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
    <aside className={`admin-sidebar${isOpen ? ' is-open' : ''}`} aria-label="Admin navigation">
      <div className="admin-sidebar-brand">
        <div className="admin-sidebar-mark" aria-hidden="true">
          iL
        </div>
        <div className="admin-sidebar-title">
          <strong>{appConfig.appName}</strong>
          <span>Enterprise LMS</span>
        </div>
      </div>

      <nav className="admin-sidebar-nav">
        {visibleItems.map((item) => {
          const Icon = item.icon
          const visibleChildren = item.children?.filter(isVisible) ?? []
          const showChildren = visibleChildren.length > 0 && isParentActive(item)

          return (
            <div key={item.path}>
              <NavLink
                to={item.path}
                end={item.path === '/'}
                className="admin-nav-link"
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

      <div className="admin-sidebar-footer">React console running side by side</div>
    </aside>
  )
}
