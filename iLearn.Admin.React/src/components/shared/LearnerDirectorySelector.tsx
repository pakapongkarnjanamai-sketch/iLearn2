import { useCallback, useEffect, useState, useMemo } from 'react'
import type { ReactNode } from 'react'
import { ChevronDown, RotateCcw, Search, X } from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { Modal } from '../ui/Modal'
import { Badge } from '../ui/Badge'
import { stripGenderPrefix } from '../../lib/format'
import { LEARNER_LABELS, t, tf } from '../../lib/labels'

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
  headerLeft?: ReactNode
}

function getInitials(name: string) {
  const cleanName = stripGenderPrefix(name)
  if (!cleanName) return '?'
  const parts = cleanName.trim().split(/\s+/)
  if (parts.length >= 2) {
    const first = parts[0]
    const last = parts[parts.length - 1]
    if (first && last && first[0] && last[0]) {
      return (first[0] + last[0]).toUpperCase()
    }
  }
  return cleanName.slice(0, 2).toUpperCase()
}

export function LearnerDirectorySelector({ 
  selectedLearners, 
  onChange,
  headerLeft
}: LearnerDirectorySelectorProps) {
  const [ledgerOpen, setLedgerOpen] = useState(false)

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
  const [selectedSearch, setSelectedSearch] = useState('')

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
        toast.error(t(LEARNER_LABELS.failedToLoadDirectoryFilters))
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
        toast.error(t(LEARNER_LABELS.failedToLoadDepartments))
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
        toast.error(t(LEARNER_LABELS.failedToLoadSections))
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
    setPageIndex(0)
  }

  // Combine organizational cascading and unified search into DevExtreme compound filter
  const buildCombinedFilter = useCallback(() => {
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
  }, [selectedDiv, selectedDept, selectedSec, selectedPos, debouncedSearch])

  const loadLearners = useCallback(async () => {
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
      toast.error(t(LEARNER_LABELS.noLearnersFound))
    } finally {
      setLoading(false)
    }
  }, [buildCombinedFilter, pageIndex, pageSize])

  useEffect(() => {
    void loadLearners()
  }, [loadLearners])

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

  const handleSelectAllFiltered = async () => {
    setLoading(true)
    try {
      const combinedFilter = buildCombinedFilter()
      const take = Math.max(totalCount, pageSize)
      let url = `Learners/Get?skip=0&take=${take}&requireTotalCount=false`

      if (combinedFilter) {
        url += `&filter=${encodeURIComponent(JSON.stringify(combinedFilter))}`
      }

      const response = await fetchWithAccessControl<any>(url)
      let list: any[] = []

      if (response) {
        if (Array.isArray(response)) {
          list = response
        } else if (response.data && Array.isArray(response.data)) {
          list = response.data
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

      const merged = [...selectedLearners]
      const currentCodes = new Set(merged.map(x => x.code))
      formatted.forEach(item => {
        if (!currentCodes.has(item.code)) {
          merged.push(item)
        }
      })

      onChange(merged)
      toast.success(tf(LEARNER_LABELS.selectedAllMatching, formatted.length))
    } catch (err) {
      console.error(err)
      toast.error(t(LEARNER_LABELS.failedToSelectAll))
    } finally {
      setLoading(false)
    }
  }

  const filteredChips = useMemo(() => {
    if (!selectedSearch.trim()) return selectedLearners
    const term = selectedSearch.trim().toLowerCase()
    return selectedLearners.filter(item => (
      item.name.toLowerCase().includes(term) || item.code.toLowerCase().includes(term)
    ))
  }, [selectedLearners, selectedSearch])

  const handleRemoveChip = (code: string) => {
    onChange(selectedLearners.filter(x => x.code !== code))
  }

  const handleClearAll = () => {
    onChange([])
    setSelectedSearch('')
    setLedgerOpen(false)
  }

  return (
    <>
      <div className="flex-1 flex flex-col md:flex-row border border-slate-200 rounded-lg bg-white overflow-hidden min-h-0">
        
        {/* Left Rail: FILTERS Cascading panel */}
        <div className="w-full md:w-60 max-[1440px]:md:w-52 shrink-0 border-b md:border-b-0 md:border-r border-slate-200 bg-slate-50/50 p-2 flex flex-col gap-2.5 overflow-y-auto custom-scrollbar min-h-0 text-xs font-semibold">
          <div className="px-2 py-1 text-xxs font-bold text-slate-400 uppercase tracking-wider select-none">
            {t(LEARNER_LABELS.filters)}
          </div>

          {/* Division Select */}
          <div className="space-y-1">
            <label className="block text-xxs font-bold text-slate-400 uppercase">{t(LEARNER_LABELS.division)}</label>
            <div className="relative">
              <select
                value={selectedDiv}
                onChange={e => { setSelectedDiv(e.target.value); setPageIndex(0) }}
                className="w-full appearance-none px-2 py-1.5 pr-7 border border-slate-200 rounded bg-white text-slate-700 focus:outline-none focus:border-indigo-500 cursor-pointer transition text-xs font-medium"
              >
                <option value="">{t(LEARNER_LABELS.allDivisions)}</option>
                {divisions.map(d => <option key={d} value={d}>{d}</option>)}
              </select>
              <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400 pointer-events-none" />
            </div>
          </div>

          {/* Department Select */}
          <div className="space-y-1">
            <label className="block text-xxs font-bold text-slate-400 uppercase">{t(LEARNER_LABELS.department)}</label>
            <div className="relative">
              <select
                value={selectedDept}
                onChange={e => { setSelectedDept(e.target.value); setPageIndex(0) }}
                disabled={!selectedDiv}
                className="w-full appearance-none px-2 py-1.5 pr-7 border border-slate-200 rounded bg-white text-slate-700 focus:outline-none focus:border-indigo-500 disabled:bg-slate-50 disabled:text-slate-400 cursor-pointer disabled:cursor-not-allowed transition text-xs font-medium"
              >
                <option value="">{selectedDiv ? t(LEARNER_LABELS.allDepartments) : t(LEARNER_LABELS.selectDivisionFirst)}</option>
                {departments.map(d => <option key={d} value={d}>{d}</option>)}
              </select>
              <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400 pointer-events-none" />
            </div>
          </div>

          {/* Section Select */}
          <div className="space-y-1">
            <label className="block text-xxs font-bold text-slate-400 uppercase">{t(LEARNER_LABELS.section)}</label>
            <div className="relative">
              <select
                value={selectedSec}
                onChange={e => { setSelectedSec(e.target.value); setPageIndex(0) }}
                disabled={!selectedDept}
                className="w-full appearance-none px-2 py-1.5 pr-7 border border-slate-200 rounded bg-white text-slate-700 focus:outline-none focus:border-indigo-500 disabled:bg-slate-50 disabled:text-slate-400 cursor-pointer disabled:cursor-not-allowed transition text-xs font-medium"
              >
                <option value="">{selectedDept ? t(LEARNER_LABELS.allSections) : t(LEARNER_LABELS.selectDepartmentFirst)}</option>
                {sections.map(s => <option key={s} value={s}>{s}</option>)}
              </select>
              <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400 pointer-events-none" />
            </div>
          </div>

          {/* Position Select */}
          <div className="space-y-1">
            <label className="block text-xxs font-bold text-slate-400 uppercase">{t(LEARNER_LABELS.position)}</label>
            <div className="relative">
              <select
                value={selectedPos}
                onChange={e => { setSelectedPos(e.target.value); setPageIndex(0) }}
                className="w-full appearance-none px-2 py-1.5 pr-7 border border-slate-200 rounded bg-white text-slate-700 focus:outline-none focus:border-indigo-500 cursor-pointer transition text-xs font-medium"
              >
                <option value="">{t(LEARNER_LABELS.allPositions)}</option>
                {positions.map(p => <option key={p} value={p}>{p}</option>)}
              </select>
              <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400 pointer-events-none" />
            </div>
          </div>

          <button
            type="button"
            onClick={handleClearFilters}
            className="mt-1 w-full py-1.5 border border-slate-200 hover:bg-slate-100 hover:text-slate-800 text-slate-600 font-bold rounded flex items-center justify-center gap-1.5 transition cursor-pointer text-xs"
          >
            <RotateCcw className="h-3 w-3" />
            <span>{t(LEARNER_LABELS.clearFilters)}</span>
          </button>
        </div>

        {/* Right Area: Search Bar & Table Grid */}
        <div className="flex-1 min-w-0 flex flex-col min-h-0 bg-white">
          
          <div className="p-2 border-b border-slate-100 flex items-center gap-2 shrink-0 select-none">
            {headerLeft}

            {/* Global Search Box */}
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400 pointer-events-none" />
              <input
                type="text"
                placeholder={t(LEARNER_LABELS.searchNameOrEid)}
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                className="w-full pl-9 pr-8 py-1.5 border border-slate-200 rounded-md text-xs font-semibold placeholder:text-slate-400 bg-white focus:outline-none focus:border-indigo-500"
              />
              {searchTerm && (
                <button
                  type="button"
                  onClick={() => setSearchTerm('')}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 p-0.5 rounded-full hover:bg-slate-100 transition"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>

            <Badge tone="neutral">{totalCount}</Badge>
          </div>

          {/* Active-filter chips conditional second row */}
          {(selectedDiv || selectedDept || selectedSec || selectedPos) && (
            <div className="px-2 py-1.5 border-b border-slate-100 flex flex-wrap items-center gap-1.5 bg-slate-50/30 text-xs shrink-0 select-none">
              <span className="text-[10px] font-bold text-slate-400 uppercase">{t(LEARNER_LABELS.filters)}</span>
              {selectedDiv && (
                <span className="inline-flex items-center gap-1 bg-indigo-50/50 text-indigo-700 border border-indigo-100 px-2 py-0.5 rounded text-[11px] font-semibold">
                  <span>Div: {selectedDiv}</span>
                  <button
                    type="button"
                    onClick={() => { setSelectedDiv(''); setSelectedDept(''); setSelectedSec(''); setPageIndex(0) }}
                    className="hover:text-red-500 transition cursor-pointer"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </span>
              )}
              {selectedDept && (
                <span className="inline-flex items-center gap-1 bg-indigo-50/50 text-indigo-700 border border-indigo-100 px-2 py-0.5 rounded text-[11px] font-semibold">
                  <span>Dept: {selectedDept}</span>
                  <button
                    type="button"
                    onClick={() => { setSelectedDept(''); setSelectedSec(''); setPageIndex(0) }}
                    className="hover:text-red-500 transition cursor-pointer"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </span>
              )}
              {selectedSec && (
                <span className="inline-flex items-center gap-1 bg-indigo-50/50 text-indigo-700 border border-indigo-100 px-2 py-0.5 rounded text-[11px] font-semibold">
                  <span>Sec: {selectedSec}</span>
                  <button
                    type="button"
                    onClick={() => { setSelectedSec(''); setPageIndex(0) }}
                    className="hover:text-red-500 transition cursor-pointer"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </span>
              )}
              {selectedPos && (
                <span className="inline-flex items-center gap-1 bg-indigo-50/50 text-indigo-700 border border-indigo-100 px-2 py-0.5 rounded text-[11px] font-semibold">
                  <span>Pos: {selectedPos}</span>
                  <button
                    type="button"
                    onClick={() => { setSelectedPos(''); setPageIndex(0) }}
                    className="hover:text-red-500 transition cursor-pointer"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </span>
              )}
            </div>
          )}

          {isPageAllSelected && totalCount > learners.length && (
            <div className="px-4 py-2 bg-indigo-50 text-indigo-700 border-b border-indigo-100 flex items-center justify-between text-xs shrink-0 select-none animate-fade-in">
              <span>
                {tf(LEARNER_LABELS.selectedAllPage, learners.length)}
              </span>
              <button
                type="button"
                onClick={handleSelectAllFiltered}
                className="font-bold underline hover:text-indigo-900 cursor-pointer flex items-center gap-1"
              >
                {tf(LEARNER_LABELS.selectAllMatching, totalCount)}
              </button>
            </div>
          )}

          <div className="relative w-full h-0.5 bg-slate-100 overflow-hidden shrink-0 z-10">
            {loading && (
              <div className="absolute top-0 bottom-0 left-0 w-full bg-indigo-600 rounded animate-pulse" />
            )}
          </div>

          {/* Scrollable table viewport */}
          <div 
            onScroll={handleScroll}
            className="flex-1 overflow-auto custom-scrollbar relative min-h-0"
          >
            <table className="min-w-full table-fixed divide-y divide-slate-100 text-left text-xs font-semibold text-slate-700">
              <thead className="bg-slate-50/80 backdrop-blur-xs sticky top-0 z-10 border-b border-slate-200 shadow-3xs">
                <tr className="text-xxs font-extrabold text-slate-500 uppercase tracking-wider border-b border-slate-200 select-none">
                  <th className="px-3 py-2 short:py-1.5 w-12 text-center">
                    <input
                      type="checkbox"
                      checked={isPageAllSelected}
                      onChange={handleToggleSelectAll}
                      className="h-4 w-4 text-indigo-500 rounded border-slate-300 focus:ring-indigo-400 cursor-pointer"
                    />
                  </th>
                  <th className="px-3 py-2 short:py-1.5 w-28">{t(LEARNER_LABELS.employeeId)}</th>
                  <th className="px-3 py-2 short:py-1.5">{t(LEARNER_LABELS.learnerName)}</th>
                  <th className="px-3 py-2 short:py-1.5 w-36">{t(LEARNER_LABELS.position)}</th>
                  <th className="px-3 py-2 short:py-1.5 w-32">{t(LEARNER_LABELS.department)}</th>
                  <th className="px-3 py-2 short:py-1.5 w-36">{t(LEARNER_LABELS.section)}</th>
                </tr>
              </thead>
              
              <tbody className="divide-y divide-slate-100 bg-white relative">
                {learners.length === 0 && !loading ? (
                  <tr>
                    <td colSpan={6} className="p-12 text-center text-slate-400 font-semibold">
                      {t(LEARNER_LABELS.noMatchingLearners)}
                    </td>
                  </tr>
                ) : learners.map(l => {
                    const isChecked = selectedLearners.some(x => x.code === l.code)
                    return (
                      <tr
                        key={l.code}
                        onClick={() => handleToggleRow(l)}
                        className={`hover:bg-slate-50/70 border-b border-slate-100/50 transition duration-150 cursor-pointer select-none ${
                          isChecked ? 'bg-indigo-50/30' : ''
                        }`}
                      >
                        <td className="px-3 py-2 short:py-1.5 w-12 text-center" onClick={e => e.stopPropagation()}>
                          <input
                            type="checkbox"
                            checked={isChecked}
                            onChange={() => handleToggleRow(l)}
                            className="h-4 w-4 text-indigo-500 rounded border-slate-300 focus:ring-indigo-400 cursor-pointer transition"
                          />
                        </td>
                        <td className="px-3 py-2 short:py-1.5 w-28 font-mono font-bold text-slate-800 truncate">{l.code}</td>
                        <td className="px-3 py-2 short:py-1.5 font-semibold text-slate-900 truncate">
                          <div className="flex items-center gap-3">
                            <div className="h-7 w-7 rounded-full bg-slate-100 border border-slate-200 flex items-center justify-center text-slate-600 text-xxs font-extrabold uppercase shrink-0 shadow-3xs select-none">
                              {getInitials(l.name)}
                            </div>
                            <span className="truncate">{l.name || '—'}</span>
                          </div>
                        </td>
                        <td className="px-3 py-2 short:py-1.5 w-36 text-slate-500 font-semibold text-xs truncate">{l.position || '—'}</td>
                        <td className="px-3 py-2 short:py-1.5 w-32 text-slate-500 font-semibold text-xs truncate">{l.department || '—'}</td>
                        <td className="px-3 py-2 short:py-1.5 w-36 text-slate-400 font-semibold text-xs truncate">{l.section || '—'}</td>
                      </tr>
                    )
                  })}
              </tbody>
            </table>
          </div>

          <div className="p-2.5 short:p-2 border-t border-slate-100 bg-slate-50/60 flex justify-end items-center text-slate-500 text-xs font-semibold select-none shrink-0">
            <div className="flex items-center gap-3.5 select-none">
              <div className="flex items-center gap-2">
                <Badge tone="neutral" variant="soft" size="xxs">
                  {tf(LEARNER_LABELS.selected, selectedLearners.length)}
                </Badge>
                
                {selectedLearners.length > 0 && (
                  <>
                    <button
                      type="button"
                      onClick={() => setLedgerOpen(true)}
                      className="text-indigo-600 hover:text-indigo-800 font-extrabold cursor-pointer text-xxs uppercase tracking-wider transition border border-indigo-200 hover:border-indigo-300 px-2 py-0.5 rounded bg-white shadow-3xs"
                    >
                      {t(LEARNER_LABELS.review)}
                    </button>
                    <button
                      type="button"
                      onClick={handleClearAll}
                      className="text-red-500 hover:text-red-700 font-extrabold cursor-pointer text-xxs uppercase tracking-wider transition border border-red-200 hover:border-red-300 px-2 py-0.5 rounded bg-white shadow-3xs"
                    >
                      {t(LEARNER_LABELS.clear)}
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>

        </div>
      </div>

      <Modal
        open={ledgerOpen}
        onClose={() => setLedgerOpen(false)}
        size="lg"
        title={tf(LEARNER_LABELS.selectedLearners, selectedLearners.length)}
      >
        <div className="p-6 flex flex-col gap-4 select-none">
          <div className="flex justify-between items-center shrink-0">
            <span className="text-xs font-semibold text-slate-500">
              {t(LEARNER_LABELS.reviewSelections)}
            </span>
            {selectedLearners.length > 0 && (
              <button
                type="button"
                onClick={handleClearAll}
                className="text-red-500 hover:text-red-700 font-extrabold transition cursor-pointer flex items-center gap-1 text-xs"
              >
                <span>{t(LEARNER_LABELS.clearSelection)}</span>
              </button>
            )}
          </div>

          {selectedLearners.length > 5 && (
            <div className="relative w-full max-w-sm shrink-0">
              <Search className="absolute left-3 top-2 h-4 w-4 text-slate-400 pointer-events-none" />
              <input
                type="text"
                placeholder={t(LEARNER_LABELS.searchSelectedLearners)}
                value={selectedSearch}
                onChange={e => setSelectedSearch(e.target.value)}
                className="w-full pl-9 pr-3 py-1.5 border border-slate-200 rounded-md text-xs font-semibold placeholder:text-slate-400 bg-white focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition"
              />
            </div>
          )}

          <div className="flex flex-wrap gap-2 max-h-[55vh] overflow-y-auto custom-scrollbar pr-1">
            {filteredChips.length === 0 ? (
              <span className="text-slate-400 text-xs font-semibold py-4 w-full text-center">
                {selectedSearch ? tf(LEARNER_LABELS.noSelectedItemsMatch, selectedSearch) : t(LEARNER_LABELS.noLearnersSelected)}
              </span>
            ) : (
              filteredChips.map(learner => (
                <span
                  key={learner.code}
                  className="inline-flex items-center gap-1.5 bg-indigo-50 text-blue-700 border border-blue-200 px-3 py-1 text-xs font-semibold rounded-full hover:bg-blue-100/70 transition shadow-3xs"
                >
                  <span>{learner.name === learner.code ? learner.code : `${learner.name} (${learner.code})`}</span>
                  <button
                    type="button"
                    onClick={() => handleRemoveChip(learner.code)}
                    className="text-blue-500 hover:text-blue-700 focus:outline-none flex items-center justify-center rounded-full hover:bg-blue-200/40 p-0.5 cursor-pointer"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </span>
              ))
            )}
          </div>
        </div>
      </Modal>
    </>
  )
}

