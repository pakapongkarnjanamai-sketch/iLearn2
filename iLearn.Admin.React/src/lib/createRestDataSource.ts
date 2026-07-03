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

export type RestDataSourceOptions = {
  controller: string // e.g. "ContentItems" or "LearnerGroups"
  key?: string | string[]
  loadParams?: Record<string, string | number | boolean | null | undefined>
  enableCrud?: boolean
  basePath?: string | undefined // override, e.g. "api/ContentItems"
}

export const createRestDataSource = <T>({
  controller,
  key = 'id',
  loadParams,
  enableCrud = false,
  basePath: customBasePath,
}: RestDataSourceOptions): AppClientStore<T> => {
  // Default base path for modern REST API is "{controller}"
  const basePath = customBasePath ?? controller
  const loadUrl = buildApiUrl(`${basePath}/paged`)
  const insertUrl = buildApiUrl(basePath)
  const updateUrl = (id: any) => buildApiUrl(`${basePath}/${id}`)
  const deleteUrl = (id: any) => buildApiUrl(`${basePath}/${id}`)

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

    // Calculate 1-based page index from skip/take
    const skip = options.skip ?? 0
    const take = options.take ?? 20
    const page = Math.floor(skip / take) + 1
    const limit = take

    url.searchParams.append('page', String(page))
    url.searchParams.append('pageSize', String(limit))

    // Apply custom generic loadParams
    if (loadParams) {
      Object.entries(loadParams).forEach(([k, v]) => {
        if (v !== undefined && v !== null) {
          url.searchParams.append(k, String(v))
        }
      })
    }

    // Apply standard search string
    if (options.searchValue && options.searchValue.trim()) {
      url.searchParams.append('search', options.searchValue.trim())
    }

    // Apply custom status filter from options.filter if present in simplified format
    if (options.filter && options.filter.length > 0) {
      const processFilterNode = (node: any) => {
        if (!node) return
        if (Array.isArray(node)) {
          if (node.length === 3 && typeof node[0] === 'string' && typeof node[1] === 'string') {
            const [field, , val] = node
            if (field && val !== undefined && val !== null) {
              url.searchParams.append(field, String(val))
            }
          } else {
            node.forEach(subNode => processFilterNode(subNode))
          }
        }
      }
      processFilterNode(options.filter)
    }

    // Apply sorting parameter (standard sortBy and sortDescending)
    if (options.sort && options.sort.length > 0 && options.sort[0]) {
      const firstSort = options.sort[0]
      url.searchParams.append('sortBy', firstSort.selector)
      url.searchParams.append('sortDescending', String(firstSort.desc))
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
      
      // If the backend returns: { success: true, data: [...], totalCount: ... }
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
      const resp = await fetch(insertUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
        body: JSON.stringify(values),
        credentials: 'include',
      })

      if (!resp.ok) {
        const errorData = await resp.json().catch(() => ({}))
        const errorText = errorData.message || 'Insertion failed'
        throw new Error(errorText)
      }

      const body = await resp.json()
      toast.success('Record created successfully')
      return (body && body.data ? body.data : body) as T
    } catch (err: any) {
      const message = err.message || 'Unable to create record'
      toast.error(message)
      throw err
    }
  }

  const update = async (itemKey: any, values: Partial<T>): Promise<T> => {
    try {
      const resp = await fetch(updateUrl(itemKey), {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
        body: JSON.stringify(values),
        credentials: 'include',
      })

      if (!resp.ok) {
        const errorData = await resp.json().catch(() => ({}))
        const errorText = errorData.message || 'Update failed'
        throw new Error(errorText)
      }

      const body = await resp.json()
      toast.success('Changes saved successfully')
      return (body && body.data ? body.data : body) as T
    } catch (err: any) {
      const message = err.message || 'Unable to save modifications'
      toast.error(message)
      throw err
    }
  }

  const remove = async (itemKey: any): Promise<void> => {
    try {
      const resp = await fetch(deleteUrl(itemKey), {
        method: 'DELETE',
        credentials: 'include',
      })

      if (!resp.ok) {
        const errorData = await resp.json().catch(() => ({}))
        const errorText = errorData.message || 'Deletion failed'
        throw new Error(errorText)
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
