import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { 
  Plus, 
  Save, 
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
    <div className="space-y-4">

      <div className="space-y-1.5">
        <label htmlFor="name" className="wiz-label">
          Group Name <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          id="name"
          name="name"
          value={formData.name}
          onChange={handleChange}
          placeholder="e.g. New Hires 2026 Q1"
          className="wiz-input"
        />
      </div>

      <div className="space-y-1.5">
        <label htmlFor="categoryId" className="wiz-label">Category</label>
        <select
          id="categoryId"
          name="categoryId"
          value={formData.categoryId}
          onChange={handleChange}
          className="wiz-input max-w-md"
        >
          <option value={0}>No category (root)</option>
          {categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
        </select>
      </div>

      <div className="space-y-1.5">
        <label htmlFor="description" className="wiz-label">
          Description <span className="text-red-500">*</span>
        </label>
        <textarea
          id="description"
          name="description"
          value={formData.description}
          onChange={handleChange}
          rows={4}
          placeholder="Brief description of this group's purpose"
          className="wiz-input resize-y"
        />
      </div>
    </div>
  )

  const renderMembersStep = () => (
    <div className="flex flex-col gap-3 h-[calc(100vh-340px)] min-h-[360px] w-full">
      <div className="flex justify-end select-none shrink-0">
        <div className="flex items-center gap-1.5 bg-slate-100 p-1 rounded-lg text-xs">
          <button
            type="button"
            onClick={() => setActiveTab('picker')}
            className={`px-2.5 py-1 text-center font-extrabold rounded-md transition cursor-pointer ${
              activeTab === 'picker' ? 'bg-white text-indigo-700 shadow-3xs' : 'text-slate-500 hover:text-slate-700'
            }`}
          >
            Directory Search
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('bulk')}
            className={`px-2.5 py-1 text-center font-extrabold rounded-md transition cursor-pointer ${
              activeTab === 'bulk' ? 'bg-white text-indigo-700 shadow-3xs' : 'text-slate-500 hover:text-slate-700'
            }`}
          >
            Bulk Import (EIds)
          </button>
        </div>
      </div>

      <div className="flex-1 min-h-0 flex flex-col">
        {activeTab === 'picker' ? (
          <div className="flex-1 flex flex-col min-h-0">
            <LearnerDirectorySelector selectedLearners={selectedLearners} onChange={setSelectedLearners} />
          </div>
        ) : (
          <div className="space-y-3.5 overflow-y-auto custom-scrollbar pr-1 flex-1 min-h-0">
            <p className="text-xs font-semibold text-slate-400 select-none leading-relaxed">
              Bulk paste employee EIds here. They will be integrated directly into your selection workspace.
            </p>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-[minmax(0,1fr)_auto]">
              <textarea
                value={memberInput}
                onChange={event => setMemberInput(event.target.value)}
                rows={5}
                placeholder="Paste employee EIds separated by comma, space, or new line (e.g. N130812, N142715)"
                className="wiz-input resize-y font-mono bg-slate-50/10"
              />
              <button
                type="button"
                onClick={addMemberCodes}
                disabled={!memberInput.trim()}
                className="inline-flex items-center gap-1.75 rounded-md border border-transparent bg-indigo-600 px-3 py-1.5 text-white hover:bg-indigo-700 cursor-pointer self-start text-xs font-extrabold shadow-3xs disabled:opacity-55"
              >
                <Plus className="h-3.5 w-3.5" aria-hidden="true" />
                <span>Import Codes</span>
              </button>
            </div>

            {/* List of currently selected ones for preview */}
            <div className="border border-slate-200 rounded overflow-hidden">
              <div className="bg-slate-50 px-3 py-1.5 border-b border-slate-200 flex justify-between items-center text-xs font-bold text-slate-400 uppercase select-none">
                <span>Selected Codes Ledger</span>
                <span>{selectedLearners.length} Users</span>
              </div>
              <div className="max-h-56 overflow-y-auto custom-scrollbar bg-white divide-y divide-slate-100">
                {selectedLearners.length === 0 ? (
                  <div className="px-3 py-6 text-center text-slate-400 text-xs font-bold select-none">No initial members selected</div>
                ) : (
                  selectedLearners.map((learner, index) => (
                    <div key={learner.code} className="px-3 py-1.5 text-xs flex items-center justify-between font-semibold">
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

      <dl className="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">
        <div className="border-b border-slate-100 pb-2.5 sm:col-span-2">
          <dt className="wiz-label">Group Name</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{formData.name || 'Not set'}</dd>
        </div>
        <div className="border-b border-slate-100 pb-2.5">
          <dt className="wiz-label">Category</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{selectedCategoryName}</dd>
        </div>
        <div className="border-b border-slate-100 pb-2.5">
          <dt className="wiz-label">Initial Members</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{selectedLearners.length}</dd>
        </div>
        <div className="border-b border-slate-100 pb-2.5 sm:col-span-2">
          <dt className="wiz-label">Description</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{formData.description || 'Not set'}</dd>
        </div>
      </dl>

      <div>
        <div className="mb-2 text-sm font-bold text-slate-800 select-none">Initial Members</div>
        <div className="flex flex-wrap gap-1.5 max-h-56 overflow-y-auto pr-1 custom-scrollbar">
          {selectedLearners.length === 0 ? (
            <span className="font-semibold text-slate-400 text-xs select-none">No initial members selected</span>
          ) : selectedLearners.map(learner => (
            <span
              key={learner.code}
              className="border border-slate-200 bg-white px-2 py-0.5 font-mono text-xs font-extrabold text-slate-700 rounded-sm select-none"
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
      <div className="wizard-surface flex min-h-0 flex-1 flex-col overflow-hidden bg-white border border-slate-200/80 rounded-xl shadow-xs">
        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col">
          {/* Header with Title */}
          <div className="flex flex-col gap-3 bg-white px-6 pt-5 pb-3 border-b border-slate-200 shrink-0 select-none">
            <div>
              <div className="text-xs font-bold uppercase tracking-wider text-slate-400">
                Learner Directory
              </div>
              <h1 className="text-base font-extrabold text-slate-800 tracking-tight leading-tight">
                Edit Learner Group
              </h1>
              <p className="text-xs font-semibold text-slate-400 mt-0.5 leading-normal">
                Adjust group names, descriptive categories, and targets.
              </p>
            </div>
          </div>
          
          {/* Content Panel Zone */}
          <div className="min-h-0 flex-1 flex flex-col relative bg-slate-50/60">
            <div className="overflow-y-auto custom-scrollbar flex-1 px-6 py-6">
              <div className="w-full h-full flex flex-col">
                {renderInformationStep()}
              </div>
            </div>
            
            {saving && (
              <div className="absolute inset-0 bg-white/60 backdrop-blur-xs flex items-center justify-center z-50 rounded-lg animate-fade-in">
                <div className="flex flex-col items-center gap-2.5 select-none">
                  <Loader2 className="h-7 w-7 animate-spin text-indigo-500" />
                  <span className="text-xs text-slate-500 font-bold tracking-wide uppercase animate-pulse">Saving...</span>
                </div>
              </div>
            )}
          </div>
          
          {/* Navigation Buttons Pinned Footer */}
          <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-200 bg-white shrink-0">
            <button 
              type="button" 
              onClick={() => navigate(`/learner-groups/${id}`)} 
              className="inline-flex items-center gap-1.5 rounded-md border border-slate-200 bg-white px-4 py-2 font-bold text-slate-500 hover:border-slate-300 hover:bg-slate-50 hover:text-slate-700 cursor-pointer text-xs shadow-3xs"
            >
              <X className="h-4 w-4" aria-hidden="true" />
              <span>Cancel</span>
            </button>
            <button 
              type="submit" 
              disabled={saving} 
              className="inline-flex items-center gap-1.5 rounded-md border border-transparent bg-indigo-600 px-4 py-2 font-bold text-white hover:bg-indigo-700 cursor-pointer text-xs shadow-3xs disabled:opacity-55"
            >
              {saving ? (
                <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
              ) : (
                <Save className="h-4 w-4" aria-hidden="true" />
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