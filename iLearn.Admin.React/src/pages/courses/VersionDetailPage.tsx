import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { ArrowDown, ArrowUp, BookOpen, Edit3, FileText, Loader2, Plus, Search, X } from 'lucide-react'

import { ApiError, fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { DetailLayout, Fact, FactGrid } from '../../components/ui/detail'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { formatDate } from '../../lib/format'

type LookupResult<T> = T[] | { data?: T[] }

type ApiResponse<T> = {
  success: boolean
  message?: string
  data?: T
}

// Mirrors CourseContentItemDto (iLearn.Application/DTOs/CourseDetailDto.cs)
type CourseContentItem = {
  id: number
  name: string
  typeId: number
  typeName: string
  isActive: boolean
  url?: string | null
}

// Mirrors CourseDetailDto (iLearn.Application/DTOs/CourseDetailDto.cs)
type CourseDetail = {
  id: number
  courseCode: string
  courseName: string
}

// Mirrors CourseVersionDto (iLearn.Application/DTOs/CourseDetailDto.cs)
type CourseVersion = {
  id: number
  courseId: number
  versionNumber: number
  note: string
  isActive: boolean
  versionState: string
  createdAt: string
  contentItems: CourseContentItem[]
}

// Mirrors CourseDashboardDto (iLearn.Application/DTOs/CourseDashboardDtos.cs)
type CourseDashboardData = {
  course: CourseDetail
  versions: CourseVersion[]
}

type ContentLibraryItem = {
  id: number
  name: string
  typeId: number
  typeName?: string
  isActive?: boolean
  isPublished?: boolean
  publishState?: string
  courseIdsCount?: number
  url?: string | null
  URL?: string | null
}

type VersionGeneralForm = {
  note: string
  isActive: boolean
}

type VersionContentDraftItem = {
  uid: string
  id: number
  name: string
  typeId: number
  typeName?: string
  isPublished: boolean
  url?: string | null
}

const unwrapList = <T,>(value: LookupResult<T> | undefined): T[] => {
  if (!value) return []
  return Array.isArray(value) ? value : value.data ?? []
}

const getContentUrl = (item: { url?: string | null; URL?: string | null }) => item.url ?? item.URL ?? null

const createDraftFromVersionItem = (item: CourseContentItem): VersionContentDraftItem => ({
  uid: `VER_${item.id}`,
  id: item.id,
  name: item.name,
  typeId: item.typeId || 1,
  typeName: item.typeName,
  isPublished: !!item.isActive,
  url: item.url ?? null,
})

const createDraftFromLibraryItem = (item: ContentLibraryItem): VersionContentDraftItem => ({
  uid: `LIB_${item.id}`,
  id: item.id,
  name: item.name,
  typeId: item.typeId || 1,
  ...(item.typeName ? { typeName: item.typeName } : {}),
  isPublished: item.isPublished ?? item.isActive ?? false,
  url: getContentUrl(item),
})

const getContentTypeLabel = (item: Pick<VersionContentDraftItem, 'typeId' | 'typeName'>) => {
  if (item.typeName) return item.typeName
  return item.typeId === 2 ? 'Exam' : 'Learn'
}

const getContentReadiness = (item: Pick<VersionContentDraftItem, 'isPublished' | 'url'>) => {
  if (!item.isPublished) {
    return { label: 'Unpublished', tone: 'neutral' as const }
  }

  if (!item.url) {
    return { label: 'Missing Launch', tone: 'warning' as const }
  }

  return { label: 'Published', tone: 'success' as const }
}

const getApiErrorText = (error: unknown, fallback: string) => {
  if (error instanceof ApiError) {
    try {
      const parsed = JSON.parse(error.responseBody) as { message?: string }
      if (parsed.message) return parsed.message
    } catch {
      // Keep fallback path when responseBody is not JSON.
    }

    if (error.message) return error.message
  }

  if (error instanceof Error && error.message) return error.message
  return fallback
}

export function VersionDetailPage() {
  const { courseId, versionId } = useParams()
  const { setLabel } = useBreadcrumbs()

  const parsedCourseId = Number(courseId)
  const parsedVersionId = Number(versionId)

  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<CourseDashboardData | null>(null)

  const [loadingContentLibrary, setLoadingContentLibrary] = useState(false)
  const [contentLibrary, setContentLibrary] = useState<ContentLibraryItem[]>([])

  const [showGeneralEditModal, setShowGeneralEditModal] = useState(false)
  const [showContentEditModal, setShowContentEditModal] = useState(false)
  const [savingGeneral, setSavingGeneral] = useState(false)
  const [savingContent, setSavingContent] = useState(false)

  const [generalForm, setGeneralForm] = useState<VersionGeneralForm>({
    note: '',
    isActive: false,
  })

  const [contentSearch, setContentSearch] = useState('')
  const [contentDraft, setContentDraft] = useState<VersionContentDraftItem[]>([])

  const selectedVersion = useMemo(
    () => data?.versions.find(item => item.id === parsedVersionId) ?? null,
    [data, parsedVersionId],
  )

  useEffect(() => {
    if (data?.course?.courseCode && courseId) {
      setLabel(courseId, data.course.courseCode)
    }
    if (selectedVersion && versionId) {
      setLabel(versionId, `v${selectedVersion.versionNumber}`)
    }
  }, [courseId, data, selectedVersion, setLabel, versionId])

  const loadDashboardData = useCallback(async () => {
    if (!parsedCourseId) return

    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; data: CourseDashboardData }>(
        `Courses/${parsedCourseId}/dashboard`,
      )
      if (resp.success) {
        setData(resp.data)
      }
    } catch (err) {
      console.error(err)
      toast.error('Unable to load version details')
    } finally {
      setLoading(false)
    }
  }, [parsedCourseId])

  const loadContentLibrary = useCallback(async () => {
    setLoadingContentLibrary(true)
    try {
      const libraryData = await fetchWithAccessControl<LookupResult<ContentLibraryItem>>('ContentLibrary/lookup')
      setContentLibrary(unwrapList(libraryData))
    } catch (err) {
      console.error(err)
      toast.error('Unable to load content library')
    } finally {
      setLoadingContentLibrary(false)
    }
  }, [])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadDashboardData()
      void loadContentLibrary()
    }, 0)

    return () => window.clearTimeout(timeoutId)
  }, [loadContentLibrary, loadDashboardData])

  const availableLibraryItems = useMemo(() => {
    const selectedIds = new Set(contentDraft.map(item => item.id))
    const searchText = contentSearch.trim().toLowerCase()

    return contentLibrary
      .filter(item => !selectedIds.has(item.id))
      .filter(item => {
        if (!searchText) return true
        return `${item.name} ${item.typeName ?? ''} ${item.publishState ?? ''}`.toLowerCase().includes(searchText)
      })
  }, [contentDraft, contentLibrary, contentSearch])

  const openGeneralEditModal = () => {
    if (!selectedVersion) return

    setGeneralForm({
      note: selectedVersion.note || '',
      isActive: selectedVersion.isActive,
    })
    setShowGeneralEditModal(true)
  }

  const openContentEditModal = () => {
    if (!selectedVersion) return

    setContentSearch('')
    setContentDraft(selectedVersion.contentItems.map(createDraftFromVersionItem))
    if (!contentLibrary.length && !loadingContentLibrary) {
      void loadContentLibrary()
    }
    setShowContentEditModal(true)
  }

  const addContentToDraft = (item: ContentLibraryItem) => {
    setContentDraft(prev => {
      if (prev.some(existing => existing.id === item.id)) return prev
      return [...prev, createDraftFromLibraryItem(item)]
    })
  }

  const removeContentFromDraft = (uid: string) => {
    setContentDraft(prev => prev.filter(item => item.uid !== uid))
  }

  const moveContentInDraft = (uid: string, direction: -1 | 1) => {
    setContentDraft(prev => {
      const currentIndex = prev.findIndex(item => item.uid === uid)
      const nextIndex = currentIndex + direction
      if (currentIndex < 0 || nextIndex < 0 || nextIndex >= prev.length) return prev

      const next = [...prev]
      const [item] = next.splice(currentIndex, 1)
      if (!item) return prev
      next.splice(nextIndex, 0, item)
      return next
    })
  }

  const saveVersionChanges = useCallback(
    async (input: { note: string; isActive: boolean; contentItems: VersionContentDraftItem[]; successMessage: string }) => {
      if (!selectedVersion || !parsedCourseId) return false

      const body = new FormData()
      body.append('CourseId', String(parsedCourseId))
      body.append('Note', input.note.trim())
      body.append('IsActive', String(input.isActive))
      body.append('LearnerPolicy', 'NewLearnersOnly')

      input.contentItems.forEach(item => {
        body.append('ContentItemIds', String(item.id))
        body.append('ContentTypeIds', String(item.typeId || 1))
      })

      try {
        const response = await fetchWithAccessControl<ApiResponse<CourseVersion>>(`Courses/versions/${selectedVersion.id}`, {
          method: 'PUT',
          body,
        })

        if (response?.success) {
          toast.success(response.message || input.successMessage)
          await loadDashboardData()
          return true
        }

        toast.error(response?.message || 'Unable to update version')
        return false
      } catch (error) {
        console.error(error)
        toast.error(getApiErrorText(error, 'Unable to update version'))
        return false
      }
    },
    [loadDashboardData, parsedCourseId, selectedVersion],
  )

  const handleGeneralSave = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selectedVersion) return

    if (!generalForm.note.trim()) {
      toast.error('Version note is required')
      return
    }

    setSavingGeneral(true)
    try {
      const currentContent = selectedVersion.contentItems.map(createDraftFromVersionItem)
      const isSaved = await saveVersionChanges({
        note: generalForm.note,
        isActive: generalForm.isActive,
        contentItems: currentContent,
        successMessage: 'Version details updated',
      })

      if (isSaved) {
        setShowGeneralEditModal(false)
      }
    } finally {
      setSavingGeneral(false)
    }
  }

  const handleContentSave = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selectedVersion) return

    setSavingContent(true)
    try {
      const isSaved = await saveVersionChanges({
        note: selectedVersion.note || '',
        isActive: selectedVersion.isActive,
        contentItems: contentDraft,
        successMessage: 'Version content updated',
      })

      if (isSaved) {
        setShowContentEditModal(false)
      }
    } finally {
      setSavingContent(false)
    }
  }

  if (loading) {
    return <LoadingState />
  }

  if (!data || !selectedVersion || !courseId) {
    return (
      <NotFoundState
        title="Version Not Found"
        message="The requested course version is missing or has been deleted."
        backTo={courseId ? `/courses/${courseId}` : '/courses'}
        backLabel="Back to course"
      />
    )
  }

  return (
    <>
      <DetailLayout
        sidebar={
          <ControlsSidebar>
            <ControlAction icon={Edit3} onClick={openGeneralEditModal}>
              Edit General Info
            </ControlAction>
            <ControlAction icon={BookOpen} onClick={openContentEditModal}>
              Edit Content
            </ControlAction>
            <ControlAction to={`/courses/${courseId}`} icon={FileText}>
              Back to Course
            </ControlAction>
          </ControlsSidebar>
        }
      >
        <main className="space-y-6">
          <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            <SectionHeader icon={FileText} variant="card">Version Overview</SectionHeader>

            <div className="p-5 space-y-5">
              {selectedVersion.note && (
                <p className="text-sm text-slate-500 leading-relaxed max-w-2xl border-l-2 border-slate-200 pl-3 whitespace-pre-wrap">
                  {selectedVersion.note}
                </p>
              )}

              <FactGrid className={`text-sm ${selectedVersion.note ? 'border-t border-slate-100 pt-5' : 'pt-2'}`}>
                <Fact label="Version" valueClassName="font-semibold">v{selectedVersion.versionNumber}</Fact>
                <Fact label="Course Code" mono valueClassName="font-semibold">{data.course.courseCode}</Fact>
                <Fact label="Status">
                  <StatusBadge tone={selectedVersion.isActive ? 'success' : 'neutral'}>
                    {selectedVersion.isActive ? 'Active Version' : 'Inactive'}
                  </StatusBadge>
                </Fact>
                <Fact label="Created Date" valueClassName="font-semibold">{formatDate(selectedVersion.createdAt)}</Fact>
                <Fact label="SCORM Content Items" valueClassName="font-semibold">{selectedVersion.contentItems.length}</Fact>
              </FactGrid>

              <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-500">
                Use the Controls panel to edit general information and manage version content.
              </div>
            </div>
          </section>
        </main>
      </DetailLayout>

      {showGeneralEditModal && (
        <div className="modal-overlay" onClick={() => setShowGeneralEditModal(false)}>
          <form
            className="modal-window p-5 relative animate-scale-in"
            onClick={event => event.stopPropagation()}
            onSubmit={handleGeneralSave}
          >
            <button
              type="button"
              onClick={() => setShowGeneralEditModal(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 hover:bg-slate-100 rounded transition cursor-pointer"
              aria-label="Close modal"
            >
              <X className="h-4 w-4" />
            </button>

            <div className="mb-4 border-b border-slate-100 pb-3 pr-8 select-none">
              <h3 className="text-sm font-bold text-slate-800">Edit Version General Info</h3>
              <p className="text-xs text-slate-500 mt-1">Update version note and active status.</p>
            </div>

            <div className="space-y-4">
              <div className="space-y-1.5">
                <label htmlFor="version-note" className="wiz-label">Version Name / Note</label>
                <textarea
                  id="version-note"
                  rows={4}
                  value={generalForm.note}
                  onChange={event => setGeneralForm(prev => ({ ...prev, note: event.target.value }))}
                  className="wiz-input resize-y"
                  placeholder="e.g. Added new package set"
                />
              </div>

              <label className="flex items-center gap-3 border border-slate-200 bg-slate-50/30 p-3 rounded cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={generalForm.isActive}
                  onChange={event => setGeneralForm(prev => ({ ...prev, isActive: event.target.checked }))}
                  className="h-4 w-4 rounded border-slate-300 text-indigo-500 focus:ring-indigo-400 cursor-pointer"
                />
                <span className="wiz-label">Set as Active Version</span>
              </label>
            </div>

            <div className="mt-5 pt-4 border-t border-slate-100 flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={() => setShowGeneralEditModal(false)}
                className="px-4 py-2 text-sm font-semibold text-slate-600 hover:bg-slate-100 rounded transition cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={savingGeneral}
                className="inline-flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded transition cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed"
              >
                {savingGeneral && <Loader2 className="h-4 w-4 animate-spin" />}
                Save General Info
              </button>
            </div>
          </form>
        </div>
      )}

      {showContentEditModal && (
        <div className="modal-overlay" onClick={() => setShowContentEditModal(false)}>
          <form
            className="modal-window modal-window-lg p-5 relative animate-scale-in"
            onClick={event => event.stopPropagation()}
            onSubmit={handleContentSave}
          >
            <button
              type="button"
              onClick={() => setShowContentEditModal(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 hover:bg-slate-100 rounded transition cursor-pointer"
              aria-label="Close modal"
            >
              <X className="h-4 w-4" />
            </button>

            <div className="mb-4 border-b border-slate-100 pb-3 pr-8 select-none">
              <h3 className="text-sm font-bold text-slate-800">Edit Content</h3>
              <p className="text-xs text-slate-500 mt-1">Reorder, remove, and add content from SCORM Content library.</p>
            </div>

            <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_280px]">
              <div className="space-y-3">
                <div className="flex items-center justify-between select-none">
                  <h4 className="text-xs font-bold uppercase text-slate-500">Version Content</h4>
                  <span className="text-xs font-semibold text-slate-500">{contentDraft.length} item{contentDraft.length === 1 ? '' : 's'}</span>
                </div>

                <div className="max-h-96 overflow-auto border border-slate-200 rounded custom-scrollbar">
                  <table className="min-w-full divide-y divide-slate-200 text-sm">
                    <thead className="bg-slate-50 text-xs font-bold uppercase text-slate-500 select-none">
                      <tr>
                        <th className="w-14 px-3 py-2 text-left">Order</th>
                        <th className="px-3 py-2 text-left">Content Name</th>
                        <th className="w-24 px-3 py-2 text-left">Type</th>
                        <th className="w-30 px-3 py-2 text-left">Status</th>
                        <th className="w-28 px-3 py-2 text-right">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 bg-white">
                      {contentDraft.length === 0 ? (
                        <tr>
                          <td colSpan={5} className="px-3 py-8 text-center text-xs font-semibold text-slate-400">
                            No content selected.
                          </td>
                        </tr>
                      ) : (
                        contentDraft.map((item, index) => {
                          const readiness = getContentReadiness(item)
                          return (
                            <tr key={item.uid} className="hover:bg-slate-50/40 transition-colors">
                              <td className="px-3 py-2 font-bold text-slate-400">{index + 1}</td>
                              <td className="px-3 py-2 font-semibold text-slate-700 max-w-xs truncate">{item.name}</td>
                              <td className="px-3 py-2 text-xs font-semibold text-slate-500">{getContentTypeLabel(item)}</td>
                              <td className="px-3 py-2">
                                <StatusBadge tone={readiness.tone}>{readiness.label}</StatusBadge>
                              </td>
                              <td className="px-3 py-2">
                                <div className="flex justify-end gap-1">
                                  <button
                                    type="button"
                                    onClick={() => moveContentInDraft(item.uid, -1)}
                                    disabled={index === 0}
                                    className="p-1 text-indigo-500 hover:bg-indigo-50 rounded-md transition cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed"
                                    aria-label="Move content up"
                                  >
                                    <ArrowUp className="h-3.5 w-3.5" />
                                  </button>
                                  <button
                                    type="button"
                                    onClick={() => moveContentInDraft(item.uid, 1)}
                                    disabled={index === contentDraft.length - 1}
                                    className="p-1 text-indigo-500 hover:bg-indigo-50 rounded-md transition cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed"
                                    aria-label="Move content down"
                                  >
                                    <ArrowDown className="h-3.5 w-3.5" />
                                  </button>
                                  <button
                                    type="button"
                                    onClick={() => removeContentFromDraft(item.uid)}
                                    className="p-1 text-red-500 hover:bg-rose-50 rounded-md transition cursor-pointer"
                                    aria-label="Remove content"
                                  >
                                    <X className="h-3.5 w-3.5" />
                                  </button>
                                </div>
                              </td>
                            </tr>
                          )
                        })
                      )}
                    </tbody>
                  </table>
                </div>
              </div>

              <div className="space-y-3">
                <h4 className="text-xs font-bold uppercase text-slate-500 select-none">SCORM Content</h4>

                <div className="flex items-center gap-2 border border-slate-200 bg-white px-2.5 py-1.5 rounded text-xs select-none">
                  <Search className="h-4 w-4 text-slate-400" />
                  <input
                    value={contentSearch}
                    onChange={event => setContentSearch(event.target.value)}
                    placeholder="Search SCORM Content..."
                    className="min-w-0 flex-1 border-0 bg-transparent text-xs text-slate-800 outline-none"
                  />
                </div>

                <div className="max-h-96 divide-y divide-slate-100 overflow-y-auto border border-slate-200 rounded custom-scrollbar select-none">
                  {loadingContentLibrary ? (
                    <div className="px-3 py-8 text-center text-xs font-semibold text-slate-400">Loading SCORM Content...</div>
                  ) : availableLibraryItems.length === 0 ? (
                    <div className="px-3 py-8 text-center text-xs font-semibold text-slate-400">No content found.</div>
                  ) : (
                    availableLibraryItems.map(item => {
                      const draftItem = createDraftFromLibraryItem(item)
                      const readiness = getContentReadiness(draftItem)
                      return (
                        <div key={item.id} className="flex items-center justify-between gap-3 bg-white px-3 py-2 hover:bg-slate-50/50 transition">
                          <div className="min-w-0">
                            <div className="truncate font-bold text-slate-800 text-sm">{item.name}</div>
                            <div className="mt-0.5 flex items-center gap-2 text-xs text-slate-500 font-semibold">
                              <span>{getContentTypeLabel(draftItem)}</span>
                              <StatusBadge size="xxs" tone={readiness.tone}>{readiness.label}</StatusBadge>
                            </div>
                          </div>
                          <button
                            type="button"
                            onClick={() => addContentToDraft(item)}
                            className="rounded-md border border-indigo-100 p-1.5 text-indigo-600 hover:bg-indigo-50 transition cursor-pointer shrink-0"
                            aria-label="Add content"
                          >
                            <Plus className="h-3.5 w-3.5" />
                          </button>
                        </div>
                      )
                    })
                  )}
                </div>
              </div>
            </div>

            <div className="mt-5 pt-4 border-t border-slate-100 flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={() => setShowContentEditModal(false)}
                className="px-4 py-2 text-sm font-semibold text-slate-600 hover:bg-slate-100 rounded transition cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={savingContent}
                className="inline-flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded transition cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed"
              >
                {savingContent && <Loader2 className="h-4 w-4 animate-spin" />}
                Save Content
              </button>
            </div>
          </form>
        </div>
      )}
    </>
  )
}
