import { useCallback, useEffect, useLayoutEffect, useRef, useState, useTransition, type ReactNode } from 'react'
import { 
  ChevronLeft, 
  ChevronRight, 
  ChevronsLeft, 
  ChevronsRight, 
  Search, 
  Trash2, 
  Edit3, 
  Check, 
  X, 
  Loader2, 
  ArrowUpDown, 
  ArrowUp, 
  ArrowDown,
  Info
} from 'lucide-react'
import { type AppClientStore } from '../../lib/createDataSource'

type TableRecord = Record<string, unknown>
type TableValue = string | number | boolean | Date | null | undefined
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
  }> | undefined
}

const asInputValue = (value: unknown) => value === undefined || value === null ? '' : String(value)

const formatDateValue = (value: unknown) => {
  if (value instanceof Date) return value.toLocaleDateString()
  if (typeof value === 'string' || typeof value === 'number') return new Date(value).toLocaleDateString()
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
  const [pageSize, setPageSize] = useState(10)

  // Search states
  const [searchValue, setSearchValue] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')

  // Sorting state
  const [sortField, setSortField] = useState<string | null>(null)
  const [sortDesc, setSortDesc] = useState(false)
  const externalFilterKey = JSON.stringify(externalFilters)

  // Inline CRUD states
  const [editingKey, setEditingKey] = useState<unknown | null>(null)
  const [editValues, setEditValues] = useState<Partial<T>>({})
  const [inserting, setInserting] = useState(false)
  const [insertValues, setInsertValues] = useState<Partial<T>>({})

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

  const fetchData = useCallback(async () => {
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

    const nextTotalPages = Math.max(1, Math.ceil(result.totalCount / take))
    if (page > nextTotalPages) {
      setPage(nextTotalPages)
      setLoading(false)
      return
    }

    startTransition(() => {
      setData(result.data)
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

  // Cancel edit row
  const handleCancelEdit = () => {
    setEditingKey(null)
    setEditValues({})
  }

  // Save inline edit changes
  const handleSaveEdit = async (row: T) => {
    const keyVal = row[store.key]
    if (keyVal === undefined || keyVal === null) return
    try {
      setLoading(true)
      await store.update!(keyVal, editValues as Partial<T>)
      setEditingKey(null)
      setEditValues({})
      void fetchData()
    } catch {
      setLoading(false)
    }
  }

  // Start inline editing
  const handleStartEdit = (row: T) => {
    setEditingKey(row[store.key])
    setEditValues({ ...row })
  }

  // Cancel insert inline
  const handleCancelInsert = () => {
    setInserting(false)
    setInsertValues({})
  }

  // Save new record inline
  const handleSaveInsert = async () => {
    try {
      setLoading(true)
      await store.insert!(insertValues as Partial<T>)
      setInserting(false)
      setInsertValues({})
      void fetchData()
    } catch {
      setLoading(false)
    }
  }

  // Delete record with inline confirmation
  const handleDeleteRow = async (row: T) => {
    const keyVal = row[store.key]
    if (keyVal === undefined || keyVal === null) return
    if (!window.confirm('Are you absolutely sure you want to delete this record? This action cannot be undone.')) return
    try {
      setLoading(true)
      await store.remove!(keyVal)
      void fetchData()
    } catch {
      setLoading(false)
    }
  }

  const handleEditChange = (field: string, val: TableValue) => {
    setEditValues(prev => ({ ...prev, [field]: val } as Partial<T>))
  }

  const handleInsertChange = (field: string, val: TableValue) => {
    setInsertValues(prev => ({ ...prev, [field]: val } as Partial<T>))
  }

  // Calculated pagination helpers
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const startRecord = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const endRecord = Math.min(totalCount, page * pageSize)

  const visibleColumns = columns.filter(col => col.visible !== false)

  useLayoutEffect(() => {
    const viewport = tableViewportRef.current
    if (!viewport) return undefined

    const updateAutoPageSize = () => {
      const headerHeight = viewport.querySelector('thead')?.getBoundingClientRect().height ?? 42
      const usableHeight = Math.max(0, viewport.clientHeight - headerHeight)
      const rowHeight = 46
      const nextPageSize = Math.max(5, Math.min(100, Math.floor(usableHeight / rowHeight)))

      setPageSize(prev => (prev === nextPageSize ? prev : nextPageSize))
      setPage(prev => (prev === 1 ? prev : 1))
    }

    updateAutoPageSize()

    const observer = new ResizeObserver(updateAutoPageSize)
    observer.observe(viewport)
    window.addEventListener('resize', updateAutoPageSize)

    return () => {
      observer.disconnect()
      window.removeEventListener('resize', updateAutoPageSize)
    }
  }, [visibleColumns.length, inserting])

  return (
    <div className="admin-table-shell flex flex-col h-full min-h-0 overflow-hidden">
      
      <div className="flex flex-col gap-3 border-b border-slate-100/50 bg-slate-50/30 p-4 lg:flex-row lg:items-center">
        
        <div className="relative w-full sm:max-w-lg lg:max-w-xl">
          <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
          <input
            type="text"
            value={searchValue}
            onChange={(e) => setSearchValue(e.target.value)}
            placeholder={searchPlaceholder}
            className="w-full rounded-md border border-slate-200 bg-white py-2 pl-9 pr-4 text-xs font-semibold text-slate-700 shadow-xs transition focus:border-blue-600 focus:outline-none"
          />
        </div>

        {toolbarContent && (
          <div className="flex min-w-0 flex-1 flex-wrap items-center gap-2">
            {toolbarContent}
          </div>
        )}
      </div>

      {/* Grid Table Workspace */}
      <div ref={tableViewportRef} className="relative flex-1 min-h-0 overflow-auto custom-scrollbar">
        <table className="min-w-full divide-y divide-slate-100 text-left">
          
          {/* Table Headers */}
          <thead className="bg-slate-50 sticky top-0 z-10 border-b border-slate-200">
            <tr>
              {visibleColumns.map(col => (
                <th
                  key={col.dataField}
                  onClick={() => handleSort(col.dataField)}
                  style={{ width: col.width, minWidth: col.minWidth }}
                  className="px-4 py-3 text-xxs font-extrabold text-slate-500 uppercase cursor-pointer hover:bg-slate-100/80 transition select-none"
                >
                  <div className={`flex items-center gap-1 ${
                    col.alignment === 'center' ? 'justify-center' : col.alignment === 'right' ? 'justify-end' : 'justify-start'
                  }`}>
                    <span>{col.caption}</span>
                    {sortField === col.dataField ? (
                      sortDesc ? <ArrowDown className="h-3 w-3 text-blue-600 shrink-0" /> : <ArrowUp className="h-3 w-3 text-blue-600 shrink-0" />
                    ) : (
                      <ArrowUpDown className="h-3 w-3 text-slate-300 shrink-0 opacity-0 group-hover:opacity-100 transition" />
                    )}
                  </div>
                </th>
              ))}
              {(store.update || store.remove || actionButtons) && (
                <th className="px-4 py-3 text-xxs font-extrabold text-slate-500 uppercase text-center w-24 select-none">
                  Actions
                </th>
              )}
            </tr>
          </thead>

          {/* Table Body */}
          <tbody className="divide-y divide-slate-100 bg-white relative">
            
            {/* Inline Inserting Row */}
            {inserting && (
              <tr className="bg-blue-50/20 border-b border-blue-100">
                {visibleColumns.map(col => {
                  const insertValue = insertValues[col.dataField]
                  return (
                  <td key={col.dataField} className="px-4 py-2">
                    {col.dataType === 'boolean' ? (
                      <div className="flex justify-center">
                        <input
                          type="checkbox"
                          checked={Boolean(insertValue)}
                          onChange={(e) => handleInsertChange(col.dataField, e.target.checked)}
                          className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                        />
                      </div>
                    ) : col.dataType === 'number' ? (
                      <input
                        type="number"
                        value={asInputValue(insertValue)}
                        onChange={(e) => handleInsertChange(col.dataField, e.target.value === '' ? '' : Number(e.target.value))}
                        className="w-full px-2.5 py-1 border border-slate-200 rounded text-xs focus:outline-none focus:border-blue-600"
                      />
                    ) : (
                      <input
                        type="text"
                        value={asInputValue(insertValue)}
                        onChange={(e) => handleInsertChange(col.dataField, e.target.value)}
                        className="w-full px-2.5 py-1 border border-slate-200 rounded text-xs focus:outline-none focus:border-blue-600"
                      />
                    )}
                  </td>
                )})}
                <td className="px-4 py-2 text-center">
                  <div className="flex items-center justify-center gap-1">
                    <button
                      onClick={handleSaveInsert}
                      className="p-1 text-emerald-600 hover:bg-emerald-50 rounded-md transition cursor-pointer"
                      title="Save New Row"
                    >
                      <Check className="h-4 w-4" />
                    </button>
                    <button
                      onClick={handleCancelInsert}
                      className="p-1 text-slate-400 hover:bg-slate-50 rounded-md transition cursor-pointer"
                      title="Cancel Addition"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </div>
                </td>
              </tr>
            )}

            {/* Main Data Rows */}
            {data.length === 0 && !loading ? (
              <tr>
                <td
                  colSpan={visibleColumns.length + ((store.update || store.remove || actionButtons) ? 1 : 0)}
                  className="px-4 py-12 text-center text-slate-400 font-medium"
                >
                  {noDataText}
                </td>
              </tr>
            ) : (
              data.map((row, index) => {
                const rowKey = row[store.key]
                const isEditing = editingKey === rowKey
                return (
                  <tr
                    key={asInputValue(rowKey) || index}
                    onDoubleClick={() => onRowDblClick && onRowDblClick({ data: row })}
                    className="premium-hover-row group cursor-pointer border-b border-slate-100/50"
                  >
                    {visibleColumns.map(col => {
                      const val = row[col.dataField]
                      const editValue = editValues[col.dataField]
                      return (
                        <td key={col.dataField} className="px-4 py-3">
                          {isEditing ? (
                            col.dataType === 'boolean' ? (
                              <div className="flex justify-center">
                                <input
                                  type="checkbox"
                                  checked={Boolean(editValue)}
                                  onChange={(e) => handleEditChange(col.dataField, e.target.checked)}
                                  className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                                />
                              </div>
                            ) : col.dataType === 'number' ? (
                              <input
                                type="number"
                                value={asInputValue(editValue)}
                                onChange={(e) => handleEditChange(col.dataField, e.target.value === '' ? '' : Number(e.target.value))}
                                className="w-full px-2.5 py-1 border border-slate-200 rounded text-xs focus:outline-none focus:border-blue-600"
                              />
                            ) : (
                              <input
                                type="text"
                                value={asInputValue(editValue)}
                                onChange={(e) => handleEditChange(col.dataField, e.target.value)}
                                className="w-full px-2.5 py-1 border border-slate-200 rounded text-xs focus:outline-none focus:border-blue-600"
                              />
                            )
                          ) : col.cellRender ? (
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
                            <span className="text-slate-400 font-medium text-xs">
                              {formatDateValue(val)}
                            </span>
                          ) : (
                            <span className={`text-slate-700 font-semibold ${
                              col.alignment === 'center' ? 'block text-center' : col.alignment === 'right' ? 'block text-right' : 'block text-left'
                            }`}>
                              {val !== undefined && val !== null ? String(val) : '—'}
                            </span>
                          )}
                        </td>
                      )
                    })}
                    
                    {/* Action Column */}
                    {(store.update || store.remove || actionButtons) && (
                      <td className="px-4 py-3 text-center">
                        <div className="flex items-center justify-center gap-1.5 opacity-70 group-hover:opacity-100 transition">
                          {isEditing ? (
                            <>
                              <button
                                onClick={() => handleSaveEdit(row)}
                                className="p-1 text-emerald-600 hover:bg-emerald-50 rounded-md transition cursor-pointer animate-pulse"
                                title="Save Row Changes"
                              >
                                <Check className="h-3.5 w-3.5" />
                              </button>
                              <button
                                onClick={handleCancelEdit}
                                className="p-1 text-slate-400 hover:bg-slate-50 rounded-md transition cursor-pointer"
                                title="Cancel Modifications"
                              >
                                <X className="h-3.5 w-3.5" />
                              </button>
                            </>
                          ) : (
                            <>
                              {actionButtons && actionButtons.map((btn, idx) => (
                                <button
                                  key={idx}
                                  onClick={(e) => {
                                    e.stopPropagation()
                                    btn.onClick({ row: { data: row } })
                                  }}
                                  className="p-1 text-blue-600 hover:bg-blue-50 rounded-md transition cursor-pointer"
                                  title={btn.hint}
                                >
                                  {btn.icon === 'info' ? <Info className="h-3.5 w-3.5" /> : btn.icon}
                                </button>
                              ))}
                              {store.update && (
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation()
                                    handleStartEdit(row)
                                  }}
                                  className="p-1 text-slate-500 hover:text-blue-600 hover:bg-blue-50 rounded-md transition cursor-pointer"
                                  title="Edit Inline"
                                >
                                  <Edit3 className="h-3.5 w-3.5" />
                                </button>
                              )}
                              {store.remove && (
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation()
                                    handleDeleteRow(row)
                                  }}
                                  className="p-1 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-md transition cursor-pointer"
                                  title="Delete Record"
                                >
                                  <Trash2 className="h-3.5 w-3.5" />
                                </button>
                              )}
                            </>
                          )}
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
            <Loader2 className="h-6 w-6 animate-spin text-blue-600" />
          </div>
        )}
      </div>

      {/* Pagination Footer */}
      <footer className="flex flex-col sm:flex-row items-center sm:justify-between p-4 gap-3 border-t border-slate-100 bg-slate-50/50 text-xs font-semibold text-slate-500">
        
        {/* Record count indicator */}
        <div className="select-none">
          {totalCount > 0 ? (
            <span>Showing <strong className="text-slate-800">{startRecord}</strong> to <strong className="text-slate-800">{endRecord}</strong> of <strong className="text-slate-800">{totalCount}</strong> records</span>
          ) : (
            <span>No records loaded</span>
          )}
        </div>

        {/* Navigation / Dropdowns */}
        <div className="flex items-center gap-4 flex-wrap">
          
          {/* Auto page sizing */}
          <div className="flex items-center gap-1.5">
            <span className="select-none">Rows per page:</span>
            <span className="rounded border border-slate-200 bg-white px-2 py-1 font-bold text-slate-700 shadow-2xs">
              Auto {pageSize}
            </span>
          </div>

          {/* Navigation arrow buttons */}
          <div className="flex items-center gap-1">
            <button
              onClick={() => setPage(1)}
              disabled={page === 1}
              className="p-1 border border-slate-200 rounded-md bg-white hover:bg-slate-50 text-slate-500 disabled:opacity-30 disabled:hover:bg-white cursor-pointer select-none"
              title="First Page"
            >
              <ChevronsLeft className="h-4 w-4" />
            </button>
            <button
              onClick={() => setPage(prev => Math.max(1, prev - 1))}
              disabled={page === 1}
              className="p-1 border border-slate-200 rounded-md bg-white hover:bg-slate-50 text-slate-500 disabled:opacity-30 disabled:hover:bg-white cursor-pointer select-none"
              title="Previous Page"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            
            <div className="px-2 font-bold text-slate-700 select-none">
              Page {page} of {totalPages}
            </div>

            <button
              onClick={() => setPage(prev => Math.min(totalPages, prev + 1))}
              disabled={page === totalPages}
              className="p-1 border border-slate-200 rounded-md bg-white hover:bg-slate-50 text-slate-500 disabled:opacity-30 disabled:hover:bg-white cursor-pointer select-none"
              title="Next Page"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
            <button
              onClick={() => setPage(totalPages)}
              disabled={page === totalPages}
              className="p-1 border border-slate-200 rounded-md bg-white hover:bg-slate-50 text-slate-500 disabled:opacity-30 disabled:hover:bg-white cursor-pointer select-none"
              title="Last Page"
            >
              <ChevronsRight className="h-4 w-4" />
            </button>
          </div>

        </div>
      </footer>

    </div>
  )
}
