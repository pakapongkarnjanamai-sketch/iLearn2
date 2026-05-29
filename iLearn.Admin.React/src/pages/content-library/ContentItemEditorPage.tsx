import { useEffect, useState, useMemo, type FormEvent } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import { 
  ArrowLeft, 
  ArrowRight,
  Upload, 
  Loader2, 
  Check, 
  X, 
  Library, 
  Info, 
  FileArchive 
} from 'lucide-react'
import { fetchWithAccessControl, buildApiUrl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

const TYPE_OPTIONS = [
  { value: 1, label: 'Learn — instructional content' },
  { value: 2, label: 'Exam — assessment content' },
]

type ContentItemForm = {
  name: string
  typeId: number
}

export function ContentItemEditorPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isCreate = !id

  const [loading, setLoading] = useState(!isCreate)
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [form, setForm] = useState<ContentItemForm>({ name: '', typeId: 1 })
  const [file, setFile] = useState<File | null>(null)
  
  // Wizard state
  const [currentStep, setCurrentStep] = useState(1)

  const stepLabels = useMemo(() => (
    isCreate 
      ? ['Metadata', 'Package Upload', 'Review']
      : ['Metadata', 'Review']
  ), [isCreate])

  useEffect(() => {
    if (isCreate) return
    let cancelled = false
    fetchWithAccessControl<ContentItemForm & { id: number }>(
      `admin/ContentItemsCRUD/Get/${id}`,
    )
      .then((data) => {
        if (cancelled) return
        setForm({ name: data.name, typeId: data.typeId })
      })
      .catch(() => toast.error('Failed to load content item'))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [id, isCreate])

  const handleUpload = async (e: FormEvent) => {
    e.preventDefault()
    if (!file) {
      toast.error('Please choose a SCORM .zip file')
      return
    }
    if (!file.name.toLowerCase().endsWith('.zip')) {
      toast.error('File must be a .zip SCORM package')
      return
    }
    setUploading(true)
    try {
      const fd = new FormData()
      fd.append('file', file)
      const created = await fetch(
        buildApiUrl(`ContentItems/upload?typeId=${form.typeId}`),
        { method: 'POST', credentials: 'include', body: fd },
      )
      if (!created.ok) {
        const text = await created.text()
        throw new Error(text || 'Upload failed')
      }
      const result = (await created.json()) as { id: number }
      if (form.name.trim()) {
        const update = new FormData()
        update.append('key', String(result.id))
        update.append('values', JSON.stringify({ name: form.name.trim(), typeId: form.typeId }))
        await fetchWithAccessControl('admin/ContentItemsCRUD/Put', { method: 'PUT', body: update })
      }
      toast.success('SCORM package uploaded')
      navigate(`/content-library/${result.id}`)
    } catch (err) {
      console.error(err)
      toast.error((err as Error).message || 'Upload failed')
    } finally {
      setUploading(false)
    }
  }

  const handleSave = async (e: FormEvent) => {
    e.preventDefault()
    if (!id) return
    if (!form.name.trim()) {
      toast.error('Name is required')
      return
    }
    setSaving(true)
    try {
      const fd = new FormData()
      fd.append('key', id)
      fd.append('values', JSON.stringify({ name: form.name, typeId: form.typeId }))
      await fetchWithAccessControl('admin/ContentItemsCRUD/Put', { method: 'PUT', body: fd })
      toast.success('Content item updated')
      navigate(`/content-library/${id}`)
    } catch {
      toast.error('Save failed')
    } finally {
      setSaving(false)
    }
  }

  const validateMetadata = () => {
    if (!isCreate && !form.name.trim()) {
      toast.error('Name is required')
      return false
    }
    return true
  }

  const validateUpload = () => {
    if (isCreate && !file) {
      toast.error('Please choose a SCORM .zip file')
      return false
    }
    if (isCreate && file && !file.name.toLowerCase().endsWith('.zip')) {
      toast.error('File must be a .zip SCORM package')
      return false
    }
    return true
  }

  const goNext = () => {
    if (currentStep === 1 && !validateMetadata()) return
    if (isCreate && currentStep === 2 && !validateUpload()) return
    setCurrentStep(prev => Math.min(stepLabels.length, prev + 1))
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
          if (step <= currentStep || (currentStep === 1 && validateMetadata()) || (isCreate && currentStep === 2 && validateUpload())) {
            setCurrentStep(step)
          }
        }}
        className={`flex min-w-31 items-center gap-2 border px-3 py-2 text-left text-xs font-bold rounded transition cursor-pointer select-none ${
          isActive 
            ? 'border-blue-500 bg-blue-50 text-blue-700' 
            : isComplete 
              ? 'border-emerald-200 bg-emerald-50 text-emerald-700' 
              : 'border-slate-200 bg-white text-slate-500'
        }`}
        aria-current={isActive ? 'step' : undefined}
      >
        <span className="flex h-5 w-5 items-center justify-center rounded-sm border border-current text-xxs">{step}</span>
        <span>{label}</span>
      </button>
    )
  }

  const renderMetadataStep = () => (
    <div className="admin-card p-5 max-w-2xl space-y-4">
      <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-2">
        <Library className="h-5 w-5 text-blue-600" />
        <h2 className="text-sm font-bold text-slate-800">Content Item Specifications</h2>
      </div>

      <div className="space-y-1.5">
        <label className="block text-xs font-bold text-slate-500 uppercase">
          Display Name {!isCreate && <span className="text-red-500">*</span>}
        </label>
        <input
          type="text"
          value={form.name}
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          placeholder={isCreate ? 'Leave blank to use ZIP filename as fallback' : 'Required'}
          className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
        />
      </div>

      <div className="space-y-1.5">
        <label className="block text-xs font-bold text-slate-500 uppercase">Content Type</label>
        <select
          value={form.typeId}
          onChange={(e) => setForm((f) => ({ ...f, typeId: Number(e.target.value) }))}
          className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none cursor-pointer"
        >
          {TYPE_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      </div>
    </div>
  )

  const renderUploadStep = () => (
    <div className="admin-card p-5 max-w-2xl space-y-4">
      <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-2">
        <Upload className="h-5 w-5 text-blue-600" />
        <h2 className="text-sm font-bold text-slate-800">SCORM Package Upload</h2>
      </div>

      <div className="space-y-1.5">
        <label className="block text-xs font-bold text-slate-500 uppercase">SCORM Package (.zip)</label>
        <input
          type="file"
          accept=".zip,application/zip"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          className="block w-full rounded border border-dashed border-slate-300 px-4 py-6 text-xs text-slate-600 bg-slate-50/30 hover:bg-slate-50 hover:border-blue-500 transition cursor-pointer"
        />
        <p className="mt-1.5 text-xxs text-slate-400 font-semibold leading-relaxed">
          Supports SCORM 1.2 and SCORM 2004 standards. Maximum bundle size limit is 100 MB or 1,000 internal directory entries.
        </p>
      </div>

      {file && (
        <div className="mt-4 p-4 border border-blue-100 bg-blue-50/20 rounded flex items-center justify-between select-none">
          <div className="flex items-center gap-3">
            <FileArchive className="h-8 w-8 text-blue-600 shrink-0" />
            <div className="min-w-0">
              <p className="text-xs font-bold text-slate-700 truncate">{file.name}</p>
              <p className="text-[10px] font-mono text-slate-400 mt-0.5">{Math.round(file.size / 1024)} KB</p>
            </div>
          </div>
          <button 
            type="button" 
            onClick={() => setFile(null)}
            className="p-1 rounded-full text-slate-400 hover:bg-slate-150 hover:text-slate-600 transition"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      )}
    </div>
  )

  const renderReviewStep = () => {
    const selectedTypeName = TYPE_OPTIONS.find(o => o.value === form.typeId)?.label || 'Instructional Content'
    return (
      <div className="max-w-2xl space-y-4">
        <div className="admin-card p-5">
          <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-4">
            <Info className="h-5 w-5 text-blue-600" />
            <h2 className="text-sm font-bold text-slate-800">Review Specifications</h2>
          </div>

          <dl className="divide-y divide-slate-100 text-xs select-none">
            <div className="grid grid-cols-3 py-3 font-semibold">
              <dt className="text-slate-500 uppercase font-bold">Display Name</dt>
              <dd className="col-span-2 text-slate-800 font-bold">{form.name.trim() || (file ? file.name : '—') || 'Unnamed package'}</dd>
            </div>
            <div className="grid grid-cols-3 py-3 font-semibold">
              <dt className="text-slate-500 uppercase font-bold">Content Type</dt>
              <dd className="col-span-2 text-slate-800">{selectedTypeName}</dd>
            </div>
            {isCreate && (
              <div className="grid grid-cols-3 py-3 font-semibold">
                <dt className="text-slate-500 uppercase font-bold">Target SCORM Package</dt>
                <dd className="col-span-2 text-slate-800 font-mono">
                  {file ? `${file.name} (${Math.round(file.size / 1024)} KB)` : 'No file selected'}
                </dd>
              </div>
            )}
          </dl>
        </div>

        <div className="p-4 border border-blue-100 bg-blue-50/20 rounded-lg text-xs leading-relaxed text-blue-700 font-semibold select-none">
          {isCreate ? (
            <p>
              Upon clicking "Upload Package", the SCORM zip package will be processed, uploaded, and extracted on the server. The content starts in Draft status. You can publish and test launch it immediately from the details view page.
            </p>
          ) : (
            <p>
              Please verify your display metadata values. Changes will take effect immediately across all versions, courses, and active learner logs referencing this content item.
            </p>
          )}
        </div>
      </div>
    )
  }

  if (loading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
          <span className="text-sm text-slate-500 font-medium">Loading content item...</span>
        </div>
      </div>
    )
  }

  return (
    <div className="admin-grid-surface">
      <form onSubmit={isCreate ? handleUpload : handleSave} className="flex min-h-0 flex-1 flex-col gap-4">
        <div className="flex flex-wrap items-center justify-between gap-3 shrink-0">
          <div>
            <h1 className="text-xl font-extrabold text-slate-800">
              {isCreate ? 'Upload SCORM Package' : 'Edit Content Item'}
            </h1>
            <p className="text-sm font-medium text-slate-500">
              {isCreate
                ? 'Upload a SCORM 1.2 or SCORM 2004 ZIP package. Review details before uploading.'
                : 'Adjust display name and content type lookup values.'}
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2 select-none">
            {stepLabels.map(renderStepButton)}
          </div>
        </div>

        <div className="min-h-0 flex-1 flex flex-col">
          {currentStep === 1 ? (
            <div className="overflow-y-auto custom-scrollbar flex-1 pr-1">
              {renderMetadataStep()}
            </div>
          ) : null}
          {isCreate && currentStep === 2 ? (
            <div className="overflow-y-auto custom-scrollbar flex-1 pr-1">
              {renderUploadStep()}
            </div>
          ) : null}
          {((isCreate && currentStep === 3) || (!isCreate && currentStep === 2)) ? (
            <div className="overflow-y-auto custom-scrollbar flex-1 pr-1">
              {renderReviewStep()}
            </div>
          ) : null}
        </div>

        <div className="flex items-center justify-end gap-3 border-t border-slate-200 pt-3 shrink-0">
          <Link to={isCreate ? '/content-library' : `/content-library/${id}`}>
            <button type="button" className="admin-button admin-button--secondary">
              <X aria-hidden="true" />
              <span>Cancel</span>
            </button>
          </Link>

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
            <button type="submit" disabled={saving || uploading} className="admin-button admin-button--primary disabled:opacity-55">
              {uploading || saving ? (
                <Loader2 className="animate-spin" aria-hidden="true" />
              ) : isCreate ? (
                <Upload aria-hidden="true" />
              ) : (
                <Check aria-hidden="true" />
              )}
              <span>{isCreate ? (uploading ? 'Uploading...' : 'Upload Package') : saving ? 'Saving...' : 'Save Changes'}</span>
            </button>
          )}
        </div>
      </form>
    </div>
  )
}
