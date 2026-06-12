import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Download,
  Edit3,
  ExternalLink,
  Layers,
  PowerOff,
  Power,
  Trash2,
} from 'lucide-react'
import { StatusText } from '../../components/ui/StatusText'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { useConfirm } from '../../components/ui/ConfirmDialog'
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
  const { confirm, confirmDialog } = useConfirm()
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
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
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
    if (!(await confirm({
      title: 'Unpublish Content',
      message: 'Unpublish this content? Extracted files will be removed from the server.',
      confirmLabel: 'Unpublish',
    }))) return
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
    if (!(await confirm({
      title: 'Delete Content Item',
      message: 'Delete this content item permanently? This cannot be undone.',
      confirmLabel: 'Delete',
      danger: true,
    }))) return
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
    return <LoadingState />
  }

  if (!item) {
    return (
      <NotFoundState
        title="Content Item Not Found"
        message="The requested content item is missing or has been deleted."
        backTo="/content-library"
        backLabel="Back to library"
      />
    )
  }

  return (
    <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start">
      <div className="min-w-0">
        <section className="space-y-6">
          <SectionHeader icon={Layers}>Content Overview</SectionHeader>

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
      <ControlsSidebar backTo="/content-library" backLabel="Back to Library">
        <ControlAction to={`/content-library/${item.id}/edit`} icon={Edit3}>Edit Metadata</ControlAction>
        <ControlAction icon={ExternalLink} disabled={!item.isActive || !item.url || busy} onClick={handleOpenContent} title={!item.isActive ? 'Content must be published' : !item.url ? 'No launch URL' : undefined}>Open SCORM Player</ControlAction>
        <ControlAction icon={item.isActive ? PowerOff : Power} disabled={busy} onClick={item.isActive ? handleUnpublish : handlePublish}>
          {item.isActive ? 'Unpublish' : 'Publish'}
        </ControlAction>
        <ControlAction icon={Download} disabled={!item.fileStorageId} onClick={() => { if (item.fileStorageId) window.open(buildApiUrl(`ContentItems/${item.id}/content`), '_blank') }} title={!item.fileStorageId ? 'No file available' : undefined}>
          Download ZIP
        </ControlAction>
        <ControlAction icon={Trash2} disabled={item.isActive || busy} onClick={handleDelete} variant="danger" title={item.isActive ? 'Unpublish before deleting' : undefined}>
          Delete
        </ControlAction>
      </ControlsSidebar>

      {confirmDialog}
    </div>
  )
}
