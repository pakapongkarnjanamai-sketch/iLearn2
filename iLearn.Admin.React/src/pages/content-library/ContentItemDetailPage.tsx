import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  BookOpen,
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
import {
  DetailLayout,
  Fact,
  FactGrid,
} from '../../components/ui/detail'
import { Card } from '../../components/ui/Card'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { fetchWithAccessControl, buildApiUrl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { formatBytes, formatDateTime, formatNumber } from '../../lib/format'

// Mirrors ContentItemCourseReferenceDto (iLearn.Application/DTOs/ContentItemDto.cs)
type CourseReference = {
  courseId: number
  courseTitle: string
  courseCode: string
  courseVersionId: number
  versionNumber: number
}

// Mirrors ContentItemDto (iLearn.Application/DTOs/ContentItemDto.cs)
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
  courseContentItems?: CourseReference[]
  createdAt?: string
  updatedAt?: string
}

type ContentLaunchResponse = {
  url?: string
}

const TYPE_LABEL: Record<number, string> = { 1: 'Learn', 2: 'Exam' }

export function ContentItemDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { confirm, confirmDialog } = useConfirm()
  const { setLabel } = useBreadcrumbs()
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [item, setItem] = useState<ContentItemDetail | null>(null)

  useEffect(() => {
    if (item?.name && id) {
      setLabel(String(id), item.name)
    }
  }, [item, id, setLabel])

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
    <>
      <DetailLayout
        sidebar={
          <ControlsSidebar>
            <ControlAction to={`/content-library/${item.id}/edit`} icon={Edit3}>
              Edit Metadata
            </ControlAction>
            <ControlAction
              icon={ExternalLink}
              disabled={!item.isActive || !item.url || busy}
              onClick={handleOpenContent}
              title={
                !item.isActive
                  ? 'Content must be published'
                  : !item.url
                    ? 'No launch URL'
                    : undefined
              }
            >
              Open SCORM Player
            </ControlAction>
            <ControlAction
              icon={item.isActive ? PowerOff : Power}
              disabled={busy}
              onClick={item.isActive ? handleUnpublish : handlePublish}
            >
              {item.isActive ? 'Unpublish' : 'Publish'}
            </ControlAction>
            <ControlAction
              icon={Download}
              disabled={!item.fileStorageId}
              onClick={() => {
                if (item.fileStorageId) {
                  window.open(buildApiUrl(`ContentItems/${item.id}/content`), '_blank')
                }
              }}
              title={!item.fileStorageId ? 'No file available' : undefined}
            >
              Download ZIP
            </ControlAction>
            <ControlAction
              icon={Trash2}
              disabled={item.isActive || busy}
              onClick={handleDelete}
              variant="danger"
              title={item.isActive ? 'Unpublish before deleting' : undefined}
            >
              Delete
            </ControlAction>
          </ControlsSidebar>
        }
      >
        <main className="space-y-6">
        <Card icon={Layers} title="Overview" bodyClassName="p-5 space-y-5">
          <FactGrid>
            <Fact label="Status">
              <StatusText tone={item.isActive ? 'success' : 'neutral'}>
                {item.isActive ? 'Published' : 'Draft'}
              </StatusText>
            </Fact>

            <Fact label="Type" valueClassName="font-semibold">
              {TYPE_LABEL[item.typeId] ?? `Type ${item.typeId}`}
            </Fact>

            <Fact label="SCORM Version" valueClassName="font-semibold">
              {item.schemaVersion || '—'}
            </Fact>

            <Fact label="Package Size" valueClassName="font-bold text-slate-800">
                {formatBytes(item.fileLength)}
            </Fact>

            <Fact label="Courses Linked" valueClassName="font-bold text-slate-800">
                {formatNumber(item.courseIdsCount ?? 0)}
            </Fact>

            <Fact label="File Storage Id" mono valueClassName="font-semibold">
              {item.fileStorageId ?? '—'}
            </Fact>

            <Fact label="Created" valueClassName="font-semibold">
              {item.createdAt ? formatDateTime(item.createdAt) : '—'}
            </Fact>

            <Fact label="Updated" valueClassName="font-semibold">
              {item.updatedAt ? formatDateTime(item.updatedAt) : '—'}
            </Fact>
          </FactGrid>

          <FactGrid className="border-t border-slate-100 pt-5">
            <Fact label="Launch Resource" mono colSpan="full">
              {item.launchHref || '—'}
            </Fact>
            <Fact label="Server Path" mono colSpan="full">
              {item.url || '—'}
            </Fact>
          </FactGrid>
        </Card>

        {(item.courseContentItems?.length ?? 0) > 0 && (
          <Card icon={BookOpen} title="Related Courses" bodyClassName="p-0">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-slate-100 text-[10px] font-extrabold uppercase text-slate-400">
                  <th className="px-5 py-2.5 text-left">Course</th>
                  <th className="px-5 py-2.5 text-left">Code</th>
                  <th className="px-5 py-2.5 text-right">Version</th>
                </tr>
              </thead>
              <tbody>
                {item.courseContentItems!.map((ref) => (
                  <tr key={`${ref.courseId}-${ref.courseVersionId}`} className="border-b border-slate-50 last:border-0 hover:bg-slate-50/50">
                    <td className="px-5 py-2.5">
                      <Link to={`/courses/${ref.courseId}`} className="text-indigo-600 hover:text-indigo-800 hover:underline font-medium">
                        {ref.courseTitle}
                      </Link>
                    </td>
                    <td className="px-5 py-2.5 font-mono text-slate-500">{ref.courseCode}</td>
                    <td className="px-5 py-2.5 text-right text-slate-600">v{ref.versionNumber}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>
        )}
        </main>
      </DetailLayout>

      {confirmDialog}
    </>
  )
}
