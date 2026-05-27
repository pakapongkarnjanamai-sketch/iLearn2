import { useEffect, useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import {
  CheckCircle2,
  Download,
  Edit3,
  ExternalLink,
  Layers,
  Loader2,
  PowerOff,
  Power,
  Trash2,
} from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { AppButton } from '../../components/ui/AppButton'
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
    <>
      <PageHeader
        title=""
        actions={
          <div className="flex items-center gap-2">
            <Link to={`/content-library/${item.id}/edit`}>
              <AppButton variant="secondary" icon={Edit3}>
                Edit
              </AppButton>
            </Link>
          </div>
        }
      />

      <header className="mb-3">
        <div className="text-xxs font-extrabold uppercase text-slate-400">Content Library</div>
        <h1 className="font-display text-2xl font-bold text-slate-900">{item.name}</h1>
      </header>

      {/* KPI strip */}
      <div className="admin-card admin-kpi-strip mb-4">
        {[
          { label: 'Type', value: TYPE_LABEL[item.typeId] ?? `Type ${item.typeId}` },
          {
            label: 'Status',
            value: item.isActive ? (
              <span className="inline-flex items-center gap-1 rounded bg-emerald-100 px-1.5 py-0.5 text-xs font-bold text-emerald-700">
                <CheckCircle2 className="h-3 w-3" /> Published
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 rounded bg-slate-100 px-1.5 py-0.5 text-xs font-bold text-slate-600">
                Draft
              </span>
            ),
          },
          { label: 'SCORM', value: item.schemaVersion || '—' },
          { label: 'Package Size', value: fmtBytes(item.fileLength) },
          { label: 'Courses Linked', value: item.courseIdsCount ?? 0 },
        ].map((kpi) => (
          <div
            key={kpi.label}
            className="admin-kpi-item"
          >
            <div className="admin-kpi-label">{kpi.label}</div>
            <div className="mt-1 text-base font-bold text-slate-800">{kpi.value}</div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-4">
        {/* Main: metadata */}
        <section className="admin-card lg:col-span-3">
          <div className="admin-card-head">
            <h2 className="admin-card-head-title"><Layers aria-hidden="true" />Metadata</h2>
          </div>
          <dl className="admin-card-meta-grid">
            {[
              { label: 'Launch Resource', value: item.launchHref || '—' },
              { label: 'Server Path', value: item.url || '—' },
              { label: 'File Storage Id', value: item.fileStorageId ?? '—' },
              { label: 'SCORM Version', value: item.schemaVersion || '—' },
              { label: 'Created', value: item.createdAt ? formatDateTime(item.createdAt) : '—' },
              { label: 'Updated', value: item.updatedAt ? formatDateTime(item.updatedAt) : '—' },
            ].map((row) => (
              <div
                key={row.label}
                className="admin-card-meta-item"
              >
                <dt className="admin-card-meta-label">{row.label}</dt>
                <dd className="admin-card-meta-value text-sm">
                  {row.value}
                </dd>
              </div>
            ))}
          </dl>
        </section>

        {/* Sidebar: control hub */}
        <aside className="admin-card lg:col-span-1">
            <div className="admin-card-head">
              <h2 className="admin-card-head-title">Controls</h2>
            </div>
            <div className="flex flex-col gap-2">
              {item.isActive ? (
                <>
                  {item.url && (
                    <AppButton
                      variant="secondary"
                      icon={ExternalLink}
                      disabled={busy}
                      onClick={handleOpenContent}
                    >
                      Open SCORM Player
                    </AppButton>
                  )}
                  <AppButton variant="danger" icon={PowerOff} disabled={busy} onClick={handleUnpublish}>
                    Unpublish
                  </AppButton>
                </>
              ) : (
                <AppButton variant="primary" icon={Power} disabled={busy} onClick={handlePublish}>
                  Publish
                </AppButton>
              )}
              {!item.isActive && item.fileStorageId && (
                <a
                  href={buildApiUrl(`ContentItems/${item.id}/content`)}
                  className="inline-flex items-center justify-center gap-2 rounded border border-slate-200 px-2 py-2 text-xs font-bold text-slate-700 hover:border-slate-300 hover:bg-slate-50"
                >
                  <Download className="h-4 w-4" /> Download ZIP
                </a>
              )}
              {!item.isActive && (
                <AppButton variant="ghost" icon={Trash2} disabled={busy} onClick={handleDelete}>
                  Delete
                </AppButton>
              )}
            </div>
        </aside>
      </div>
    </>
  )
}
