import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, ArrowRight, Check, Plus, RefreshCw, Save, Search, Users, X } from 'lucide-react'

import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

type LoadResult<T> = T[] | { data?: T[] }

type GroupCategoryLookup = {
  id: number
  name: string
}

type GroupFormData = {
  name: string
  description: string
  categoryId: number
}

type LearnerSelection = {
  code: string
  name: string
  division?: string
  department?: string
  section?: string
  position?: string
}

type GroupApiResponse<T = GroupFormData & { id: number }> = {
  success: boolean
  message?: string
  data?: T
}

const stepLabels = ['Information', 'Members', 'Review']

function unwrapList<T>(value: LoadResult<T> | undefined): T[] {
  if (!value) return []
  return Array.isArray(value) ? value : value.data ?? []
}

function parseLearnerCodes(value: string) {
  return Array.from(new Set(
    value
      .split(/[\n,;\s]+/)
      .map(code => code.trim())
      .filter(Boolean)
      .map(code => code.toUpperCase())
  ))
}

function getApiErrorText(error: unknown, fallback: string) {
  if (error instanceof Error && error.message) return error.message
  return fallback
}

export function LearnerGroupEditorPage() {
  const { id } = useParams()
  const isEditMode = !!id
  const navigate = useNavigate()

  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [currentStep, setCurrentStep] = useState(1)
  const [memberInput, setMemberInput] = useState('')
  const [categories, setCategories] = useState<GroupCategoryLookup[]>([])
  
  // Selection states
  const [selectedLearners, setSelectedLearners] = useState<LearnerSelection[]>([])
  const [activeTab, setActiveTab] = useState<'picker' | 'bulk'>('picker')
  const [searchQuery, setSearchQuery] = useState('')
  const [searching, setSearching] = useState(false)
  const [searchResults, setSearchResults] = useState<LearnerSelection[]>([])

  const [formData, setFormData] = useState<GroupFormData>({
    name: '',
    description: '',
    categoryId: 0
  })

  const selectedLearnerCodes = useMemo(() => (
    selectedLearners.map(l => l.code)
  ), [selectedLearners])

  const selectedCategoryName = useMemo(() => (
    categories.find(category => category.id === formData.categoryId)?.name || 'No category'
  ), [categories, formData.categoryId])

  const loadCategories = useCallback(async () => {
    try {
      const response = await fetchWithAccessControl<LoadResult<GroupCategoryLookup>>('LearnerGroupCategories')
      setCategories(unwrapList(response))
    } catch (error) {
      console.error(error)
      toast.error('Failed to load group categories')
    }
  }, [])

  const loadGroupDetails = useCallback(async () => {
    if (!id) return
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<GroupApiResponse<GroupFormData & { id: number }>>(`LearnerGroups/${id}`)
      if (resp.success && resp.data) {
        setFormData({
          name: resp.data.name,
          description: resp.data.description || '',
          categoryId: resp.data.categoryId || 0
        })
      }
    } catch (error) {
      console.error(error)
      toast.error('Failed to load group details')
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadCategories()
    }, 0)

    return () => window.clearTimeout(timeoutId)
  }, [loadCategories])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadGroupDetails()
    }, 0)

    return () => window.clearTimeout(timeoutId)
  }, [loadGroupDetails])

  const handleChange = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = event.target
    setFormData(prev => ({
      ...prev,
      [name]: name === 'categoryId' ? Number(value) : value
    }))
  }

  const validateInformation = () => {
    if (!formData.name.trim()) {
      toast.error('Group Name is required')
      return false
    }
    if (!formData.description.trim()) {
      toast.error('Group Description is required')
      return false
    }

    return true
  }

  const goNext = () => {
    if (currentStep === 1 && !validateInformation()) return
    setCurrentStep(prev => Math.min(stepLabels.length, prev + 1))
  }

  // Learner search directory handler
  const handleSearch = async () => {
    const query = searchQuery.trim()
    if (!query) {
      toast.error('Please enter a name or NID to search')
      return
    }

    setSearching(true)
    try {
      // Build a compound filter for DevExtreme
      const filter = [
        ['name', 'contains', query],
        'or',
        ['code', 'contains', query]
      ]
      const url = `Learners/Get?take=40&filter=${encodeURIComponent(JSON.stringify(filter))}`
      const response = await fetchWithAccessControl<any>(url)

      let list: any[] = []
      if (response) {
        if (Array.isArray(response)) {
          list = response
        } else if (response.data && Array.isArray(response.data)) {
          list = response.data
        }
      }

      const formattedResults = list.map(item => {
        const code = String(item.code || item.eId || item.eid || item.nid || item.EId || '').trim()
        const name = String(item.name || item.fullName || (item.englishFirstName ? `${item.englishFirstName} ${item.englishLastName || ''}`.trim() : '') || item.thaiFirstName || '').trim()
        const division = item.division || ''
        const department = item.department || ''
        const section = item.section || ''
        const position = item.position || ''

        return { code, name, division, department, section, position }
      }).filter(item => item.code)

      setSearchResults(formattedResults)
      if (formattedResults.length === 0) {
        toast.info('No learners found matching search criteria')
      }
    } catch (error) {
      console.error(error)
      toast.error('Failed to search learners directory')
    } finally {
      setSearching(false)
    }
  }

  const addLearner = (learner: LearnerSelection) => {
    setSelectedLearners(prev => {
      if (prev.some(item => item.code === learner.code)) {
        toast.warning(`${learner.name} is already selected`)
        return prev
      }
      return [...prev, learner]
    })
  }

  const addMemberCodes = () => {
    const parsedCodes = parseLearnerCodes(memberInput)
    if (parsedCodes.length === 0) {
      toast.error('Enter at least one learner code')
      return
    }

    const newSelections = parsedCodes.map(code => ({
      code,
      name: code, // default to code since we don't have name
      division: '',
      department: ''
    }))

    setSelectedLearners(prev => {
      const existingCodes = new Set(prev.map(l => l.code))
      const uniqueNew = newSelections.filter(l => !existingCodes.has(l.code))
      return [...prev, ...uniqueNew]
    })
    setMemberInput('')
    toast.success(`Imported ${parsedCodes.length} learner code(s)`)
  }

  const removeMemberCode = (code: string) => {
    setSelectedLearners(prev => prev.filter(item => item.code !== code))
  }

  const buildPayload = () => ({
    name: formData.name.trim(),
    description: formData.description.trim(),
    categoryId: formData.categoryId > 0 ? formData.categoryId : null,
    learnerCodes: selectedLearnerCodes
  })

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!validateInformation()) return

    setSaving(true)
    try {
      if (isEditMode) {
        const resp = await fetchWithAccessControl<GroupApiResponse>(`LearnerGroups/${id}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            name: formData.name.trim(),
            description: formData.description.trim(),
            categoryId: formData.categoryId > 0 ? formData.categoryId : null
          })
        })
        if (resp.success) {
          toast.success(resp.message || 'Group updated successfully')
          navigate(`/learner-groups/${id}`)
        }
        return
      }

      const resp = await fetchWithAccessControl<GroupApiResponse>('LearnerGroups', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(buildPayload())
      })

      if (resp.success && resp.data) {
        toast.success(resp.message || 'Learner group registered successfully')
        navigate(`/learner-groups/${resp.data.id}`)
      }
    } catch (error: unknown) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to save learner group details'))
    } finally {
      setSaving(false)
    }
  }

  const renderStepButton = (label: string, index: number) => {
    const step = index + 1
    const isActive = currentStep === step
    const isComplete = currentStep > step

    return (
      <button
        key={label}
        type="button"
        onClick={() => {
          if (step <= currentStep || validateInformation()) {
            setCurrentStep(step)
          }
        }}
        className={`flex min-w-31 items-center gap-2 border px-3 py-2 text-left text-xs font-bold ${isActive ? 'border-blue-500 bg-blue-50 text-blue-700' : isComplete ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-slate-200 bg-white text-slate-500'}`}
        aria-current={isActive ? 'step' : undefined}
      >
        <span className="flex h-5 w-5 items-center justify-center rounded-sm border border-current text-xxs">{step}</span>
        <span>{label}</span>
      </button>
    )
  }

  const renderInformationStep = () => (
    <div className="admin-card p-5">
      <div className="mb-4 flex items-center gap-2 border-b border-slate-100 pb-3">
        <Users className="h-5 w-5 text-blue-600" />
        <h2 className="text-sm font-bold text-slate-800">Group Information</h2>
      </div>

      <div className="space-y-1.5">
        <label htmlFor="name" className="block text-xs font-bold text-slate-500 uppercase">
          Group Name <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          id="name"
          name="name"
          value={formData.name}
          onChange={handleChange}
          placeholder="e.g. New Hires 2026 Q1"
          className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
        />
      </div>

      <div className="mt-5 space-y-1.5">
        <label htmlFor="categoryId" className="block text-xs font-bold text-slate-500 uppercase">Category</label>
        <select
          id="categoryId"
          name="categoryId"
          value={formData.categoryId}
          onChange={handleChange}
          className="w-full max-w-lg rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
        >
          <option value={0}>No category (root)</option>
          {categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
        </select>
      </div>

      <div className="mt-5 space-y-1.5">
        <label htmlFor="description" className="block text-xs font-bold text-slate-500 uppercase">
          Description <span className="text-red-500">*</span>
        </label>
        <textarea
          id="description"
          name="description"
          value={formData.description}
          onChange={handleChange}
          rows={5}
          placeholder="Brief description of this group's purpose"
          className="w-full resize-y rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
        />
      </div>
    </div>
  )

  const renderMembersStep = () => (
    <div className="min-h-0 flex flex-col gap-4">
      <div className="admin-card p-5">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-3">
          <div className="flex items-center gap-2">
            <Users className="h-5 w-5 text-blue-600" />
            <h2 className="text-sm font-bold text-slate-800">Add Group Members</h2>
          </div>
          
          <div className="flex items-center gap-4 bg-slate-50 p-1.5 rounded border border-slate-100">
            <button
              type="button"
              onClick={() => setActiveTab('picker')}
              className={`px-3 py-1 text-center text-xs font-bold rounded transition ${
                activeTab === 'picker' ? 'bg-white text-blue-700 shadow-xs' : 'text-slate-500 hover:text-slate-850'
              }`}
            >
              Directory Search
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('bulk')}
              className={`px-3 py-1 text-center text-xs font-bold rounded transition ${
                activeTab === 'bulk' ? 'bg-white text-blue-700 shadow-xs' : 'text-slate-500 hover:text-slate-850'
              }`}
            >
              Bulk Import (NIDs)
            </button>
          </div>

          <span className="border border-blue-200 bg-blue-50 px-2 py-1 text-xs font-bold text-blue-700 rounded-sm">
            {selectedLearners.length} selected
          </span>
        </div>

        {activeTab === 'picker' ? (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 min-h-[420px]">
            {/* Left Column: Search & Directory */}
            <div className="flex flex-col border border-slate-200 rounded bg-white min-h-0">
              <div className="p-3 bg-slate-50 border-b border-slate-200 flex items-center justify-between shrink-0">
                <span className="font-bold text-xs text-slate-600 uppercase tracking-wider">Learner Directory</span>
              </div>
              
              <div className="p-3 border-b border-slate-100 flex gap-2 shrink-0">
                <div className="relative flex-1">
                  <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-slate-400" />
                  <input
                    type="text"
                    placeholder="Search by name or NID code..."
                    value={searchQuery}
                    onChange={e => setSearchQuery(e.target.value)}
                    onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); void handleSearch(); } }}
                    className="w-full pl-9 pr-3 py-2 text-xs border border-slate-200 rounded bg-white focus:outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-500"
                  />
                </div>
                <button
                  type="button"
                  onClick={handleSearch}
                  disabled={searching}
                  className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded text-xs font-bold transition disabled:opacity-55 flex items-center gap-1.5"
                >
                  {searching ? <RefreshCw className="h-3.5 w-3.5 animate-spin" /> : <Search className="h-3.5 w-3.5" />}
                  <span>Search</span>
                </button>
              </div>
              
              <div className="flex-1 overflow-y-auto custom-scrollbar p-3 space-y-2 max-h-96">
                {searchResults.length === 0 ? (
                  <div className="flex h-full items-center justify-center text-xs font-semibold text-slate-400 py-12 text-center">
                    Enter a search query to search available learners.
                  </div>
                ) : (
                  searchResults.map(learner => {
                    const isAdded = selectedLearnerCodes.includes(learner.code)
                    return (
                      <div
                        key={learner.code}
                        className={`p-3 rounded border transition flex items-center justify-between ${
                          isAdded ? 'border-emerald-100 bg-emerald-50/20' : 'border-slate-200 hover:border-blue-400 hover:bg-slate-50/50'
                        }`}
                      >
                        <div className="flex flex-col min-w-0 pr-2">
                          <span className="text-slate-800 font-bold text-sm leading-tight truncate">{learner.name}</span>
                          <div className="flex items-center gap-2 mt-0.5">
                            <span className="text-slate-400 font-mono text-xxs font-bold">{learner.code}</span>
                            {learner.division && (
                              <span className="text-slate-400 text-xxs truncate">
                                • {learner.division} {learner.department ? `/ ${learner.department}` : ''}
                              </span>
                            )}
                          </div>
                        </div>
                        <button
                          type="button"
                          disabled={isAdded}
                          onClick={() => addLearner(learner)}
                          className={`h-7 px-2.5 rounded text-xs font-bold flex items-center gap-1 transition ${
                            isAdded
                              ? 'bg-emerald-100 text-emerald-700 cursor-default border border-transparent'
                              : 'bg-blue-50 hover:bg-blue-100 text-blue-700 border border-blue-200'
                          }`}
                        >
                          {isAdded ? <Check className="h-3 w-3" /> : <Plus className="h-3 w-3" />}
                          <span>{isAdded ? 'Added' : 'Select'}</span>
                        </button>
                      </div>
                    )
                  })
                )}
              </div>
            </div>

            {/* Right Column: Selected list */}
            <div className="flex flex-col border border-slate-200 rounded bg-white min-h-0">
              <div className="p-3 bg-slate-50 border-b border-slate-200 flex items-center justify-between shrink-0">
                <span className="font-bold text-xs text-slate-600 uppercase tracking-wider">Group Scope ({selectedLearners.length})</span>
                {selectedLearners.length > 0 && (
                  <button
                    type="button"
                    onClick={() => setSelectedLearners([])}
                    className="text-xxs font-bold text-red-600 hover:text-red-700 cursor-pointer"
                  >
                    Clear All
                  </button>
                )}
              </div>
              
              <div className="flex-1 overflow-y-auto custom-scrollbar p-3 space-y-2 max-h-[400px]">
                {selectedLearners.length === 0 ? (
                  <div className="flex h-full items-center justify-center text-xs font-semibold text-slate-400 py-12 text-center">
                    No members selected yet. Add learners from the directory list on the left.
                  </div>
                ) : (
                  selectedLearners.map(learner => (
                    <div
                      key={learner.code}
                      className="p-3 rounded border border-blue-100 bg-blue-50/5 flex items-center justify-between"
                    >
                      <div className="flex flex-col min-w-0 pr-2">
                        <span className="text-slate-800 font-bold text-sm leading-tight truncate">
                          {learner.name === learner.code ? `Learner: ${learner.code}` : learner.name}
                        </span>
                        <div className="flex items-center gap-2 mt-0.5">
                          <span className="text-slate-400 font-mono text-xxs font-bold">{learner.code}</span>
                          {learner.division && (
                            <span className="text-slate-400 text-xxs truncate">• {learner.division}</span>
                          )}
                        </div>
                      </div>
                      <button
                        type="button"
                        onClick={() => removeMemberCode(learner.code)}
                        className="h-6 w-6 shrink-0 rounded border border-red-150 text-red-650 flex items-center justify-center hover:bg-red-50 hover:border-red-300 transition cursor-pointer"
                        aria-label="Remove learner"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        ) : (
          <div className="space-y-4">
            <p className="text-xs font-medium text-slate-500">
              Bulk paste standard employee NIDs/Codes here. They will be integrated directly into your selection workspace.
            </p>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-[minmax(0,1fr)_auto]">
              <textarea
                value={memberInput}
                onChange={event => setMemberInput(event.target.value)}
                rows={7}
                placeholder="Paste learner codes separated by comma, space, or new line"
                className="w-full resize-y rounded border border-slate-200 bg-white px-3 py-2 font-mono text-sm text-slate-800 focus:border-blue-600 focus:outline-none bg-slate-50/50"
              />
              <button
                type="button"
                onClick={addMemberCodes}
                disabled={!memberInput.trim()}
                className="admin-button admin-button--primary self-start shadow-xs disabled:opacity-55"
              >
                <Plus aria-hidden="true" />
                <span>Import Codes</span>
              </button>
            </div>

            {/* List of currently selected ones for preview */}
            <div className="mt-4 border border-slate-200 rounded overflow-hidden">
              <div className="bg-slate-50 px-3 py-2 border-b border-slate-200 flex justify-between items-center text-xs font-bold text-slate-500 uppercase">
                <span>Selected Codes Ledger</span>
                <span>{selectedLearners.length} Users</span>
              </div>
              <div className="max-h-60 overflow-y-auto custom-scrollbar bg-white divide-y divide-slate-100">
                {selectedLearners.length === 0 ? (
                  <div className="px-3 py-6 text-center text-slate-400 text-xs font-semibold">No initial members selected</div>
                ) : (
                  selectedLearners.map((learner, index) => (
                    <div key={learner.code} className="px-4 py-2 text-xs flex items-center justify-between font-medium">
                      <div className="flex gap-4 items-center">
                        <span className="font-bold text-slate-400 w-8">{index + 1}</span>
                        <span className="font-mono text-slate-800 font-semibold">{learner.code}</span>
                        {learner.name !== learner.code && (
                          <span className="text-slate-500 truncate">({learner.name})</span>
                        )}
                      </div>
                      <button
                        type="button"
                        onClick={() => removeMemberCode(learner.code)}
                        className="text-red-500 hover:text-red-700 font-semibold"
                      >
                        Remove
                      </button>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )

  const renderReviewStep = () => (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="admin-card p-4">
          <div className="text-xl font-bold text-slate-800">{selectedLearners.length}</div>
          <div className="text-xs font-bold uppercase text-slate-500">Initial Members</div>
        </div>
        <div className="admin-card p-4 md:col-span-2">
          <div className="text-xl font-bold text-slate-800">{selectedCategoryName}</div>
          <div className="text-xs font-bold uppercase text-slate-500">Category</div>
        </div>
      </div>

      <div className="admin-card p-5">
        <div className="mb-3 text-sm font-bold text-slate-800">Group Details</div>
        <dl className="grid grid-cols-1 gap-x-5 gap-y-3 md:grid-cols-2">
          <div className="border-b border-slate-100 pb-2 md:col-span-2">
            <dt className="text-xs font-bold uppercase text-slate-500">Group Name</dt>
            <dd className="mt-1 font-semibold text-slate-800">{formData.name || 'Not set'}</dd>
          </div>
          <div className="border-b border-slate-100 pb-2 md:col-span-2">
            <dt className="text-xs font-bold uppercase text-slate-500">Description</dt>
            <dd className="mt-1 font-semibold text-slate-800">{formData.description || 'Not set'}</dd>
          </div>
        </dl>
      </div>

      <div className="admin-card p-5">
        <div className="mb-3 text-sm font-bold text-slate-800">Initial Members</div>
        <div className="flex flex-wrap gap-2">
          {selectedLearners.length === 0 ? (
            <span className="font-semibold text-slate-400">No initial members selected</span>
          ) : selectedLearners.map(learner => (
            <span
              key={learner.code}
              className="border border-slate-200 bg-white px-2 py-1 font-mono text-sm font-semibold text-slate-700 rounded-sm"
              title={learner.name}
            >
              {learner.code}
            </span>
          ))}
        </div>
      </div>
    </div>
  )

  if (loading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <RefreshCw className="h-8 w-8 animate-spin text-blue-600" />
      </div>
    )
  }

  if (isEditMode) {
    return (
      <form onSubmit={handleSubmit} className="max-w-3xl space-y-4">
        <div className="flex items-center justify-between gap-3">
          <h1 className="text-xl font-extrabold text-slate-800">Edit Learner Group</h1>
        </div>
        {renderInformationStep()}
        <div className="flex items-center justify-end gap-3 border-t border-slate-200 pt-3">
          <button type="button" onClick={() => navigate(`/learner-groups/${id}`)} className="admin-button admin-button--secondary">
            <X aria-hidden="true" />
            <span>Cancel</span>
          </button>
          <button type="submit" disabled={saving} className="admin-button admin-button--primary disabled:opacity-55">
            {saving ? <RefreshCw className="animate-spin" aria-hidden="true" /> : <Save aria-hidden="true" />}
            <span>Save Changes</span>
          </button>
        </div>
      </form>
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-extrabold text-slate-800">New Learner Group</h1>
          <p className="text-sm font-medium text-slate-500">Create the group, add optional initial members, then review before saving.</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {stepLabels.map(renderStepButton)}
        </div>
      </div>

      <div className="min-h-0 flex-1">
        {currentStep === 1 ? renderInformationStep() : null}
        {currentStep === 2 ? renderMembersStep() : null}
        {currentStep === 3 ? renderReviewStep() : null}
      </div>

      <div className="flex items-center justify-end gap-3 border-t border-slate-200 pt-3">
        <button type="button" onClick={() => navigate('/learner-groups')} className="admin-button admin-button--secondary">
          <X aria-hidden="true" />
          <span>Cancel</span>
        </button>

        {currentStep > 1 ? (
          <button type="button" onClick={() => setCurrentStep(prev => Math.max(1, prev - 1))} className="admin-button admin-button--secondary">
            <ArrowLeft aria-hidden="true" />
            <span>Previous</span>
          </button>
        ) : null}

        {currentStep < stepLabels.length ? (
          <button type="button" onClick={event => { event.preventDefault(); event.stopPropagation(); goNext() }} className="admin-button admin-button--primary">
            <ArrowRight aria-hidden="true" />
            <span>Continue</span>
          </button>
        ) : (
          <button type="submit" disabled={saving} className="admin-button admin-button--primary disabled:opacity-55">
            {saving ? <RefreshCw className="animate-spin" aria-hidden="true" /> : <Check aria-hidden="true" />}
            <span>Create Group</span>
          </button>
        )}
      </div>
    </form>
  )
}