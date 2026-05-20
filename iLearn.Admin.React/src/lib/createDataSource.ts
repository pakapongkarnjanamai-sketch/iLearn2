import { createStore } from 'devextreme-aspnet-data-nojquery'
import { buildApiUrl } from './apiClient'
import { toast } from './toast'

type AjaxSettings = {
  headers?: Record<string, string>
  xhrFields?: Record<string, boolean>
}

export type AdminDataSourceOptions = {
  controller: string
  key?: string | string[]
  loadParams?: Record<string, string | number | boolean | null | undefined>
  enableCrud?: boolean
}

const withCredentials = (_operation: string, ajaxSettings: AjaxSettings) => {
  ajaxSettings.xhrFields = {
    ...(ajaxSettings.xhrFields ?? {}),
    withCredentials: true,
  }

  ajaxSettings.headers = {
    ...(ajaxSettings.headers ?? {}),
    Accept: 'application/json',
  }
}

export const createAdminDataSource = ({
  controller,
  key = 'id',
  loadParams,
  enableCrud = false,
}: AdminDataSourceOptions) => {
  const basePath = `admin/${controller}`
  const optionalLoadParams = loadParams === undefined ? {} : { loadParams }
  const baseOptions = {
    key,
    loadUrl: buildApiUrl(basePath),
    ...optionalLoadParams,
    onBeforeSend: withCredentials,
    onAjaxError: (event: { error: string | Error }) => {
      const message = event.error instanceof Error ? event.error.message : event.error
      toast.error(message || 'Unable to load data')
    },
  }

  if (!enableCrud) {
    return createStore(baseOptions)
  }

  return createStore({
    ...baseOptions,
    insertUrl: buildApiUrl(basePath),
    updateUrl: buildApiUrl(basePath),
    deleteUrl: buildApiUrl(basePath),
  })
}