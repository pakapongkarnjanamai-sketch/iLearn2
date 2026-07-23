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
  X,
} from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { IconButton } from '../../components/ui/IconButton'
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
import { COMMON_LABELS, COURSE_LABELS, UI_LABELS, contentTypeLabel, t } from '../../lib/labels'
import { useSession } from '../../lib/sessionContext'

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


export function ContentItemDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { confirm, confirmDialog } = useConfirm()
  const { setLabel } = useBreadcrumbs()
  const { isSuperAdmin } = useSession()
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [item, setItem] = useState<ContentItemDetail | null>(null)

  // Edit Metadata modal state
  const [showEditMetadataModal, setShowEditMetadataModal] = useState(false)
  const [editName, setEditName] = useState('')
  const [editTypeId, setEditTypeId] = useState(1)
  const [savingMetadata, setSavingMetadata] = useState(false)

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
      toast.error(t(COURSE_LABELS.failedToLoadContentItem))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  const handleSaveMetadata = async () => {
    if (!item) return
    if (!editName.trim()) {
      toast.error(t(COURSE_LABELS.displayNameRequired))
      return
    }
    setSavingMetadata(true)
    try {
      const fd = new FormData()
      fd.append('key', String(item.id))
      fd.append('values', JSON.stringify({ name: editName.trim(), typeId: editTypeId }))
      await fetchWithAccessControl('admin/ContentItemsCRUD/Put', { method: 'PUT', body: fd })
      toast.success(t(COURSE_LABELS.metadataUpdated))
      setShowEditMetadataModal(false)
      await load()
    } catch {
      toast.error(t(COURSE_LABELS.saveFailed))
    } finally {
      setSavingMetadata(false)
    }
  }

  const handlePublish = async () => {
    if (!item) return
    setBusy(true)
    try {
      await fetchWithAccessControl(`ContentItems/SetPublic?key=${item.id}`, { method: 'POST' })
      toast.success(t(COURSE_LABELS.contentPublished))
      await load()
    } catch {
      toast.error(t(COURSE_LABELS.publishFailed))
    } finally {
      setBusy(false)
    }
  }

  const handleUnpublish = async () => {
    if (!item) return
    if (!(await confirm({
      title: t(COURSE_LABELS.unpublish),
      message: t(COURSE_LABELS.unpublishContentConfirm),
      confirmLabel: t(COURSE_LABELS.unpublish),
    }))) return
    setBusy(true)
    try {
      await fetchWithAccessControl(`ContentItems/Unpublish?key=${item.id}`, { method: 'POST' })
      toast.success(t(COURSE_LABELS.contentUnpublished))
      await load()
    } catch {
      toast.error(t(COURSE_LABELS.unpublishFailed))
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
        toast.error(t(COURSE_LABELS.launchUrlUnavailable))
        return
      }
      window.open(result.url, '_blank', 'noopener,noreferrer')
    } catch {
      toast.error(t(COURSE_LABELS.failedToOpenScorm))
    } finally {
      setBusy(false)
    }
  }

  const handleDelete = async () => {
    if (!item) return
    if (!(await confirm({
      title: t(COURSE_LABELS.delete),
      message: t(COURSE_LABELS.deleteContentConfirm),
      confirmLabel: t(COURSE_LABELS.delete),
      danger: true,
    }))) return
    setBusy(true)
    try {
      const fd = new FormData()
      fd.append('key', String(item.id))
      await fetchWithAccessControl('admin/ContentItemsCRUD/Delete', { method: 'DELETE', body: fd })
      toast.success(t(COURSE_LABELS.delete))
      navigate('/content-library')
    } catch {
      toast.error(t(COURSE_LABELS.deleteFailed))
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
        title={t(COURSE_LABELS.contentNotFound)}
        message={t(COURSE_LABELS.contentNotFound)}
        backTo="/content-library"
        backLabel={t(COURSE_LABELS.backToLibrary)}
      />
    )
  }

  return (
    <>
      <DetailLayout
        sidebar={
          <ControlsSidebar>
            {isSuperAdmin && (
              <ControlAction
                icon={Edit3}
                onClick={() => {
                  setEditName(item.name)
                  setEditTypeId(item.typeId)
                  setShowEditMetadataModal(true)
                }}
              >
                {t(COURSE_LABELS.editGeneralInfo)}
              </ControlAction>
            )}
            <ControlAction
              icon={ExternalLink}
              disabled={!item.isActive || !item.url || busy}
              onClick={handleOpenContent}
              title={
                !item.isActive
                  ? t(COURSE_LABELS.publish)
                  : !item.url
                    ? t(COURSE_LABELS.launchUrlUnavailable)
                    : undefined
              }
            >
              {t(COURSE_LABELS.openScormPlayer)}
            </ControlAction>
            {isSuperAdmin && (
              <ControlAction
                icon={item.isActive ? PowerOff : Power}
                disabled={busy}
                onClick={item.isActive ? handleUnpublish : handlePublish}
              >
                {t(item.isActive ? COURSE_LABELS.unpublish : COURSE_LABELS.publish)}
              </ControlAction>
            )}
            <ControlAction
              icon={Download}
              disabled={!item.fileStorageId}
              onClick={() => {
                if (item.fileStorageId) {
                  window.open(buildApiUrl(`ContentItems/${item.id}/content`), '_blank')
                }
              }}
              title={!item.fileStorageId ? t(COURSE_LABELS.noFileSelected) : undefined}
            >
              {t(COURSE_LABELS.downloadZip)}
            </ControlAction>
            {isSuperAdmin && (
              <ControlAction
                icon={Trash2}
                disabled={item.isActive || busy}
                onClick={handleDelete}
                variant="danger"
                title={item.isActive ? t(COURSE_LABELS.unpublish) : undefined}
              >
                {t(COURSE_LABELS.delete)}
              </ControlAction>
            )}
          </ControlsSidebar>
        }
      >
        <main className="space-y-6">
        <Card icon={Layers} title={t(COURSE_LABELS.overview)} bodyClassName="p-5 space-y-5">
          <FactGrid>
            <Fact label={t(COURSE_LABELS.status)}>
              <StatusText
                active={item.isActive}
                activeLabel={t(COMMON_LABELS.published)}
                inactiveLabel={t(COMMON_LABELS.draft)}
              />
            </Fact>

            <Fact label={t(COURSE_LABELS.type)} valueClassName="font-semibold">
              {contentTypeLabel(item.typeId)}
            </Fact>

            <Fact label={t(COURSE_LABELS.scormVersion)} valueClassName="font-semibold">
              {item.schemaVersion || '—'}
            </Fact>

            <Fact label={t(COURSE_LABELS.packageSize)} valueClassName="font-bold text-slate-800">
                {formatBytes(item.fileLength)}
            </Fact>

            <Fact label={t(COURSE_LABELS.coursesLinked)} valueClassName="font-bold text-slate-800">
                {formatNumber(item.courseIdsCount ?? 0)}
            </Fact>

            <Fact label={t(COURSE_LABELS.fileStorageId)} mono valueClassName="font-semibold">
              {item.fileStorageId ?? '—'}
            </Fact>

            <Fact label={t(COURSE_LABELS.createdDate)} valueClassName="font-semibold">
              {item.createdAt ? formatDateTime(item.createdAt) : '—'}
            </Fact>

            <Fact label={t(COURSE_LABELS.updated)} valueClassName="font-semibold">
              {item.updatedAt ? formatDateTime(item.updatedAt) : '—'}
            </Fact>
          </FactGrid>

          <FactGrid className="border-t border-slate-100 pt-5">
            <Fact label={t(COURSE_LABELS.launchResource)} mono colSpan="full">
              {item.launchHref || '—'}
            </Fact>
            <Fact label={t(COURSE_LABELS.serverPath)} mono colSpan="full">
              {item.url || '—'}
            </Fact>
          </FactGrid>
        </Card>

        {(item.courseContentItems?.length ?? 0) > 0 && (
          <Card icon={BookOpen} title={t(COURSE_LABELS.courses)} bodyClassName="p-0">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-slate-100 text-[10px] font-extrabold uppercase text-slate-400">
                  <th className="px-5 py-2.5 text-left">{t(COURSE_LABELS.courseTitle)}</th>
                  <th className="px-5 py-2.5 text-left">{t(COURSE_LABELS.code)}</th>
                  <th className="px-5 py-2.5 text-right">{t(COURSE_LABELS.version)}</th>
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

      {/* Edit Metadata Modal */}
      {showEditMetadataModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-fade-in"
          onClick={() => setShowEditMetadataModal(false)}
        >
          <div
            className="bg-white border border-slate-200 rounded-xl shadow-2xl w-full max-w-lg flex flex-col overflow-hidden animate-scale-up"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none shrink-0">
              <div className="flex items-center gap-2">
                <Edit3 className="h-5 w-5 text-indigo-600" />
                <h3 className="text-sm font-extrabold text-slate-800 uppercase tracking-wider">
                  {t(COURSE_LABELS.editGeneralInfo)}
                </h3>
              </div>
              <IconButton
                onClick={() => setShowEditMetadataModal(false)}
                icon={X}
                title={t(COURSE_LABELS.close)}
                tone="neutral"
              />
            </div>

            <div className="p-6 space-y-4">
              <div className="space-y-1.5">
                <label className="block text-xs font-bold text-slate-700">
                  {t(COURSE_LABELS.name)} <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                  placeholder={t(COURSE_LABELS.required)}
                  className="w-full px-3 py-2 border border-slate-200 rounded-lg text-xs font-medium focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition"
                />
              </div>

              <div className="space-y-1.5">
                <label className="block text-xs font-bold text-slate-700">{t(COURSE_LABELS.contentType)}</label>
                <select
                  value={editTypeId}
                  onChange={(e) => setEditTypeId(Number(e.target.value))}
                  className="w-full px-3 py-2 border border-slate-200 rounded-lg text-xs font-medium focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition bg-white cursor-pointer"
                >
                  <option value={1}>{contentTypeLabel(1)}</option>
                  <option value={2}>{contentTypeLabel(2)}</option>
                </select>
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50 shrink-0 select-none">
              <AppButton
                variant="ghost"
                onClick={() => setShowEditMetadataModal(false)}
              >
                {t(UI_LABELS.cancel)}
              </AppButton>
              <AppButton
                variant="primary"
                loading={savingMetadata}
                onClick={handleSaveMetadata}
              >
                {t(COURSE_LABELS.saveChanges)}
              </AppButton>
            </div>
          </div>
        </div>
      )}

      {confirmDialog}
    </>
  )
}
