import { useCallback, useEffect, useLayoutEffect, useRef, useState, useTransition, type ReactNode } from 'react'
import { 
  ArrowUpDown, 
  ArrowUp, 
  ArrowDown,
  Info,
  Loader2
} from 'lucide-react'
import { type AppClientStore } from '../../lib/createDataSource'
import { formatDate } from '../../lib/format'
import { AppTableSearch } from './table/AppTableSearch'
import { AppTableFooter } from './table/AppTableFooter'

type TableRecord = Record<string, unknown>
type FilterExpression = unknown[]

export type AdminGridColumn<T extends TableRecord = TableRecord> = {
  dataField: string
  caption: string
  dataType?: 'string' | 'number' | 'boolean' | 'date' | 'datetime'
  width?: number
  minWidth?: number
  alignment?: 'left' | 'center' | 'right'
  visible?: boolean
  cellRender?: (cellInfo: { value: unknown; data: T; index: number }) => ReactNode
}

type AppTableProps<T extends TableRecord> = {
  store: AppClientStore<T>
  columns: AdminGridColumn<T>[]
  noDataText?: string | undefined
  onRowDblClick?: ((event: { data: T }) => void) | undefined
  searchPlaceholder?: string | undefined
  searchExpr?: string | string[] | undefined
  externalFilters?: FilterExpression | undefined
  toolbarContent?: ReactNode
  actionButtons?: Array<{
    hint: string
    icon: 'info' | ReactNode
    onClick: (event: { row: { data: T } }) => void
    variant?: 'primary' | 'danger' | 'success' | 'ghost' | undefined
  }> | undefined
}

const asInputValue = (value: unknown) => value === undefined || value === null ? '' : String(value)

const formatDateValue = (value: unknown) => {
  if (value instanceof Date) return formatDate(value)
  if (typeof value === 'string' || typeof value === 'number') return formatDate(new Date(value))
  return '—'
}

