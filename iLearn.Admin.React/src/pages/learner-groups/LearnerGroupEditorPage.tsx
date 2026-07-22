import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { 
  Folder,
  FolderOpen,
  Plus, 
  X
} from 'lucide-react'

import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useSession } from '../../lib/sessionContext'
import { LearnerDirectorySelector, type LearnerSelection } from '../../components/shared/LearnerDirectorySelector'
import { AppTreeView, type TreeViewNode } from '../../components/ui/AppTreeView'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { AppButton } from '../../components/ui/AppButton'
import { Badge } from '../../components/ui/Badge'
import { IconButton } from '../../components/ui/IconButton'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'

type LoadResult<T> = T[] | { data?: T[] }

type GroupCategoryLookup = {
  id: number
  name: string
  parentId?: number | null
  depth?: number
}

type DivisionLookup = {
  id: number
  name: string
}

type GroupFormData = {
  name: string
  description: string
  categoryId: number
  divisionId?: number | null
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
  const navigate = useNavigate()
  const { isSuperAdmin } = useSession()

  const [saving, setSaving] = useState(false)
  const [currentStep, setCurrentStep] = useState(1)
  const [memberInput, setMemberInput] = useState('')
  const [categories, setCategories] = useState<GroupCategoryLookup[]>([])
  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [isExplorerOpen, setIsExplorerOpen] = useState(false)
  const [tempCategoryId, setTempCategoryId] = useState<number>(0)
  const [didApplyQueryCategory, setDidApplyQueryCategory] = useState(false)
  
  // Selection states
  const [selectedLearners, setSelectedLearners] = useState<LearnerSelection[]>([])
  const [activeTab, setActiveTab] = useState<'picker' | 'bulk'>('picker')

  const [formData, setFormData] = useState<GroupFormData>({
    name: '',
    description: '',
    categoryId: 0,
    divisionId: null
  })

  const selectedLearnerCodes = useMemo(() => (
    selectedLearners.map(l => l.code)
  ), [selectedLearners])

  const selectedCategoryPath = useMemo(() => {
    if (!formData.categoryId) return 'No category (Root folder)'

    const path: string[] = []
    const visited = new Set<number>()
    let current: GroupCategoryLookup | undefined = categories.find(c => c.id === formData.categoryId)

    while (current && !visited.has(current.id)) {
      visited.add(current.id)
      path.unshift(current.name)
      const pId: number | null | undefined = current.parentId
      current = pId ? categories.find(c => c.id === pId) : undefined
    }

    return path.length > 0 ? path.join(' / ') : 'No category (Root folder)'
  }, [categories, formData.categoryId])

  const tempCategoryPath = useMemo(() => {
    if (tempCategoryId === 0) return 'Root folder'

    const path: string[] = []
    const visited = new Set<number>()
    let current: GroupCategoryLookup | undefined = categories.find(c => c.id === tempCategoryId)

    while (current && !visited.has(current.id)) {
      visited.add(current.id)
      path.unshift(current.name)
      const pId: number | null | undefined = current.parentId
      current = pId ? categories.find(c => c.id === pId) : undefined
    }

    return path.length > 0 ? path.join(' / ') : 'Root folder'
  }, [categories, tempCategoryId])

  const treeNodes = useMemo<TreeViewNode[]>(() => {
    const byParent: Record<number, GroupCategoryLookup[]> = {}
    const roots: GroupCategoryLookup[] = []

    categories.forEach(category => {
      const pId = category.parentId || 0
      if (pId === 0) {
        roots.push(category)
      } else {
        if (!byParent[pId]) byParent[pId] = []
        byParent[pId].push(category)
      }
    })

    roots.sort((a, b) => a.name.localeCompare(b.name))
    Object.values(byParent).forEach(children => {
      children.sort((a, b) => a.name.localeCompare(b.name))
    })

    const mapNode = (category: GroupCategoryLookup): TreeViewNode => {
      const children = byParent[category.id] || []
      return {
        id: `cat-${category.id}`,
        text: category.name,
        categoryId: category.id,
        items: children.map(mapNode)
      }
    }

    return [
      {
        id: 'root',
        text: 'Root Folder (No Category)',
        isRoot: true,
        categoryId: 0,
        items: roots.map(mapNode)
      }
    ]
  }, [categories])

  const loadCategories = useCallback(async () => {
    try {
      const response = await fetchWithAccessControl<LoadResult<GroupCategoryLookup>>('LearnerGroupCategories')
      setCategories(unwrapList(response))
    } catch (error) {
      console.error(error)
      toast.error('Failed to load group categories')
    }
  }, [])



  const loadDivisions = useCallback(async () => {
    try {
      const response = await fetchWithAccessControl<{ data?: DivisionLookup[] } | DivisionLookup[]>('Divisions/lookup')
      const list = unwrapList(response)
      setDivisions(list)
      if (!isSuperAdmin && list.length === 1) {
        setFormData(prev => ({ ...prev, divisionId: list[0]?.id ?? null }))
      }
    } catch (error) {
      console.error(error)
      toast.error('Failed to load divisions')
    }
  }, [isSuperAdmin])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadCategories()
      void loadDivisions()
    }, 0)

    return () => window.clearTimeout(timeoutId)
  }, [loadCategories, loadDivisions])

  useEffect(() => {
    if (didApplyQueryCategory) return

    const searchParams = new URLSearchParams(window.location.search)
    const queryValue = searchParams.get('categoryId')

    if (!queryValue) {
      setDidApplyQueryCategory(true)
      return
    }

    const parsedCategoryId = Number(queryValue)
    if (!Number.isInteger(parsedCategoryId) || parsedCategoryId <= 0) {
      setDidApplyQueryCategory(true)
      return
    }

    if (categories.length === 0) {
      return
    }

    const exists = categories.some(category => category.id === parsedCategoryId)
    if (exists) {
      setFormData(prev => ({ ...prev, categoryId: parsedCategoryId }))
    }

    setDidApplyQueryCategory(true)
  }, [categories, didApplyQueryCategory])

  const handleChange = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = event.target
    setFormData(prev => ({
      ...prev,
      [name]: name === 'categoryId' ? Number(value) : (name === 'divisionId' ? (value ? Number(value) : null) : value)
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
    divisionId: isSuperAdmin && formData.divisionId ? formData.divisionId : null,
    learnerCodes: selectedLearnerCodes
  })

  const handleSubmit = async (event?: React.FormEvent) => {
    if (event) {
      event.preventDefault()
    }
    if (!validateInformation()) return

    setSaving(true)
    try {
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

      {(isSuperAdmin || divisions.length > 0) && (
        <div className="space-y-1.5">
          <label htmlFor="divisionId" className="wiz-label">
            Division (แผนก)
          </label>
          <select
            id="divisionId"
            name="divisionId"
            value={formData.divisionId || ''}
            onChange={handleChange}
            disabled={!isSuperAdmin}
            className="wiz-input max-w-lg disabled:bg-slate-50 disabled:text-slate-500 disabled:border-slate-200"
          >
            <option value="">Global / ไม่ระบุแผนก</option>
            {divisions.map(div => (
              <option key={div.id} value={div.id}>
                {div.name}
              </option>
            ))}
          </select>
        </div>
      )}

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
        <label className="wiz-label">Category (Location Folder)</label>
        <div className="flex flex-col sm:flex-row gap-2 max-w-lg items-stretch sm:items-center">
          <div className="flex-1 flex items-center gap-2 px-3 py-2 border border-slate-200 rounded-md bg-slate-50/50 text-slate-700 min-w-0 select-none">
            <Folder className="h-4 w-4 text-indigo-500 shrink-0" />
            <span className="text-sm font-semibold truncate">
              {selectedCategoryPath}
            </span>
          </div>

          <AppButton
            variant="secondary"
            icon={FolderOpen}
            onClick={() => {
              setTempCategoryId(formData.categoryId || 0)
              setIsExplorerOpen(true)
            }}
          >
            Select Category Folder...
          </AppButton>
        </div>
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
    <div className="flex flex-col gap-3 flex-1 min-h-0 w-full">
      <div className="flex justify-end select-none shrink-0">
        <SegmentedToggle
          options={[
            { value: 'picker', label: 'Directory Search' },
            { value: 'bulk', label: 'Bulk Import (EIds)' },
          ]}
          value={activeTab}
          onChange={setActiveTab}
        />
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
              <AppButton
                variant="primary"
                size="sm"
                icon={Plus}
                onClick={addMemberCodes}
                disabled={!memberInput.trim()}
                className="self-start"
              >
                Import Codes
              </AppButton>
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
          <dd className="mt-1 text-sm font-semibold text-slate-700">{selectedCategoryPath}</dd>
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

  const renderCategoryExplorerModal = () => {
    if (!isExplorerOpen) return null

    return (
      <div
        className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-fade-in"
        onClick={() => setIsExplorerOpen(false)}
      >
        <div
          className="bg-white border border-slate-100 rounded-xl shadow-2xl w-full max-w-md overflow-hidden flex flex-col animate-scale-up"
          onClick={event => event.stopPropagation()}
        >
          <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100 select-none">
            <div className="flex items-center gap-2">
              <FolderOpen className="h-5 w-5 text-indigo-500" />
              <h3 className="text-sm font-extrabold text-slate-800 uppercase tracking-wide">Category Folder Explorer</h3>
            </div>

            <IconButton
              type="button"
              onClick={() => setIsExplorerOpen(false)}
              icon={X}
              title="Close"
              tone="neutral"
            />
          </div>

          <div className="px-5 py-2.5 bg-slate-50 border-b border-slate-100 text-xxs font-semibold text-slate-400 uppercase select-none">
            Navigate folder structure to assign learner group category
          </div>

          <div className="p-4 flex-1 overflow-y-auto max-h-80 min-h-60 bg-slate-50/30 border-b border-slate-100">
            <div className="bg-white border border-slate-200/60 rounded-lg p-2 shadow-3xs max-h-72 overflow-y-auto custom-scrollbar">
              <AppTreeView
                items={treeNodes}
                onItemClick={event => {
                  const idVal = event.itemData.categoryId ?? 0
                  setTempCategoryId(idVal)
                }}
              />
            </div>
          </div>

          <div className="px-5 py-4 bg-slate-50 flex flex-col gap-3 select-none">
            <div className="flex items-center gap-1.5 text-xs">
              <span className="text-slate-400 font-bold uppercase text-xxs">Selected:</span>
              <Badge tone="neutral" variant="soft" className="truncate flex-1 font-bold">
                {tempCategoryPath}
              </Badge>
            </div>

            <div className="flex justify-end gap-2">
              <AppButton
                variant="ghost"
                onClick={() => setIsExplorerOpen(false)}
              >
                Cancel
              </AppButton>
              <AppButton
                variant="primary"
                onClick={() => {
                  setFormData(prev => ({ ...prev, categoryId: tempCategoryId }))
                  setIsExplorerOpen(false)
                }}
              >
                Confirm Selection
              </AppButton>
            </div>
          </div>
        </div>
      </div>
    )
  }

  const steps: WizardStep[] = [
    { label: 'Information', validate: () => validateInformation(), render: () => renderInformationStep() },
    { label: 'Members', render: () => renderMembersStep() },
    { label: 'Review', render: () => renderReviewStep() }
  ]

  return (
    <>
      <AppWizard
        title="New Learner Group"
        description="Create a new group of learners."
        eyebrow="Learner Directory"
        steps={steps}
        currentStep={currentStep}
        onStepChange={setCurrentStep}
        onCancel={() => navigate('/learner-groups')}
        onSubmit={handleSubmit}
        submitLabel="Create Group"
        isSubmitting={saving}
      />
      {renderCategoryExplorerModal()}
    </>
  )
}