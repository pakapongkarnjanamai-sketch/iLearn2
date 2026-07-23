import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { 
  ArrowDown, 
  ArrowUp, 
  BookOpen, 
  Plus, 
  Search, 
  Upload,
  X
} from 'lucide-react'

import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { Badge } from '../../components/ui/Badge'
import { IconButton } from '../../components/ui/IconButton'
import { LoadingState } from '../../components/ui/LoadingState'
import { getContentReadinessBadgeModel, ReadinessBadge } from '../../components/ui/ReadinessBadge'
import { CONTENT_TYPE_LABELS, COURSE_LABELS, contentTypeLabel, t, tf } from '../../lib/labels'

type LoadResult<T> = T[] | { data?: T[] }

type ContentLibraryItem = CourseContentApiItem & {
  courseIdsCount?: number
}

// Mirrors ApiResponse<T> (iLearn.Domain/Common/ApiResponse.cs)
type ApiResponse<T> = {
  success: boolean
  message?: string
  data?: T
}

type CourseContentApiItem = {
  id: number
  name: string
  typeId: number
  typeName?: string | undefined
  isActive?: boolean
  isPublished?: boolean
  publishState?: string
  url?: string | null
  URL?: string | null
}

type CourseVersionData = {
  id: number
  note?: string
  isActive?: boolean
  contentItems?: CourseContentApiItem[]
}

// Mirrors CourseVersionLearnerImpactDto (iLearn.Application/DTOs/CourseVersionLearnerImpactDto.cs)
type VersionImpact = {
  courseId: number
  notStartedCount: number
  inProgressCount: number
  completedCount: number
  otherOpenCount: number
  eligibleOpenCount: number
  hasEligibleOpenLearners: boolean
}

type VersionFormData = {
  note: string
  isActive: boolean
  learnerPolicy: 'NewLearnersOnly' | 'MoveNotStarted' | 'ResetInProgress'
}

type SelectedContentItem = {
  uid: string
  source: 'library' | 'upload'
  id?: number
  name: string
  typeId: number
  typeName?: string | undefined
  isActive?: boolean
  url?: string | null
  file?: File
}

const contentTypeOptions = [
  { id: 1, name: CONTENT_TYPE_LABELS.learn },
  { id: 2, name: CONTENT_TYPE_LABELS.exam }
]

const learnerPolicyOptions = [
  {
    value: 'NewLearnersOnly' as const,
    title: COURSE_LABELS.learners,
    note: COURSE_LABELS.version
  },
  {
    value: 'MoveNotStarted' as const,
    title: COURSE_LABELS.notSelected,
    note: COURSE_LABELS.version
  },
  {
    value: 'ResetInProgress' as const,
    title: COURSE_LABELS.progress,
    note: COURSE_LABELS.version
  }
]

function unwrapList<T>(value: LoadResult<T> | undefined): T[] {
  if (!value) return []
  return Array.isArray(value) ? value : value.data ?? []
}

function getUrl(contentItem: CourseContentApiItem) {
  return contentItem.url ?? contentItem.URL ?? null
}

function createLibrarySelection(contentItem: CourseContentApiItem): SelectedContentItem {
  return {
    uid: `LIB_${contentItem.id}`,
    source: 'library',
    id: contentItem.id,
    name: contentItem.name,
    typeId: contentItem.typeId || 1,
    typeName: contentItem.typeName,
    isActive: contentItem.isPublished ?? contentItem.isActive ?? false,
    url: getUrl(contentItem)
  }
}

function getContentReadiness(item: SelectedContentItem) {
  return getContentReadinessBadgeModel({
    source: item.source,
    isActive: item.isActive,
    url: item.url,
  })
}

function getApiErrorText(error: unknown, fallback: string) {
  if (error instanceof Error && error.message) return error.message
  return fallback
}

