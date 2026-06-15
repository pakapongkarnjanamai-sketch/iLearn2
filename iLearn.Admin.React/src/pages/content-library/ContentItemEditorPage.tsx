import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Upload,
  Check,
  X
} from 'lucide-react'
import { fetchWithAccessControl, buildApiUrl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { LoadingState } from '../../components/ui/LoadingState'

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
        let errorMsg = 'Upload failed'
        try {
          const errData = await created.json()
          errorMsg = errData.message || errData.error || errorMsg
        } catch {
          errorMsg = (await created.text()) || errorMsg
        }
        throw new Error(errorMsg)
      }
      const result = (await created.json()) as { id: number }
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
    if (!form.name.trim()) {
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
    <div className="space-y-4">

      <div className="space-y-1.5">
        <label className="wiz-label">
          Display Name <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          value={form.name}
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          placeholder="Required"
          className="wiz-input"
        />
      </div>

      <div className="space-y-1.5">
        <label className="wiz-label">Content Type</label>
        <select
          value={form.typeId}
          onChange={(e) => setForm((f) => ({ ...f, typeId: Number(e.target.value) }))}
          className="wiz-input"
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
    <div className="space-y-4">

      <div className="space-y-1.5">
        <label className="wiz-label">SCORM Package (.zip)</label>
        <label className="flex flex-col items-center justify-center gap-2 border border-dashed border-slate-300 bg-slate-50/20 px-4 py-8 rounded-lg text-sm font-bold text-slate-600 hover:bg-slate-50 hover:border-indigo-500 transition cursor-pointer select-none">
          <Upload className="h-6 w-6 text-indigo-500 animate-pulse" />
          <span>Select SCORM ZIP Package</span>
          <span className="text-[11px] font-semibold text-slate-400">Supports SCORM 1.2 & 2004 (Max 100MB or 1,000 internal directory entries)</span>
          <input
            type="file"
            accept=".zip,application/zip"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            className="sr-only"
          />
        </label>
      </div>

      {file && (
        <div className="overflow-x-auto border border-slate-200 rounded mt-4">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-xs font-bold uppercase text-slate-500 select-none">
              <tr>
                <th className="px-3 py-2 text-left">Content Name</th>
                <th className="w-28 px-3 py-2 text-left">Source</th>
                <th className="w-36 px-3 py-2 text-left">Content Type</th>
                <th className="w-28 px-3 py-2 text-left">Status</th>
                <th className="w-28 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              <tr className="hover:bg-slate-50/30 transition-colors">
                <td className="px-3 py-2 font-bold text-slate-700 truncate max-w-xs">
                  <span>{file.name}</span>
                  <span className="block text-[10px] font-semibold text-slate-400 mt-0.5 font-mono">{Math.round(file.size / 1024)} KB</span>
                </td>
                <td className="px-3 py-2 text-slate-400 font-semibold">New upload</td>
                <td className="px-3 py-2">
                  <select
                    value={form.typeId}
                    onChange={(e) => setForm((f) => ({ ...f, typeId: Number(e.target.value) }))}
                    className="wiz-input py-1 text-xs"
                  >
                    {TYPE_OPTIONS.map((o) => (
                      <option key={o.value} value={o.value}>
                        {o.value === 1 ? 'Learn' : 'Exam'}
                      </option>
                    ))}
                  </select>
                </td>
                <td className="px-3 py-2 select-none">
                  <span className="inline-flex border px-1.5 py-0.5 text-xs font-extrabold rounded-sm border-blue-200 bg-blue-50 text-blue-700">Ready</span>
                </td>
                <td className="px-3 py-2 text-right">
                  <button
                    type="button"
                    onClick={() => setFile(null)}
                    className="p-1 text-red-500 hover:bg-rose-50 rounded-md transition cursor-pointer"
                    aria-label="Remove content"
                  >
                    <X className="h-3.5 w-3.5" />
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </div>
  )

  const renderReviewStep = () => {
    const selectedTypeName = TYPE_OPTIONS.find(o => o.value === form.typeId)?.label || 'Instructional Content'
    return (
      <div className="space-y-4">

        <dl className="divide-y divide-slate-100 text-sm select-none">
          <div className="grid grid-cols-3 py-2.5 font-semibold">
            <dt className="wiz-label">Display Name</dt>
            <dd className="col-span-2 text-slate-700 font-bold">{form.name.trim() || (file ? file.name : '—') || 'Unnamed package'}</dd>
          </div>
          <div className="grid grid-cols-3 py-2.5 font-semibold">
            <dt className="wiz-label">Content Type</dt>
            <dd className="col-span-2 text-slate-700">{selectedTypeName}</dd>
          </div>
          {isCreate && (
            <div className="grid grid-cols-3 py-2.5 font-semibold">
              <dt className="wiz-label">Target SCORM Package</dt>
              <dd className="col-span-2 text-slate-700 font-mono">
                {file ? `${file.name} (${Math.round(file.size / 1024)} KB)` : 'No file selected'}
              </dd>
            </div>
          )}
        </dl>

        <div className="p-3 border border-indigo-100 bg-indigo-50/15 rounded-lg text-xs leading-relaxed text-indigo-500 font-semibold select-none">
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

  const steps: WizardStep[] = isCreate
    ? [
        { label: 'Package Upload', validate: validateUpload, render: renderUploadStep },
        { label: 'Review', render: renderReviewStep }
      ]
    : [
        { label: 'Metadata', validate: validateMetadata, render: renderMetadataStep },
        { label: 'Review', render: renderReviewStep }
      ]

  if (loading) {
    return <LoadingState label="Loading content item..." />
  }

  return (
    <AppWizard
      title={isCreate ? 'Upload SCORM Package' : 'Edit SCORM Package'}
      description={isCreate ? 'Upload a SCORM package (.zip).' : 'Update SCORM package details.'}
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
