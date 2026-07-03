import { buildApiUrl } from './apiClient'
import { toast } from './toast'

export type AppClientStore<T> = {
  key: string
  load: (options: {
    skip?: number | undefined
    take?: number | undefined
    sort?: Array<{ selector: string; desc: boolean }> | undefined
    searchValue?: string | undefined
    searchExpr?: string | string[] | undefined
    filter?: any[] | undefined
  }) => Promise<{ data: T[]; totalCount: number }>
  insert?: (values: Partial<T>) => Promise<T>
  update?: (key: any, values: Partial<T>) => Promise<T>
  remove?: (key: any) => Promise<void>
}

export type AdminDataSourceOptions = {
  controller: string
  key?: string | string[]
  loadParams?: Record<string, string | number | boolean | null | undefined>
  enableCrud?: boolean
  /** Override the default `admin/{controller}` base path. Use for non-CRUD controllers (e.g. `Learners`). */
  basePath?: string | undefined
}

export const createAdminDataSource = <T>({
  controller,
  key = 'id',
  loadParams,
  enableCrud = false,
  basePath: customBasePath,
}: AdminDataSourceOptions): AppClientStore<T> => {
  const basePath = customBasePath ?? `admin/${controller}`
  const loadUrl = buildApiUrl(`${basePath}/Get`)
  const insertUrl = buildApiUrl(`${basePath}/Post`)
  const updateUrl = buildApiUrl(`${basePath}/Put`)
  const deleteUrl = buildApiUrl(`${basePath}/Delete`)

  const keyField = (Array.isArray(key) ? key[0] : key) || 'id'

  const load = async (options: {
    skip?: number | undefined
    take?: number | undefined
    sort?: Array<{ selector: string; desc: boolean }> | undefined
    searchValue?: string | undefined
    searchExpr?: string | string[] | undefined
    filter?: any[] | undefined
  }): Promise<{ data: T[]; totalCount: number }> => {
    const url = new URL(loadUrl, window.location.origin)

    // Apply generic custom loadParams first if configured
    if (loadParams) {
      Object.entries(loadParams).forEach(([k, v]) => {
        if (v !== undefined && v !== null) {
          url.searchParams.append(k, String(v))
        }
      })
    }

    // Apply DevExtreme ASP.NET LoadOptions emulated properties
    if (options.skip !== undefined) {
      url.searchParams.append('skip', String(options.skip))
    }
    if (options.take !== undefined) {
      url.searchParams.append('take', String(options.take))
    }
    url.searchParams.append('requireTotalCount', 'true')

    // Apply sorting parameter (JSON Array of sort properties)
    if (options.sort && options.sort.length > 0) {
      url.searchParams.append('sort', JSON.stringify(options.sort))
    }

    // Process filters
    let finalFilter: any[] = []
    
    if (options.filter && options.filter.length > 0) {
      finalFilter = [...options.filter]
    }

    // If search text is present, build a compound search filter array
    if (options.searchValue && options.searchValue.trim() && options.searchExpr) {
      const searchVal = options.searchValue.trim()
      const searchExpressions = Array.isArray(options.searchExpr)
        ? options.searchExpr.filter(Boolean)
        : [options.searchExpr]

      if (searchExpressions.length > 0) {
        const searchConditions: any[] = []
        searchExpressions.forEach((expr, idx) => {
          searchConditions.push([expr, 'contains', searchVal])
          if (idx < searchExpressions.length - 1) {
            searchConditions.push('or')
          }
        })

        if (finalFilter.length > 0) {
          finalFilter = [finalFilter, 'and', searchConditions]
        } else {
          finalFilter = searchConditions
        }
      }
    }

    if (finalFilter.length > 0) {
      url.searchParams.append('filter', JSON.stringify(finalFilter))
    }

    try {
      const resp = await fetch(url.toString(), {
        method: 'GET',
        headers: {
          'Accept': 'application/json',
        },
        credentials: 'include',
      })

      if (!resp.ok) {
        throw new Error(`Load failed: ${resp.statusText}`)
      }

      const body = await resp.json()
      
      // Handle standard DevExtreme AspNet wrapped payloads
      if (body && Array.isArray(body.data)) {
        return {
          data: body.data,
          totalCount: typeof body.totalCount === 'number' ? body.totalCount : body.data.length
        }
      }

      if (Array.isArray(body)) {
        return {
          data: body,
          totalCount: body.length
        }
      }

      return { data: [], totalCount: 0 }
    } catch (err: any) {
      const message = err.message || 'Unable to retrieve data records'
      toast.error(message)
      return { data: [], totalCount: 0 }
    }
  }

  if (!enableCrud) {
    return { key: keyField, load }
  }

  const insert = async (values: Partial<T>): Promise<T> => {
    try {
      const form = new URLSearchParams()
      form.append('values', JSON.stringify(values))

      const resp = await fetch(insertUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'Accept': 'application/json',
        },
        body: form.toString(),
        credentials: 'include',
      })

      if (!resp.ok) {
        const errorText = await resp.text()
        throw new Error(errorText || 'Insertion failed')
      }

      const data = await resp.json()
      toast.success('Record created successfully')
      return data as T
    } catch (err: any) {
      const message = err.message || 'Unable to create record'
      toast.error(message)
      throw err
    }
  }

  const update = async (itemKey: any, values: Partial<T>): Promise<T> => {
    try {
      const form = new URLSearchParams()
      form.append('key', String(itemKey))
      form.append('values', JSON.stringify(values))

      const resp = await fetch(updateUrl, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'Accept': 'application/json',
        },
        body: form.toString(),
        credentials: 'include',
      })

      if (!resp.ok) {
        const errorText = await resp.text()
        throw new Error(errorText || 'Update failed')
      }

      const data = await resp.json()
      toast.success('Changes saved successfully')
      return data as T
    } catch (err: any) {
      const message = err.message || 'Unable to save modifications'
      toast.error(message)
      throw err
    }
  }

  const remove = async (itemKey: any): Promise<void> => {
    try {
      const form = new URLSearchParams()
      form.append('key', String(itemKey))

      const resp = await fetch(deleteUrl, {
        method: 'DELETE',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: form.toString(),
        credentials: 'include',
      })

      if (!resp.ok) {
        const errorText = await resp.text()
        throw new Error(errorText || 'Deletion failed')
      }

      toast.success('Record deleted successfully')
    } catch (err: any) {
      const message = err.message || 'Unable to remove record'
      toast.error(message)
      throw err
    }
  }

  return {
    key: keyField,
    load,
    insert,
    update,
    remove,
  }
}