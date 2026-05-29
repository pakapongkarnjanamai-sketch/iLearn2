import { useEffect, useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import {
  Download,
  Edit3,
  ExternalLink,
  Layers,
  Loader2,
  PowerOff,
  Power,
  Settings,
  Trash2,
} from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { StatusText } from '../../components/ui/StatusText'
import { fetchWithAccessControl, buildApiUrl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { formatDateTime } from '../../lib/format'

type ContentItemDetail = {
  id: number
  name: string
  typeId: number
  isActive: boolean
  schemaVersion?: string | null
  launchHref?: string | null
  url?: string | null
  fileStorageId?: number | null
  fileLength?: number | null
  courseIdsCount?: number
  createdAt?: string
  updatedAt?: string
}

type ContentLaunchResponse = {
  url?: string
}

const TYPE_LABEL: Record<number, string> = { 1: 'Learn', 2: 'Exam' }

const fmtBytes = (bytes?: number | null) => {
  if (!bytes || bytes <= 0) return '—'
  const units = ['B', 'KB', 'MB', 'GB']
  let n = bytes
  let i = 0
  while (n >= 1024 && i < units.length - 1) {
    n /= 1024
    i++
  }
  return `${n.toFixed(n >= 10 || i === 0 ? 0 : 1)} ${units[i]}`
}

export function ContentItemDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [item, setItem] = useState<ContentItemDetail | null>(null)

  const load = async () => {
    setLoading(true)
    try {
      const data = await fetchWithAccessControl<ContentItemDetail>(
        `admin/ContentItemsCRUD/Get/${id}`,
      )
      setItem(data)
    } catch {
      toast.error('Failed to load content item')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelled = false
    const run = async () => {
      try {
        const data = await fetchWithAccessControl<ContentItemDetail>(
          `admin/ContentItemsCRUD/Get/${id}`,
        )
        if (!cancelled) setItem(data)
      } catch {
        if (!cancelled) toast.error('Failed to load content item')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    void run()
    return () => {
      cancelled = true
    }
  }, [id])

  const handlePublish = async () => {
    if (!item) return
    setBusy(true)
    try {
      await fetchWithAccessControl(`ContentItems/SetPublic?key=${item.id}`, { method: 'POST' })
      toast.success('Content published')
      await load()
    } catch {
      toast.error('Publish failed')
    } finally {
      setBusy(false)
    }
  }

  const handleUnpublish = async () => {
    if (!item) return
    if (!window.confirm('Unpublish this content? Extracted files will be removed from the server.')) return
    setBusy(true)
    try {
      await fetchWithAccessControl(`ContentItems/Unpublish?key=${item.id}`, { method: 'POST' })
      toast.success('Content unpublished')
      await load()
    } catch {
      toast.error('Unpublish failed — content may be linked to active course versions')
    } finally {
      setBusy(false)
    }
  }

  const handleOpenContent = async () => {
    if (!item) return
    setBusy(true)
    try {
      const result = await fetchWithAccessControl<ContentLaunchResponse>(`ContentItems/${item.id}/content`)
      if (!result.url) {
        toast.error('Launch URL is not available for this content item')
        return
      }
      window.open(result.url, '_blank', 'noopener,noreferrer')
    } catch {
      toast.error('Failed to open SCORM content')
    } finally {
      setBusy(false)
    }
  }

  const handleDelete = async () => {
    if (!item) return
    if (!window.confirm('Delete this content item permanently? This cannot be undone.')) return
    setBusy(true)
    try {
      const fd = new FormData()
      fd.append('key', String(item.id))
      await fetchWithAccessControl('admin/ContentItemsCRUD/Delete', { method: 'DELETE', body: fd })
      toast.success('Content deleted')
      navigate('/content-library')
    } catch {
      toast.error('Delete failed')
    } finally {
      setBusy(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center gap-2 p-8 text-sm text-slate-500">
        <Loader2 className="h-4 w-4 animate-spin" /> Loading content item...
      </div>
    )
  }

  if (!item) {
    return (
      <div className="p-8 text-sm text-slate-500">
        Content item not found.{' '}
        <Link to="/content-library" className="text-indigo-600 hover:underline">
          Back to library
        </Link>
      </div>
    )
  }

  return (
    <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_320px] xl:items-start">
      <div className="min-w-0">
        <section className="space-y-6">
          <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3">
            <h2 className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600"><Layers aria-hidden="true" />Content Overview</h2>
          </div>

          {/* Minimalist Title */}
          <div>
            <h1 className="text-xl font-extrabold text-slate-900 leading-tight">{item.name}</h1>
            <span className="inline-block mt-1 font-mono text-xs text-slate-400">{TYPE_LABEL[item.typeId] ?? `Type ${item.typeId}`}</span>
          </div>

          {/* Quick facts */}
          <dl className="grid grid-cols-2 sm:grid-cols-3 gap-x-6 gap-y-5 text-xs border-t border-slate-100 pt-5">
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Status</dt>
              <dd className="mt-1">
                <StatusText tone={item.isActive ? 'success' : 'neutral'}>
                  {item.isActive ? 'Published' : 'Draft'}
                </StatusText>
              </dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Type</dt>
              <dd className="mt-1 font-semibold text-slate-700">{TYPE_LABEL[item.typeId] ?? `Type ${item.typeId}`}</dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">SCORM Version</dt>
              <dd className="mt-1 font-semibold text-slate-700">{item.schemaVersion || '—'}</dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Package Size</dt>
              <dd className="mt-1 font-bold text-slate-800">{fmtBytes(item.fileLength)}</dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Courses Linked</dt>
              <dd className="mt-1 font-bold text-slate-800">{item.courseIdsCount ?? 0}</dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">File Storage Id</dt>
              <dd className="mt-1 font-semibold text-slate-700">{item.fileStorageId ?? '—'}</dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Created</dt>
              <dd className="mt-1 font-semibold text-slate-700">{item.createdAt ? formatDateTime(item.createdAt) : '—'}</dd>
            </div>
            <div>
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Updated</dt>
              <dd className="mt-1 font-semibold text-slate-700">{item.updatedAt ? formatDateTime(item.updatedAt) : '—'}</dd>
            </div>
          </dl>

          {/* Technical paths */}
          <dl className="grid grid-cols-1 gap-4 text-xs border-t border-slate-100 pt-5">
            <div className="min-w-0">
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Launch Resource</dt>
              <dd className="mt-1 font-mono text-slate-700 wrap-break-word">{item.launchHref || '—'}</dd>
            </div>
            <div className="min-w-0">
              <dt className="text-slate-400 font-bold uppercase tracking-wider">Server Path</dt>
              <dd className="mt-1 font-mono text-slate-700 wrap-break-word">{item.url || '—'}</dd>
            </div>
          </dl>
        </section>
      </div>

      {/* Controls sidebar */}
      <aside className="space-y-5 xl:sticky xl:top-5">
        <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3">
          <h2 className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600"><Settings aria-hidden="true" />Controls</h2>
        </div>

        <div className="space-y-3">
          <span className="block text-xxs font-extrabold text-slate-400 uppercase">Management Actions</span>
          <Link to={`/content-library/${item.id}/edit`} className="block">
            <AppButton variant="secondary" icon={Edit3} className="w-full">Edit Metadata</AppButton>
          </Link>
          {item.isActive ? (
            <>
              {item.url && (
                <AppButton variant="secondary" icon={ExternalLink} disabled={busy} onClick={handleOpenContent} className="w-full">
                  Open SCORM Player
                </AppButton>
              )}
              <AppButton variant="danger" icon={PowerOff} disabled={busy} onClick={handleUnpublish} className="w-full">
                Unpublish
              </AppButton>
            </>
          ) : (
            <AppButton variant="primary" icon={Power} disabled={busy} onClick={handlePublish} className="w-full">
              Publish
            </AppButton>
          )}
          {!item.isActive && item.fileStorageId && (
            <a
              href={buildApiUrl(`ContentItems/${item.id}/content`)}
              className="inline-flex w-full items-center justify-center gap-2 rounded-md border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 hover:border-slate-300 hover:bg-slate-50"
            >
              <Download className="h-4 w-4" /> Download ZIP
            </a>
          )}
        </div>

        {!item.isActive && (
          <div className="border-t border-slate-200 pt-4">
            <AppButton variant="danger" icon={Trash2} disabled={busy} onClick={handleDelete} className="w-full">
              Delete
            </AppButton>
          </div>
        )}
      </aside>
    </div>
  )
}
