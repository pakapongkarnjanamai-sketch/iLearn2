import { ApiError, fetchWithAccessControl } from './apiClient'
import { appConfig } from '../config/appConfig'

export type CurrentAdminUser = {
  isAuthenticated: boolean
  nid: string
  displayName: string
  divisionId?: number | null
  divisionName?: string | undefined
  isSuperAdmin: boolean
  roles: string[]
  isFallback?: boolean
}

const fallbackAdminUser: CurrentAdminUser = {
  isAuthenticated: false,
  nid: 'unknown',
  displayName: 'Not Connected',
  divisionId: null,
  divisionName: undefined,
  isSuperAdmin: false,
  roles: [],
  isFallback: true,
}

export const loadCurrentAdminUser = async () => {
  if (!appConfig.enableSessionBootstrap) {
    return fallbackAdminUser
  }

  try {
    return await fetchWithAccessControl<CurrentAdminUser>('admin/session/me')
  } catch (error) {
    if (error instanceof ApiError && (error.status === 401 || error.status === 403)) {
      return {
        ...fallbackAdminUser,
        isAuthenticated: false,
        roles: [],
      }
    }

    return fallbackAdminUser
  }
}