export function VersionFormPage() {
  const { courseId, id } = useParams()
  const isEditMode = !!id
  const navigate = useNavigate()
  const { setLabel } = useBreadcrumbs()

  const parsedCourseId = Number(courseId)

  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [currentStep, setCurrentStep] = useState(1)
  const [showLibraryPopup, setShowLibraryPopup] = useState(false)
  const [contentSearch, setContentSearch] = useState('')
  const [contentLibrary, setContentLibrary] = useState<ContentLibraryItem[]>([])
  const [contentItems, setContentItems] = useState<SelectedContentItem[]>([])
  const [impact, setImpact] = useState<VersionImpact | null>(null)

  const [formData, setFormData] = useState<VersionFormData>({
    note: '',
    isActive: true,
    learnerPolicy: 'NewLearnersOnly'
  })

  useEffect(() => {
    if (parsedCourseId) {
      fetchWithAccessControl<ApiResponse<{ code: string; title: string; courseCode?: string }>>(`Courses/${parsedCourseId}`)
        .then(resp => {
          if (resp.success && resp.data) {
            const code = resp.data.courseCode || resp.data.code
            if (code) {
              setLabel(String(courseId), code)
            }
          }
        })
        .catch(error => {
          console.error(error)
          toast.error(t(COURSE_LABELS.failedToLoadCourseDetails))
        })
    }
  }, [parsedCourseId, courseId, setLabel])

  const loadContentLibrary = useCallback(async () => {
    try {
      const result = await fetchWithAccessControl<LoadResult<ContentLibraryItem>>('ContentLibrary/lookup')
      setContentLibrary(unwrapList(result))
    } catch (error) {
      console.error(error)
      toast.error(t(COURSE_LABELS.failedToLoadContentLibrary))
    }
  }, [])

  const visibleContentLibrary = useMemo(() => {
    const normalizedSearch = contentSearch.trim().toLowerCase()
    const selectedIds = new Set(contentItems.filter(item => item.source === 'library').map(item => item.id))

    return contentLibrary
      .filter(item => !selectedIds.has(item.id))
      .filter(item => {
        if (!normalizedSearch) return true
        return `${item.name} ${item.typeName || ''} ${item.publishState || ''}`.toLowerCase().includes(normalizedSearch)
      })
      .slice(0, 50)
  }, [contentLibrary, contentItems, contentSearch])

  const addExistingContent = (item: ContentLibraryItem) => {
    setContentItems(prev => {
      if (prev.some(contentItem => contentItem.source === 'library' && contentItem.id === item.id)) return prev
      return [...prev, createLibrarySelection(item)]
    })
  }

  const loadVersionImpact = useCallback(async () => {
    if (!parsedCourseId) return
    try {
      const result = await fetchWithAccessControl<ApiResponse<VersionImpact>>(`Courses/${parsedCourseId}/version-impact`)
      setImpact(result.data || null)
    } catch (error) {
      console.error(error)
      toast.error(t(COURSE_LABELS.saveFailed))
      setImpact(null)
    }
  }, [parsedCourseId])

  const loadVersionData = useCallback(async () => {
    if (!isEditMode || !id) return
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<ApiResponse<CourseVersionData>>(`Courses/versions/${id}`)
      if (resp.success && resp.data) {
        setFormData(prev => ({
          ...prev,
          note: resp.data?.note || '',
          isActive: resp.data?.isActive ?? true
        }))
        setContentItems((resp.data.contentItems || []).map(createLibrarySelection))
      }
    } catch (error) {
      console.error(error)
      toast.error(t(COURSE_LABELS.failedToLoadVersionDetails))
    } finally {
      setLoading(false)
    }
  }, [id, isEditMode])

  useEffect(() => {
    void loadContentLibrary()
    void loadVersionImpact()
  }, [loadContentLibrary, loadVersionImpact])

  useEffect(() => {
    void loadVersionData()
  }, [loadVersionData])

  const handleChange = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = event.target
    setFormData(prev => ({ ...prev, [name]: value }))
  }

  const handleCheckboxChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { name, checked } = event.target
    setFormData(prev => ({ ...prev, [name]: checked }))
  }

  const validateDetails = () => {
    if (!formData.note.trim()) {
      toast.error(t(COURSE_LABELS.versionNoteRequired))
      return false
    }

    return true
  }

  const validateContent = () => {
    if (contentItems.length === 0) {
      toast.error(t(COURSE_LABELS.contentRequired))
      return false
    }

    return true
  }

  const addUploadedFiles = (files: FileList | null) => {
    if (!files || files.length === 0) return

    const queuedFiles = Array.from(files).map((file, index): SelectedContentItem => ({
      uid: `UPLOAD_${Date.now()}_${index}_${file.name}`,
      source: 'upload',
      name: file.name,
      typeId: 1,
      file
    }))

    setContentItems(prev => [...prev, ...queuedFiles])
    toast.success(tf(COURSE_LABELS.fileCountQueued, queuedFiles.length, queuedFiles.length === 1 ? '' : 's'))
  }

  const removeContentItem = (uid: number | string) => {
    setContentItems(prev => prev.filter(item => item.uid !== uid))
  }

  const moveContentItem = (uid: string, direction: -1 | 1) => {
    setContentItems(prev => {
      const index = prev.findIndex(item => item.uid === uid)
      const nextIndex = index + direction
      if (index < 0 || nextIndex < 0 || nextIndex >= prev.length) return prev

      const nextItems = [...prev]
      const [item] = nextItems.splice(index, 1)
      if (!item) return prev
      nextItems.splice(nextIndex, 0, item)
      return nextItems
    })
  }

  const updateUploadContentType = (uid: string, typeId: number) => {
    setContentItems(prev => prev.map(item => item.uid === uid ? { ...item, typeId } : item))
  }

  const buildVersionFormData = () => {
    const body = new FormData()
    body.append('CourseId', String(parsedCourseId))
    body.append('Note', formData.note.trim())
    body.append('IsActive', String(formData.isActive))
    body.append('LearnerPolicy', formData.learnerPolicy)

    contentItems.forEach(item => {
      if (item.source === 'upload' && item.file) {
        body.append('Files', item.file)
        body.append('ContentItemIds', '0')
      } else if (item.id) {
        body.append('ContentItemIds', String(item.id))
      }

      body.append('ContentTypeIds', String(item.typeId || 1))
    })

    return body
  }

  const handleSubmit = async () => {
    if (!validateDetails() || !validateContent()) return

    setSaving(true)
    try {
      const endpoint = isEditMode ? `Courses/versions/${id}` : `Courses/${parsedCourseId}/versions`
      const method = isEditMode ? 'PUT' : 'POST'
      const resp = await fetchWithAccessControl<ApiResponse<CourseVersionData>>(endpoint, {
        method,
        body: buildVersionFormData()
      })

      if (resp.success) {
        toast.success(resp.message || t(isEditMode ? COURSE_LABELS.versionUpdated : COURSE_LABELS.versionCreated))
        navigate(`/courses/${parsedCourseId}`)
      }
    } catch (error: unknown) {
      console.error(error)
      toast.error(getApiErrorText(error, t(COURSE_LABELS.failedToSaveVersion)))
    } finally {
      setSaving(false)
    }
  }

  const renderDetailsStep = () => (
    <div className="space-y-4">
      <div className="space-y-1.5">
        <label htmlFor="note" className="wiz-label">
          {t(COURSE_LABELS.version)} <span className="text-red-500">*</span>
        </label>
        <textarea
          id="note"
          name="note"
          value={formData.note}
          onChange={handleChange}
          rows={4}
          placeholder="e.g. Added new V2 materials"
          className="wiz-input resize-y"
        />
      </div>
      <div className="flex items-center gap-3 border border-slate-200 bg-slate-50/20 p-3 rounded select-none">
        <input
          type="checkbox"
          id="isActive"
          name="isActive"
          checked={formData.isActive}
          onChange={handleCheckboxChange}
          className="h-4 w-4 rounded border-slate-300 text-indigo-500 focus:ring-indigo-400 cursor-pointer"
        />
        <label htmlFor="isActive" className="wiz-label cursor-pointer">{t(COURSE_LABELS.setActiveVersion)}</label>
      </div>
    </div>
  )

  const renderContentRows = () => {
    if (contentItems.length === 0) {
      return (
        <div className="flex min-h-28 items-center justify-center border border-dashed border-slate-200 rounded text-sm font-semibold text-slate-400 select-none py-6">
          {t(COURSE_LABELS.noContentSelected)}
        </div>
      )
    }

    return (
      <div className="overflow-x-auto border border-slate-200 rounded">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50 text-xs font-bold uppercase text-slate-500 select-none">
            <tr>
              <th className="w-12 px-3 py-2 text-left">{t(COURSE_LABELS.order)}</th>
              <th className="px-3 py-2 text-left">{t(COURSE_LABELS.contentName)}</th>
              <th className="w-28 px-3 py-2 text-left">{t(COURSE_LABELS.source)}</th>
              <th className="w-36 px-3 py-2 text-left">{t(COURSE_LABELS.contentType)}</th>
              <th className="w-28 px-3 py-2 text-left">{t(COURSE_LABELS.status)}</th>
              <th className="w-28 px-3 py-2 text-right">{t(COURSE_LABELS.actions)}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {contentItems.map((item, index) => {
              const readiness = getContentReadiness(item)
              return (
                <tr key={item.uid} className="hover:bg-slate-50/30 transition-colors">
                  <td className="px-3 py-2 font-bold text-slate-400">{index + 1}</td>
                  <td className="px-3 py-2 font-bold text-slate-700 truncate max-w-xs">{item.name}</td>
                  <td className="px-3 py-2 text-slate-400 font-semibold">{t(item.source === 'upload' ? COURSE_LABELS.newUpload : COURSE_LABELS.contentLibrary)}</td>
                  <td className="px-3 py-2">
                    {item.source === 'upload' ? (
                      <select
                        value={item.typeId}
                        onChange={event => updateUploadContentType(item.uid, Number(event.target.value))}
                        className="wiz-input py-1"
                      >
                        {contentTypeOptions.map(option => <option key={option.id} value={option.id}>{t(option.name)}</option>)}
                      </select>
                    ) : (
                      <span className="font-semibold text-slate-600">{item.typeName || contentTypeLabel(item.typeId)}</span>
                    )}
                  </td>
                  <td className="px-3 py-2 select-none">
                    <ReadinessBadge label={readiness.label} tone={readiness.tone} ready={readiness.ready} />
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex justify-end gap-1">
                      <IconButton
                        type="button"
                        onClick={() => moveContentItem(item.uid, -1)}
                        disabled={index === 0}
                        icon={ArrowUp}
                        tone="primary"
                        size="sm"
                        title={t(COURSE_LABELS.moveContentUp)}
                      />
                      <IconButton
                        type="button"
                        onClick={() => moveContentItem(item.uid, 1)}
                        disabled={index === contentItems.length - 1}
                        icon={ArrowDown}
                        tone="primary"
                        size="sm"
                        title={t(COURSE_LABELS.moveContentDown)}
                      />
                      <IconButton
                        type="button"
                        onClick={() => removeContentItem(item.uid)}
                        icon={X}
                        tone="danger"
                        size="sm"
                        title={t(COURSE_LABELS.removeContent)}
                      />
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    )
  }

  const renderContentStep = () => (
    <div className="space-y-4">
      <div className="flex justify-end mb-1 select-none">
        <Badge tone="neutral">{tf(COURSE_LABELS.itemCount, contentItems.length, contentItems.length === 1 ? '' : 's')}</Badge>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 select-none">
        <label className="flex cursor-pointer flex-col items-center justify-center gap-1.5 border border-dashed border-slate-300 bg-slate-50/30 px-3 py-6 rounded text-sm font-bold text-slate-600 hover:bg-slate-50 hover:border-blue-500 transition duration-150">
          <Upload className="h-5 w-5 text-indigo-500" />
          <span>{t(COURSE_LABELS.uploadNewScorm)}</span>
          <span className="text-xs font-semibold text-slate-400">.zip packages · multiple allowed</span>
          <input
            type="file"
            accept=".zip"
            multiple
            className="sr-only"
            onChange={event => {
              addUploadedFiles(event.target.files)
              event.target.value = ''
            }}
          />
        </label>

        <button
          type="button"
          onClick={() => {
            setContentSearch('')
            setShowLibraryPopup(true)
          }}
          className="flex cursor-pointer flex-col items-center justify-center gap-1.5 border border-dashed border-slate-300 bg-slate-50/30 px-3 py-6 rounded text-sm font-bold text-slate-600 hover:bg-slate-50 hover:border-indigo-500 transition duration-150"
        >
          <BookOpen className="h-5 w-5 text-indigo-600" />
          <span>{t(COURSE_LABELS.selectExistingContent)}</span>
          <span className="text-xs font-semibold text-slate-400">Reuse packages from the Content Library</span>
        </button>
      </div>

      <div>
        {renderContentRows()}
      </div>
    </div>
  )

  const renderOptionsStep = () => (
    <div className="space-y-4 select-none">

      <div className="grid grid-cols-2 gap-2.5 md:grid-cols-4 select-none">
        <div className="border border-slate-200 rounded p-3 bg-slate-50/10">
          <div className="text-lg font-extrabold text-slate-800">{impact?.notStartedCount ?? 0}</div>
          <div className="text-xs font-extrabold uppercase text-slate-400">Not Started</div>
        </div>
        <div className="border border-slate-200 rounded p-3 bg-slate-50/10">
          <div className="text-lg font-extrabold text-slate-800">{impact?.inProgressCount ?? 0}</div>
          <div className="text-xs font-extrabold uppercase text-slate-400">In Progress</div>
        </div>
        <div className="border border-slate-200 rounded p-3 bg-slate-50/10">
          <div className="text-lg font-extrabold text-slate-800">{impact?.completedCount ?? 0}</div>
          <div className="text-xs font-extrabold uppercase text-slate-400">Completed</div>
        </div>
        <div className="border border-slate-200 rounded p-3 bg-slate-50/10">
          <div className="text-lg font-extrabold text-slate-800">{impact?.otherOpenCount ?? 0}</div>
          <div className="text-xs font-extrabold uppercase text-slate-400">Other Open</div>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-3 select-none">
        {learnerPolicyOptions.map(option => (
          <label key={option.value} className={`flex cursor-pointer gap-2.5 border rounded p-3 transition-all duration-150 ${formData.learnerPolicy === option.value ? 'border-blue-500 bg-indigo-50/20' : 'border-slate-200 bg-white hover:border-slate-300'}`}>
            <input
              type="radio"
              name="learnerPolicy"
              value={option.value}
              checked={formData.learnerPolicy === option.value}
              onChange={() => setFormData(prev => ({ ...prev, learnerPolicy: option.value }))}
              disabled={!formData.isActive}
              className="mt-0.5 h-3.5 w-3.5 border-slate-300 text-indigo-500 focus:ring-indigo-400 disabled:opacity-40 cursor-pointer"
            />
            <span>
                <span className="block text-sm font-bold text-slate-800">{t(option.title)}</span>
                <span className="mt-1 block text-xs font-semibold text-slate-400 leading-normal">{t(option.note)}</span>
            </span>
          </label>
        ))}
      </div>
    </div>
  )

  const renderReviewStep = () => (
    <div className="space-y-4">

      <dl className="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">
        <div className="border-b border-slate-100 pb-2.5 sm:col-span-2">
          <dt className="wiz-label">Version Name / Note</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{formData.note || 'Not set'}</dd>
        </div>
        <div className="border-b border-slate-100 pb-2.5">
          <dt className="wiz-label">Status</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{formData.isActive ? 'Active Version' : 'Inactive Version'}</dd>
        </div>
        <div className="border-b border-slate-100 pb-2.5">
          <dt className="wiz-label">Learner Policy</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{t(learnerPolicyOptions.find(option => option.value === formData.learnerPolicy)?.title || COURSE_LABELS.notSet)}</dd>
        </div>
      </dl>

      <div className="flex items-center justify-between pt-1 select-none">
        <span className="wiz-label">{t(COURSE_LABELS.contentItems)}</span>
        <Badge tone="neutral">{tf(COURSE_LABELS.itemCount, contentItems.length, contentItems.length === 1 ? '' : 's')}</Badge>
      </div>
      {renderContentRows()}
    </div>
  )

  const steps: WizardStep[] = [
    { label: t(COURSE_LABELS.details), validate: () => validateDetails(), render: () => renderDetailsStep() },
    { label: t(COURSE_LABELS.content), validate: () => validateContent(), render: () => renderContentStep() },
    { label: t(COURSE_LABELS.options), render: () => renderOptionsStep() },
    { label: t(COURSE_LABELS.review), render: () => renderReviewStep() }
  ]

  if (loading) {
    return <LoadingState label={t(COURSE_LABELS.failedToLoadVersionDetails)} />
  }

  return (
    <>
      <AppWizard
        title={t(isEditMode ? COURSE_LABELS.editVersion : COURSE_LABELS.newVersion)}
        description={t(isEditMode ? COURSE_LABELS.editVersion : COURSE_LABELS.newVersion)}
        eyebrow={t(COURSE_LABELS.versionControl)}
        steps={steps}
        currentStep={currentStep}
        onStepChange={setCurrentStep}
        onCancel={() => navigate(`/courses/${parsedCourseId}`)}
        onSubmit={handleSubmit}
        submitLabel={t(isEditMode ? COURSE_LABELS.updateVersion : COURSE_LABELS.createVersion)}
        isSubmitting={saving}
      />

      {/* Backdrop-blurred Library Picker Modal Overlay */}
      {showLibraryPopup && (
        <div
          className="modal-overlay"
          onClick={() => setShowLibraryPopup(false)}
        >
          <div
            className="modal-window modal-window-lg p-5 relative animate-scale-in"
            onClick={e => e.stopPropagation()}
          >
            <IconButton
              type="button"
              onClick={() => setShowLibraryPopup(false)}
              icon={X}
              title="Close"
              tone="neutral"
              size="sm"
              className="absolute top-4 right-4 z-10"
            />

            <div className="mb-4 flex items-center gap-2 border-b border-slate-100 pb-3 pr-8 select-none">
              <BookOpen className="h-5 w-5 text-indigo-600" />
              <div>
                <h3 className="text-sm font-bold text-slate-800">Select Existing Content</h3>
                <p className="text-xs font-semibold text-slate-400">Choose from SCORM packages in the Content Library</p>
              </div>
            </div>

            <div className="mb-3.5 flex items-center gap-2 border border-slate-200 bg-white px-2.5 py-1.5 rounded text-xs select-none">
              <Search className="h-4 w-4 text-slate-400" />
              <input
                value={contentSearch}
                onChange={event => setContentSearch(event.target.value)}
                placeholder="Search Content Library..."
                className="min-w-0 flex-1 border-0 bg-transparent text-xs text-slate-800 outline-none"
              />
            </div>

            <div className="max-h-80 divide-y divide-slate-100 overflow-y-auto border border-slate-200 rounded custom-scrollbar select-none">
              {visibleContentLibrary.length === 0 ? (
                <div className="px-3 py-8 text-center text-xs font-semibold text-slate-400">No content found matching search</div>
              ) : visibleContentLibrary.map(item => {
                const readiness = getContentReadiness(createLibrarySelection(item))
                return (
                  <div key={item.id} className="flex items-center justify-between gap-3 bg-white px-3 py-2 hover:bg-slate-50/50 transition">
                    <div className="min-w-0">
                      <div className="truncate font-bold text-slate-800 text-sm">{item.name}</div>
                      <div className="mt-0.5 flex items-center gap-2 text-xs text-slate-500 font-semibold">
                        <span>{item.typeName || (item.typeId === 2 ? 'Exam' : 'Learn')}</span>
                        <ReadinessBadge size="xxs" label={readiness.label} tone={readiness.tone} ready={readiness.ready} />
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => addExistingContent(item)}
                      className="rounded-md border border-indigo-100 p-1.5 text-indigo-600 hover:bg-indigo-50 transition cursor-pointer shrink-0"
                      aria-label="Add content"
                    >
                      <Plus className="h-3.5 w-3.5" />
                    </button>
                  </div>
                )
              })}
            </div>
          </div>
        </div>
      )}
    </>
  )
}