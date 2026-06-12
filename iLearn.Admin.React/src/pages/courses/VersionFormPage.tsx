import { useEffect, useMemo, useState } from 'react'
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
import { LoadingState } from '../../components/ui/LoadingState'

type LoadResult<T> = T[] | { data?: T[] }

type ContentLibraryItem = CourseContentApiItem & {
  courseIdsCount?: number
}

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
  { id: 1, name: 'Learn' },
  { id: 2, name: 'Exam' }
]

const learnerPolicyOptions = [
  {
    value: 'NewLearnersOnly' as const,
    title: 'New Learners Only',
    note: 'Existing learners stay on their enrolled version.'
  },
  {
    value: 'MoveNotStarted' as const,
    title: 'Move Not Started',
    note: 'Only learners who have not started move to the new version.'
  },
  {
    value: 'ResetInProgress' as const,
    title: 'Reset In Progress',
    note: 'Open learners move to the new version and in-progress learning resets.'
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
  if (item.source === 'upload') {
    return { label: 'Queued Upload', className: 'bg-indigo-50 text-blue-700 border-indigo-100' }
  }

  if (!item.isActive) {
    return { label: 'Not Ready', className: 'bg-red-50 text-red-700 border-red-100' }
  }

  if (!item.url) {
    return { label: 'Missing Launch', className: 'bg-amber-50 text-amber-700 border-amber-100' }
  }

  return { label: 'Published', className: 'bg-emerald-50 text-emerald-700 border-emerald-100' }
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
        .catch(console.error)
    }
  }, [parsedCourseId, courseId, setLabel])

  const loadContentLibrary = async () => {
    try {
      const result = await fetchWithAccessControl<LoadResult<ContentLibraryItem>>('ContentLibrary/lookup')
      setContentLibrary(unwrapList(result))
    } catch (error) {
      console.error(error)
      toast.error('Failed to load content library')
    }
  }

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

  const loadVersionImpact = async () => {
    if (!parsedCourseId) return
    try {
      const result = await fetchWithAccessControl<ApiResponse<VersionImpact>>(`Courses/${parsedCourseId}/version-impact`)
      setImpact(result.data || null)
    } catch (error) {
      console.error(error)
      setImpact(null)
    }
  }

  const loadVersionData = async () => {
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
      toast.error('Failed to load version details')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadContentLibrary()
    void loadVersionImpact()
  }, [parsedCourseId])

  useEffect(() => {
    void loadVersionData()
  }, [id])

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
      toast.error('Version note is required')
      return false
    }

    return true
  }

  const validateContent = () => {
    if (contentItems.length === 0) {
      toast.error('Please add at least one content item')
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
    toast.success(`${queuedFiles.length} file${queuedFiles.length === 1 ? '' : 's'} added to queue`)
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
        toast.success(resp.message || (isEditMode ? 'Version updated successfully' : 'Version created successfully'))
        navigate(`/courses/${parsedCourseId}`)
      }
    } catch (error: unknown) {
      console.error(error)
      toast.error(getApiErrorText(error, 'Error occurred while saving course version'))
    } finally {
      setSaving(false)
    }
  }

  const renderDetailsStep = () => (
    <div className="space-y-4">
      <div className="space-y-1.5">
        <label htmlFor="note" className="wiz-label">
          Version Name / Note <span className="text-red-500">*</span>
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
        <label htmlFor="isActive" className="wiz-label cursor-pointer">Set as Active Version</label>
      </div>
    </div>
  )

  const renderContentRows = () => {
    if (contentItems.length === 0) {
      return (
        <div className="flex min-h-28 items-center justify-center border border-dashed border-slate-200 rounded text-sm font-semibold text-slate-400 select-none py-6">
          No content selected
        </div>
      )
    }

    return (
      <div className="overflow-x-auto border border-slate-200 rounded">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50 text-xs font-bold uppercase text-slate-500 select-none">
            <tr>
              <th className="w-12 px-3 py-2 text-left">Order</th>
              <th className="px-3 py-2 text-left">Content Name</th>
              <th className="w-28 px-3 py-2 text-left">Source</th>
              <th className="w-36 px-3 py-2 text-left">Content Type</th>
              <th className="w-28 px-3 py-2 text-left">Status</th>
              <th className="w-28 px-3 py-2 text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {contentItems.map((item, index) => {
              const readiness = getContentReadiness(item)
              return (
                <tr key={item.uid} className="hover:bg-slate-50/30 transition-colors">
                  <td className="px-3 py-2 font-bold text-slate-400">{index + 1}</td>
                  <td className="px-3 py-2 font-bold text-slate-700 truncate max-w-xs">{item.name}</td>
                  <td className="px-3 py-2 text-slate-400 font-semibold">{item.source === 'upload' ? 'New upload' : 'Content library'}</td>
                  <td className="px-3 py-2">
                    {item.source === 'upload' ? (
                      <select
                        value={item.typeId}
                        onChange={event => updateUploadContentType(item.uid, Number(event.target.value))}
                        className="wiz-input py-1"
                      >
                        {contentTypeOptions.map(option => <option key={option.id} value={option.id}>{option.name}</option>)}
                      </select>
                    ) : (
                      <span className="font-semibold text-slate-600">{item.typeName || (item.typeId === 2 ? 'Exam' : 'Learn')}</span>
                    )}
                  </td>
                  <td className="px-3 py-2 select-none">
                    <span className={`inline-flex border px-1.5 py-0.5 text-xs font-extrabold rounded-sm ${readiness.className}`}>{readiness.label}</span>
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex justify-end gap-1">
                      <button type="button" onClick={() => moveContentItem(item.uid, -1)} disabled={index === 0} className="rounded border border-slate-200 p-1 text-slate-400 hover:text-slate-600 hover:border-slate-300 disabled:opacity-30 transition cursor-pointer" aria-label="Move content up">
                        <ArrowUp className="h-3.5 w-3.5" />
                      </button>
                      <button type="button" onClick={() => moveContentItem(item.uid, 1)} disabled={index === contentItems.length - 1} className="rounded border border-slate-200 p-1 text-slate-400 hover:text-slate-600 hover:border-slate-300 disabled:opacity-30 transition cursor-pointer" aria-label="Move content down">
                        <ArrowDown className="h-3.5 w-3.5" />
                      </button>
                      <button type="button" onClick={() => removeContentItem(item.uid)} className="rounded border border-slate-200 p-1 text-red-500 hover:text-red-700 hover:border-red-200 transition cursor-pointer" aria-label="Remove content">
                        <X className="h-3.5 w-3.5" />
                      </button>
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
        <span className="border border-slate-200 bg-white px-2 py-0.5 rounded text-xs font-bold text-slate-500">{contentItems.length} item{contentItems.length === 1 ? '' : 's'}</span>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 select-none">
        <label className="flex cursor-pointer flex-col items-center justify-center gap-1.5 border border-dashed border-slate-300 bg-slate-50/30 px-3 py-6 rounded text-sm font-bold text-slate-600 hover:bg-slate-50 hover:border-blue-500 transition duration-150">
          <Upload className="h-5 w-5 text-indigo-500" />
          <span>Upload New SCORM</span>
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
          <span>Select Existing Content</span>
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
              <span className="block text-sm font-bold text-slate-800">{option.title}</span>
              <span className="mt-1 block text-xs font-semibold text-slate-400 leading-normal">{option.note}</span>
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
          <dd className="mt-1 text-sm font-semibold text-slate-700">{learnerPolicyOptions.find(option => option.value === formData.learnerPolicy)?.title}</dd>
        </div>
      </dl>

      <div className="flex items-center justify-between pt-1 select-none">
        <span className="wiz-label">Content Items</span>
        <span className="text-sm font-semibold text-slate-500">{contentItems.length} item{contentItems.length === 1 ? '' : 's'}</span>
      </div>
      {renderContentRows()}
    </div>
  )

  const steps: WizardStep[] = useMemo(() => [
    { label: 'Details', validate: () => validateDetails(), render: () => renderDetailsStep() },
    { label: 'Content', validate: () => validateContent(), render: () => renderContentStep() },
    { label: 'Options', render: () => renderOptionsStep() },
    { label: 'Review', render: () => renderReviewStep() }
  ], [formData, contentItems, impact, showLibraryPopup, contentSearch, contentLibrary])

  if (loading) {
    return <LoadingState label="Loading version details..." />
  }

  return (
    <>
      <AppWizard
        title={isEditMode ? 'Edit Course Version' : 'Create New Version'}
        description="Prepare version details, content, learner impact options, and review before saving."
        eyebrow="Version Control"
        steps={steps}
        currentStep={currentStep}
        onStepChange={setCurrentStep}
        onCancel={() => navigate(`/courses/${parsedCourseId}`)}
        onSubmit={handleSubmit}
        submitLabel={isEditMode ? 'Update Version' : 'Create Version'}
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
            <button
              type="button"
              onClick={() => setShowLibraryPopup(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 hover:bg-slate-100 rounded transition cursor-pointer z-10"
              aria-label="Close modal"
            >
              <X className="h-4 w-4" />
            </button>

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
                        <span className={`border px-1 py-0.5 rounded-sm font-extrabold ${readiness.className}`}>{readiness.label}</span>
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