import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import { ArrowLeft, Save, Upload, Loader2 } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { AppButton } from '../../components/ui/AppButton'
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

  if (loading) {
    return (
      <div className="flex items-center gap-2 p-8 text-sm text-slate-500">
        <Loader2 className="h-4 w-4 animate-spin" /> Loading content item...
      </div>
    )
  }

  return (
    <>
      <PageHeader
        title=""
        actions={
          <Link to={isCreate ? '/content-library' : `/content-library/${id}`}>
            <AppButton variant="ghost" icon={ArrowLeft}>
              Cancel
            </AppButton>
          </Link>
        }
      />

      <div className="mb-4">
        <div className="text-xxs font-extrabold uppercase text-slate-400">Content Library</div>
        <h1 className="font-display text-2xl font-bold text-slate-900">
          {isCreate ? 'Upload SCORM Package' : 'Edit Content Item'}
        </h1>
        <p className="mt-1 text-xs text-slate-500">
          {isCreate
            ? 'Upload a SCORM 1.2 or SCORM 2004 ZIP package. The item starts as Draft; publish from the detail page to extract and serve content.'
            : 'Adjust display name and content type. To replace the SCORM package, delete the item and upload a new one.'}
        </p>
      </div>

      <form
        onSubmit={isCreate ? handleUpload : handleSave}
        className="max-w-xl space-y-4 border-t border-slate-200/60 pt-4"
      >
        <div>
          <label className="text-xxs font-extrabold uppercase text-slate-500">Display Name</label>
          <input
            type="text"
            value={form.name}
            onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
            placeholder={isCreate ? 'Leave blank to use ZIP filename' : 'Required'}
            className="mt-1 w-full rounded border border-slate-200 px-2 py-1.5 text-sm focus:border-blue-600 focus:outline-none"
          />
        </div>

        <div>
          <label className="text-xxs font-extrabold uppercase text-slate-500">Content Type</label>
          <select
            value={form.typeId}
            onChange={(e) => setForm((f) => ({ ...f, typeId: Number(e.target.value) }))}
            className="mt-1 w-full rounded border border-slate-200 px-2 py-1.5 text-sm focus:border-blue-600 focus:outline-none"
          >
            {TYPE_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </div>

        {isCreate && (
          <div>
            <label className="text-xxs font-extrabold uppercase text-slate-500">SCORM Package (.zip)</label>
            <input
              type="file"
              accept=".zip,application/zip"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className="mt-1 block w-full rounded border border-dashed border-slate-300 px-3 py-3 text-xs"
            />
            <p className="mt-1.5 text-xxs text-slate-400">
              Supports SCORM 1.2 and SCORM 2004. Maximum 100&nbsp;MB / 1000 entries.
            </p>
            {file && (
              <p className="mt-1 text-xs font-semibold text-slate-600">
                Selected: {file.name} ({Math.round(file.size / 1024)} KB)
              </p>
            )}
          </div>
        )}

        <div className="pt-2">
          <AppButton
            variant="primary"
            type="submit"
            icon={isCreate ? Upload : Save}
            disabled={saving || uploading}
          >
            {isCreate ? (uploading ? 'Uploading...' : 'Upload Package') : saving ? 'Saving...' : 'Save Changes'}
          </AppButton>
        </div>
      </form>
    </>
  )
}
