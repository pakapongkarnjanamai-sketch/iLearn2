import { useCallback, useEffect, useMemo, useState, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { 
  ArrowDown, 
  ArrowUp, 
  BookOpen, 
  Plus, 
  Save, 
  Search, 
  Upload, 
  X
} from 'lucide-react'

import { fetchWithAccessControl, uploadWithProgress, type UploadProgress } from '../../lib/apiClient'
import { UploadProgressOverlay } from '../../components/shared/UploadProgressOverlay'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { AppButton } from '../../components/ui/AppButton'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { Badge } from '../../components/ui/Badge'
import { DetailTabs } from '../../components/ui/DetailTabs'
import { IconButton } from '../../components/ui/IconButton'
import { LoadingState } from '../../components/ui/LoadingState'
import { getContentReadinessBadgeModel, ReadinessBadge } from '../../components/ui/ReadinessBadge'
import { CONTENT_TYPE_LABELS, COURSE_LABELS, contentTypeLabel, t, tf } from '../../lib/labels'

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
  { id: 1, name: CONTENT_TYPE_LABELS.learn },
  { id: 2, name: CONTENT_TYPE_LABELS.exam }
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
  return getContentReadinessBadgeModel({
    source: item.source,
    isActive: item.isActive,
    url: item.url,
  })
}

