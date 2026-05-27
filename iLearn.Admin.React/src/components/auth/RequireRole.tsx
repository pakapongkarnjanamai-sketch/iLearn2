import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useSession } from '../../lib/sessionContext'

type RequireRoleProps = {
  roles?: readonly string[]
  superAdminOnly?: boolean
  children: ReactNode
}

/**
 * Client-side visibility guard. Server-side authorization remains the source of truth;
 * this only prevents non-privileged users from seeing the page chrome.
 */
export function RequireRole({ roles, superAdminOnly, children }: RequireRoleProps) {
  const { state, isSuperAdmin, hasAnyRole } = useSession()

  if (state === 'loading') {
    return <div className="admin-empty-state" aria-busy="true">Loading session…</div>
  }

  if (superAdminOnly && !isSuperAdmin) {
    return <Navigate to="/access-denied" replace />
  }

  if (roles && roles.length > 0 && !hasAnyRole(roles)) {
    return <Navigate to="/access-denied" replace />
  }

  return <>{children}</>
}
