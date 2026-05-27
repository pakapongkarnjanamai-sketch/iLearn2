import { useCallback, useEffect, useMemo, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowDown, ArrowLeft, ArrowRight, ArrowUp, BookOpen, Check, FileArchive, RefreshCw, Save, Upload, X } from 'lucide-react'

import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'

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

type CourseApiResponse<T = CourseDetailData> = {
  success: boolean
  message?: string
  data?: T
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

const stepLabels = ['Information', 'Content', 'Review']

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
    return { label: 'Queued Upload', className: 'bg-blue-50 text-blue-700 border-blue-100' }
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
      const [typesData, catData, divisionData] = await Promise.all([
        fetchWithAccessControl<LoadResult<CourseTypeLookup>>('Courses/course-types-lookup'),
        fetchWithAccessControl<LoadResult<CategoryLookup>>('Categories/lookup'),
        fetchWithAccessControl<LoadResult<DivisionLookup>>('Divisions/lookup')
      ])

      setCourseTypes(unwrapList(typesData))
      setCategories(unwrapList(catData))
      setDivisions(unwrapList(divisionData))
    } catch (err) {
      console.error('Failed to load lookup data', err)
      toast.error('Failed to load dynamic dropdown options')
    }
  }, [])

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



  const contentSummary = useMemo(() => ({
    total: contentItems.length,
    existing: contentItems.filter(item => item.source === 'library').length,
    uploads: contentItems.filter(item => item.source === 'upload').length
  }), [contentItems])

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

  const goNext = () => {
    if (currentStep === 1 && !validateDetails()) return
    if (currentStep === 2 && !validateContent()) return
    setCurrentStep(prev => Math.min(stepLabels.length, prev + 1))
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

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

  const renderStepButton = (label: string, index: number) => {
    const step = index + 1
    const isActive = currentStep === step
    const isComplete = currentStep > step

    return (
      <button
        key={label}
        type="button"
        onClick={() => {
          if (step <= currentStep || (step === 2 && validateDetails()) || (step === 3 && validateDetails() && validateContent())) {
            setCurrentStep(step)
          }
        }}
        className={`flex min-w-31 items-center gap-2 border px-3 py-2 text-left text-xs font-bold ${isActive ? 'border-blue-500 bg-blue-50 text-blue-700' : isComplete ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-slate-200 bg-white text-slate-500'}`}
        aria-current={isActive ? 'step' : undefined}
      >
        <span className="flex h-5 w-5 items-center justify-center rounded-sm border border-current text-xxs">{step}</span>
        <span>{label}</span>
      </button>
    )
  }

  const renderInformationStep = () => (
    <div className="admin-card p-5">
      <div className="mb-4 flex items-center gap-2 border-b border-slate-100 pb-3">
        <BookOpen className="h-5 w-5 text-blue-600" />
        <h2 className="text-sm font-bold text-slate-800">Course Information</h2>
      </div>

      <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
        <div className="space-y-1.5">
          <label htmlFor="courseType" className="block text-xs font-bold text-slate-500 uppercase">
            Course Type <span className="text-red-500">*</span>
          </label>
          <select
            id="courseType"
            name="courseType"
            value={formData.courseType}
            onChange={handleChange}
            className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
          >
            <option value={0}>Select Type</option>
            {courseTypes.map(type => <option key={type.id} value={type.id}>{type.name}</option>)}
          </select>
        </div>

        <div className="space-y-1.5">
          <label htmlFor="courseCode" className="block text-xs font-bold text-slate-500 uppercase">
            Course Code <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            id="courseCode"
            name="courseCode"
            value={formData.courseCode}
            onChange={handleChange}
            placeholder="e.g. CS-101"
            className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
          />
        </div>

        <div className="space-y-1.5">
          <label htmlFor="courseName" className="block text-xs font-bold text-slate-500 uppercase">
            Course Title <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            id="courseName"
            name="courseName"
            value={formData.courseName}
            onChange={handleChange}
            placeholder="e.g. Intro to Cybersecurity"
            className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
          />
        </div>
      </div>

      <div className="mt-5 grid grid-cols-1 gap-5 md:grid-cols-2">
        <div className="space-y-1.5">
          <label htmlFor="divisionId" className="block text-xs font-bold text-slate-500 uppercase">Division</label>
          <select
            id="divisionId"
            name="divisionId"
            value={formData.divisionId}
            onChange={handleChange}
            className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
          >
            <option value={0}>Select Division</option>
            {divisions.map(division => <option key={division.id} value={division.id}>{division.name}</option>)}
          </select>
        </div>

        <div className="space-y-1.5">
          <label htmlFor="categoryId" className="block text-xs font-bold text-slate-500 uppercase">
            Category <span className="text-red-500">*</span>
          </label>
          <select
            id="categoryId"
            name="categoryId"
            value={formData.categoryId}
            onChange={handleChange}
            disabled={formData.divisionId > 0 && filteredCategories.length === 0}
            className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none disabled:bg-slate-50 disabled:text-slate-400"
          >
            <option value={0}>Select Category</option>
            {filteredCategories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
          </select>
        </div>
      </div>

      <div className="mt-5 space-y-1.5">
        <label htmlFor="description" className="block text-xs font-bold text-slate-500 uppercase">Description</label>
        <textarea
          id="description"
          name="description"
          value={formData.description}
          onChange={handleChange}
          rows={5}
          placeholder="Course summary and objectives..."
          className="w-full resize-y rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-blue-600 focus:outline-none"
        />
      </div>
    </div>
  )

  const renderContentRows = () => {
    if (contentItems.length === 0) {
      return (
        <div className="flex min-h-36 items-center justify-center border border-dashed border-slate-200 text-sm font-semibold text-slate-400">
          No content selected
        </div>
      )
    }

    return (
      <div className="overflow-x-auto border border-slate-200">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50 text-xs font-bold uppercase text-slate-500">
            <tr>
              <th className="w-16 px-3 py-2 text-left">Order</th>
              <th className="px-3 py-2 text-left">Content Name</th>
              <th className="w-36 px-3 py-2 text-left">Source</th>
              <th className="w-40 px-3 py-2 text-left">Content Type</th>
              <th className="w-36 px-3 py-2 text-left">Status</th>
              <th className="w-32 px-3 py-2 text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {contentItems.map((item, index) => {
              const readiness = getContentReadiness(item)
              return (
                <tr key={item.uid}>
                  <td className="px-3 py-2 font-bold text-slate-500">{index + 1}</td>
                  <td className="px-3 py-2 font-semibold text-slate-800">{item.name}</td>
                  <td className="px-3 py-2 text-slate-500">{item.source === 'upload' ? 'New upload' : 'Content library'}</td>
                  <td className="px-3 py-2">
                    {item.source === 'upload' ? (
                      <select
                        value={item.typeId}
                        onChange={event => updateUploadContentType(item.uid, Number(event.target.value))}
                        className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-sm focus:border-blue-600 focus:outline-none"
                      >
                        {contentTypeOptions.map(option => <option key={option.id} value={option.id}>{option.name}</option>)}
                      </select>
                    ) : (
                      <span>{item.typeName || (item.typeId === 2 ? 'Exam' : 'Learn')}</span>
                    )}
                  </td>
                  <td className="px-3 py-2">
                    <span className={`inline-flex border px-2 py-0.5 text-xs font-bold ${readiness.className}`}>{readiness.label}</span>
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex justify-end gap-1">
                      <button type="button" onClick={() => moveContentItem(item.uid, -1)} disabled={index === 0} className="rounded border border-slate-200 p-1 text-slate-500 disabled:opacity-30" aria-label="Move content up">
                        <ArrowUp className="h-3.5 w-3.5" />
                      </button>
                      <button type="button" onClick={() => moveContentItem(item.uid, 1)} disabled={index === contentItems.length - 1} className="rounded border border-slate-200 p-1 text-slate-500 disabled:opacity-30" aria-label="Move content down">
                        <ArrowDown className="h-3.5 w-3.5" />
                      </button>
                      <button type="button" onClick={() => removeContentItem(item.uid)} className="rounded border border-slate-200 p-1 text-red-600" aria-label="Remove content">
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
    <div className="admin-card min-h-0 p-5">
      <div className="mb-4 flex items-center justify-between gap-3 border-b border-slate-100 pb-3">
        <div className="flex items-center gap-2">
          <FileArchive className="h-5 w-5 text-blue-600" />
          <h2 className="text-sm font-bold text-slate-800">Course Content</h2>
        </div>
        <span className="border border-slate-200 px-2 py-1 text-xs font-bold text-slate-500">{contentItems.length} item{contentItems.length === 1 ? '' : 's'}</span>
      </div>

      <div className="mb-4">
        <label className="flex cursor-pointer items-center justify-center gap-2 border border-slate-200 bg-slate-50 px-4 py-4 text-sm font-bold text-slate-700 hover:bg-white">
          <Upload className="h-5 w-5 text-blue-600" />
          <span>Upload New SCORM</span>
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
      </div>

      {renderContentRows()}
    </div>
  )

  const renderReviewStep = () => (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="admin-card p-4">
          <div className="text-xl font-bold text-slate-800">{contentSummary.total}</div>
          <div className="text-xs font-bold uppercase text-slate-500">Total Content Items</div>
        </div>
        <div className="admin-card p-4">
          <div className="text-xl font-bold text-slate-800">{contentSummary.existing}</div>
          <div className="text-xs font-bold uppercase text-slate-500">Existing Content</div>
        </div>
        <div className="admin-card p-4">
          <div className="text-xl font-bold text-slate-800">{contentSummary.uploads}</div>
          <div className="text-xs font-bold uppercase text-slate-500">New Uploads</div>
        </div>
      </div>

      <div className="admin-card p-5">
        <div className="mb-3 text-sm font-bold text-slate-800">Course Details</div>
        <dl className="grid grid-cols-1 gap-x-5 gap-y-3 md:grid-cols-2">
          <div className="border-b border-slate-100 pb-2">
            <dt className="text-xs font-bold uppercase text-slate-500">Course Type</dt>
            <dd className="mt-1 font-semibold text-slate-800">{selectedCourseTypeName}</dd>
          </div>
          <div className="border-b border-slate-100 pb-2">
            <dt className="text-xs font-bold uppercase text-slate-500">Course Code</dt>
            <dd className="mt-1 font-semibold text-slate-800">{formData.courseCode || 'Not set'}</dd>
          </div>
          <div className="border-b border-slate-100 pb-2 md:col-span-2">
            <dt className="text-xs font-bold uppercase text-slate-500">Course Title</dt>
            <dd className="mt-1 font-semibold text-slate-800">{formData.courseName || 'Not set'}</dd>
          </div>
          <div className="border-b border-slate-100 pb-2">
            <dt className="text-xs font-bold uppercase text-slate-500">Division</dt>
            <dd className="mt-1 font-semibold text-slate-800">{selectedDivisionName}</dd>
          </div>
          <div className="border-b border-slate-100 pb-2">
            <dt className="text-xs font-bold uppercase text-slate-500">Category</dt>
            <dd className="mt-1 font-semibold text-slate-800">{selectedCategoryName}</dd>
          </div>
        </dl>
      </div>

      <div className="admin-card p-5">
        <div className="mb-3 text-sm font-bold text-slate-800">Content Review</div>
        {renderContentRows()}
      </div>
    </div>
  )

  if (loading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <RefreshCw className="h-8 w-8 animate-spin text-blue-600" />
      </div>
    )
  }

  return (
    <>
      <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col gap-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-xl font-extrabold text-slate-800">{isEditMode ? 'Edit Course' : 'New Course'}</h1>
            <p className="text-sm font-medium text-slate-500">{isEditMode ? 'Update course properties and learning packages.' : 'Create the course, attach content, then review before saving.'}</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            {!isEditMode ? stepLabels.map(renderStepButton) : null}
          </div>
        </div>

        {/* Tab Controls for Edit Mode */}
        {isEditMode && (
          <div className="border-b border-slate-200 mb-2 flex gap-4">
            <button
              type="button"
              onClick={() => setActiveEditTab('properties')}
              className={`pb-2.5 font-bold text-xs uppercase tracking-wider transition relative cursor-pointer ${
                activeEditTab === 'properties'
                  ? 'text-blue-600 border-b-2 border-blue-600'
                  : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              Course Properties
            </button>
            <button
              type="button"
              onClick={() => setActiveEditTab('content')}
              className={`pb-2.5 font-bold text-xs uppercase tracking-wider transition relative cursor-pointer ${
                activeEditTab === 'content'
                  ? 'text-blue-600 border-b-2 border-blue-600'
                  : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              SCORM Content & Library
            </button>
          </div>
        )}

        <div className="min-h-0 flex-1">
          {!isEditMode ? (
            <>
              {currentStep === 1 ? renderInformationStep() : null}
              {currentStep === 2 ? renderContentStep() : null}
              {currentStep === 3 ? renderReviewStep() : null}
            </>
          ) : (
            <>
              {activeEditTab === 'properties' ? renderInformationStep() : null}
              {activeEditTab === 'content' ? <div className="mt-1">{renderContentStep()}</div> : null}
            </>
          )}
        </div>

        <div className="flex items-center justify-end gap-3 border-t border-slate-200 pt-3">
          <button
            type="button"
            onClick={() => navigate(isEditMode && id ? `/courses/${id}` : '/courses')}
            className="admin-button admin-button--secondary"
          >
            <X aria-hidden="true" />
            <span>Cancel</span>
          </button>

          {!isEditMode && currentStep > 1 ? (
            <button type="button" onClick={() => setCurrentStep(prev => Math.max(1, prev - 1))} className="admin-button admin-button--secondary">
              <ArrowLeft aria-hidden="true" />
              <span>Previous</span>
            </button>
          ) : null}

          {!isEditMode && currentStep < stepLabels.length ? (
            <button type="button" onClick={event => { event.preventDefault(); event.stopPropagation(); goNext() }} className="admin-button admin-button--primary">
              <ArrowRight aria-hidden="true" />
              <span>Continue</span>
            </button>
          ) : (
            <button type="submit" disabled={saving} className="admin-button admin-button--primary disabled:opacity-55">
              {saving ? <RefreshCw className="animate-spin" aria-hidden="true" /> : isEditMode ? <Save aria-hidden="true" /> : <Check aria-hidden="true" />}
              <span>{isEditMode ? 'Save Changes' : 'Create Course'}</span>
            </button>
          )}
        </div>
      </form>
    </>
  )
}
