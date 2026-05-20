import { ApiError, fetchWithAccessControl } from './apiClient'
import { appConfig } from '../config/appConfig'

export type CurrentAdminUser = {
  isAuthenticated: boolean
  nid: string
  displayName: string
  divisionName?: string
  roles: string[]
  isFallback?: boolean
}

const fallbackAdminUser: CurrentAdminUser = {
  isAuthenticated: true,
  nid: 'windows-user',
  displayName: 'Windows Admin',
  divisionName: 'Current division',
  roles: ['Admin'],
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