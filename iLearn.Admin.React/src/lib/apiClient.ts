import { appConfig } from '../config/appConfig'

export class ApiError extends Error {
  status: number
  responseBody: string

  constructor(message: string, status: number, responseBody: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.responseBody = responseBody
  }
}

export const buildApiUrl = (path: string) => {
  const normalizedPath = path.replace(/^\/+/, '')
  return `${appConfig.apiBaseUrl}/${normalizedPath}`
}

const buildHeaders = (headers: HeadersInit | undefined) => {
  const mergedHeaders = new Headers(headers)

  if (!mergedHeaders.has('Accept')) {
    mergedHeaders.set('Accept', 'application/json')
  }

  return mergedHeaders
}

const readResponseBody = async (response: Response) => {
  const contentType = response.headers.get('content-type') || ''

  if (contentType.includes('application/json')) {
    return JSON.stringify(await response.json())
  }

  return response.text()
}

export const fetchWithAccessControl = async <TResponse>(path: string, init: RequestInit = {}) => {
  const response = await fetch(buildApiUrl(path), {
    ...init,
    credentials: 'include',
    headers: buildHeaders(init.headers),
  })

  if (!response.ok) {
    const responseBody = await readResponseBody(response)
    throw new ApiError(response.statusText || 'API request failed', response.status, responseBody)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  const responseText = await response.text()
  if (!responseText) {
    return undefined as TResponse
  }

  return JSON.parse(responseText) as TResponse
}