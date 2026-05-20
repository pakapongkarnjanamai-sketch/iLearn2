import { NavLink } from 'react-router-dom'
import { appConfig } from '../../config/appConfig'
import { navigationItems } from '../../config/navigation'

type SidebarProps = {
  isOpen: boolean
  onNavigate: () => void
}

export function Sidebar({ isOpen, onNavigate }: SidebarProps) {
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
        {navigationItems.map((item) => {
          const Icon = item.icon

          return (
            <NavLink key={item.path} to={item.path} end={item.path === '/'} className="admin-nav-link" onClick={onNavigate}>
              <Icon aria-hidden="true" />
              <span>{item.label}</span>
            </NavLink>
          )
        })}
      </nav>

      <div className="admin-sidebar-footer">React console running side by side</div>
    </aside>
  )
}