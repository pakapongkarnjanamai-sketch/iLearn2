import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Check } from 'lucide-react'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { useSession } from '../../lib/sessionContext'
import { toast } from '../../lib/toast'
import type { ApiListResponse, LearnerGroupCategory } from './LearnerGroupCategoriesPage'

type DivisionLookup = {
  id: number
  name: string
}

type CategoryForm = {
  name: string
  description: string
  parentId: number | ''
  divisionId?: number | null
}

const EMPTY_FORM: CategoryForm = {
  name: '',
  description: '',
  parentId: '',
  divisionId: null,
}


function unwrapCategories(
  result: LearnerGroupCategory[] | ApiListResponse<LearnerGroupCategory[]>,
): LearnerGroupCategory[] {
  if (Array.isArray(result)) return result
  return Array.isArray(result.data) ? result.data : []
}

export function LearnerGroupCategoryEditorPage() {
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const isEditMode = Boolean(id)
  const { isSuperAdmin } = useSession()

  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [currentStep, setCurrentStep] = useState(1)

  const [categories, setCategories] = useState<LearnerGroupCategory[]>([])
  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [form, setForm] = useState<CategoryForm>(EMPTY_FORM)

  const editId = useMemo(() => {
    if (!isEditMode || !id) return null
    const value = Number(id)
    return Number.isFinite(value) && value > 0 ? value : null
  }, [id, isEditMode])

  useEffect(() => {
    let cancelled = false

    const load = async () => {
      setLoading(true)
      setNotFound(false)
      setCurrentStep(1)

      if (isEditMode && editId == null) {
        setNotFound(true)
        setLoading(false)
        return
      }

      try {
        const result = await fetchWithAccessControl<
          LearnerGroupCategory[] | ApiListResponse<LearnerGroupCategory[]>
        >('LearnerGroupCategories')

        if (cancelled) return

        const list = unwrapCategories(result)
        setCategories(list)

        if (isEditMode && editId != null) {
          const target = list.find((item) => item.id === editId)
          if (!target) {
            setNotFound(true)
            return
          }

          setForm({
            name: target.name,
            description: target.description ?? '',
            parentId: target.parentId ?? '',
            divisionId: target.divisionId ?? null,
          })
        } else {
          setForm(EMPTY_FORM)
        }

        try {
          const divResult = await fetchWithAccessControl<
            DivisionLookup[] | { data?: DivisionLookup[] }
          >('Divisions/lookup')
          if (!cancelled) {
            const divList = Array.isArray(divResult) ? divResult : divResult.data ?? []
            setDivisions(divList)
            if (!isSuperAdmin && divList.length === 1 && !isEditMode) {
              setForm((prev) => ({ ...prev, divisionId: divList[0]?.id ?? null }))
            }
          }
        } catch (err) {
          console.error('Failed to load divisions', err)
          toast.error('Failed to load divisions')
        }
      } catch {
        toast.error('Failed to load categories')
        if (!cancelled && isEditMode) {
          setNotFound(true)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void load()

    return () => {
      cancelled = true
    }
  }, [editId, isEditMode, isSuperAdmin])


  const parentOptions = useMemo(() => {
    if (!isEditMode || editId == null) return categories
    return categories.filter((item) => item.id !== editId)
  }, [categories, editId, isEditMode])

  const selectedParent = useMemo(() => {
    if (form.parentId === '') return null
    return categories.find((item) => item.id === Number(form.parentId))
  }, [categories, form.parentId])

  const parentText = useMemo(() => {
    if (form.parentId === '') return 'Root (no parent)'
    return selectedParent?.name ?? `Category #${form.parentId}`
  }, [selectedParent, form.parentId])

  const divisionText = useMemo(() => {
    const divId = selectedParent ? selectedParent.divisionId : form.divisionId
    if (!divId) return 'Global (All Divisions)'
    const div = divisions.find((d) => d.id === divId)
    return div ? div.name : `Division #${divId}`
  }, [selectedParent, form.divisionId, divisions])

  const validateDetails = () => {
    if (!form.name.trim()) {
      toast.error('Name is required')
      return false
    }
    return true
  }

  const handleSubmit = async () => {
    setSubmitting(true)
    try {
      if (isEditMode && editId != null) {
        const payload = {
          name: form.name.trim(),
          description: form.description.trim() || null,
          parentId: form.parentId === '' ? null : Number(form.parentId),
          divisionId: isSuperAdmin ? (selectedParent ? (selectedParent.divisionId ?? null) : form.divisionId) : null,
        }
        await fetchWithAccessControl(`learnerGroupCategories/${editId}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
        toast.success('Category updated')
      } else {
        const payload = {
          name: form.name.trim(),
          description: form.description.trim() || null,
          parentId: form.parentId === '' ? null : Number(form.parentId),
          divisionId: isSuperAdmin ? (selectedParent ? (selectedParent.divisionId ?? null) : form.divisionId) : null,
        }
        await fetchWithAccessControl('learnerGroupCategories', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
        toast.success('Category created')
      }

      navigate('/master-data/learner-group-categories')
    } catch {
      toast.error(isEditMode ? 'Update failed' : 'Create failed')
    } finally {
      setSubmitting(false)
    }
  }


  const renderDetailsStep = () => (
    <div className="space-y-4">
      {(isSuperAdmin || divisions.length > 0) && (
        <div className="space-y-1.5">
          <label htmlFor="divisionId" className="wiz-label">
            Division (แผนก)
          </label>
          <select
            id="divisionId"
            value={selectedParent ? (selectedParent.divisionId || '') : (form.divisionId || '')}
            onChange={(event) =>
              setForm((prev) => ({
                ...prev,
                divisionId: event.target.value === '' ? null : Number(event.target.value),
              }))
            }
            disabled={selectedParent !== null || !isSuperAdmin}
            className="wiz-input disabled:bg-slate-50 disabled:text-slate-500 disabled:border-slate-200"
          >
            <option value="">Global / ไม่ระบุแผนก</option>
            {divisions.map((div) => (
              <option key={div.id} value={div.id}>
                {div.name}
              </option>
            ))}
          </select>
          {selectedParent && (
            <p className="text-xs text-amber-600 font-medium">
              * ใช้แผนกตามหมวดหมู่หลัก (Inherited from parent category)
            </p>
          )}
        </div>
      )}

      <div className="space-y-1.5">
        <label className="wiz-label">
          Name <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          value={form.name}
          onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
          className="wiz-input"
          placeholder="e.g. HR & Administration"
          autoFocus
        />
      </div>

      <div className="space-y-1.5">
        <label className="wiz-label">Description</label>
        <input
          type="text"
          value={form.description}
          onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
          className="wiz-input"
          placeholder="Optional description"
        />
      </div>

      <div className="space-y-1.5">
        <label className="wiz-label">Parent Category</label>
        <select
          value={form.parentId}
          onChange={(event) =>
            setForm((prev) => ({
              ...prev,
              parentId: event.target.value === '' ? '' : Number(event.target.value),
            }))
          }
          className="wiz-input"
        >
          <option value="">— Root (no parent) —</option>
          {parentOptions.map((item) => (
            <option key={item.id} value={item.id}>
              {'  '.repeat(item.depth)}
              {item.name}
            </option>
          ))}
        </select>
      </div>
    </div>
  )

  const renderReviewStep = () => (
    <div className="space-y-4">
      <dl className="divide-y divide-slate-100 text-sm">
        {isSuperAdmin && (
          <div className="grid grid-cols-3 py-2.5">
            <dt className="wiz-label">Division</dt>
            <dd className="col-span-2 text-slate-700 font-medium">{divisionText}</dd>
          </div>
        )}
        <div className="grid grid-cols-3 py-2.5">
          <dt className="wiz-label">Name</dt>
          <dd className="col-span-2 text-slate-700 font-bold">{form.name.trim() || '-'}</dd>
        </div>
        <div className="grid grid-cols-3 py-2.5">
          <dt className="wiz-label">Description</dt>
          <dd className="col-span-2 text-slate-700">{form.description.trim() || '-'}</dd>
        </div>
        <div className="grid grid-cols-3 py-2.5">
          <dt className="wiz-label">Parent Category</dt>
          <dd className="col-span-2 text-slate-700">{parentText}</dd>
        </div>
      </dl>
    </div>
  )


  const steps: WizardStep[] = [
    { label: 'Details', validate: validateDetails, render: renderDetailsStep },
    { label: 'Review', render: renderReviewStep },
  ]

  if (loading) {
    return <LoadingState label="Loading category..." />
  }

  if (notFound) {
    return (
      <NotFoundState
        title="Category Not Found"
        message="The category you are trying to edit does not exist or has been deleted."
        backTo="/master-data/learner-group-categories"
        backLabel="Back to Categories"
      />
    )
  }

  return (
    <AppWizard
      title={isEditMode ? 'Edit Category' : 'New Category'}
      description={isEditMode ? 'Update learner group category details.' : 'Create a new category for organizing learner groups.'}
      eyebrow="Master Data"
      steps={steps}
      currentStep={currentStep}
      onStepChange={setCurrentStep}
      onCancel={() => navigate('/master-data/learner-group-categories')}
      onSubmit={handleSubmit}
      submitLabel={isEditMode ? 'Save Changes' : 'Create Category'}
      isSubmitting={submitting}
      submitIcon={<Check className="h-3.5 w-3.5" />}
    />
  )
}
