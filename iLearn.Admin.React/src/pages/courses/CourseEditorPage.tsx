import { useCallback, useEffect, useMemo, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { 
  ArrowDown, 
  ArrowUp, 
  BookOpen, 
  Plus, 
  Save, 
  Search, 
  Upload, 
  X,
  Loader2 
} from 'lucide-react'

import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { LoadingState } from '../../components/ui/LoadingState'

type LoadResult<T> = T[] | { data?: T[] }

type DivisionLookup = {
  id: number
  name: string
}

type CategoryLookup = {
  id: number
  name: string
  divisionId?: number
}

// Mirrors subset response of GET Courses/course-types-lookup
type CourseTypeLookup = {
  id: number
  name: string
}

type CourseFormData = {
  courseCode: string
  courseName: string
  description: string
  divisionId: number
  categoryId: number
  courseType: number
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

// Mirrors CourseDetailDto (iLearn.Application/DTOs/CourseDetailDto.cs)
type CourseDetailData = {
  id: number
  courseCode?: string
  courseName?: string
  code?: string
  title?: string
  description?: string | null
  categoryId?: number
  courseType?: number
  courseTypeId?: number
  isActive?: boolean
  contentItems?: CourseContentApiItem[]
}

// Mirrors ApiResponse<T> (iLearn.Domain/Common/ApiResponse.cs)
type CourseApiResponse<T = CourseDetailData> = {
  success: boolean
  message?: string
  data?: T
}

type ContentLibraryItem = CourseContentApiItem & {
  courseIdsCount?: number
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

type CourseVersion = {
  id: number
  isActive?: boolean
}

const contentTypeOptions = [
  { id: 1, name: 'Learn' },
  { id: 2, name: 'Exam' }
]

function unwrapList<T>(value: LoadResult<T> | undefined): T[] {
  if (!value) return []
  return Array.isArray(value) ? value : value.data ?? []
}

function getUrl(contentItem: CourseContentApiItem) {
  return contentItem.url ?? contentItem.URL ?? null
}

function getApiErrorText(error: unknown, fallback: string) {
  if (error instanceof Error && error.message) {
    return error.message
  }
  return fallback
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

export function CourseEditorPage() {
  const { id } = useParams()
  const isEditMode = !!id
  const navigate = useNavigate()
  const { setLabel } = useBreadcrumbs()

  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [currentStep, setCurrentStep] = useState(1)
  const [activeEditTab, setActiveEditTab] = useState<'properties' | 'content'>('properties')
  const [showLibraryPopup, setShowLibraryPopup] = useState(false)
  const [contentSearch, setContentSearch] = useState('')
  const [contentLibrary, setContentLibrary] = useState<ContentLibraryItem[]>([])

  const [divisions, setDivisions] = useState<DivisionLookup[]>([])
  const [categories, setCategories] = useState<CategoryLookup[]>([])
  const [courseTypes, setCourseTypes] = useState<CourseTypeLookup[]>([])
  const [contentItems, setContentItems] = useState<SelectedContentItem[]>([])
  
  const [formData, setFormData] = useState<CourseFormData>({
    courseCode: '',
    courseName: '',
    description: '',
    divisionId: 0,
    categoryId: 0,
    courseType: 0,
  })

  useEffect(() => {
    if (isEditMode && formData.courseCode) {
      setLabel(String(id), formData.courseCode)
    }
  }, [formData.courseCode, id, isEditMode, setLabel])

  const loadLookups = useCallback(async () => {
    try {
      const [typesData, catData, divisionData, libraryData] = await Promise.all([
        fetchWithAccessControl<LoadResult<CourseTypeLookup>>('Courses/course-types-lookup'),
        fetchWithAccessControl<LoadResult<CategoryLookup>>('Categories/lookup'),
        fetchWithAccessControl<LoadResult<DivisionLookup>>('Divisions/lookup'),
        fetchWithAccessControl<LoadResult<ContentLibraryItem>>('ContentLibrary/lookup')
      ])

      setCourseTypes(unwrapList(typesData))
      setCategories(unwrapList(catData))
      setDivisions(unwrapList(divisionData))
      setContentLibrary(unwrapList(libraryData))
    } catch (err) {
      console.error('Failed to load lookup data', err)
      toast.error('Failed to load dynamic dropdown options')
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

  const loadCourseData = useCallback(async () => {
    if (!id) return
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<CourseApiResponse>(`Courses/${id}`)
      if (resp.success && resp.data) {
        const categoryId = resp.data.categoryId || 0
        const category = categories.find(item => item.id === categoryId)
        setFormData({
          courseCode: resp.data.courseCode || resp.data.code || '',
          courseName: resp.data.courseName || resp.data.title || '',
          description: resp.data.description || '',
          divisionId: category?.divisionId || 0,
          categoryId,
          courseType: resp.data.courseType || resp.data.courseTypeId || 0,
        })
        setContentItems((resp.data.contentItems || []).map(createLibrarySelection))
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to load course details')
    } finally {
      setLoading(false)
    }
  }, [categories, id])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadLookups()
    }, 0)

    return () => window.clearTimeout(timeoutId)
  }, [loadLookups])

  useEffect(() => {
    if (categories.length > 0) {
      const timeoutId = window.setTimeout(() => {
        void loadCourseData()
      }, 0)

      return () => window.clearTimeout(timeoutId)
    }
  }, [categories.length, loadCourseData])

  const filteredCategories = useMemo(() => {
    if (!formData.divisionId) return categories
    return categories.filter(category => category.divisionId === formData.divisionId)
  }, [categories, formData.divisionId])

  const selectedCourseTypeName = useMemo(() => (
    courseTypes.find(item => item.id === formData.courseType)?.name || 'Not selected'
  ), [courseTypes, formData.courseType])

  const selectedDivisionName = useMemo(() => (
    divisions.find(item => item.id === formData.divisionId)?.name || 'No division'
  ), [divisions, formData.divisionId])

  const selectedCategoryName = useMemo(() => (
    categories.find(item => item.id === formData.categoryId)?.name || 'No category'
  ), [categories, formData.categoryId])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target
    const numericFields = ['divisionId', 'categoryId', 'courseType']
    const nextValue = numericFields.includes(name) ? Number(value) : value

    setFormData(prev => ({
      ...prev,
      [name]: nextValue,
      ...(name === 'divisionId' ? { categoryId: 0 } : {})
    }))
  }

  const validateDetails = () => {
    if (!formData.courseCode.trim()) {
      toast.error('Course Code is required')
      return false
    }
    if (!formData.courseName.trim()) {
      toast.error('Course Title is required')
      return false
    }
    if (formData.categoryId === 0) {
      toast.error('Please select a Course Category')
      return false
    }
    if (formData.courseType === 0) {
      toast.error('Please select a Course Type')
      return false
    }

    return true
  }

  const validateContent = () => {
    if (contentItems.length === 0) {
      toast.error('Please add at least one content item before reviewing the course')
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

  const buildCoursePayload = () => ({
    courseCode: formData.courseCode.trim(),
    courseName: formData.courseName.trim(),
    description: formData.description.trim(),
    courseType: formData.courseType,
    categoryId: formData.categoryId,
    contentItemIds: contentItems
      .filter(item => item.source === 'library' && typeof item.id === 'number')
      .map(item => item.id as number)
  })

  const saveCourse = async () => {
    const payload = buildCoursePayload()

    if (isEditMode) {
      await fetchWithAccessControl<CourseApiResponse>(`Courses/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      })
      return Number(id)
    }

    const resp = await fetchWithAccessControl<CourseApiResponse<{ id: number }>>('Courses/Create', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })

    if (!resp.success || !resp.data?.id) {
      throw new Error(resp.message || 'Course was not created')
    }

    return resp.data.id
  }

  const getContentVersionId = async (courseId: number) => {
    const response = await fetchWithAccessControl<CourseApiResponse<CourseVersion[]>>(`Courses/${courseId}/versions`)
    const versions = response.data || []
    return versions.find(version => version.isActive)?.id || versions[0]?.id || null
  }

  const buildVersionFormData = (courseId: number) => {
    const versionFormData = new FormData()
    versionFormData.append('CourseId', String(courseId))
    versionFormData.append('Note', isEditMode ? 'Updated content via React Admin' : 'Initial content upload')
    versionFormData.append('IsActive', 'true')
    versionFormData.append('LearnerPolicy', 'NewLearnersOnly')

    contentItems.forEach(item => {
      if (item.source === 'upload' && item.file) {
        versionFormData.append('Files', item.file)
        versionFormData.append('ContentItemIds', '0')
      } else if (item.id) {
        versionFormData.append('ContentItemIds', String(item.id))
      }

      versionFormData.append('ContentTypeIds', String(item.typeId || 1))
    })

    return versionFormData
  }

  const saveContentItemsToVersion = async (courseId: number) => {
    const versionId = await getContentVersionId(courseId)
    const endpoint = versionId ? `Courses/versions/${versionId}` : `Courses/${courseId}/versions`
    const method = versionId ? 'PUT' : 'POST'

    await fetchWithAccessControl<CourseApiResponse>(endpoint, {
      method,
      body: buildVersionFormData(courseId)
    })
  }

  const handleSubmit = async (e?: React.FormEvent) => {
    if (e) {
      e.preventDefault()
    }

    if (!validateDetails() || !validateContent()) return

    setSaving(true)
    try {
      const courseId = await saveCourse()
      await saveContentItemsToVersion(courseId)
      toast.success(isEditMode ? 'Course updated successfully' : 'Course and content created successfully')
      navigate(`/courses/${courseId}`)
    } catch (err: unknown) {
      console.error(err)
      toast.error(getApiErrorText(err, 'Failed to save course data'))
    } finally {
      setSaving(false)
    }
  }

  const renderInformationStep = () => (
    <div className="space-y-4">

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="space-y-1.5">
          <label htmlFor="courseType" className="wiz-label">
            Course Type <span className="text-red-500">*</span>
          </label>
          <select
            id="courseType"
            name="courseType"
            value={formData.courseType}
            onChange={handleChange}
            className="wiz-input"
          >
            <option value={0}>Select Type</option>
            {courseTypes.map(type => <option key={type.id} value={type.id}>{type.name}</option>)}
          </select>
        </div>

        <div className="space-y-1.5">
          <label htmlFor="courseCode" className="wiz-label">
            Course Code <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            id="courseCode"
            name="courseCode"
            value={formData.courseCode}
            onChange={handleChange}
            placeholder="e.g. CS-101"
            className="wiz-input"
          />
        </div>

        <div className="space-y-1.5 sm:col-span-2">
          <label htmlFor="courseName" className="wiz-label">
            Course Title <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            id="courseName"
            name="courseName"
            value={formData.courseName}
            onChange={handleChange}
            placeholder="e.g. Intro to Cybersecurity"
            className="wiz-input"
          />
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="space-y-1.5">
          <label htmlFor="divisionId" className="wiz-label">Division</label>
          <select
            id="divisionId"
            name="divisionId"
            value={formData.divisionId}
            onChange={handleChange}
            className="wiz-input"
          >
            <option value={0}>Select Division</option>
            {divisions.map(division => <option key={division.id} value={division.id}>{division.name}</option>)}
          </select>
        </div>

        <div className="space-y-1.5">
          <label htmlFor="categoryId" className="wiz-label">
            Category <span className="text-red-500">*</span>
          </label>
          <select
            id="categoryId"
            name="categoryId"
            value={formData.categoryId}
            onChange={handleChange}
            disabled={formData.divisionId > 0 && filteredCategories.length === 0}
            className="wiz-input"
          >
            <option value={0}>Select Category</option>
            {filteredCategories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
          </select>
        </div>
      </div>

      <div className="space-y-1.5">
        <label htmlFor="description" className="wiz-label">Description</label>
        <textarea
          id="description"
          name="description"
          value={formData.description}
          onChange={handleChange}
          rows={6}
          placeholder="Course summary and objectives..."
          className="wiz-input resize-y"
        />
      </div>
    </div>
  )

  const renderContentRows = () => {
    if (contentItems.length === 0) {
      return (
        <div className="flex min-h-28 items-center justify-center border border-dashed border-slate-200 rounded text-[13px] font-semibold text-slate-400 select-none py-6">
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
                      <button type="button" onClick={() => moveContentItem(item.uid, -1)} disabled={index === 0} className="p-1 text-indigo-500 hover:bg-indigo-50 rounded-md transition cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed" aria-label="Move content up">
                        <ArrowUp className="h-3.5 w-3.5" />
                      </button>
                      <button type="button" onClick={() => moveContentItem(item.uid, 1)} disabled={index === contentItems.length - 1} className="p-1 text-indigo-500 hover:bg-indigo-50 rounded-md transition cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed" aria-label="Move content down">
                        <ArrowDown className="h-3.5 w-3.5" />
                      </button>
                      <button type="button" onClick={() => removeContentItem(item.uid)} className="p-1 text-red-500 hover:bg-rose-50 rounded-md transition cursor-pointer" aria-label="Remove content">
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

  const renderReviewStep = () => (
    <div className="space-y-4">

      <dl className="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">
        <div className="border-b border-slate-100 pb-2.5">
          <dt className="wiz-label">Course Type</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{selectedCourseTypeName}</dd>
        </div>
        <div className="border-b border-slate-100 pb-2.5">
          <dt className="wiz-label">Course Code</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{formData.courseCode || 'Not set'}</dd>
        </div>
        <div className="border-b border-slate-100 pb-2.5 sm:col-span-2">
          <dt className="wiz-label">Course Title</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{formData.courseName || 'Not set'}</dd>
        </div>
        <div className="border-b border-slate-100 pb-2.5">
          <dt className="wiz-label">Division</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{selectedDivisionName}</dd>
        </div>
        <div className="border-b border-slate-100 pb-2.5">
          <dt className="wiz-label">Category</dt>
          <dd className="mt-1 text-sm font-semibold text-slate-700">{selectedCategoryName}</dd>
        </div>
      </dl>

      <div className="flex items-center justify-between pt-1 select-none">
        <span className="wiz-label">Content Items</span>
        <span className="text-sm font-semibold text-slate-500">{contentItems.length} item{contentItems.length === 1 ? '' : 's'}</span>
      </div>
      {renderContentRows()}
    </div>
  )

  const steps: WizardStep[] = [
    { label: 'Information', validate: () => validateDetails(), render: () => renderInformationStep() },
    { label: 'Content', validate: () => validateContent(), render: () => renderContentStep() },
    { label: 'Review', render: () => renderReviewStep() }
  ]

  if (loading) {
    return <LoadingState label="Loading course details..." />
  }

  if (!isEditMode) {
    return (
      <>
        <AppWizard
          title="New Course"
          description="Create a new training course."
          eyebrow="Course Catalog"
          steps={steps}
          currentStep={currentStep}
          onStepChange={setCurrentStep}
          onCancel={() => navigate('/courses')}
          onSubmit={handleSubmit}
          submitLabel="Create Course"
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
                  <h3 className="text-xs font-bold text-slate-800">Select Existing Content</h3>
                  <p className="text-xxs font-semibold text-slate-400">Choose from SCORM packages in the Content Library</p>
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
                        <div className="truncate font-bold text-slate-800 text-xs">{item.name}</div>
                        <div className="mt-0.5 flex items-center gap-2 text-xxs text-slate-500 font-semibold">
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

  return (
    <div className="wizard-surface flex min-h-0 flex-1 flex-col overflow-hidden bg-white border border-slate-200/80 rounded-xl shadow-xs">
      <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col">
        {/* Header with Title and Tabs */}
        <div className="flex flex-col gap-3 bg-white px-6 pt-5 pb-3 border-b border-slate-200 shrink-0 select-none">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <div className="text-xs font-bold uppercase tracking-wider text-slate-400">
                Course Catalog
              </div>
              <h1 className="text-base font-extrabold text-slate-800 tracking-tight leading-tight">
                Edit Course
              </h1>
              <p className="text-xs font-semibold text-slate-400 mt-0.5 leading-normal">
                Update course details and content settings.
              </p>
            </div>
          </div>

          {/* Tab Controls for Edit Mode */}
          <div className="flex gap-4 mt-2">
            <button
              type="button"
              onClick={() => setActiveEditTab('properties')}
              className={`pb-1 font-bold text-xs uppercase tracking-wider transition relative cursor-pointer ${
                activeEditTab === 'properties'
                  ? 'text-indigo-500 border-b-2 border-indigo-500'
                  : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              Course Properties
            </button>
            <button
              type="button"
              onClick={() => setActiveEditTab('content')}
              className={`pb-1 font-bold text-xs uppercase tracking-wider transition relative cursor-pointer ${
                activeEditTab === 'content'
                  ? 'text-indigo-500 border-b-2 border-indigo-500'
                  : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              SCORM Content & Library
            </button>
          </div>
        </div>

        {/* Content Panel Zone */}
        <div className="min-h-0 flex-1 flex flex-col relative bg-slate-50/60">
          <div className="overflow-y-auto custom-scrollbar flex-1 px-6 py-6">
            <div className="w-full h-full flex flex-col">
              {activeEditTab === 'properties' ? renderInformationStep() : renderContentStep()}
            </div>
          </div>
          
          {saving && (
            <div className="absolute inset-0 bg-white/60 backdrop-blur-xs flex items-center justify-center z-50 rounded-lg animate-fade-in">
              <div className="flex flex-col items-center gap-2.5 select-none">
                <Loader2 className="h-7 w-7 animate-spin text-indigo-500" />
                <span className="text-xs text-slate-500 font-bold tracking-wide uppercase animate-pulse">Saving...</span>
              </div>
            </div>
          )}
        </div>

        {/* Navigation Buttons Pinned Footer */}
        <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-200 bg-white shrink-0">
          <button
            type="button"
            onClick={() => navigate(`/courses/${id}`)}
            className="inline-flex items-center gap-1.5 rounded-md border border-slate-200 bg-white px-4 py-2 text-slate-500 hover:border-slate-300 hover:bg-slate-50 hover:text-slate-700 cursor-pointer text-xs font-bold shadow-3xs"
          >
            <X className="h-4 w-4" aria-hidden="true" />
            <span>Cancel</span>
          </button>

          <button 
            type="submit" 
            disabled={saving} 
            className="inline-flex items-center gap-1.5 rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-white hover:bg-indigo-700 cursor-pointer text-xs font-bold shadow-3xs disabled:opacity-55"
          >
            {saving ? (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : (
              <Save className="h-4 w-4" aria-hidden="true" />
            )}
            <span>Save Changes</span>
          </button>
        </div>
      </form>

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
    </div>
  )
}