export function CourseEditorPage() {
  const { id } = useParams()
  const isEditMode = !!id
  const navigate = useNavigate()
  const { setLabel } = useBreadcrumbs()

  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [uploadProgress, setUploadProgress] = useState<UploadProgress | null>(null)
  const abortUploadRef = useRef<(() => void) | null>(null)
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

  const editTabs: Array<{ key: 'properties' | 'content'; label: string }> = [
    { key: 'properties', label: t(COURSE_LABELS.editCourseProperties) },
    { key: 'content', label: t(COURSE_LABELS.contentLibrary) },
  ]

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
      toast.error(t(COURSE_LABELS.failedToLoadDropdowns))
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
      toast.error(t(COURSE_LABELS.failedToLoadCourseDetails))
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
    courseTypes.find(item => item.id === formData.courseType)?.name || t(COURSE_LABELS.notSelected)
  ), [courseTypes, formData.courseType])

  const selectedDivisionName = useMemo(() => (
    divisions.find(item => item.id === formData.divisionId)?.name || t(COURSE_LABELS.noDivision)
  ), [divisions, formData.divisionId])

  const selectedCategoryName = useMemo(() => (
    categories.find(item => item.id === formData.categoryId)?.name || t(COURSE_LABELS.noCategory)
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
      toast.error(t(COURSE_LABELS.courseCodeRequired))
      return false
    }
    if (!formData.courseName.trim()) {
      toast.error(t(COURSE_LABELS.courseTitleRequired))
      return false
    }
    if (formData.categoryId === 0) {
      toast.error(t(COURSE_LABELS.categoryRequired))
      return false
    }
    if (formData.courseType === 0) {
      toast.error(t(COURSE_LABELS.courseTypeRequired))
      return false
    }

    return true
  }

  const validateContent = () => {
    if (contentItems.length === 0) {
      toast.error(t(COURSE_LABELS.contentRequiredBeforeReview))
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

    const hasFileUpload = contentItems.some(i => i.source === 'upload' && i.file)

    if (hasFileUpload) {
      const totalSize = contentItems
        .filter(i => i.source === 'upload' && i.file)
        .reduce((acc, curr) => acc + (curr.file?.size || 0), 0)

      setUploadProgress({
        phase: 'uploading',
        loadedBytes: 0,
        totalBytes: totalSize,
        percent: 0,
      })

      const fd = buildVersionFormData(courseId)

      const { promise, abort } = uploadWithProgress<CourseApiResponse>(
        endpoint,
        fd,
        {
          method,
          onProgress: (p) => {
            setUploadProgress(p)
          },
        }
      )
      abortUploadRef.current = abort

      try {
        await promise
      } catch (err: any) {
        if (err.isAborted) {
          toast.info(t(COURSE_LABELS.uploadCancelled))
        }
        throw err
      } finally {
        setUploadProgress(null)
        abortUploadRef.current = null
      }
    } else {
      await fetchWithAccessControl<CourseApiResponse>(endpoint, {
        method,
        body: buildVersionFormData(courseId)
      })
    }
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
      toast.success(t(isEditMode ? COURSE_LABELS.courseUpdated : COURSE_LABELS.courseCreated))
      navigate(`/courses/${courseId}`)
    } catch (err: unknown) {
      console.error(err)
      if (err instanceof Error && (err as any).isAborted) {
        // Do not toast error since we already toasted 'Upload cancelled'
      } else {
        toast.error(getApiErrorText(err, t(COURSE_LABELS.failedToSaveCourse)))
      }
    } finally {
      setSaving(false)
    }
  }

  const renderInformationStep = () => (
    <div className="space-y-4">

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="space-y-1.5">
          <label htmlFor="courseType" className="wiz-label">
            {t(COURSE_LABELS.courseType)} <span className="text-red-500">*</span>
          </label>
          <select
            id="courseType"
            name="courseType"
            value={formData.courseType}
            onChange={handleChange}
            className="wiz-input"
          >
            <option value={0}>{t(COURSE_LABELS.selectType)}</option>
            {courseTypes.map(type => <option key={type.id} value={type.id}>{type.name}</option>)}
          </select>
        </div>

        <div className="space-y-1.5">
          <label htmlFor="courseCode" className="wiz-label">
            {t(COURSE_LABELS.courseCode)} <span className="text-red-500">*</span>
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
            {t(COURSE_LABELS.courseTitle)} <span className="text-red-500">*</span>
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
          <label htmlFor="divisionId" className="wiz-label">{t(COURSE_LABELS.division)}</label>
          <select
            id="divisionId"
            name="divisionId"
            value={formData.divisionId}
            onChange={handleChange}
            className="wiz-input"
          >
            <option value={0}>{t(COURSE_LABELS.selectDivision)}</option>
            {divisions.map(division => <option key={division.id} value={division.id}>{division.name}</option>)}
          </select>
        </div>

        <div className="space-y-1.5">
          <label htmlFor="categoryId" className="wiz-label">
            {t(COURSE_LABELS.category)} <span className="text-red-500">*</span>
          </label>
          <select
            id="categoryId"
            name="categoryId"
            value={formData.categoryId}
            onChange={handleChange}
            disabled={formData.divisionId > 0 && filteredCategories.length === 0}
            className="wiz-input"
          >
            <option value={0}>{t(COURSE_LABELS.selectCategory)}</option>
            {filteredCategories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
          </select>
        </div>
      </div>

      <div className="space-y-1.5">
        <label htmlFor="description" className="wiz-label">{t(COURSE_LABELS.description)}</label>
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
        <span className="wiz-label">{t(COURSE_LABELS.contentItems)}</span>
        <Badge tone="neutral">{tf(COURSE_LABELS.itemCount, contentItems.length, contentItems.length === 1 ? '' : 's')}</Badge>
      </div>
      {renderContentRows()}
    </div>
  )

  const steps: WizardStep[] = [
    { label: t(COURSE_LABELS.information), validate: () => validateDetails(), render: () => renderInformationStep() },
    { label: t(COURSE_LABELS.content), validate: () => validateContent(), render: () => renderContentStep() },
    { label: t(COURSE_LABELS.review), render: () => renderReviewStep() }
  ]

  if (loading) {
    return <LoadingState label={t(COURSE_LABELS.failedToLoadCourseDetails)} />
  }

  if (!isEditMode) {
    return (
      <>
        <AppWizard
          title={t(COURSE_LABELS.newCourse)}
          description={t(COURSE_LABELS.newCourse)}
          eyebrow={t(COURSE_LABELS.courseCatalog)}
          steps={steps}
          currentStep={currentStep}
          onStepChange={setCurrentStep}
          onCancel={() => navigate('/courses')}
          onSubmit={handleSubmit}
          submitLabel={t(COURSE_LABELS.createCourse)}
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

  return (
    <>
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

            <DetailTabs
              tabs={editTabs}
              active={activeEditTab}
              onChange={setActiveEditTab}
              variant="compact"
            />
          </div>

          {/* Content Panel Zone */}
          <div className="min-h-0 flex-1 flex flex-col relative bg-slate-50/60">
            <div className="overflow-y-auto custom-scrollbar flex-1 px-6 py-6">
              <div className="w-full h-full flex flex-col">
                {activeEditTab === 'properties' ? renderInformationStep() : renderContentStep()}
              </div>
            </div>
            
            {/* Sticky Actions Footer */}
            <div className="flex items-center justify-between gap-3 bg-white border-t border-slate-200 px-6 py-4 shrink-0">
              <AppButton
                type="button"
                variant="secondary"
                onClick={() => navigate(`/courses/${id}`)}
              >
                Cancel
              </AppButton>

              <AppButton
                type="submit"
                variant="primary"
                icon={Save}
                loading={saving}
                className="px-4 py-2 text-xs font-bold shadow-3xs"
              >
                Save Changes
              </AppButton>
            </div>
          </div>

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
        </form>
      </div>
      {uploadProgress && (
        <UploadProgressOverlay
          phase={uploadProgress.phase}
          loadedBytes={uploadProgress.loadedBytes}
          totalBytes={uploadProgress.totalBytes}
          percent={uploadProgress.percent}
          fileName={
            contentItems
              .filter(i => i.source === 'upload' && i.file)
              .map(i => i.file!.name)
              .join(', ') || 'SCORM Package'
          }
          onCancel={() => {
            if (abortUploadRef.current) {
              abortUploadRef.current()
            }
          }}
        />
      )}
    </>
  )
}
