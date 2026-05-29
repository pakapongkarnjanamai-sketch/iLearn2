import { useEffect, useState, useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { 
  Upload, 
  Loader2, 
  Check, 
  Library, 
  Info, 
  FileArchive,
  X 
} from 'lucide-react'
import { fetchWithAccessControl, buildApiUrl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'

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

  const handleUpload = async () => {
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

  const handleSave = async () => {
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

  const renderMetadataStep = () => (
    <div className="max-w-xl space-y-3.5">
      <div className="flex items-center gap-2 border-b border-slate-100 pb-2.5 mb-1.5">
        <Library className="h-4 w-4 text-indigo-500" />
        <h2 className="text-xs font-bold text-slate-800">Content Item Specifications</h2>
      </div>

      <div className="space-y-1">
        <label className="block text-xxs font-extrabold text-slate-400 uppercase">
          Display Name {!isCreate && <span className="text-red-500">*</span>}
        </label>
        <input
          type="text"
          value={form.name}
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          placeholder={isCreate ? 'Leave blank to use ZIP filename as fallback' : 'Required'}
          className="w-full rounded border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none"
        />
      </div>

      <div className="space-y-1">
        <label className="block text-xxs font-extrabold text-slate-400 uppercase">Content Type</label>
        <select
          value={form.typeId}
          onChange={(e) => setForm((f) => ({ ...f, typeId: Number(e.target.value) }))}
          className="w-full rounded border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none cursor-pointer"
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
    <div className="max-w-xl space-y-3.5">
      <div className="flex items-center gap-2 border-b border-slate-100 pb-2.5 mb-1.5">
        <Upload className="h-4 w-4 text-indigo-500" />
        <h2 className="text-xs font-bold text-slate-800">SCORM Package Upload</h2>
      </div>

      <div className="space-y-1">
        <label className="block text-xxs font-extrabold text-slate-400 uppercase">SCORM Package (.zip)</label>
        <input
          type="file"
          accept=".zip,application/zip"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          className="block w-full rounded border border-dashed border-slate-300 px-3 py-4 text-xs text-slate-500 bg-slate-50/20 hover:bg-slate-50 hover:border-blue-500 transition cursor-pointer"
        />
        <p className="mt-1 text-xxs text-slate-400 font-medium leading-relaxed">
          Supports SCORM 1.2 and SCORM 2004 standards. Maximum bundle size limit is 100 MB or 1,000 internal directory entries.
        </p>
      </div>

      {file && (
        <div className="mt-3 p-3 border border-indigo-100 bg-indigo-50/15 rounded flex items-center justify-between select-none">
          <div className="flex items-center gap-2.5">
            <FileArchive className="h-6 w-6 text-indigo-500 shrink-0" />
            <div className="min-w-0">
              <p className="text-xs font-bold text-slate-700 truncate">{file.name}</p>
              <p className="text-xxs font-mono text-slate-400 mt-0.5">{Math.round(file.size / 1024)} KB</p>
            </div>
          </div>
          <button 
            type="button" 
            onClick={() => setFile(null)}
            className="p-1 rounded-full text-slate-400 hover:bg-slate-100 hover:text-slate-600 transition"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </div>
      )}
    </div>
  )

  const renderReviewStep = () => {
    const selectedTypeName = TYPE_OPTIONS.find(o => o.value === form.typeId)?.label || 'Instructional Content'
    return (
      <div className="max-w-xl space-y-3.5">
        <div className="border border-slate-200 rounded-lg bg-white shadow-xs p-4">
          <div className="flex items-center gap-2 border-b border-slate-100 pb-2.5 mb-3">
            <Info className="h-4 w-4 text-indigo-500" />
            <h2 className="text-xs font-bold text-slate-800">Review Specifications</h2>
          </div>

          <dl className="divide-y divide-slate-100 text-xs select-none">
            <div className="grid grid-cols-3 py-2 font-semibold">
              <dt className="text-slate-400 uppercase font-bold text-xxs">Display Name</dt>
              <dd className="col-span-2 text-slate-700 font-bold">{form.name.trim() || (file ? file.name : '—') || 'Unnamed package'}</dd>
            </div>
            <div className="grid grid-cols-3 py-2 font-semibold">
              <dt className="text-slate-400 uppercase font-bold text-xxs">Content Type</dt>
              <dd className="col-span-2 text-slate-700">{selectedTypeName}</dd>
            </div>
            {isCreate && (
              <div className="grid grid-cols-3 py-2 font-semibold">
                <dt className="text-slate-400 uppercase font-bold text-xxs">Target SCORM Package</dt>
                <dd className="col-span-2 text-slate-700 font-mono">
                  {file ? `${file.name} (${Math.round(file.size / 1024)} KB)` : 'No file selected'}
                </dd>
              </div>
            )}
          </dl>
        </div>

        <div className="p-3 border border-indigo-100 bg-indigo-50/15 rounded-lg text-xxs leading-relaxed text-indigo-500 font-semibold select-none">
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

  const steps: WizardStep[] = useMemo(() => {
    if (isCreate) {
      return [
        { label: 'Metadata', validate: validateMetadata, render: renderMetadataStep },
        { label: 'Package Upload', validate: validateUpload, render: renderUploadStep },
        { label: 'Review', render: renderReviewStep }
      ]
    }
    return [
      { label: 'Metadata', validate: validateMetadata, render: renderMetadataStep },
      { label: 'Review', render: renderReviewStep }
    ]
  }, [isCreate, form, file])

  if (loading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="h-6 w-6 animate-spin text-indigo-500" />
          <span className="text-xs text-slate-500 font-medium">Loading content item...</span>
        </div>
      </div>
    )
  }

  return (
    <AppWizard
      title={isCreate ? 'Upload SCORM Package' : 'Edit Content Item'}
      description={isCreate ? 'Upload a SCORM 1.2 or SCORM 2004 ZIP package.' : 'Adjust display name and content type lookup values.'}
      eyebrow="Content Library"
      steps={steps}
      currentStep={currentStep}
      onStepChange={setCurrentStep}
      onCancel={() => navigate(isCreate ? '/content-library' : `/content-library/${id}`)}
      onSubmit={isCreate ? handleUpload : handleSave}
      submitLabel={isCreate ? 'Upload Package' : 'Save Changes'}
      isSubmitting={saving || uploading}
      submitIcon={isCreate ? <Upload className="h-3.5 w-3.5" /> : <Check className="h-3.5 w-3.5" />}
    />
  )
}
