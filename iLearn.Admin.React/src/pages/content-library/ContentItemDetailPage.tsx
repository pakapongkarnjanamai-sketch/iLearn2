import { useEffect, useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import type { LucideIcon } from 'lucide-react'
import {
  ArrowLeft,
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
    <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start">
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
      <aside className="lg:sticky lg:top-5 rounded-lg border border-slate-200 bg-white p-4 space-y-2">
        <div className="flex items-center gap-2 pb-2 mb-1 border-b border-slate-200">
          <Settings className="h-4 w-4 text-indigo-600" aria-hidden="true" />
          <h2 className="text-sm font-bold text-slate-800">Controls</h2>
        </div>

        <CIControlLink to={`/content-library/${item.id}/edit`} icon={Edit3}>Edit Metadata</CIControlLink>
        <CIControlBtn icon={ExternalLink} disabled={!item.isActive || !item.url || busy} onClick={handleOpenContent} title={!item.isActive ? 'Content must be published' : !item.url ? 'No launch URL' : undefined}>Open SCORM Player</CIControlBtn>
        <CIControlBtn icon={item.isActive ? PowerOff : Power} disabled={busy} onClick={item.isActive ? handleUnpublish : handlePublish}>
          {item.isActive ? 'Unpublish' : 'Publish'}
        </CIControlBtn>
        <CIControlBtn icon={Download} disabled={!item.fileStorageId} onClick={() => { if (item.fileStorageId) window.open(buildApiUrl(`ContentItems/${item.id}/content`), '_blank') }} title={!item.fileStorageId ? 'No file available' : undefined}>
          Download ZIP
        </CIControlBtn>
        <CIControlBtn icon={Trash2} disabled={item.isActive || busy} onClick={handleDelete} variant="danger" title={item.isActive ? 'Unpublish before deleting' : undefined}>
          Delete
        </CIControlBtn>

        <div className="pt-2 border-t border-slate-100">
          <Link to="/content-library" className="w-full flex items-center justify-center gap-1.5 text-slate-400 hover:text-slate-700 transition font-semibold text-xs py-1.5">
            <ArrowLeft className="h-3.5 w-3.5" />
            <span>Back to Library</span>
          </Link>
        </div>
      </aside>
    </div>
  )
}

/* ── Uniform control buttons ── */

type CIControlLinkProps = {
  to: string
  icon: LucideIcon
  children: React.ReactNode
  disabled?: boolean
  title?: string | undefined
}

function CIControlLink({
  to,
  icon: Icon,
  children,
  disabled = false,
  title,
}: CIControlLinkProps) {
  if (disabled) {
    return (
      <button
        type="button"
        disabled
        className="w-full flex items-center gap-2.5 rounded-md border border-slate-100 bg-slate-50 p-2 text-slate-300 cursor-not-allowed text-left focus:outline-none"
        title={title}
      >
        <div className="h-7 w-7 rounded bg-slate-100/50 flex items-center justify-center shrink-0 text-slate-300">
          <Icon className="h-3.5 w-3.5" aria-hidden="true" />
        </div>
        <span className="text-[13px] font-bold">{children}</span>
      </button>
    )
  }

  return (
    <Link
      to={to}
      className="group w-full flex items-center gap-2.5 rounded-md border border-slate-200 bg-white p-2 text-slate-700 hover:border-indigo-300 hover:bg-indigo-50/40 transition cursor-pointer text-left"
      title={title}
    >
      <div className="h-7 w-7 rounded bg-slate-100 group-hover:bg-indigo-100 flex items-center justify-center shrink-0 text-slate-500 group-hover:text-indigo-600 transition-colors">
        <Icon className="h-3.5 w-3.5" aria-hidden="true" />
      </div>
      <span className="text-[13px] font-bold text-slate-800 group-hover:text-indigo-800 transition-colors">{children}</span>
    </Link>
  )
}

type CIControlBtnProps = {
  icon: LucideIcon
  children: React.ReactNode
  disabled?: boolean
  title?: string | undefined
  onClick: () => void
  variant?: 'default' | 'danger'
}

function CIControlBtn({
  icon: Icon,
  children,
  disabled = false,
  title,
  onClick,
  variant = 'default',
}: CIControlBtnProps) {
  if (disabled) {
    return (
      <button
        type="button"
        disabled
        className="w-full flex items-center gap-2.5 rounded-md border border-slate-100 bg-slate-50 p-2 text-slate-300 cursor-not-allowed text-left focus:outline-none"
        title={title}
      >
        <div className="h-7 w-7 rounded bg-slate-100/50 flex items-center justify-center shrink-0 text-slate-300">
          <Icon className="h-3.5 w-3.5" aria-hidden="true" />
        </div>
        <span className="text-[13px] font-bold">{children}</span>
      </button>
    )
  }

  if (variant === 'danger') {
    return (
      <button
        type="button"
        onClick={onClick}
        className="group w-full flex items-center gap-2.5 rounded-md border border-red-200 bg-white p-2 text-red-600 hover:border-red-300 hover:bg-red-50/50 transition cursor-pointer text-left"
        title={title}
      >
        <div className="h-7 w-7 rounded bg-red-50 group-hover:bg-red-100 flex items-center justify-center shrink-0 text-red-500 group-hover:text-red-600 transition-colors">
          <Icon className="h-3.5 w-3.5" aria-hidden="true" />
        </div>
        <span className="text-[13px] font-bold text-red-700 group-hover:text-red-800 transition-colors">{children}</span>
      </button>
    )
  }

  return (
    <button
      type="button"
      onClick={onClick}
      className="group w-full flex items-center gap-2.5 rounded-md border border-slate-200 bg-white p-2 text-slate-700 hover:border-indigo-300 hover:bg-indigo-50/40 transition cursor-pointer text-left"
      title={title}
    >
      <div className="h-7 w-7 rounded bg-slate-100 group-hover:bg-indigo-100 flex items-center justify-center shrink-0 text-slate-500 group-hover:text-indigo-600 transition-colors">
        <Icon className="h-3.5 w-3.5" aria-hidden="true" />
      </div>
      <span className="text-[13px] font-bold text-slate-800 group-hover:text-indigo-800 transition-colors">{children}</span>
    </button>
  )
}