export function AppTable<T extends TableRecord>({
  store,
  columns,
  noDataText = 'No data records found',
  onRowDblClick,
  searchPlaceholder = 'Search...',
  searchExpr,
  externalFilters = [],
  toolbarContent,
  actionButtons
}: AppTableProps<T>) {
  const [data, setData] = useState<T[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [, startTransition] = useTransition()

  // Pagination states
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(0)

  // Search states
  const [searchValue, setSearchValue] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')

  // Sorting state
  const [sortField, setSortField] = useState<string | null>(null)
  const [sortDesc, setSortDesc] = useState(false)
  const externalFilterKey = JSON.stringify(externalFilters)

  // Debounce search input
  const searchTimeoutRef = useRef<number | null>(null)
  const tableViewportRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current)
    }
    searchTimeoutRef.current = setTimeout(() => {
      setDebouncedSearch(searchValue)
      setPage(1) // reset page when search changes
    }, 400)

    return () => {
      if (searchTimeoutRef.current) clearTimeout(searchTimeoutRef.current)
    }
  }, [searchValue])

  // Reset page to 1 when external filters change
  useEffect(() => {
    setPage(1)
  }, [externalFilterKey])

  const fetchData = useCallback(async () => {
    if (pageSize === 0) return
    setLoading(true)
    const skip = (page - 1) * pageSize
    const take = pageSize

    const sortOption = sortField 
      ? [{ selector: sortField, desc: sortDesc }] 
      : undefined

    const result = await store.load({
      skip,
      take,
      sort: sortOption,
      searchValue: debouncedSearch,
      searchExpr,
      filter: JSON.parse(externalFilterKey) as FilterExpression
    })

    startTransition(() => {
      if (page > 1) {
        setData(prev => {
          const existingKeys = new Set(prev.map(x => x[store.key]))
          const uniqueNew = result.data.filter(x => !existingKeys.has(x[store.key]))
          return [...prev, ...uniqueNew]
        })
      } else {
        setData(result.data)
      }
      setTotalCount(result.totalCount)
      setLoading(false)
    })
  }, [debouncedSearch, externalFilterKey, page, pageSize, searchExpr, sortDesc, sortField, startTransition, store])

  // Reload data when query factors change
  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void fetchData()
    }, 0)

    return () => window.clearTimeout(timeoutId)
  }, [fetchData])

  // Handle click to sort column
  const handleSort = (field: string) => {
    setPage(1)
    if (sortField === field) {
      if (!sortDesc) {
        setSortDesc(true)
      } else {
        setSortField(null)
        setSortDesc(false)
      }
    } else {
      setSortField(field)
      setSortDesc(false)
    }
  }

  // Infinite Scroll Handler
  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const target = e.currentTarget
    const threshold = target.scrollHeight - target.scrollTop - target.clientHeight
    if (threshold <= 20 && !loading && data.length < totalCount) {
      setPage(prev => prev + 1)
    }
  }

  // Automatically load next page if the loaded data does not fill the viewport height
  // and there are still more records to fetch.
  useEffect(() => {
    const viewport = tableViewportRef.current
    if (!viewport || loading || data.length === 0 || data.length >= totalCount) return

    // If scrollHeight is less than or equal to clientHeight, there is no scrollbar
    // but we still have more records to load!
    if (viewport.scrollHeight <= viewport.clientHeight) {
      setPage(prev => prev + 1)
    }
  }, [data.length, totalCount, loading])

  const visibleColumns = columns.filter(col => col.visible !== false)

  useLayoutEffect(() => {
    const viewport = tableViewportRef.current
    if (!viewport) return undefined

    const updateAutoPageSize = () => {
      const headerHeight = viewport.querySelector('thead')?.getBoundingClientRect().height ?? 42
      const usableHeight = Math.max(0, viewport.clientHeight - headerHeight)
      const rowHeight = 38
      const nextPageSize = Math.max(10, Math.min(100, Math.floor(usableHeight / rowHeight)))

      setPageSize(prev => (prev === nextPageSize ? prev : nextPageSize))
    }

    updateAutoPageSize()

    const observer = new ResizeObserver(updateAutoPageSize)
    observer.observe(viewport)
    window.addEventListener('resize', updateAutoPageSize)

    return () => {
      observer.disconnect()
      window.removeEventListener('resize', updateAutoPageSize)
    }
  }, [visibleColumns.length])

  return (
    <div className="flex flex-col h-full min-h-0 overflow-hidden bg-white">
      
      <AppTableSearch
        value={searchValue}
        onChange={setSearchValue}
        totalCount={totalCount}
        placeholder={searchPlaceholder}
        toolbarContent={toolbarContent}
      />

      <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg border border-slate-200/80 bg-white shadow-3xs">
        {/* Grid Table Workspace */}
        <div 
          ref={tableViewportRef} 
          onScroll={handleScroll}
          className="relative flex-1 min-h-0 overflow-auto custom-scrollbar"
        >
          <table className="min-w-full divide-y divide-slate-100 text-left text-xxs sm:text-[12px]">
          
          {/* Table Headers */}
          <thead className="bg-slate-50/90 sticky top-0 z-10 border-b border-slate-200">
            <tr>
              {visibleColumns.map(col => (
                <th
                  key={col.dataField}
                  onClick={() => handleSort(col.dataField)}
                  style={{ width: col.width, minWidth: col.minWidth }}
                  className="px-4 py-2.5 text-xxs font-extrabold text-slate-500 uppercase cursor-pointer hover:bg-slate-100/80 transition select-none"
                >
                  <div className={`flex items-center gap-1 ${
                    col.alignment === 'center' ? 'justify-center' : col.alignment === 'right' ? 'justify-end' : 'justify-start'
                  }`}>
                    <span>{col.caption}</span>
                    {sortField === col.dataField ? (
                      sortDesc ? <ArrowDown className="h-3 w-3 text-indigo-500 shrink-0" /> : <ArrowUp className="h-3 w-3 text-indigo-500 shrink-0" />
                    ) : (
                      <ArrowUpDown className="h-3 w-3 text-slate-300 shrink-0 opacity-0 group-hover:opacity-100 transition" />
                    )}
                  </div>
                </th>
              ))}
              {actionButtons && (
                <th className="px-4 py-2.5 text-xxs font-extrabold text-slate-500 uppercase text-center w-24 select-none">
                  Actions
                </th>
              )}
            </tr>
          </thead>

          {/* Table Body */}
          <tbody className="divide-y divide-slate-100 bg-white relative">
            
            {/* Main Data Rows */}
            {data.length === 0 && !loading ? (
              <tr>
                <td
                  colSpan={visibleColumns.length + (actionButtons ? 1 : 0)}
                  className="px-4 py-12 text-center text-slate-400 font-medium"
                >
                  {noDataText}
                </td>
              </tr>
            ) : (
              data.map((row, index) => {
                const rowKey = row[store.key]
                return (
                  <tr
                    key={asInputValue(rowKey) || index}
                    onDoubleClick={() => onRowDblClick && onRowDblClick({ data: row })}
                    className="premium-hover-row group cursor-pointer border-b border-slate-100/50"
                  >
                    {visibleColumns.map(col => {
                      const val = row[col.dataField]
                      return (
                        <td key={col.dataField} className={`px-4 py-2.5 ${col.dataField === 'description' || col.dataField === 'name' || col.dataField === 'title' ? '' : 'whitespace-nowrap'}`}>
                          {col.cellRender ? (
                            col.cellRender({ value: val, data: row, index })
                          ) : col.dataType === 'boolean' ? (
                            <div className="flex justify-center">
                              <span className={`inline-flex items-center px-2 py-0.5 rounded text-xxs font-bold ${
                                val ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-800'
                              }`}>
                                {val ? 'Yes' : 'No'}
                              </span>
                            </div>
                          ) : col.dataType === 'datetime' || col.dataType === 'date' ? (
                            <span className="text-slate-400 font-medium text-xxs sm:text-[12px]">
                              {formatDateValue(val)}
                            </span>
                          ) : (
                            <span className={`text-slate-700 font-semibold text-xxs sm:text-[12px] ${
                              col.alignment === 'center' ? 'block text-center' : col.alignment === 'right' ? 'block text-right' : 'block text-left'
                            }`}>
                              {val !== undefined && val !== null ? String(val) : '—'}
                            </span>
                          )}
                        </td>
                      )
                    })}
                    
                    {/* Action Column */}
                    {actionButtons && (
                      <td className="px-4 py-2.5 text-center">
                        <div className="flex items-center justify-center gap-1.5 opacity-70 group-hover:opacity-100 transition">
                          {actionButtons.map((btn, idx) => {
                            const hintLower = btn.hint.toLowerCase()
                            const isDanger = btn.variant === 'danger' || hintLower.includes('delete') || hintLower.includes('remove')
                            const isSuccess = btn.variant === 'success' || hintLower.includes('active')
                            const isGhost = btn.variant === 'ghost' || hintLower.includes('details') || hintLower.includes('open')
                            
                            let colorClass = 'text-indigo-500 hover:bg-indigo-50'
                            if (isDanger) {
                              colorClass = 'text-red-500 hover:bg-rose-50'
                            } else if (isSuccess) {
                              colorClass = 'text-emerald-600 hover:bg-emerald-50'
                            } else if (isGhost) {
                              colorClass = 'text-slate-400 hover:bg-slate-100 hover:text-slate-700'
                            }

                            return (
                              <button
                                key={idx}
                                onClick={(e) => {
                                  e.stopPropagation()
                                  btn.onClick({ row: { data: row } })
                                }}
                                className={`p-1 rounded-md transition cursor-pointer ${colorClass}`}
                                title={btn.hint}
                              >
                                {btn.icon === 'info' ? <Info className="h-3.5 w-3.5" /> : btn.icon}
                              </button>
                            )
                          })}
                        </div>
                      </td>
                    )}
                  </tr>
                )
              })
            )}
          </tbody>

          </table>

          {/* Spinner Overlay */}
          {loading && (
            <div className="absolute inset-0 bg-white/45 backdrop-blur-xs flex items-center justify-center z-10 transition duration-150">
              <Loader2 className="h-6 w-6 animate-spin text-indigo-500" />
            </div>
          )}
        </div>

        {/* Infinite Scroll Footer */}
        <AppTableFooter
          loadedCount={data.length}
          totalCount={totalCount}
          loading={loading}
        />
      </div>
    </div>
  )
}
