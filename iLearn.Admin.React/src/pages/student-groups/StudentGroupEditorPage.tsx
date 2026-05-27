import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, ArrowRight, Check, Plus, RefreshCw, Save, Users, X } from 'lucide-react'

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

export function StudentGroupEditorPage() {
  const { id } = useParams()
  const isEditMode = !!id
  const navigate = useNavigate()

  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [currentStep, setCurrentStep] = useState(1)
  const [memberInput, setMemberInput] = useState('')
  const [categories, setCategories] = useState<GroupCategoryLookup[]>([])
  const [selectedLearnerCodes, setSelectedLearnerCodes] = useState<string[]>([])
  const [formData, setFormData] = useState<GroupFormData>({
    name: '',
    description: '',
    categoryId: 0
  })

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

  const addMemberCodes = () => {
    const parsedCodes = parseLearnerCodes(memberInput)
    if (parsedCodes.length === 0) {
      toast.error('Enter at least one learner code')
      return
    }

    setSelectedLearnerCodes(prev => Array.from(new Set([...prev, ...parsedCodes])))
    setMemberInput('')
  }

  const removeMemberCode = (code: number | string) => {
    setSelectedLearnerCodes(prev => prev.filter(item => item !== String(code)))
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
          navigate(`/student-groups/${id}`)
        }
        return
      }

      const resp = await fetchWithAccessControl<GroupApiResponse>('LearnerGroups', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(buildPayload())
      })

      if (resp.success && resp.data) {
        toast.success(resp.message || 'Student group registered successfully')
        navigate(`/student-groups/${resp.data.id}`)
      }
    } catch (error: unknown) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Failed to save student group details'))
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
    <div className="min-h-0">
      <div className="admin-card p-5">
        <div className="mb-4 flex items-center justify-between gap-3 border-b border-slate-100 pb-3">
          <div className="flex items-center gap-2">
            <Users className="h-5 w-5 text-blue-600" />
            <h2 className="text-sm font-bold text-slate-800">Initial Members</h2>
          </div>
          <span className="border border-slate-200 px-2 py-1 text-xs font-bold text-slate-500">{selectedLearnerCodes.length} learner{selectedLearnerCodes.length === 1 ? '' : 's'}</span>
        </div>

        <div className="grid grid-cols-1 gap-3 md:grid-cols-[minmax(0,1fr)_auto]">
          <textarea
            value={memberInput}
            onChange={event => setMemberInput(event.target.value)}
            rows={7}
            placeholder="Paste learner codes separated by comma, space, or new line"
            className="w-full resize-y rounded border border-slate-200 bg-white px-3 py-2 font-mono text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
          />
          <button type="button" onClick={addMemberCodes} className="admin-button admin-button--secondary self-start">
            <Plus aria-hidden="true" />
            <span>Add Codes</span>
          </button>
        </div>

        <div className="mt-4 overflow-x-auto border border-slate-200">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-xs font-bold uppercase text-slate-500">
              <tr>
                <th className="w-20 px-3 py-2 text-left">No.</th>
                <th className="px-3 py-2 text-left">Learner Code</th>
                <th className="w-20 px-3 py-2 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {selectedLearnerCodes.length === 0 ? (
                <tr><td className="px-3 py-8 text-center font-semibold text-slate-400" colSpan={3}>No initial members selected</td></tr>
              ) : selectedLearnerCodes.map((code, index) => (
                <tr key={code}>
                  <td className="px-3 py-2 font-bold text-slate-500">{index + 1}</td>
                  <td className="px-3 py-2 font-mono font-semibold text-slate-800">{code}</td>
                  <td className="px-3 py-2 text-right">
                    <button type="button" onClick={() => removeMemberCode(code)} className="rounded border border-slate-200 p-1 text-red-600" aria-label="Remove learner code">
                      <X className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )

  const renderReviewStep = () => (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="admin-card p-4">
          <div className="text-xl font-bold text-slate-800">{selectedLearnerCodes.length}</div>
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
          {selectedLearnerCodes.length === 0 ? (
            <span className="font-semibold text-slate-400">No initial members selected</span>
          ) : selectedLearnerCodes.map(code => (
            <span key={code} className="border border-slate-200 bg-white px-2 py-1 font-mono text-sm font-semibold text-slate-700">{code}</span>
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
          <h1 className="text-lg font-extrabold text-slate-800">Edit Student Group</h1>
        </div>
        {renderInformationStep()}
        <div className="flex items-center justify-end gap-3 border-t border-slate-200 pt-3">
          <button type="button" onClick={() => navigate(`/student-groups/${id}`)} className="admin-button admin-button--secondary">
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
          <h1 className="text-lg font-extrabold text-slate-800">New Student Group</h1>
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
        <button type="button" onClick={() => navigate('/student-groups')} className="admin-button admin-button--secondary">
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