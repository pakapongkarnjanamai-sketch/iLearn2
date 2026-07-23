import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Filter, X } from 'lucide-react'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { AppTable } from '../../components/ui/AppTable'
import { createAdminDataSource } from '../../lib/createDataSource'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { LEARNER_LABELS, t } from '../../lib/labels'
import { adminListConfigs } from '../moduleConfigs'

type LookupItem = { name: string }

const config = adminListConfigs.learners

export function LearnerListPage() {
  const navigate = useNavigate()

  // ── Lookup data ──────────────────────────────────────────────────
  const [divisions, setDivisions] = useState<string[]>([])
  const [departments, setDepartments] = useState<string[]>([])
  const [sections, setSections] = useState<string[]>([])

  // ── Selected filters ─────────────────────────────────────────────
  const [division, setDivision] = useState('')
  const [department, setDepartment] = useState('')
  const [section, setSection] = useState('')

  // ── Load lookups ─────────────────────────────────────────────────
  useEffect(() => {
    fetchWithAccessControl<LookupItem[]>('Learners/GetDivisions')
      .then(res => {
        if (Array.isArray(res)) setDivisions(res.map(d => d.name).filter(Boolean).sort())
      })
      .catch(() => { /* swallow — filter just won't populate */ })
  }, [])

  const loadDepartments = useCallback((div: string) => {
    if (!div) { setDepartments([]); return }
    const qs = `?filter=${encodeURIComponent(JSON.stringify(['Division', '=', div]))}`
    fetchWithAccessControl<LookupItem[]>(`Learners/GetDepartments${qs}`)
      .then(res => {
        if (Array.isArray(res)) setDepartments(res.map(d => d.name).filter(Boolean).sort())
      })
      .catch(() => setDepartments([]))
  }, [])

  const loadSections = useCallback((dept: string) => {
    if (!dept) { setSections([]); return }
    const qs = `?filter=${encodeURIComponent(JSON.stringify(['Department', '=', dept]))}`
    fetchWithAccessControl<LookupItem[]>(`Learners/GetSections${qs}`)
      .then(res => {
        if (Array.isArray(res)) setSections(res.map(d => d.name).filter(Boolean).sort())
      })
      .catch(() => setSections([]))
  }, [])

  // Cascade: division → department → section
  const handleDivisionChange = (val: string) => {
    setDivision(val)
    setDepartment('')
    setSection('')
    setSections([])
    loadDepartments(val)
  }

  const handleDepartmentChange = (val: string) => {
    setDepartment(val)
    setSection('')
    loadSections(val)
  }

  const handleClearFilters = () => {
    setDivision('')
    setDepartment('')
    setSection('')
    setDepartments([])
    setSections([])
  }

  // ── Build external filter expression ─────────────────────────────
  const externalFilters = useMemo(() => {
    const parts: unknown[][] = []
    if (division) parts.push(['division', '=', division])
    if (department) parts.push(['department', '=', department])
    if (section) parts.push(['section', '=', section])
    if (parts.length === 0) return []
    if (parts.length === 1) return parts[0]
    // AND-join multiple filters: [f1, "and", f2, "and", f3]
    const combined: unknown[] = []
    parts.forEach((p, i) => {
      combined.push(p)
      if (i < parts.length - 1) combined.push('and')
    })
    return combined
  }, [division, department, section])

  // ── Data source ──────────────────────────────────────────────────
  const store = useMemo(
    () => createAdminDataSource<Record<string, unknown>>({
      controller: config.controller,
      key: config.key,
      basePath: config.basePath,
    }),
    [],
  )

  // ── Row navigation ───────────────────────────────────────────────
  const handleRowDoubleClick = (e: { data: Record<string, unknown> }) => {
    const code = e.data.nid || e.data.eId
    if (code) navigate(`/learners/${code}/profile`)
  }

  const actionButtons = useMemo(() => [{
    hint: t(LEARNER_LABELS.openProfile),
    icon: 'info' as const,
    onClick: (e: { row: { data: Record<string, unknown> } }) => {
      const code = e.row.data.nid || e.row.data.eId
      if (code) navigate(`/learners/${code}/profile`)
    },
  }], [navigate])

  // ── Filter has value? ────────────────────────────────────────────
  const hasActiveFilter = !!(division || department || section)

  // ── Toolbar filter controls ──────────────────────────────────────
  const filterToolbar = (
    <div className="flex flex-wrap items-center gap-2">
      <Filter className="h-3.5 w-3.5 text-slate-400 shrink-0" />

      <select
        value={division}
        onChange={e => handleDivisionChange(e.target.value)}
        className="rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 shadow-3xs transition focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-100"
      >
        <option value="">{t(LEARNER_LABELS.allDivisions)}</option>
        {divisions.map(d => <option key={d} value={d}>{d}</option>)}
      </select>

      <select
        value={department}
        onChange={e => handleDepartmentChange(e.target.value)}
        disabled={!division}
        className="rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 shadow-3xs transition focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-100 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <option value="">{t(LEARNER_LABELS.allDepartments)}</option>
        {departments.map(d => <option key={d} value={d}>{d}</option>)}
      </select>

      <select
        value={section}
        onChange={e => setSection(e.target.value)}
        disabled={!department}
        className="rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 shadow-3xs transition focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-100 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <option value="">{t(LEARNER_LABELS.allSections)}</option>
        {sections.map(s => <option key={s} value={s}>{s}</option>)}
      </select>

      {hasActiveFilter && (
        <button
          type="button"
          onClick={handleClearFilters}
          className="inline-flex items-center gap-1 rounded-md px-2 py-1.5 text-xs font-semibold text-slate-500 transition hover:bg-slate-100 hover:text-slate-700"
          title={t(LEARNER_LABELS.clearAllFilters)}
        >
          <X className="h-3 w-3" />
          {t(LEARNER_LABELS.clear)}
        </button>
      )}
    </div>
  )

  return (
    <DataGridSurface title={t(config.gridTitle)} note={t(config.gridNote)}>
      <AppTable
        store={store}
        columns={config.columns}
        noDataText={t(LEARNER_LABELS.noLearnersFound)}
        onRowDblClick={handleRowDoubleClick}
        searchPlaceholder={t(LEARNER_LABELS.searchNameOrEmployeeId)}
        searchExpr={config.searchExpr}
        externalFilters={externalFilters}
        toolbarContent={filterToolbar}
        actionButtons={actionButtons}
      />
    </DataGridSurface>
  )
}
