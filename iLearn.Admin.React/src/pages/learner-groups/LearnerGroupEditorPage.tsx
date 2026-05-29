import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { 
  Plus, 
  Save, 
  Users, 
  X,
  Loader2
} from 'lucide-react'

import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { LearnerDirectorySelector, type LearnerSelection } from '../../components/shared/LearnerDirectorySelector'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'

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

type GroupApiResponse<T = GroupFormData & { id: number }> = {
  success: boolean
  message?: string
  data?: T
}

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

  const handleSubmit = async (event?: React.FormEvent) => {
    if (event) {
      event.preventDefault()
    }
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

  const renderInformationStep = () => (
    <div className="space-y-3.5">
      <div className="flex items-center gap-2 border-b border-slate-100 pb-2.5 mb-1 select-none">
        <Users className="h-4 w-4 text-indigo-500" />
        <h2 className="text-xs font-bold text-slate-800">Group Information</h2>
      </div>

      <div className="space-y-1">
        <label htmlFor="name" className="block text-xxs font-extrabold text-slate-400 uppercase select-none">
          Group Name <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          id="name"
          name="name"
          value={formData.name}
          onChange={handleChange}
          placeholder="e.g. New Hires 2026 Q1"
          className="w-full rounded border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none"
        />
      </div>

      <div className="space-y-1">
        <label htmlFor="categoryId" className="block text-xxs font-extrabold text-slate-400 uppercase select-none">Category</label>
        <select
          id="categoryId"
          name="categoryId"
          value={formData.categoryId}
          onChange={handleChange}
          className="w-full max-w-md rounded border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none cursor-pointer"
        >
          <option value={0}>No category (root)</option>
          {categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
        </select>
      </div>

      <div className="space-y-1">
        <label htmlFor="description" className="block text-xxs font-extrabold text-slate-400 uppercase select-none">
          Description <span className="text-red-500">*</span>
        </label>
        <textarea
          id="description"
          name="description"
          value={formData.description}
          onChange={handleChange}
          rows={4}
          placeholder="Brief description of this group's purpose"
          className="w-full resize-y rounded border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none"
        />
      </div>
    </div>
  )

  const renderMembersStep = () => (
    <div className="min-h-0 flex-1 flex flex-col gap-3.5">
      <div className="flex flex-col min-h-0 flex-1 gap-3.5">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-2.5 shrink-0 select-none">
          <div className="flex items-center gap-2">
            <Users className="h-4 w-4 text-indigo-500" />
            <h2 className="text-xs font-bold text-slate-800">Add Group Members</h2>
          </div>
          
          <div className="flex items-center gap-2 bg-slate-50 p-1 rounded border border-slate-200 text-xxs">
            <button
              type="button"
              onClick={() => setActiveTab('picker')}
              className={`px-2.5 py-1 text-center font-extrabold rounded transition cursor-pointer ${
                activeTab === 'picker' ? 'bg-white text-blue-700 shadow-3xs' : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              Directory Search
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('bulk')}
              className={`px-2.5 py-1 text-center font-extrabold rounded transition cursor-pointer ${
                activeTab === 'bulk' ? 'bg-white text-blue-700 shadow-3xs' : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              Bulk Import (EIds)
            </button>
          </div>
        </div>

        {activeTab === 'picker' ? (
          <LearnerDirectorySelector selectedLearners={selectedLearners} onChange={setSelectedLearners} />
        ) : (
          <div className="space-y-3.5 flex-1 min-h-0 overflow-y-auto custom-scrollbar pr-1">
            <p className="text-xxs font-semibold text-slate-400 select-none leading-relaxed">
              Bulk paste employee EIds here. They will be integrated directly into your selection workspace.
            </p>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-[minmax(0,1fr)_auto]">
              <textarea
                value={memberInput}
                onChange={event => setMemberInput(event.target.value)}
                rows={5}
                placeholder="Paste employee EIds separated by comma, space, or new line (e.g. N130812, N142715)"
                className="w-full resize-y rounded border border-slate-200 bg-white px-3 py-1.5 font-mono text-xs text-slate-700 focus:border-indigo-500 focus:outline-none bg-slate-50/10"
              />
              <button
                type="button"
                onClick={addMemberCodes}
                disabled={!memberInput.trim()}
                className="inline-flex items-center gap-[7px] rounded-md border border-transparent bg-indigo-600 px-3 py-1.5 font-semibold text-white hover:bg-indigo-700 cursor-pointer self-start text-xxs font-extrabold shadow-3xs disabled:opacity-55"
              >
                <Plus className="h-3.5 w-3.5" aria-hidden="true" />
                <span>Import Codes</span>
              </button>
            </div>

            {/* List of currently selected ones for preview */}
            <div className="border border-slate-200 rounded overflow-hidden">
              <div className="bg-slate-50 px-3 py-1.5 border-b border-slate-200 flex justify-between items-center text-xxs font-extrabold text-slate-400 uppercase select-none">
                <span>Selected Codes Ledger</span>
                <span>{selectedLearners.length} Users</span>
              </div>
              <div className="max-h-56 overflow-y-auto custom-scrollbar bg-white divide-y divide-slate-100">
                {selectedLearners.length === 0 ? (
                  <div className="px-3 py-6 text-center text-slate-400 text-xxs font-bold select-none">No initial members selected</div>
                ) : (
                  selectedLearners.map((learner, index) => (
                    <div key={learner.code} className="px-3 py-1.5 text-xxs flex items-center justify-between font-semibold">
                      <div className="flex gap-3 items-center">
                        <span className="font-extrabold text-slate-400 w-6">{index + 1}</span>
                        <span className="font-mono text-slate-800 font-bold">{learner.code}</span>
                        {learner.name !== learner.code && (
                          <span className="text-slate-400 truncate font-semibold">({learner.name})</span>
                        )}
                      </div>
                      <button
                        type="button"
                        onClick={() => removeMemberCode(learner.code)}
                        className="text-red-500 hover:text-red-700 font-bold transition cursor-pointer"
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
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3 select-none">
        <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
          <div className="text-lg font-extrabold text-slate-800">{selectedLearners.length}</div>
          <div className="text-xxs font-extrabold uppercase text-slate-400">Initial Members</div>
        </div>
        <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4 sm:col-span-2">
          <div className="text-lg font-extrabold text-slate-800">{selectedCategoryName}</div>
          <div className="text-xxs font-extrabold uppercase text-slate-400">Category</div>
        </div>
      </div>

      <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
        <div className="mb-3 text-xs font-bold text-slate-800 select-none">Group Details</div>
        <dl className="grid grid-cols-1 gap-x-5 gap-y-3.5 md:grid-cols-2 text-xs">
          <div className="border-b border-slate-100 pb-2 md:col-span-2">
            <dt className="text-xxs font-extrabold uppercase text-slate-400 select-none">Group Name</dt>
            <dd className="mt-1 font-semibold text-slate-700">{formData.name || 'Not set'}</dd>
          </div>
          <div className="border-b border-slate-100 pb-2 md:col-span-2">
            <dt className="text-xxs font-extrabold uppercase text-slate-400 select-none">Description</dt>
            <dd className="mt-1 font-semibold text-slate-700">{formData.description || 'Not set'}</dd>
          </div>
        </dl>
      </div>

      <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
        <div className="mb-3 text-xs font-bold text-slate-800 select-none">Initial Members</div>
        <div className="flex flex-wrap gap-1.5 max-h-56 overflow-y-auto pr-1 custom-scrollbar">
          {selectedLearners.length === 0 ? (
            <span className="font-semibold text-slate-400 text-xxs select-none">No initial members selected</span>
          ) : selectedLearners.map(learner => (
            <span
              key={learner.code}
              className="border border-slate-200 bg-white px-2 py-0.5 font-mono text-xxs font-extrabold text-slate-700 rounded-sm select-none"
              title={learner.name}
            >
              {learner.code}
            </span>
          ))}
        </div>
      </div>
    </div>
  )

  const steps: WizardStep[] = useMemo(() => [
    { label: 'Information', validate: () => validateInformation(), render: () => renderInformationStep() },
    { label: 'Members', render: () => renderMembersStep() },
    { label: 'Review', render: () => renderReviewStep() }
  ], [formData, selectedLearners, categories, activeTab, memberInput])

  if (loading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <div className="flex flex-col items-center gap-3 select-none">
          <Loader2 className="h-6 w-6 animate-spin text-indigo-500" />
          <span className="text-xs text-slate-500 font-bold">Loading group details...</span>
        </div>
      </div>
    )
  }

  if (isEditMode) {
    return (
      <div className="wizard-surface flex min-h-0 flex-1 flex-col overflow-hidden bg-white pt-5 px-6">
        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col gap-4 max-w-3xl">
          <div className="flex items-center justify-between gap-3 shrink-0">
            <div>
              <div className="text-xxs font-extrabold uppercase tracking-wider text-slate-400 select-none">
                Learner Directory
              </div>
              <h1 className="text-base font-extrabold text-slate-800 tracking-tight leading-tight select-none">
                Edit Learner Group
              </h1>
              <p className="text-xxs font-semibold text-slate-400 mt-0.5 leading-normal select-none">
                Adjust group names, descriptive categories, and targets.
              </p>
            </div>
          </div>
          
          <div className="min-h-0 flex-1 overflow-y-auto custom-scrollbar pr-1 relative">
            {renderInformationStep()}

            {saving && (
              <div className="absolute inset-0 bg-white/60 backdrop-blur-xs flex items-center justify-center z-50 rounded-lg animate-fade-in">
                <div className="flex flex-col items-center gap-2.5 select-none">
                  <Loader2 className="h-7 w-7 animate-spin text-indigo-500" />
                  <span className="text-xs text-slate-500 font-bold tracking-wide uppercase animate-pulse">Saving...</span>
                </div>
              </div>
            )}
          </div>
          
          <div className="flex items-center justify-end gap-2.5 border-t border-slate-100 pt-3 shrink-0">
            <button 
              type="button" 
              onClick={() => navigate(`/learner-groups/${id}`)} 
              className="inline-flex items-center gap-[7px] rounded-md border border-slate-200 bg-white px-3 py-1.5 font-semibold text-slate-900 hover:border-slate-300 hover:bg-slate-50 cursor-pointer text-xxs font-extrabold shadow-3xs"
            >
              <X className="h-3.5 w-3.5" aria-hidden="true" />
              <span>Cancel</span>
            </button>
            <button 
              type="submit" 
              disabled={saving} 
              className="inline-flex items-center gap-[7px] rounded-md border border-transparent bg-indigo-600 px-3 py-1.5 font-semibold text-white hover:bg-indigo-700 cursor-pointer text-xxs font-extrabold shadow-3xs disabled:opacity-55"
            >
              {saving ? (
                <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden="true" />
              ) : (
                <Save className="h-3.5 w-3.5" aria-hidden="true" />
              )}
              <span>Save Changes</span>
            </button>
          </div>
        </form>
      </div>
    )
  }

  return (
    <AppWizard
      title="New Learner Group"
      description="Create the group, add optional initial members, then review before saving."
      eyebrow="Learner Directory"
      steps={steps}
      currentStep={currentStep}
      onStepChange={setCurrentStep}
      onCancel={() => navigate('/learner-groups')}
      onSubmit={handleSubmit}
      submitLabel="Create Group"
      isSubmitting={saving}
    />
  )
}