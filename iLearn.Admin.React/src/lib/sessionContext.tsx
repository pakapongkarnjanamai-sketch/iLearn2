import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { loadCurrentAdminUser, type CurrentAdminUser } from './auth'

export type SessionState = 'loading' | 'ready' | 'fallback' | 'unauthenticated'

export type SessionValue = {
  user: CurrentAdminUser | null
  state: SessionState
  roles: string[]
  isSuperAdmin: boolean
  hasAnyRole: (required?: readonly string[]) => boolean
  reload: () => Promise<void>
}

const SessionContext = createContext<SessionValue | undefined>(undefined)

const computeHasAnyRole = (user: CurrentAdminUser | null) => {
  return (required?: readonly string[]) => {
    if (!required || required.length === 0) return true
    if (!user) return false
    if (user.isSuperAdmin) return true
    const normalized = new Set(user.roles.map((r) => r.toLowerCase()))
    return required.some((r) => normalized.has(r.toLowerCase()))
  }
}

export function SessionProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentAdminUser | null>(null)
  const [state, setState] = useState<SessionState>('loading')

  const load = async () => {
    setState('loading')
    const next = await loadCurrentAdminUser()
    setUser(next)
    if (!next.isAuthenticated) {
      setState('unauthenticated')
    } else if (next.isFallback) {
      setState('fallback')
    } else {
      setState('ready')
    }
  }

  useEffect(() => {
    let cancelled = false
    loadCurrentAdminUser().then((next) => {
      if (cancelled) return
      setUser(next)
      if (!next.isAuthenticated) setState('unauthenticated')
      else if (next.isFallback) setState('fallback')
      else setState('ready')
    })
    return () => {
      cancelled = true
    }
  }, [])

  const value = useMemo<SessionValue>(() => ({
    user,
    state,
    roles: user?.roles ?? [],
    isSuperAdmin: user?.isSuperAdmin ?? false,
    hasAnyRole: computeHasAnyRole(user),
    reload: load,
  }), [user, state])

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}

export function useSession(): SessionValue {
  const ctx = useContext(SessionContext)
  if (!ctx) {
    throw new Error('useSession must be used within a SessionProvider')
  }
  return ctx
}
