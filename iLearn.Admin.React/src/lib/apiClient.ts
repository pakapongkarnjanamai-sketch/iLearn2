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

export type UploadPhase = 'uploading' | 'processing'
export type UploadProgress = { phase: UploadPhase; loadedBytes: number; totalBytes: number; percent: number }

export const uploadWithProgress = <TResponse>(
  path: string,
  formData: FormData,
  options: { method?: 'POST' | 'PUT'; onProgress?: (p: UploadProgress) => void } = {}
): { promise: Promise<TResponse>; abort: () => void } => {
  const xhr = new XMLHttpRequest()
  const method = options.method || 'POST'

  const promise = new Promise<TResponse>((resolve, reject) => {
    xhr.withCredentials = true

    xhr.open(method, buildApiUrl(path))
    xhr.setRequestHeader('Accept', 'application/json')

    if (xhr.upload && options.onProgress) {
      xhr.upload.onprogress = (event) => {
        if (event.lengthComputable) {
          const percent = event.total > 0 ? (event.loaded / event.total) * 100 : 0
          options.onProgress!({
            phase: 'uploading',
            loadedBytes: event.loaded,
            totalBytes: event.total,
            percent,
          })
        }
      }

      xhr.upload.onload = () => {
        options.onProgress!({
          phase: 'processing',
          loadedBytes: 0,
          totalBytes: 0,
          percent: 100,
        })
      }
    }

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        if (xhr.status === 204 || !xhr.responseText) {
          resolve(undefined as TResponse)
          return
        }
        try {
          resolve(JSON.parse(xhr.responseText) as TResponse)
        } catch {
          reject(new Error('Failed to parse response JSON'))
        }
      } else {
        const responseBody = xhr.responseText
        let errorMsg = xhr.statusText || 'API request failed'
        if (xhr.status === 413) {
          errorMsg = 'ไฟล์ใหญ่เกินลิมิตของเซิร์ฟเวอร์'
        } else if (responseBody) {
          try {
            const contentType = xhr.getResponseHeader('content-type') || ''
            if (contentType.includes('application/json')) {
              const errData = JSON.parse(responseBody)
              errorMsg = errData.message || errData.error || errorMsg
            } else {
              errorMsg = responseBody || errorMsg
            }
          } catch {
            // ignore parsing error
          }
        }
        reject(new ApiError(errorMsg, xhr.status, responseBody))
      }
    }

    xhr.onerror = () => {
      reject(new ApiError('Network error', 0, ''))
    }

    xhr.onabort = () => {
      const abortErr = new Error('Upload cancelled')
      ;(abortErr as any).isAborted = true
      reject(abortErr)
    }

    xhr.send(formData)
  })

  return {
    promise,
    abort: () => {
      xhr.abort()
    },
  }
}