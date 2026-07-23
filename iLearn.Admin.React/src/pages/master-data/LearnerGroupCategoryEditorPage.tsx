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
import { ADMIN_LABELS, t } from '../../lib/labels'

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
          toast.error(t(ADMIN_LABELS.failedToLoadDivisions))
        }
      } catch {
        toast.error(t(ADMIN_LABELS.failedToLoadGroupCategories))
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
    if (form.parentId === '') return t(ADMIN_LABELS.rootNoParent)
    return selectedParent?.name ?? `Category #${form.parentId}`
  }, [selectedParent, form.parentId])

  const divisionText = useMemo(() => {
    const divId = selectedParent ? selectedParent.divisionId : form.divisionId
    if (!divId) return t(ADMIN_LABELS.globalNoDivision)
    const div = divisions.find((d) => d.id === divId)
    return div ? div.name : `Division #${divId}`
  }, [selectedParent, form.divisionId, divisions])

  const validateDetails = () => {
    if (!form.name.trim()) {
      toast.error(t(ADMIN_LABELS.nameRequired))
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
        toast.success(t(ADMIN_LABELS.categoryUpdated))
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
        toast.success(t(ADMIN_LABELS.categoryCreated))
      }

      navigate('/master-data/learner-group-categories')
    } catch {
      toast.error(t(isEditMode ? ADMIN_LABELS.updateFailed : ADMIN_LABELS.createFailed))
    } finally {
      setSubmitting(false)
    }
  }


  const renderDetailsStep = () => (
    <div className="space-y-4">
      {(isSuperAdmin || divisions.length > 0) && (
        <div className="space-y-1.5">
          <label htmlFor="divisionId" className="wiz-label">
            {t(ADMIN_LABELS.division)}
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
            <option value="">{t(ADMIN_LABELS.globalNoDivision)}</option>
            {divisions.map((div) => (
              <option key={div.id} value={div.id}>
                {div.name}
              </option>
            ))}
          </select>
          {selectedParent && (
            <p className="text-xs text-amber-600 font-medium">
              * {t(ADMIN_LABELS.inheritedFromParent)}
            </p>
          )}
        </div>
      )}

      <div className="space-y-1.5">
        <label className="wiz-label">
          {t(ADMIN_LABELS.name)} <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          value={form.name}
          onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
          className="wiz-input"
          placeholder={t(ADMIN_LABELS.categoryNameExample)}
          autoFocus
        />
      </div>

      <div className="space-y-1.5">
        <label className="wiz-label">{t(ADMIN_LABELS.description)}</label>
        <input
          type="text"
          value={form.description}
          onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
          className="wiz-input"
          placeholder={t(ADMIN_LABELS.optionalDescription)}
        />
      </div>

      <div className="space-y-1.5">
        <label className="wiz-label">{t(ADMIN_LABELS.parentCategory)}</label>
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
          <option value="">— {t(ADMIN_LABELS.rootNoParent)} —</option>
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
            <dt className="wiz-label">{t(ADMIN_LABELS.division)}</dt>
            <dd className="col-span-2 text-slate-700 font-medium">{divisionText}</dd>
          </div>
        )}
        <div className="grid grid-cols-3 py-2.5">
          <dt className="wiz-label">{t(ADMIN_LABELS.name)}</dt>
          <dd className="col-span-2 text-slate-700 font-bold">{form.name.trim() || '-'}</dd>
        </div>
        <div className="grid grid-cols-3 py-2.5">
          <dt className="wiz-label">{t(ADMIN_LABELS.description)}</dt>
          <dd className="col-span-2 text-slate-700">{form.description.trim() || '-'}</dd>
        </div>
        <div className="grid grid-cols-3 py-2.5">
          <dt className="wiz-label">{t(ADMIN_LABELS.parentCategory)}</dt>
          <dd className="col-span-2 text-slate-700">{parentText}</dd>
        </div>
      </dl>
    </div>
  )


  const steps: WizardStep[] = [
    { label: t(ADMIN_LABELS.details), validate: validateDetails, render: renderDetailsStep }, { label: t(ADMIN_LABELS.review), render: renderReviewStep },
  ]

  if (loading) {
    return <LoadingState label={t(ADMIN_LABELS.loadingCategory)} />
  }

  if (notFound) {
    return (
      <NotFoundState
        title={t(ADMIN_LABELS.categoryNotFound)} message={t(ADMIN_LABELS.categoryNotFoundMessage)}
        backTo="/master-data/learner-group-categories"
        backLabel={t(ADMIN_LABELS.backToCategories)}
      />
    )
  }

  return (
    <AppWizard
      title={t(isEditMode ? ADMIN_LABELS.editCategory : ADMIN_LABELS.newCategory)} description={t(isEditMode ? ADMIN_LABELS.updateCategoryDescription : ADMIN_LABELS.createCategoryDescription)} eyebrow={t(ADMIN_LABELS.masterData)}
      steps={steps}
      currentStep={currentStep}
      onStepChange={setCurrentStep}
      onCancel={() => navigate('/master-data/learner-group-categories')}
      onSubmit={handleSubmit}
      submitLabel={t(isEditMode ? ADMIN_LABELS.saveChanges : ADMIN_LABELS.createCategory)}
      isSubmitting={submitting}
      submitIcon={<Check className="h-3.5 w-3.5" />}
    />
  )
}
