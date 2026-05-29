import { useEffect, useState, useMemo } from 'react'
import { Filter, RotateCcw, Search, X, RefreshCw } from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

export type LearnerSelection = {
  code: string // EId / Employee Code
  name: string
  division?: string
  department?: string
  section?: string
  position?: string
}

type LearnerDirectorySelectorProps = {
  selectedLearners: LearnerSelection[]
  onChange: (selected: LearnerSelection[]) => void
}

function getInitials(name: string) {
  if (!name) return '?'
  const parts = name.trim().split(/\s+/)
  if (parts.length >= 2) {
    const first = parts[0]
    const last = parts[parts.length - 1]
    if (first && last && first[0] && last[0]) {
      return (first[0] + last[0]).toUpperCase()
    }
  }
  return name.slice(0, 2).toUpperCase()
}

export function LearnerDirectorySelector({ selectedLearners, onChange }: LearnerDirectorySelectorProps) {
  // Cascading dropdowns options
  const [divisions, setDivisions] = useState<string[]>([])
  const [departments, setDepartments] = useState<string[]>([])
  const [sections, setSections] = useState<string[]>([])
  const [positions, setPositions] = useState<string[]>([])

  // Selected filter values in sidebar
  const [selectedDiv, setSelectedDiv] = useState('')
  const [selectedDept, setSelectedDept] = useState('')
  const [selectedSec, setSelectedSec] = useState('')
  const [selectedPos, setSelectedPos] = useState('')

  // Unified global search
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')

  // Paging states
  const [pageIndex, setPageIndex] = useState(0)
  const [pageSize] = useState(15) // Capped row count for visually compact premium look
  const [totalCount, setTotalCount] = useState(0)
  const [learners, setLearners] = useState<LearnerSelection[]>([])
  const [loading, setLoading] = useState(false)

  // Debouncing search input
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchTerm)
      setPageIndex(0) // Reset page when search parameters change
    }, 400)

    return () => clearTimeout(handler)
  }, [searchTerm])

  // Load Divisions & Positions initially
  useEffect(() => {
    const loadInit = async () => {
      try {
        const divRes = await fetchWithAccessControl<any>('Learners/GetDivisions')
        const divItems = Array.isArray(divRes) ? divRes : divRes?.data || []
        setDivisions(divItems.map((x: any) => typeof x === 'string' ? x : x.Name || x.name || '').filter(Boolean))

        const posRes = await fetchWithAccessControl<any>('Learners/GetPositions')
        const posItems = Array.isArray(posRes) ? posRes : posRes?.data || []
        setPositions(posItems.map((x: any) => typeof x === 'string' ? x : x.Name || x.name || '').filter(Boolean))
      } catch (err) {
        console.error('Failed to load initial directory lookups', err)
      }
    }
    void loadInit()
  }, [])

  // Cascade load Departments when Division changes
  useEffect(() => {
    const loadDepts = async () => {
      if (!selectedDiv) {
        setDepartments([])
        setSelectedDept('')
        return
      }
      try {
        const filter = ["Division", "=", selectedDiv]
        const res = await fetchWithAccessControl<any>(`Learners/GetDepartments?filter=${encodeURIComponent(JSON.stringify(filter))}`)
        const items = Array.isArray(res) ? res : res?.data || []
        setDepartments(items.map((x: any) => typeof x === 'string' ? x : x.Name || x.name || '').filter(Boolean))
      } catch (err) {
        console.error('Failed to load departments', err)
      }
    }
    void loadDepts()
  }, [selectedDiv])

  // Cascade load Sections when Dept changes
  useEffect(() => {
    const loadSections = async () => {
      if (!selectedDiv || !selectedDept) {
        setSections([])
        setSelectedSec('')
        return
      }
      try {
        const filter = [
          ["Division", "=", selectedDiv],
          "and",
          ["Department", "=", selectedDept]
        ]
        const res = await fetchWithAccessControl<any>(`Learners/GetSections?filter=${encodeURIComponent(JSON.stringify(filter))}`)
        const items = Array.isArray(res) ? res : res?.data || []
        setSections(items.map((x: any) => typeof x === 'string' ? x : x.Name || x.name || '').filter(Boolean))
      } catch (err) {
        console.error('Failed to load sections', err)
      }
    }
    void loadSections()
  }, [selectedDiv, selectedDept])

  const handleClearFilters = () => {
    setSelectedDiv('')
    setSelectedDept('')
    setSelectedSec('')
    setSelectedPos('')
    setSearchTerm('')
  }

  // Combine organizational cascading and unified search into DevExtreme compound filter
  const buildCombinedFilter = () => {
    const conditions: any[] = []

    if (selectedDiv) {
      conditions.push(["Division", "=", selectedDiv])
    }
    if (selectedDept) {
      conditions.push(["Department", "=", selectedDept])
    }
    if (selectedSec) {
      conditions.push(["Section", "=", selectedSec])
    }
    if (selectedPos) {
      conditions.push(["Position", "=", selectedPos])
    }

    if (debouncedSearch.trim()) {
      const val = debouncedSearch.trim()
      conditions.push([
        ["EId", "contains", val],
        "or",
        [
          ["EnglishFirstName", "contains", val],
          "or",
          ["EnglishLastName", "contains", val]
        ]
      ])
    }

    if (conditions.length === 0) return null
    if (conditions.length === 1) return conditions[0]

    let combined = conditions[0]
    for (let i = 1; i < conditions.length; i++) {
      combined = [combined, "and", conditions[i]]
    }
    return combined
  }

  const loadLearners = async () => {
    setLoading(true)
    try {
      const combinedFilter = buildCombinedFilter()
      const skip = pageIndex * pageSize
      let url = `Learners/Get?skip=${skip}&take=${pageSize}&requireTotalCount=true`
      
      if (combinedFilter) {
        url += `&filter=${encodeURIComponent(JSON.stringify(combinedFilter))}`
      }

      const response = await fetchWithAccessControl<any>(url)
      let list: any[] = []
      let total = 0
      
      if (response) {
        if (Array.isArray(response)) {
          list = response
          total = response.length
        } else if (response.data && Array.isArray(response.data)) {
          list = response.data
          total = typeof response.totalCount === 'number' ? response.totalCount : response.data.length
        }
      }

      const formatted = list.map(item => {
        const code = String(
          item.EId || item.eId || item.eid || item.code || item.nid || item.NId || ''
        ).trim()

        const firstName = item.EnglishFirstName || item.englishFirstName || item.ThaiFirstName || item.thaiFirstName || ''
        const lastName = item.EnglishLastName || item.englishLastName || item.ThaiLastName || item.thaiLastName || ''

        const name = String(
          item.Name || item.name || item.fullName || item.FullName ||
          (firstName ? `${firstName} ${lastName}`.trim() : '')
        ).trim()

        const division = item.Division || item.division || ''
        const department = item.Department || item.department || ''
        const section = item.Section || item.section || ''
        const position = item.Position || item.position || ''

        return { code, name, division, department, section, position }
      }).filter(item => item.code)

      if (pageIndex > 0) {
        setLearners(prev => {
          const existingCodes = new Set(prev.map(x => x.code))
          const uniqueNew = formatted.filter(x => !existingCodes.has(x.code))
          return [...prev, ...uniqueNew]
        })
      } else {
        setLearners(formatted)
      }
      setTotalCount(total)
    } catch (err) {
      console.error(err)
      toast.error('Failed to load learners directory')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadLearners()
  }, [
    pageIndex,
    selectedDiv,
    selectedDept,
    selectedSec,
    selectedPos,
    debouncedSearch
  ])

  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const target = e.currentTarget
    const threshold = target.scrollHeight - target.scrollTop - target.clientHeight
    if (threshold <= 20 && !loading && learners.length < totalCount) {
      setPageIndex(prev => prev + 1)
    }
  }

  // Single row toggle selection
  const handleToggleRow = (learner: LearnerSelection) => {
    const isSelected = selectedLearners.some(x => x.code === learner.code)
    if (isSelected) {
      onChange(selectedLearners.filter(x => x.code !== learner.code))
    } else {
      onChange([...selectedLearners, learner])
    }
  }

  // Select all checkbox on current page
  const isPageAllSelected = useMemo(() => {
    if (learners.length === 0) return false
    return learners.every(l => selectedLearners.some(x => x.code === l.code))
  }, [learners, selectedLearners])

  const handleToggleSelectAll = () => {
    if (isPageAllSelected) {
      const pageCodes = new Set(learners.map(l => l.code))
      onChange(selectedLearners.filter(x => !pageCodes.has(x.code)))
    } else {
      const toAdd = learners.filter(l => !selectedLearners.some(x => x.code === l.code))
      onChange([...selectedLearners, ...toAdd])
    }
  }

  const handleRemoveChip = (code: string) => {
    onChange(selectedLearners.filter(x => x.code !== code))
  }

  const handleClearAll = () => {
    onChange([])
  }

  return (
    <div className="flex flex-col gap-4 min-h-0 flex-1">
      
      {/* Search Grid Workspace with left filters panel */}
      <div className="flex gap-4 min-h-0 flex-1 items-stretch w-full min-w-0">
        
        {/* Left Column: FILTERS Cascading panel */}
        <div className="w-60 shrink-0 bg-white border border-slate-200 rounded-lg p-4 flex flex-col gap-3.5 text-xs font-semibold shadow-2xs">
          <div className="flex items-center gap-1.5 border-b border-slate-100 pb-2 mb-0.5">
            <Filter className="h-4 w-4 text-indigo-500" />
            <span className="text-slate-800 font-extrabold uppercase tracking-wider text-xxs">Filters</span>
          </div>
          
          <div className="space-y-1">
            <label className="block text-xxs font-extrabold text-slate-400 uppercase">Division</label>
            <select
              value={selectedDiv}
              onChange={e => { setSelectedDiv(e.target.value); setPageIndex(0); }}
              className="w-full px-2.5 py-2 border border-slate-200 rounded bg-white text-slate-700 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 cursor-pointer"
            >
              <option value="">All Divisions</option>
              {divisions.map(d => <option key={d} value={d}>{d}</option>)}
            </select>
          </div>

          <div className="space-y-1">
            <label className="block text-xxs font-extrabold text-slate-400 uppercase">Department</label>
            <select
              value={selectedDept}
              onChange={e => { setSelectedDept(e.target.value); setPageIndex(0); }}
              disabled={!selectedDiv}
              className="w-full px-2.5 py-2 border border-slate-200 rounded bg-white text-slate-700 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 disabled:bg-slate-50 disabled:text-slate-400 cursor-pointer disabled:cursor-not-allowed"
            >
              <option value="">{selectedDiv ? 'All Departments' : 'Select Division first'}</option>
              {departments.map(d => <option key={d} value={d}>{d}</option>)}
            </select>
          </div>

          <div className="space-y-1">
            <label className="block text-xxs font-extrabold text-slate-400 uppercase">Section</label>
            <select
              value={selectedSec}
              onChange={e => { setSelectedSec(e.target.value); setPageIndex(0); }}
              disabled={!selectedDept}
              className="w-full px-2.5 py-2 border border-slate-200 rounded bg-white text-slate-700 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 disabled:bg-slate-50 disabled:text-slate-400 cursor-pointer disabled:cursor-not-allowed"
            >
              <option value="">{selectedDept ? 'All Sections' : 'Select Department first'}</option>
              {sections.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>

          <div className="space-y-1">
            <label className="block text-xxs font-extrabold text-slate-400 uppercase">Position</label>
            <select
              value={selectedPos}
              onChange={e => { setSelectedPos(e.target.value); setPageIndex(0); }}
              className="w-full px-2.5 py-2 border border-slate-200 rounded bg-white text-slate-700 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 cursor-pointer"
            >
              <option value="">All Positions</option>
              {positions.map(p => <option key={p} value={p}>{p}</option>)}
            </select>
          </div>

          <button
            type="button"
            onClick={handleClearFilters}
            className="mt-2 w-full py-2 border border-slate-200 hover:bg-slate-50 text-slate-600 font-bold rounded flex items-center justify-center gap-1.5 transition cursor-pointer"
          >
            <RotateCcw className="h-3.5 w-3.5" />
            <span>Clear Filters</span>
          </button>
        </div>

        {/* Right Column: Interactive Table Grid with Unified Search Bar */}
        <div className="flex-1 min-w-0 flex flex-col border border-slate-200 rounded-lg bg-white overflow-hidden min-h-0 shadow-2xs">
          
          <div className="px-4 py-3.5 bg-slate-50 border-b border-slate-200 flex flex-col sm:flex-row gap-3 sm:items-center justify-between shrink-0 select-none">
            <div className="flex items-center gap-2">
              <span className="font-extrabold text-xs text-slate-700 uppercase tracking-wider">Learner Directory</span>
              <span className="bg-indigo-50 text-blue-700 border border-indigo-100 px-2.5 py-0.5 text-xxs font-extrabold rounded-full shadow-3xs">
                {totalCount} learner(s)
              </span>
            </div>

            {/* Premium Global Search Box */}
            <div className="relative w-full sm:w-80">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400 pointer-events-none" />
              <input
                type="text"
                placeholder="Search Name or Employee ID (EId)..."
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                className="w-full pl-9 pr-8 py-2 border border-slate-200 rounded-lg text-xs font-semibold placeholder:text-slate-400 bg-white focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition shadow-3xs"
              />
              {searchTerm && (
                <button
                  type="button"
                  onClick={() => setSearchTerm('')}
                  className="absolute right-2.5 top-2.5 text-slate-400 hover:text-slate-600 p-0.5 rounded-full hover:bg-slate-100 transition"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
          </div>

          {/* Scrollable table viewport */}
          <div 
            onScroll={handleScroll}
            className="flex-1 overflow-auto custom-scrollbar relative min-h-0"
          >
            <table className="min-w-full table-fixed divide-y divide-slate-100 text-left text-xs font-semibold text-slate-700">
              <thead className="bg-slate-50 sticky top-0 z-10 border-b border-slate-200 shadow-3xs">
                <tr className="text-xxs font-extrabold text-slate-500 uppercase tracking-wider border-b border-slate-200 select-none">
                  <th className="p-3 w-12 text-center">
                    <input
                      type="checkbox"
                      checked={isPageAllSelected}
                      onChange={handleToggleSelectAll}
                      className="h-4 w-4 text-indigo-500 rounded border-slate-300 focus:ring-indigo-400 cursor-pointer"
                    />
                  </th>
                  <th className="p-3 w-28">ID (EId)</th>
                  <th className="p-3">Learner Name</th>
                  <th className="p-3 w-36">Position</th>
                  <th className="p-3 w-32">Dept.</th>
                  <th className="p-3 w-36">Section</th>
                </tr>
              </thead>
              
              <tbody className="divide-y divide-slate-100 bg-white relative">
                {loading && learners.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="p-12 text-center text-slate-400 font-semibold">
                      <RefreshCw className="h-6 w-6 animate-spin text-indigo-500 mx-auto mb-2" />
                      <span>Loading learners directory...</span>
                    </td>
                  </tr>
                ) : learners.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="p-12 text-center text-slate-400 font-semibold">
                      No matching learners found. Try modifying your filter criteria.
                    </td>
                  </tr>
                ) : (
                  learners.map(l => {
                    const isChecked = selectedLearners.some(x => x.code === l.code)
                    return (
                      <tr
                        key={l.code}
                        onClick={() => handleToggleRow(l)}
                        className={`hover:bg-slate-50/70 border-b border-slate-100/50 transition cursor-pointer select-none ${
                          isChecked ? 'bg-indigo-50/20' : ''
                        }`}
                      >
                        <td className="p-3 w-12 text-center" onClick={e => e.stopPropagation()}>
                          <input
                            type="checkbox"
                            checked={isChecked}
                            onChange={() => handleToggleRow(l)}
                            className="h-4 w-4 text-indigo-500 rounded border-slate-300 focus:ring-indigo-400 cursor-pointer"
                          />
                        </td>
                        <td className="p-3 w-28 font-mono font-bold text-slate-800 truncate">{l.code}</td>
                        <td className="p-3 font-semibold text-slate-900 truncate">
                          <div className="flex items-center gap-3">
                            <div className="h-8 w-8 rounded-full bg-slate-100 border border-slate-200 flex items-center justify-center text-slate-600 text-xxs font-extrabold uppercase shrink-0 shadow-3xs select-none">
                              {getInitials(l.name)}
                            </div>
                            <span className="truncate">{l.name || '—'}</span>
                          </div>
                        </td>
                        <td className="p-3 w-36 text-slate-500 font-semibold text-xs truncate">{l.position || '—'}</td>
                        <td className="p-3 w-32 text-slate-500 font-semibold text-xs truncate">{l.department || '—'}</td>
                        <td className="p-3 w-36 text-slate-400 font-semibold text-xs truncate">{l.section || '—'}</td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </table>
            
            {loading && learners.length > 0 && (
              <div className="absolute inset-0 bg-white/45 flex items-center justify-center z-10 transition duration-150">
                <RefreshCw className="h-6 w-6 animate-spin text-indigo-500" />
              </div>
            )}
          </div>

          {/* Grid pagination footer */}
          <footer className="p-3.5 border-t border-slate-100 bg-slate-50/60 flex justify-between items-center text-slate-500 text-xs font-semibold select-none shrink-0">
            <div>
              {totalCount > 0 ? (
                <span>
                  Showing <strong className="text-slate-800">{learners.length}</strong> of{" "}
                  <strong className="text-slate-800">{totalCount}</strong> learners
                  {learners.length < totalCount ? (
                    <span className="text-slate-400 font-normal"> (Scroll down to load more)</span>
                  ) : (
                    <span className="text-emerald-600 font-bold"> (All records loaded)</span>
                  )}
                </span>
              ) : (
                <span>No learners directory records loaded</span>
              )}
            </div>
            
            {loading && learners.length > 0 && (
              <div className="flex items-center gap-1.5 text-indigo-500 text-xxs uppercase font-bold tracking-wider animate-pulse">
                <RefreshCw className="h-3 w-3 animate-spin" />
                <span>Loading more...</span>
              </div>
            )}
          </footer>

        </div>
      </div>

      {/* Tray Area: SELECTED blue chips list and eraser */}
      <div className="bg-slate-50 border border-slate-200 rounded-lg p-3.5 flex flex-col gap-2 select-none shrink-0 shadow-2xs">
        <div className="flex justify-between items-center text-slate-500 text-xxs font-extrabold uppercase tracking-wider">
          <span>Selected Learners Ledger ({selectedLearners.length})</span>
          {selectedLearners.length > 0 && (
            <button
              type="button"
              onClick={handleClearAll}
              className="text-red-500 hover:text-red-700 font-extrabold transition cursor-pointer flex items-center gap-1"
            >
              <span>Clear Selection</span>
            </button>
          )}
        </div>
        <div className="flex flex-wrap gap-2 max-h-28 overflow-y-auto custom-scrollbar">
          {selectedLearners.length === 0 ? (
            <span className="text-slate-400 text-xs font-semibold py-1">No learners selected yet. Click row checkboxes above.</span>
          ) : (
            selectedLearners.map(learner => (
              <span
                key={learner.code}
                className="inline-flex items-center gap-1.5 bg-indigo-50 text-blue-700 border border-blue-200 px-3 py-1 text-xs font-semibold rounded-full hover:bg-blue-100/70 transition shadow-3xs"
              >
                <span>{learner.name === learner.code ? learner.code : `${learner.name} (${learner.code})`}</span>
                <button
                  type="button"
                  onClick={() => handleRemoveChip(learner.code)}
                  className="text-blue-500 hover:text-blue-700 focus:outline-none flex items-center justify-center rounded-full hover:bg-blue-200/40 p-0.5"
                >
                  <X className="h-3 w-3" />
                </button>
              </span>
            ))
          )}
        </div>
      </div>

    </div>
  )
}

