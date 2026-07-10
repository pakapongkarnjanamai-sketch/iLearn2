import { useState, useEffect } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import { ChevronDown } from 'lucide-react'
import { appConfig } from '../../config/appConfig'
import { navigationSections, type NavigationItem, type NavigationSection } from '../../config/navigation'
import { useSession } from '../../lib/sessionContext'
import { Badge } from '../ui/Badge'


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

  const visibleSections = navigationSections
    .filter((section: NavigationSection) => !(section.superAdminOnly && !isSuperAdmin))
    .map(section => ({ ...section, items: section.items.filter(isVisible) }))
    .filter(section => section.items.length > 0)

  const isParentActive = (item: NavigationItem) => {
    if (!item.children?.length) return false
    return item.children.some(
      (child) =>
        location.pathname === child.path ||
        (child.path !== '/' && location.pathname.startsWith(child.path + '/')),
    )
  }

  const [expanded, setExpanded] = useState<Record<string, boolean>>(() => {
    const initial: Record<string, boolean> = {}
    navigationSections.forEach((section) => {
      section.items.forEach((item) => {
        if (item.children?.length) {
          const hasActiveChild = item.children.some(
            (child) =>
              location.pathname === child.path ||
              (child.path !== '/' && location.pathname.startsWith(child.path + '/')),
          )
          if (hasActiveChild) {
            initial[item.path || item.label] = true
          }
        }
      })
    })
    return initial
  })

  useEffect(() => {
    setExpanded((prev) => {
      const next = { ...prev }
      let changed = false

      navigationSections.forEach((section) => {
        section.items.forEach((item) => {
          if (item.children?.length) {
            const hasActiveChild = item.children.some(
              (child) =>
                location.pathname === child.path ||
                (child.path !== '/' && location.pathname.startsWith(child.path + '/')),
            )
            const key = item.path || item.label
            if (hasActiveChild && !next[key]) {
              next[key] = true
              changed = true
            }
          }
        })
      })

      return changed ? next : prev
    })
  }, [location.pathname])

  return (
    <aside
      className={`
        flex h-screen flex-col overflow-hidden border-r border-[#1d3554] bg-slate-900 text-blue-50
        transition-all duration-200 ease-in-out shrink-0 print:hidden
        
        min-[1121px]:sticky min-[1121px]:top-0
        ${isOpen ? 'min-[1121px]:w-[210px] min-[1121px]:opacity-100' : 'min-[1121px]:w-0 min-[1121px]:opacity-0 min-[1121px]:invisible min-[1121px]:border-r-0'}
        
        max-[1120px]:fixed max-[1120px]:top-0 max-[1120px]:bottom-0 max-[1120px]:left-0 max-[1120px]:z-30 max-[1120px]:w-[210px]
        ${isOpen ? 'max-[1120px]:translate-x-0' : 'max-[1120px]:-translate-x-full'}
      `.replace(/\s+/g, ' ').trim()}
      aria-label="Admin navigation"
    >
      <div className="flex min-h-[56px] items-center gap-2.5 border-b border-[#1d3554] px-5">
        <div
          className={`grid h-[34px] w-[34px] place-items-center rounded-md text-[15px] font-bold transition-colors ${
            appConfig.isProd
              ? 'bg-indigo-600 text-white'
              : 'bg-amber-500 text-slate-900'
          }`}
          aria-hidden="true"
        >
          iL
        </div>
        <div className="flex flex-col gap-px">
          <strong className="text-white text-[15px] font-bold">{appConfig.appName}</strong>
          <div className="flex items-center gap-1.5">
            <span className="text-slate-400 text-xs">Enterprise LMS</span>
            {!appConfig.isProd && (
              <Badge tone="warning" size="xxs" variant="soft" className="font-extrabold px-1 py-0.5 leading-none">
                {appConfig.environmentName}
              </Badge>
            )}
          </div>
        </div>
      </div>

      <nav className="flex flex-1 flex-col overflow-y-auto py-3.5 px-2.5">
        {visibleSections.map((section, sectionIndex) => (
          <div key={section.label || sectionIndex} className={sectionIndex > 0 ? 'mt-4' : ''}>
            {section.label && (
              <div
                className={`px-2.5 pb-1.5 text-[10px] font-extrabold uppercase tracking-wider select-none ${
                  section.superAdminOnly ? 'text-amber-400/90' : 'text-slate-500'
                }`}
              >
                {section.label}
              </div>
            )}

            <div className="flex flex-col gap-1">
              {section.items.map((item) => {
                const Icon = item.icon
                const visibleChildren = item.children?.filter(isVisible) ?? []
                const hasChildren = visibleChildren.length > 0

                if (hasChildren) {
                  const isAct = isParentActive(item)
                  const isExpanded = !!expanded[item.path || item.label]

                  return (
                    <div key={item.path}>
                      <button
                        type="button"
                        aria-expanded={isExpanded}
                        aria-controls={`submenu-${item.label.replace(/\s+/g, '-').toLowerCase()}`}
                        className={`flex w-full items-center gap-2.5 min-h-[34px] rounded-md px-2.5 text-[13.5px] font-medium [&_svg]:w-[16px] [&_svg]:h-[16px] text-left focus-visible:outline-none transition-colors duration-150 cursor-pointer ${
                          isAct
                            ? 'bg-slate-800/60 text-white font-semibold [&_svg]:text-slate-300'
                            : 'text-blue-100 [&_svg]:text-slate-400 hover:bg-[#18304d] hover:text-white focus-visible:bg-[#18304d] focus-visible:text-white'
                        }`}
                        onClick={() => {
                          setExpanded((prev) => ({
                            ...prev,
                            [item.path || item.label]: !prev[item.path || item.label],
                          }))
                        }}
                      >
                        <Icon aria-hidden="true" />
                        <span className="flex-1">{item.label}</span>
                        <ChevronDown
                          className={`h-4 w-4 shrink-0 text-slate-400 transition-transform duration-200 ${
                            isExpanded ? 'rotate-180 text-white' : ''
                          }`}
                          aria-hidden="true"
                        />
                      </button>
                      <div
                        id={`submenu-${item.label.replace(/\s+/g, '-').toLowerCase()}`}
                        className={`grid transition-[grid-template-rows] duration-200 ease-in-out ${
                          isExpanded ? 'grid-rows-[1fr]' : 'grid-rows-[0fr]'
                        }`}
                      >
                        <div className="overflow-hidden">
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
                        </div>
                      </div>
                    </div>
                  )
                }

                return (
                  <div key={item.path}>
                    <NavLink
                      to={item.path}
                      end={item.path === '/'}
                      className={({ isActive }) =>
                        `flex items-center gap-2.5 min-h-[34px] rounded-md px-2.5 text-[13.5px] font-medium [&_svg]:w-[16px] [&_svg]:h-[16px] focus-visible:outline-none ${
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
                  </div>
                )
              })}
            </div>
          </div>
        ))}
      </nav>

      <div className="border-t border-[#1d3554] px-5 py-3 text-slate-400 text-xs">React console running side by side</div>
    </aside>
  )
}
