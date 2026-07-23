import { useEffect, useState, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Upload,
  Check,
  X
} from 'lucide-react'
import { fetchWithAccessControl, uploadWithProgress, type UploadProgress } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { IconButton } from '../../components/ui/IconButton'
import { LoadingState } from '../../components/ui/LoadingState'
import { ReadinessBadge } from '../../components/ui/ReadinessBadge'
import { formatBytes } from '../../lib/format'
import { CONTENT_TYPE_LABELS, COURSE_LABELS, contentTypeLabel, t } from '../../lib/labels'
import { UploadProgressOverlay } from '../../components/shared/UploadProgressOverlay'

const TYPE_OPTIONS = [
  { value: 1, label: COURSE_LABELS.instructionalContent },
  { value: 2, label: CONTENT_TYPE_LABELS.exam },
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
  const [uploadProgress, setUploadProgress] = useState<UploadProgress | null>(null)
  const [form, setForm] = useState<ContentItemForm>({ name: '', typeId: 1 })
  const [file, setFile] = useState<File | null>(null)
  const abortUploadRef = useRef<(() => void) | null>(null)
  
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
      .catch(() => toast.error(t(COURSE_LABELS.failedToLoadContentItem)))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [id, isCreate])

  const handleUpload = async () => {
    if (!file) {
      toast.error(t(COURSE_LABELS.selectScormZip))
      return
    }
    if (!file.name.toLowerCase().endsWith('.zip')) {
      toast.error(t(COURSE_LABELS.invalidScormZip))
      return
    }
    setUploading(true)
    setUploadProgress({
      phase: 'uploading',
      loadedBytes: 0,
      totalBytes: file.size,
      percent: 0,
    })
    try {
      const fd = new FormData()
      fd.append('file', file)
      
      const { promise, abort } = uploadWithProgress<{ id: number }>(
        `ContentItems/upload?typeId=${form.typeId}`,
        fd,
        {
          method: 'POST',
          onProgress: (p) => {
            setUploadProgress(p)
          },
        }
      )
      abortUploadRef.current = abort

      const result = await promise
      toast.success(t(COURSE_LABELS.scormPackageUploaded))
      navigate(`/content-library/${result.id}`)
    } catch (err: any) {
      if (err.isAborted) {
        toast.info(t(COURSE_LABELS.uploadCancelled))
      } else {
        console.error(err)
        toast.error(err.message || t(COURSE_LABELS.uploadFailed))
      }
    } finally {
      setUploading(false)
      setUploadProgress(null)
      abortUploadRef.current = null
    }
  }

  const handleSave = async () => {
    if (!id) return
    if (!form.name.trim()) {
      toast.error(t(COURSE_LABELS.nameRequired))
      return
    }
    setSaving(true)
    try {
      const fd = new FormData()
      fd.append('key', id)
      fd.append('values', JSON.stringify({ name: form.name, typeId: form.typeId }))
      await fetchWithAccessControl('admin/ContentItemsCRUD/Put', { method: 'PUT', body: fd })
      toast.success(t(COURSE_LABELS.contentItemUpdated))
      navigate(`/content-library/${id}`)
    } catch {
      toast.error(t(COURSE_LABELS.saveFailed))
    } finally {
      setSaving(false)
    }
  }

  const validateMetadata = () => {
    if (!form.name.trim()) {
      toast.error(t(COURSE_LABELS.nameRequired))
      return false
    }
    return true
  }

  const validateUpload = () => {
    if (isCreate && !file) {
      toast.error(t(COURSE_LABELS.selectScormZip))
      return false
    }
    if (isCreate && file && !file.name.toLowerCase().endsWith('.zip')) {
      toast.error(t(COURSE_LABELS.invalidScormZip))
      return false
    }
    return true
  }

  const renderMetadataStep = () => (
    <div className="space-y-4">

      <div className="space-y-1.5">
        <label className="wiz-label">
          {t(COURSE_LABELS.name)} <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          value={form.name}
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          placeholder={t(COURSE_LABELS.required)}
          className="wiz-input"
        />
      </div>

      <div className="space-y-1.5">
        <label className="wiz-label">{t(COURSE_LABELS.contentType)}</label>
        <select
          value={form.typeId}
          onChange={(e) => setForm((f) => ({ ...f, typeId: Number(e.target.value) }))}
          className="wiz-input"
        >
          {TYPE_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>
              {t(o.label)}
            </option>
          ))}
        </select>
      </div>
    </div>
  )

  const renderUploadStep = () => (
    <div className="space-y-4">

      <div className="space-y-1.5">
        <label className="wiz-label">{t(COURSE_LABELS.scormPackage)}</label>
        <label className="flex flex-col items-center justify-center gap-2 border border-dashed border-slate-300 bg-slate-50/20 px-4 py-8 rounded-lg text-sm font-bold text-slate-600 hover:bg-slate-50 hover:border-indigo-500 transition cursor-pointer select-none">
          <Upload className="h-6 w-6 text-indigo-500 animate-pulse" />
          <span>{t(COURSE_LABELS.selectScormPackage)}</span>
          <span className="text-[11px] font-semibold text-slate-400">Supports SCORM 1.2 & 2004 (Max 1 GB, extracted up to 2.5 GB, or 1,000 internal directory entries)</span>
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
                <th className="px-3 py-2 text-left">{t(COURSE_LABELS.contentName)}</th>
                <th className="w-28 px-3 py-2 text-left">{t(COURSE_LABELS.source)}</th>
                <th className="w-36 px-3 py-2 text-left">{t(COURSE_LABELS.contentType)}</th>
                <th className="w-28 px-3 py-2 text-left">{t(COURSE_LABELS.status)}</th>
                <th className="w-28 px-3 py-2 text-right">{t(COURSE_LABELS.actions)}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              <tr className="hover:bg-slate-50/30 transition-colors">
                <td className="px-3 py-2 font-bold text-slate-700 truncate max-w-xs">
                  <span>{file.name}</span>
                  <span className="block text-[10px] font-semibold text-slate-400 mt-0.5 font-mono">{formatBytes(file.size)}</span>
                </td>
                <td className="px-3 py-2 text-slate-400 font-semibold">{t(COURSE_LABELS.newUpload)}</td>
                <td className="px-3 py-2">
                  <select
                    value={form.typeId}
                    onChange={(e) => setForm((f) => ({ ...f, typeId: Number(e.target.value) }))}
                    className="wiz-input py-1 text-xs"
                  >
                    {TYPE_OPTIONS.map((o) => (
                      <option key={o.value} value={o.value}>
                        {contentTypeLabel(o.value)}
                      </option>
                    ))}
                  </select>
                </td>
                <td className="px-3 py-2 select-none">
                  <ReadinessBadge ready tone="info" />
                </td>
                <td className="px-3 py-2 text-right">
                  <IconButton
                    type="button"
                    onClick={() => setFile(null)}
                    icon={X}
                    tone="danger"
                    size="sm"
                    aria-label={t(COURSE_LABELS.removeContent)}
                    title={t(COURSE_LABELS.removeContent)}
                  />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </div>
  )

  const renderReviewStep = () => {
    const selectedTypeName = t(TYPE_OPTIONS.find(o => o.value === form.typeId)?.label || COURSE_LABELS.instructionalContent)
    return (
      <div className="space-y-4">

        <dl className="divide-y divide-slate-100 text-sm select-none">
          <div className="grid grid-cols-3 py-2.5 font-semibold">
            <dt className="wiz-label">{t(COURSE_LABELS.name)}</dt>
            <dd className="col-span-2 text-slate-700 font-bold">{form.name.trim() || (file ? file.name : '-') || t(COURSE_LABELS.unnamedPackage)}</dd>
          </div>
          <div className="grid grid-cols-3 py-2.5 font-semibold">
            <dt className="wiz-label">{t(COURSE_LABELS.contentType)}</dt>
            <dd className="col-span-2 text-slate-700">{selectedTypeName}</dd>
          </div>
          {isCreate && (
            <div className="grid grid-cols-3 py-2.5 font-semibold">
              <dt className="wiz-label">{t(COURSE_LABELS.scormPackage)}</dt>
              <dd className="col-span-2 text-slate-700 font-mono">
                {file ? `${file.name} (${formatBytes(file.size)})` : t(COURSE_LABELS.noFileSelected)}
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
        { label: t(COURSE_LABELS.packageUpload), validate: validateUpload, render: renderUploadStep },
        { label: t(COURSE_LABELS.review), render: renderReviewStep }
      ]
    : [
        { label: t(COURSE_LABELS.metadata), validate: validateMetadata, render: renderMetadataStep },
        { label: t(COURSE_LABELS.review), render: renderReviewStep }
      ]

  if (loading) {
    return <LoadingState label={t(COURSE_LABELS.failedToLoadContentItem)} />
  }

  return (
    <>
      <AppWizard
        title={t(isCreate ? COURSE_LABELS.uploadScormPackage : COURSE_LABELS.editScormPackage)}
        description={t(isCreate ? COURSE_LABELS.uploadScormPackage : COURSE_LABELS.editScormPackage)}
        eyebrow={t(COURSE_LABELS.contentLibrary)}
        steps={steps}
        currentStep={currentStep}
        onStepChange={setCurrentStep}
        onCancel={() => navigate(isCreate ? '/content-library' : `/content-library/${id}`)}
        onSubmit={isCreate ? handleUpload : handleSave}
        submitLabel={t(isCreate ? COURSE_LABELS.uploadPackage : COURSE_LABELS.saveChanges)}
        isSubmitting={saving || uploading}
        submitIcon={isCreate ? <Upload className="h-3.5 w-3.5" /> : <Check className="h-3.5 w-3.5" />}
      />
      {uploading && uploadProgress && (
        <UploadProgressOverlay
          phase={uploadProgress.phase}
          loadedBytes={uploadProgress.loadedBytes}
          totalBytes={uploadProgress.totalBytes}
          percent={uploadProgress.percent}
          fileName={file?.name || ''}
          onCancel={() => {
            if (abortUploadRef.current) {
              abortUploadRef.current()
            }
          }}
        />
      )}
    </>
  )
}
