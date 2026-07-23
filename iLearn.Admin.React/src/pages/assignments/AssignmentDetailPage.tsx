import { useState, useEffect, useMemo, useCallback } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import {
  Users,
  BookOpen,
  BookPlus,
  RotateCcw,
  Trash2,
  UserPlus,
  FileBarChart,
  CalendarClock,
  Search,
  X,
  Plus,
  Edit3
} from 'lucide-react'
import { StatusDonut, buildStatusData } from './AssignmentReportCharts'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { Badge } from '../../components/ui/Badge'
import { Card } from '../../components/ui/Card'
import { DetailLayout, Fact, FactGrid, StatTile, StatTileRow } from '../../components/ui/detail'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { AppButton } from '../../components/ui/AppButton'
import { IconButton } from '../../components/ui/IconButton'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { LearnerDirectorySelector, type LearnerSelection } from '../../components/shared/LearnerDirectorySelector'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { formatDate } from '../../lib/format'
import { ASSIGNMENT_LABELS, COMMON_LABELS, LEARNER_STATUS_KEYS, learnerStatusLabel, t, tf } from '../../lib/labels'
import { DetailTabs } from '../../components/ui/DetailTabs'
import { DETAIL_TABLE_CHUNK_SIZE } from '../../lib/tableStandards'

// Mirrors AssignmentDashboardDto returned by GET Assignments/dashboard/{id}
type AssignmentDetail = {
  assignmentNo: string
  description: string
  createdBy?: string | null
  createdByName?: string | null
  startDate: string | null
  dueDate: string | null
  totalEmployees: number
  totalCourses: number
  completionRate: number
  chartData: {
    completed: number
    inProgress: number
    notStarted: number
  }
  courses: Array<{
    assignmentRuleId: number
    courseCode: string
    courseTitle: string
    completedLearners: number
    totalLearners: number
    isCourseDeleted: boolean
  }>
  // Mirrors LearnerProgressDto (iLearn.Application/DTOs/AssignmentDashboardDto.cs)
  learners: Array<{
    learnerCode: string
    learnerName?: string | null
    division?: string | null
    department?: string | null
    learnerGroups?: string[] | null
    assignmentRuleId?: number | null
    courseCode?: string | null
    courseTitle?: string | null
    progress: number
    isCompleted: boolean
    // AssignmentStatusKeys.Learner: Completed | InProgress | NotStarted | Overdue | Upcoming
    status: string
    completedDate?: string | null
    startDate?: string | null
    dueDate?: string | null
  }>
  learnerGroupId?: number | null
  learnerGroupName?: string | null
  hasDeletedCourse: boolean
}

// Mirrors LookupCourseDto returned by GET Assignments/lookup-courses
type LookupCourse = {
  id: number
  code: string
  title: string
  courseTypeName?: string | null
}

type GroupedLearner = {
  learnerCode: string
  learnerName: string | null | undefined
  division: string | null | undefined
  department: string | null | undefined
  courses: Array<{
    assignmentRuleId: number | null | undefined
    courseCode: string | null | undefined
    courseTitle: string | null | undefined
    progress: number
    isCompleted: boolean
    status: string
  }>
}

const deriveAssignmentStatus = (a: AssignmentDetail) => {
  if (a.completionRate >= 100) return 'Completed'
  const now = Date.now()
  if (a.startDate && now < new Date(a.startDate).getTime()) return 'Upcoming'
  if (a.dueDate && now > new Date(a.dueDate).getTime()) return 'Overdue'
  return 'In Progress'
}

export function AssignmentDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { setLabel } = useBreadcrumbs()
  const { confirm, confirmDialog } = useConfirm()

  const [loading, setLoading] = useState(true)
  const [assignment, setAssignment] = useState<AssignmentDetail | null>(null)

  useEffect(() => {
    if (assignment?.assignmentNo) {
      setLabel(String(id), assignment.assignmentNo)
    }
  }, [assignment, id, setLabel])

  // Operational states
  const [extendingDate, setExtendingDate] = useState(false)
  const [newDueDateInput, setNewDueDateInput] = useState('')
  const [showDueDateModal, setShowDueDateModal] = useState(false)

  const [savingDescription, setSavingDescription] = useState(false)
  const [editDescriptionInput, setEditDescriptionInput] = useState('')
  const [showEditDescriptionModal, setShowEditDescriptionModal] = useState(false)

  const [addingLearners, setAddingLearners] = useState(false)
  const [memberAddTab, setMemberAddTab] = useState<'picker' | 'bulk'>('picker')
  const [pendingAddLearners, setPendingAddLearners] = useState<LearnerSelection[]>([])
  const [unverifiedCodes, setUnverifiedCodes] = useState<Set<string>>(new Set())
  const [learnerCodesInput, setLearnerCodesInput] = useState('')
  const [importingCodes, setImportingCodes] = useState(false)
  const [savingLearners, setSavingLearners] = useState(false)

  const [addingCourses, setAddingCourses] = useState(false)
  const [lookupCourses, setLookupCourses] = useState<LookupCourse[]>([])
  const [loadingLookupCourses, setLoadingLookupCourses] = useState(false)
  const [pendingCourseIds, setPendingCourseIds] = useState<number[]>([])
  const [courseSearch, setCourseSearch] = useState('')
  const [savingCourses, setSavingCourses] = useState(false)

  const [activeDetailTab, setActiveDetailTab] = useState<'courses' | 'learners'>('courses')
  const [visibleCourseRows, setVisibleCourseRows] = useState(DETAIL_TABLE_CHUNK_SIZE)
  const [visibleLearnerRows, setVisibleLearnerRows] = useState(DETAIL_TABLE_CHUNK_SIZE)

  // Learners tab: search / status filter / bulk selection
  const [learnerSearch, setLearnerSearch] = useState('')
  const [learnerStatusFilter, setLearnerStatusFilter] = useState<string>('All')
  const [selectedCodes, setSelectedCodes] = useState<Set<string>>(new Set())
  const [bulkWorking, setBulkWorking] = useState<'reset' | 'remove' | null>(null)

  // Learner course popup modal & keydown / body overflow listener
  const [courseModalCode, setCourseModalCode] = useState<string | null>(null)

  useEffect(() => {
    if (!courseModalCode) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setCourseModalCode(null)
      }
    }

    const originalOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    window.addEventListener('keydown', handleKeyDown)

    return () => {
      document.body.style.overflow = originalOverflow
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [courseModalCode])

  const groupedLearners = useMemo<GroupedLearner[]>(() => {
    if (!assignment?.learners) return []

    const map = new Map<string, GroupedLearner>()

    assignment.learners.forEach(l => {
      let entry = map.get(l.learnerCode)
      if (!entry) {
        entry = {
          learnerCode: l.learnerCode,
          learnerName: l.learnerName,
          division: l.division,
          department: l.department,
          courses: []
        }
        map.set(l.learnerCode, entry)
      }
      if (l.courseCode || l.courseTitle) {
        entry.courses.push({
          assignmentRuleId: l.assignmentRuleId,
          courseCode: l.courseCode,
          courseTitle: l.courseTitle,
          progress: l.progress,
          isCompleted: l.isCompleted,
          status: l.status
        })
      }
    })

    return Array.from(map.values())
  }, [assignment])

  const modalLearner = useMemo(() => {
    if (!courseModalCode) return null
    return groupedLearners.find(l => l.learnerCode === courseModalCode) ?? null
  }, [groupedLearners, courseModalCode])

  const filteredLearners = useMemo(() => {
    const q = learnerSearch.trim().toLowerCase()
    return groupedLearners.filter(l => {
      if (learnerStatusFilter !== 'All' && !l.courses.some(c => c.status === learnerStatusFilter)) {
        return false
      }
      if (!q) return true
      return (
        l.learnerCode.toLowerCase().includes(q) ||
        (l.learnerName ?? '').toLowerCase().includes(q) ||
        (l.division ?? '').toLowerCase().includes(q) ||
        (l.department ?? '').toLowerCase().includes(q)
      )
    })
  }, [groupedLearners, learnerSearch, learnerStatusFilter])

  const loadAssignmentDetails = useCallback(async () => {
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; data: AssignmentDetail }>(`Assignments/dashboard/${id}`)
      if (resp.success && resp.data) {
        setAssignment(resp.data)
        setNewDueDateInput(resp.data.dueDate ? (resp.data.dueDate.split('T')[0] || '') : '')
      }
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.failedToLoadDetails))
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void loadAssignmentDetails()
  }, [loadAssignmentDetails])

  useEffect(() => {
    setVisibleCourseRows(DETAIL_TABLE_CHUNK_SIZE)
    setVisibleLearnerRows(DETAIL_TABLE_CHUNK_SIZE)
    setSelectedCodes(new Set())
    setCourseModalCode(null)
  }, [id])

  useEffect(() => {
    setVisibleLearnerRows(DETAIL_TABLE_CHUNK_SIZE)
  }, [learnerSearch, learnerStatusFilter])

  // Drop selections that no longer exist after a reload (e.g. removed learners)
  useEffect(() => {
    setSelectedCodes(prev => {
      if (prev.size === 0) return prev
      const valid = new Set(groupedLearners.map(l => l.learnerCode))
      const next = new Set([...prev].filter(code => valid.has(code)))
      return next.size === prev.size ? prev : next
    })
  }, [groupedLearners])

  useEffect(() => {
    if (courseModalCode && !groupedLearners.some(l => l.learnerCode === courseModalCode)) {
      setCourseModalCode(null)
    }
  }, [groupedLearners, courseModalCode])

  // Extend due date
  const handleExtendDueDate = async () => {
    if (!newDueDateInput) return
    setExtendingDate(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Assignments/${id}/extend-due-date`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ newDueDate: `${newDueDateInput}T23:59:59` })
      })
      if (resp.success) {
        toast.success(resp.message)
        loadAssignmentDetails()
      }
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || t(ASSIGNMENT_LABELS.failedToExtendDueDate))
    } finally {
      setExtendingDate(false)
    }
  }

  // Update description
  const handleUpdateDescription = async () => {
    setSavingDescription(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Assignments/${id}/description`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ description: editDescriptionInput })
      })
      if (resp.success) {
        toast.success(resp.message)
        setShowEditDescriptionModal(false)
        loadAssignmentDetails()
      }
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || t(ASSIGNMENT_LABELS.failedToUpdateDescription))
    } finally {
      setSavingDescription(false)
    }
  }

  const parseLearnerCodes = (value: string) => {
    return Array.from(new Set(
      value
        .split(/[\n,;\s]+/)
        .map(code => code.trim())
        .filter(Boolean)
        .map(code => code.toUpperCase())
    ))
  }

  // Look up pasted EIds in the employee directory so typos are caught before enrolling.
  // Returns a map of found code -> directory info.
  const verifyCodesInDirectory = async (codes: string[]) => {
    const found = new Map<string, { name: string; division: string; department: string }>()
    const chunkSize = 40 // keep the OR-filter query string well under URL limits
    for (let i = 0; i < codes.length; i += chunkSize) {
      const chunk = codes.slice(i, i + chunkSize)
      let filter: unknown = ['EId', '=', chunk[0]]
      for (let j = 1; j < chunk.length; j++) {
        filter = [filter, 'or', ['EId', '=', chunk[j]]]
      }
      const url = `Learners/Get?skip=0&take=${chunk.length}&filter=${encodeURIComponent(JSON.stringify(filter))}`
      const resp = await fetchWithAccessControl<any>(url)
      const list: any[] = Array.isArray(resp) ? resp : resp?.data || []
      list.forEach(item => {
        // Learners rows are camelCase (typed DTO on the backend)
        const code = String(item.eId || '').trim().toUpperCase()
        if (!code) return
        const name = `${item.englishFirstName || ''} ${item.englishLastName || ''}`.trim()
        found.set(code, {
          name: name || code,
          division: item.division || '',
          department: item.department || '',
        })
      })
    }
    return found
  }

  const handleImportCodes = async () => {
    const parsedCodes = parseLearnerCodes(learnerCodesInput)
    if (parsedCodes.length === 0) {
      toast.error(t(ASSIGNMENT_LABELS.enterAtLeastOneEmployeeCode))
      return
    }

    setImportingCodes(true)
    let directory = new Map<string, { name: string; division: string; department: string }>()
    try {
      directory = await verifyCodesInDirectory(parsedCodes)
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.directoryCheckFailed))
    } finally {
      setImportingCodes(false)
    }

    const notFound = parsedCodes.filter(code => !directory.has(code))
    const newSelections = parsedCodes.map(code => {
      const info = directory.get(code)
      return {
        code,
        name: info?.name || code,
        division: info?.division || '',
        department: info?.department || ''
      }
    })

    setPendingAddLearners(prev => {
      const existingCodes = new Set(prev.map(l => l.code))
      const currentCodes = new Set(assignment?.learners.map(m => m.learnerCode.toUpperCase()) || [])

      const uniqueNew = newSelections.filter(l => !existingCodes.has(l.code) && !currentCodes.has(l.code))
      const duplicateCount = parsedCodes.length - uniqueNew.length
      if (duplicateCount > 0) {
        toast.info(tf(ASSIGNMENT_LABELS.codesSkipped, duplicateCount))
      }
      if (uniqueNew.length > 0) {
        toast.success(tf(ASSIGNMENT_LABELS.learnerCodesQueued, uniqueNew.length))
      }
      return [...prev, ...uniqueNew]
    })
    if (notFound.length > 0) {
      setUnverifiedCodes(prev => new Set([...prev, ...notFound]))
      toast.info(tf(ASSIGNMENT_LABELS.codesNotFoundReview, notFound.length))
    }
    setLearnerCodesInput('')
  }

  const closeAddLearnersModal = () => {
    setAddingLearners(false)
    setPendingAddLearners([])
    setUnverifiedCodes(new Set())
    setLearnerCodesInput('')
  }

  // Add more learners to this existing batch
  const handleAddLearners = async () => {
    const codes = pendingAddLearners.map(l => l.code)
    if (codes.length === 0) {
      toast.error(t(ASSIGNMENT_LABELS.selectOrImportLearner))
      return
    }

    const unverifiedQueued = codes.filter(code => unverifiedCodes.has(code))
    if (unverifiedQueued.length > 0) {
      if (!(await confirm({
        title: t(ASSIGNMENT_LABELS.unverifiedCodesInQueue),
        message: tf(ASSIGNMENT_LABELS.unverifiedCodesMessage, unverifiedQueued.length, `${unverifiedQueued.slice(0, 5).join(', ')}${unverifiedQueued.length > 5 ? ', …' : ''}`),
        confirmLabel: t(ASSIGNMENT_LABELS.addAnyway),
      }))) return
    }

    setSavingLearners(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Assignments/${id}/learners`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ employeeCodes: codes })
      })
      if (resp.success) {
        toast.success(resp.message)
        closeAddLearnersModal()
        loadAssignmentDetails()
      }
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || t(ASSIGNMENT_LABELS.failedToAddLearners))
    } finally {
      setSavingLearners(false)
    }
  }

  // Reset progress — whole batch or a single course rule for one or more learners
  const resetEnrollments = async (learnerCodes: string[], ruleIds?: number[]) => {
    const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Assignments/${id}/reset-enrollments`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(ruleIds && ruleIds.length > 0 ? { learnerCodes, ruleIds } : { learnerCodes })
    })
    if (resp.success) {
      toast.success(resp.message)
      loadAssignmentDetails()
    }
  }

  const handleResetLearner = async (learnerCode: string) => {
    if (!(await confirm({
      title: t(ASSIGNMENT_LABELS.resetProgress),
      message: tf(ASSIGNMENT_LABELS.resetLearnerProgressConfirm, learnerCode),
      confirmLabel: t(ASSIGNMENT_LABELS.reset),
    }))) return
    try {
      await resetEnrollments([learnerCode])
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.failedToResetProgress))
    }
  }

  const handleResetLearnerCourse = async (learnerCode: string, ruleId: number, courseTitle: string) => {
    if (!(await confirm({
      title: t(ASSIGNMENT_LABELS.resetCourseProgress),
      message: tf(ASSIGNMENT_LABELS.resetLearnerCourseProgressConfirm, learnerCode, courseTitle),
      confirmLabel: t(ASSIGNMENT_LABELS.resetCourse),
    }))) return
    try {
      await resetEnrollments([learnerCode], [ruleId])
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.failedToResetCourseProgress))
    }
  }

  const handleBulkReset = async () => {
    const codes = [...selectedCodes]
    if (codes.length === 0) return
    if (!(await confirm({
      title: t(ASSIGNMENT_LABELS.resetSelectedLearners),
      message: tf(ASSIGNMENT_LABELS.resetSelectedLearnersConfirm, codes.length),
      confirmLabel: tf(ASSIGNMENT_LABELS.resetLearnerCount, codes.length),
    }))) return
    setBulkWorking('reset')
    try {
      await resetEnrollments(codes)
      setSelectedCodes(new Set())
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.failedToResetSelectedLearners))
    } finally {
      setBulkWorking(null)
    }
  }

  // Remove learner(s) from assignment — batch links are removed, learning history is retained
  const handleRemoveLearner = async (learnerCode: string) => {
    if (!(await confirm({
      title: t(ASSIGNMENT_LABELS.removeLearner),
      message: tf(ASSIGNMENT_LABELS.removeLearnerConfirm, learnerCode),
      confirmLabel: t(ASSIGNMENT_LABELS.remove),
      danger: true,
    }))) return
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Assignments/${id}/learners/${learnerCode}`, {
        method: 'DELETE'
      })
      if (resp.success) {
        toast.success(resp.message)
        loadAssignmentDetails()
      }
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.failedToRemoveLearner))
    }
  }

  const handleBulkRemove = async () => {
    const codes = [...selectedCodes]
    if (codes.length === 0) return
    if (!(await confirm({
      title: t(ASSIGNMENT_LABELS.removeSelectedLearners),
      message: tf(ASSIGNMENT_LABELS.removeSelectedLearnersConfirm, codes.length),
      confirmLabel: tf(ASSIGNMENT_LABELS.removeLearnerCount, codes.length),
      danger: true,
    }))) return
    setBulkWorking('remove')
    try {
      // Mirrors AssignmentRemoveLearnersResponseDto (POST Assignments/{id}/learners/bulk-remove)
      const resp = await fetchWithAccessControl<{ success: boolean; message: string; removedCount: number }>(`Assignments/${id}/learners/bulk-remove`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ employeeCodes: codes })
      })
      if (resp.success) {
        toast.success(resp.message)
        setSelectedCodes(new Set())
        loadAssignmentDetails()
      }
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.failedToRemoveSelectedLearners))
    } finally {
      setBulkWorking(null)
    }
  }

  // Remove Course rule from batch
  const handleRemoveCourse = async (ruleId: number) => {
    if (!(await confirm({
      title: t(ASSIGNMENT_LABELS.deleteCourseRule),
      message: t(ASSIGNMENT_LABELS.deleteCourseRuleConfirm),
      confirmLabel: t(ASSIGNMENT_LABELS.delete),
      danger: true,
    }))) return
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string; assignmentDeleted?: boolean }>(`Assignments/${id}/courses/${ruleId}`, {
        method: 'DELETE'
      })
      if (resp.success) {
        toast.success(resp.message)
        if (resp.assignmentDeleted) {
          navigate('/assignments')
        } else {
          loadAssignmentDetails()
        }
      }
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.unableToDeleteAssignedCourse))
    }
  }

  // Add courses to this existing batch (POST Assignments/{id}/courses)
  const openAddCoursesModal = async () => {
    setAddingCourses(true)
    setPendingCourseIds([])
    setCourseSearch('')
    setLoadingLookupCourses(true)
    try {
      const resp = await fetchWithAccessControl<any>('Assignments/lookup-courses')
      const list: LookupCourse[] = Array.isArray(resp) ? resp : resp?.data || []
      setLookupCourses(list)
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.failedToLoadCourseLookup))
    } finally {
      setLoadingLookupCourses(false)
    }
  }

  const availableCourses = useMemo(() => {
    const existingCodes = new Set(
      (assignment?.courses || [])
        .filter(c => !c.isCourseDeleted)
        .map(c => c.courseCode)
    )
    const q = courseSearch.trim().toLowerCase()
    return lookupCourses.filter(c => {
      if (existingCodes.has(c.code)) return false
      if (!q) return true
      return c.code.toLowerCase().includes(q) || c.title.toLowerCase().includes(q)
    })
  }, [lookupCourses, assignment, courseSearch])

  const handleAddCourses = async () => {
    if (pendingCourseIds.length === 0) {
      toast.error(t(ASSIGNMENT_LABELS.selectAtLeastOneCourseToAdd))
      return
    }
    setSavingCourses(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string; addedCount: number }>(`Assignments/${id}/courses`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ courseIds: pendingCourseIds })
      })
      if (resp.success) {
        toast.success(resp.message)
        setAddingCourses(false)
        loadAssignmentDetails()
      }
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || t(ASSIGNMENT_LABELS.failedToAddCourses))
    } finally {
      setSavingCourses(false)
    }
  }

  // Delete entire assignment batch
  const handleDeleteBatch = async () => {
    if (!(await confirm({
      title: t(ASSIGNMENT_LABELS.deleteAssignmentBatch),
      message: t(ASSIGNMENT_LABELS.deleteAssignmentBatchConfirm),
      confirmLabel: t(ASSIGNMENT_LABELS.deleteBatch),
      danger: true,
    }))) return
    try {
      await fetchWithAccessControl(`Assignments/${id}`, {
        method: 'DELETE'
      })
      toast.success(t(ASSIGNMENT_LABELS.assignmentBatchDeleted))
      navigate('/assignments')
    } catch (err) {
      console.error(err)
      toast.error(t(ASSIGNMENT_LABELS.failedToDeleteAssignmentRules))
    }
  }

  const toggleLearnerSelection = (code: string) => {
    setSelectedCodes(prev => {
      const next = new Set(prev)
      if (next.has(code)) {
        next.delete(code)
      } else {
        next.add(code)
      }
      return next
    })
  }

  if (loading) {
    return <LoadingState />
  }

  if (!assignment) {
    return (
      <NotFoundState
        title={t(ASSIGNMENT_LABELS.assignmentNotFound)}
        message={t(ASSIGNMENT_LABELS.assignmentReportUnavailable)}
        backTo="/assignments"
        backLabel={t(ASSIGNMENT_LABELS.backToRegistry)}
      />
    )
  }

  const assignmentStatus = deriveAssignmentStatus(assignment)
  const detailTabs: Array<{ key: 'courses' | 'learners'; label: string }> = [
    { key: 'courses', label: t(ASSIGNMENT_LABELS.courses) },
    { key: 'learners', label: t(ASSIGNMENT_LABELS.learners) },
  ]
  const visibleCourses = assignment.courses.slice(0, visibleCourseRows)
  const visibleGroupedLearners = filteredLearners.slice(0, visibleLearnerRows)
  const allFilteredSelected =
    filteredLearners.length > 0 && filteredLearners.every(l => selectedCodes.has(l.learnerCode))

  const toggleSelectAllFiltered = () => {
    setSelectedCodes(prev => {
      const next = new Set(prev)
      if (allFilteredSelected) {
        filteredLearners.forEach(l => next.delete(l.learnerCode))
      } else {
        filteredLearners.forEach(l => next.add(l.learnerCode))
      }
      return next
    })
  }

  return (
    <>
      <DetailLayout
        sidebar={
          <ControlsSidebar>
            <ControlAction to={`/assignments/${id}/report`} icon={FileBarChart}>{t(ASSIGNMENT_LABELS.openReport)}</ControlAction>
            <ControlAction icon={Edit3} onClick={() => { setEditDescriptionInput(assignment.description || ''); setShowEditDescriptionModal(true) }}>{t(ASSIGNMENT_LABELS.editDescription)}</ControlAction>
            <ControlAction icon={UserPlus} onClick={() => setAddingLearners(true)}>{t(ASSIGNMENT_LABELS.addMoreLearners)}</ControlAction>
            <ControlAction icon={BookPlus} onClick={openAddCoursesModal}>{t(ASSIGNMENT_LABELS.addCourses)}</ControlAction>
            <ControlAction icon={CalendarClock} onClick={() => setShowDueDateModal(true)}>{t(ASSIGNMENT_LABELS.extendDueDate)}</ControlAction>
            <ControlAction icon={Trash2} onClick={handleDeleteBatch} variant="danger">{t(ASSIGNMENT_LABELS.deleteBatch)}</ControlAction>
          </ControlsSidebar>
        }
      >
        <main className="space-y-6">
          <Card icon={FileBarChart} title={t(ASSIGNMENT_LABELS.overview)} bodyClassName="p-5 space-y-5">
            <div className="grid grid-cols-1 lg:grid-cols-[1fr_auto] gap-6 items-center">
              <div className="flex flex-col gap-4">
                <StatTileRow cols={3}>
                  <StatTile label={t(ASSIGNMENT_LABELS.learners)}>
                    {assignment.totalEmployees}
                  </StatTile>
                  <StatTile label={t(ASSIGNMENT_LABELS.courses)}>
                    {assignment.totalCourses}
                  </StatTile>
                  <StatTile label={t(ASSIGNMENT_LABELS.status)}>
                    <StatusBadge>{learnerStatusLabel(assignmentStatus)}</StatusBadge>
                  </StatTile>
                </StatTileRow>

                <div className="flex items-start justify-between gap-2">
                  <p className={`text-sm leading-relaxed max-w-2xl border-l-2 border-slate-200 pl-3 ${assignment.description ? 'text-slate-500 whitespace-pre-wrap' : 'text-slate-400 italic'}`}>
                    {assignment.description || t(ASSIGNMENT_LABELS.noDescription)}
                  </p>
                  <IconButton
                    icon={Edit3}
                    title={t(ASSIGNMENT_LABELS.editDescription)}
                    size="sm"
                    tone="neutral"
                    onClick={() => {
                      setEditDescriptionInput(assignment.description || '')
                      setShowEditDescriptionModal(true)
                    }}
                  />
                </div>

                <FactGrid className="pt-2">
                  <Fact label={t(ASSIGNMENT_LABELS.startDateLabel)} valueClassName="font-semibold">
                    {formatDate(assignment.startDate)}
                  </Fact>
                  <Fact label={t(ASSIGNMENT_LABELS.dueDateLabel)} valueClassName="font-semibold">
                    {formatDate(assignment.dueDate)}
                  </Fact>
                  {assignment.createdBy && (
                    <Fact label={t(ASSIGNMENT_LABELS.createdBy)} valueClassName="font-semibold">
                      {assignment.createdByName ?? assignment.createdBy}
                      {assignment.createdByName && (
                        <span className="block text-xxs font-mono text-slate-400">{assignment.createdBy}</span>
                      )}
                    </Fact>
                  )}
                  {assignment.learnerGroupName && (
                    <Fact label={t(ASSIGNMENT_LABELS.learnerGroup)} colSpan="full" valueClassName="font-semibold">
                      {assignment.learnerGroupName}
                    </Fact>
                  )}
                </FactGrid>
              </div>
              <div className="w-full lg:w-[280px] shrink-0 border-t lg:border-t-0 lg:border-l border-slate-100 pt-4 lg:pt-0 lg:pl-6">
                <StatusDonut
                  data={buildStatusData(assignment.learners)}
                  completionRate={assignment.completionRate}
                  activeStatus={learnerStatusFilter}
                />
              </div>
            </div>
          </Card>

          <DetailTabs
            tabs={detailTabs}
            active={activeDetailTab}
            onChange={setActiveDetailTab}
          />

          {activeDetailTab === 'courses' && (
            <Card icon={BookOpen} title={t(ASSIGNMENT_LABELS.courses)}>

              <ul className="divide-y divide-slate-100 px-4">
                {visibleCourses.map((c) => (
                  <li key={c.assignmentRuleId} className="py-2.5 flex items-center justify-between">
                    <div className="flex flex-col">
                      <span className={`text-sm font-bold ${c.isCourseDeleted ? 'text-slate-400 line-through' : 'text-slate-800'}`}>
                        {c.courseTitle}
                        {c.isCourseDeleted && <span className="ml-1.5 text-xxs font-semibold no-underline">({t(ASSIGNMENT_LABELS.deleted)})</span>}
                      </span>
                      <span className="text-xxs font-mono text-slate-400 mt-0.5">{c.courseCode}</span>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="text-xxs font-bold text-slate-500">
                        {tf(ASSIGNMENT_LABELS.completedOf, c.completedLearners, c.totalLearners)}
                      </span>
                      <IconButton
                        onClick={() => handleRemoveCourse(c.assignmentRuleId)}
                        icon={Trash2}
                        tone="danger"
                        size="sm"
                        title={t(ASSIGNMENT_LABELS.removeCourseFromAssignment)}
                      />
                    </div>
                  </li>
                ))}
              </ul>

              {assignment.courses.length > 0 && (
                <div className="flex items-center justify-between gap-2 border-t border-slate-100 bg-slate-50/40 px-3 py-2">
                  <span className="text-xxs font-semibold uppercase tracking-wide text-slate-500">
                    {tf(ASSIGNMENT_LABELS.showingOf, visibleCourses.length, assignment.courses.length)}
                  </span>
                  {assignment.courses.length > visibleCourses.length && (
                    <AppButton
                      variant="ghost"
                      onClick={() => setVisibleCourseRows(prev => prev + DETAIL_TABLE_CHUNK_SIZE)}
                      className="px-3 py-1 text-xxs font-bold"
                    >
                      {t(ASSIGNMENT_LABELS.loadMore)}
                    </AppButton>
                  )}
                </div>
              )}
            </Card>
          )}

          {activeDetailTab === 'learners' && (
            <Card icon={Users} title={t(ASSIGNMENT_LABELS.learners)}>

              <div className="border-b border-slate-100 bg-slate-50/20 px-4">
                <ListToolbar
                  searchValue={learnerSearch}
                  onSearchChange={setLearnerSearch}
                  searchPlaceholder={t(ASSIGNMENT_LABELS.searchLearners)}
                  toolbarContent={
                    <SegmentedToggle
                      variant="filter"
                      options={[
                        { value: 'All', label: t(COMMON_LABELS.all) },
                        ...LEARNER_STATUS_KEYS.map(s => ({ value: s, label: learnerStatusLabel(s) })),
                      ]}
                      value={learnerStatusFilter}
                      onChange={setLearnerStatusFilter}
                      className="flex-wrap"
                    />
                  }
                />
              </div>

              {selectedCodes.size > 0 && (
                <div className="flex flex-wrap items-center justify-between gap-3 border-b border-indigo-100 bg-indigo-50/60 px-4 py-2">
                  <span className="text-xs font-bold text-indigo-700 select-none">
                    {tf(ASSIGNMENT_LABELS.selectedLearners, selectedCodes.size)}
                  </span>
                  <div className="flex items-center gap-2">
                    <AppButton
                      variant="secondary"
                      icon={RotateCcw}
                      onClick={handleBulkReset}
                      loading={bulkWorking === 'reset'}
                      disabled={bulkWorking !== null}
                      className="px-3 py-1.5 text-xs"
                    >
                      {t(ASSIGNMENT_LABELS.resetSelected)}
                    </AppButton>
                    <AppButton
                      variant="danger"
                      icon={Trash2}
                      onClick={handleBulkRemove}
                      loading={bulkWorking === 'remove'}
                      disabled={bulkWorking !== null}
                      className="px-3 py-1.5 text-xs"
                    >
                      {t(ASSIGNMENT_LABELS.removeSelected)}
                    </AppButton>
                    <button
                      type="button"
                      onClick={() => setSelectedCodes(new Set())}
                      className="text-xs font-bold text-slate-500 hover:text-slate-700 px-2 py-1 rounded hover:bg-white/70 transition cursor-pointer"
                    >
                      {t(ASSIGNMENT_LABELS.clear)}
                    </button>
                  </div>
                </div>
              )}

              <div className="overflow-x-auto max-h-105 custom-scrollbar">
                <table className="w-full text-left text-sm border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none">
                      <th className="p-3 w-10 text-center">
                        <input
                          type="checkbox"
                          checked={allFilteredSelected}
                          onChange={toggleSelectAllFiltered}
                          className="h-4 w-4 text-indigo-500 rounded border-slate-300 focus:ring-indigo-400 cursor-pointer"
                          title={t(ASSIGNMENT_LABELS.selectAllFiltered)}
                        />
                      </th>
                      <th className="p-3">{t(ASSIGNMENT_LABELS.learners)}</th>
                      <th className="p-3">{t(ASSIGNMENT_LABELS.summary)}</th>
                      <th className="p-3 text-center">{t(ASSIGNMENT_LABELS.actions)}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-slate-700">
                    {visibleGroupedLearners.map((l) => {
                      const completedCount = l.courses.filter(c => c.isCompleted).length
                      const totalCount = l.courses.length
                      const allCompleted = totalCount > 0 && completedCount === totalCount
                      const isSelected = selectedCodes.has(l.learnerCode)

                      return (
                        <tr key={l.learnerCode} className={`transition ${isSelected ? 'bg-indigo-50/40' : 'hover:bg-slate-50/60'}`}>
                          <td className="p-3 w-10 text-center align-top">
                            <input
                              type="checkbox"
                              checked={isSelected}
                              onChange={() => toggleLearnerSelection(l.learnerCode)}
                              className="h-4 w-4 text-indigo-500 rounded border-slate-300 focus:ring-indigo-400 cursor-pointer"
                            />
                          </td>
                          <td className="p-3 align-top">
                            <div className="flex flex-col">
                              <Link
                                to={`/learners/${l.learnerCode}/profile`}
                                className="font-bold text-slate-800 leading-tight hover:text-indigo-700 hover:underline"
                                title={t(ASSIGNMENT_LABELS.openLearnerProfile)}
                              >
                                {l.learnerName || l.learnerCode}
                              </Link>
                              <span className="text-xxs font-mono text-slate-400 mt-0.5">{l.learnerCode}</span>
                              {(l.division || l.department) && (
                                <span className="text-xxs text-slate-400 mt-0.5">
                                  {[l.division, l.department].filter(Boolean).join(' · ')}
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="p-3 align-top">
                            <div className="flex flex-col gap-1.5 items-start">
                              <div className="flex items-center gap-2 flex-wrap">
                                <span className="text-xs font-bold text-slate-700">
                                  {tf(ASSIGNMENT_LABELS.completedOf, completedCount, totalCount)}
                                </span>
                                <StatusBadge size="xxs" tone={allCompleted ? 'success' : 'neutral'}>
                                  {learnerStatusLabel(allCompleted ? 'Completed' : 'InProgress')}
                                </StatusBadge>
                              </div>
                              {totalCount === 0 ? (
                                <span className="text-slate-400 text-xs italic">{t(ASSIGNMENT_LABELS.noCoursesAssigned)}</span>
                              ) : (
                                <div className="flex items-center gap-2">
                                  <Badge tone="neutral" variant="soft" size="xxs">{tf(ASSIGNMENT_LABELS.courseCount, totalCount)}</Badge>
                                  <AppButton
                                    variant="ghost"
                                    onClick={() => setCourseModalCode(l.learnerCode)}
                                    className="px-2 py-0.5 text-xxs font-bold"
                                  >
                                    {t(ASSIGNMENT_LABELS.viewCourses)}
                                  </AppButton>
                                </div>
                              )}
                            </div>
                          </td>
                          <td className="p-3 text-center align-top">
                            <div className="inline-flex items-center gap-1.5">
                              <IconButton
                                onClick={() => handleResetLearner(l.learnerCode)}
                                icon={RotateCcw}
                                tone="primary"
                                size="sm"
                                title={t(ASSIGNMENT_LABELS.resetAllLearnerCourses)}
                              />
                              <IconButton
                                onClick={() => handleRemoveLearner(l.learnerCode)}
                                icon={Trash2}
                                tone="danger"
                                size="sm"
                                title={t(ASSIGNMENT_LABELS.removeLearnerFromBatch)}
                              />
                            </div>
                          </td>
                        </tr>
                      )
                    })}
                    {filteredLearners.length === 0 && (
                      <tr>
                        <td className="p-6 text-center text-slate-400" colSpan={4}>
                          {t(ASSIGNMENT_LABELS.noLearnersMatchFilter)}
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>

              {filteredLearners.length > 0 && (
                <div className="flex items-center justify-between gap-2 border-t border-slate-100 bg-slate-50/40 px-3 py-2">
                  <span className="text-xxs font-semibold uppercase tracking-wide text-slate-500">
                    {tf(ASSIGNMENT_LABELS.showingOf, visibleGroupedLearners.length, filteredLearners.length)}
                    {filteredLearners.length !== groupedLearners.length && (
                      <span className="normal-case font-normal text-slate-400"> {tf(ASSIGNMENT_LABELS.filteredFrom, groupedLearners.length)}</span>
                    )}
                  </span>
                  {filteredLearners.length > visibleGroupedLearners.length && (
                    <AppButton
                      variant="ghost"
                      onClick={() => setVisibleLearnerRows(prev => prev + DETAIL_TABLE_CHUNK_SIZE)}
                      className="px-3 py-1 text-xxs font-bold"
                    >
                      {t(ASSIGNMENT_LABELS.loadMore)}
                    </AppButton>
                  )}
                </div>
              )}
            </Card>
          )}
        </main>

      </DetailLayout>

      {/* Extend Due Date Modal */}
      {showDueDateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-fade-in" onClick={() => setShowDueDateModal(false)}>
          <div className="bg-white border border-slate-100 rounded-xl shadow-2xl w-full max-w-sm overflow-hidden flex flex-col animate-scale-up duration-200" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <CalendarClock className="h-5 w-5 text-indigo-600" />
                <h3 className="text-base font-extrabold text-slate-800 uppercase tracking-wide">{t(ASSIGNMENT_LABELS.extendDueDate)}</h3>
              </div>
              <IconButton
                onClick={() => setShowDueDateModal(false)}
                icon={X}
                title={t(ASSIGNMENT_LABELS.close)}
                tone="neutral"
              />
            </div>

            <div className="px-6 py-5 space-y-4">
              <div className="flex items-center gap-3 text-sm text-slate-600">
                <span className="text-slate-400 font-semibold uppercase text-xs">{t(ASSIGNMENT_LABELS.currentDueDate)}</span>
                <span className="font-bold text-slate-800">{formatDate(assignment.dueDate)}</span>
              </div>

              <div className="space-y-1.5">
                <label htmlFor="newDue" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">{t(ASSIGNMENT_LABELS.newDueDate)}</label>
                <input
                  type="date"
                  id="newDue"
                  value={newDueDateInput}
                  onChange={(e) => setNewDueDateInput(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm text-slate-800 bg-slate-50/50 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 transition duration-150"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <AppButton
                variant="ghost"
                onClick={() => setShowDueDateModal(false)}
              >
                {t(ASSIGNMENT_LABELS.cancel)}
              </AppButton>
              <AppButton
                variant="primary"
                loading={extendingDate}
                disabled={!newDueDateInput}
                onClick={async () => {
                  await handleExtendDueDate()
                  setShowDueDateModal(false)
                }}
              >
                {t(ASSIGNMENT_LABELS.confirm)}
              </AppButton>
            </div>
          </div>
        </div>
      )}

      {/* Edit Description Modal */}
      {showEditDescriptionModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-fade-in" onClick={() => setShowEditDescriptionModal(false)}>
          <div className="bg-white border border-slate-200 rounded-xl shadow-2xl w-full max-w-lg flex flex-col overflow-hidden animate-scale-up" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none shrink-0">
              <div className="flex items-center gap-2">
                <Edit3 className="h-5 w-5 text-indigo-600" />
                <h3 className="text-sm font-extrabold text-slate-800 uppercase tracking-wider">{t(ASSIGNMENT_LABELS.editDescription)}</h3>
              </div>
              <IconButton
                onClick={() => setShowEditDescriptionModal(false)}
                icon={X}
                title={t(ASSIGNMENT_LABELS.close)}
                tone="neutral"
              />
            </div>

            <div className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1.5">
                  {t(ASSIGNMENT_LABELS.assignmentDescription)}
                </label>
                <textarea
                  rows={4}
                  value={editDescriptionInput}
                  onChange={(e) => setEditDescriptionInput(e.target.value)}
                  placeholder={t(ASSIGNMENT_LABELS.assignmentDescriptionPlaceholder)}
                  className="w-full px-3 py-2 border border-slate-200 rounded-lg text-xs font-medium focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition custom-scrollbar"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50 shrink-0">
              <AppButton
                variant="ghost"
                onClick={() => setShowEditDescriptionModal(false)}
              >
                {t(ASSIGNMENT_LABELS.cancel)}
              </AppButton>
              <AppButton
                variant="primary"
                loading={savingDescription}
                onClick={handleUpdateDescription}
              >
                {t(ASSIGNMENT_LABELS.save)}
              </AppButton>
            </div>
          </div>
        </div>
      )}

      {/* Add Courses Modal */}
      {addingCourses && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-fade-in" onClick={() => setAddingCourses(false)}>
          <div className="bg-white border border-slate-200 rounded-xl shadow-2xl w-full max-w-2xl h-[75vh] flex flex-col overflow-hidden animate-scale-up" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none shrink-0">
              <div className="flex items-center gap-2">
                <BookPlus className="h-5 w-5 text-indigo-600" />
                <h3 className="text-sm font-extrabold text-slate-800 uppercase tracking-wider">{t(ASSIGNMENT_LABELS.addCoursesToBatch)}</h3>
              </div>
              <IconButton
                onClick={() => setAddingCourses(false)}
                icon={X}
                title={t(ASSIGNMENT_LABELS.close)}
                tone="neutral"
              />
            </div>

            <div className="px-6 py-3 border-b border-slate-100 shrink-0">
              <div className="relative">
                <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400 pointer-events-none" />
                <input
                  type="text"
                  placeholder={t(ASSIGNMENT_LABELS.searchCourseCodeOrTitle)}
                  value={courseSearch}
                  onChange={(e) => setCourseSearch(e.target.value)}
                  className="w-full pl-9 pr-3 py-2 border border-slate-200 rounded-lg text-xs font-semibold placeholder:text-slate-400 bg-white focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition"
                />
              </div>
              <p className="text-xxs text-slate-400 font-medium mt-2">
                {t(ASSIGNMENT_LABELS.addCoursesScheduleNote)}
              </p>
            </div>

            <div className="flex-1 overflow-y-auto custom-scrollbar divide-y divide-slate-100 min-h-0">
              {loadingLookupCourses ? (
                <div className="py-12"><LoadingState label={t(ASSIGNMENT_LABELS.loadingCourses)} /></div>
              ) : availableCourses.length === 0 ? (
                <div className="text-center py-12 text-slate-400 text-xs font-semibold">
                  {t(ASSIGNMENT_LABELS.noAssignableCoursesFound)}
                </div>
              ) : (
                availableCourses.map(c => {
                  const checked = pendingCourseIds.includes(c.id)
                  return (
                    <label
                      key={c.id}
                      className={`flex items-center gap-3 px-6 py-2.5 cursor-pointer transition ${checked ? 'bg-indigo-50/40' : 'hover:bg-slate-50/70'}`}
                    >
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={() =>
                          setPendingCourseIds(prev =>
                            checked ? prev.filter(x => x !== c.id) : [...prev, c.id]
                          )
                        }
                        className="h-4 w-4 text-indigo-500 rounded border-slate-300 focus:ring-indigo-400 cursor-pointer"
                      />
                      <div className="flex flex-col min-w-0">
                        <span className="text-xs font-bold text-slate-800 truncate">{c.title}</span>
                        <span className="text-xxs font-mono text-slate-400 mt-0.5">
                          {c.code}
                          {c.courseTypeName ? ` · ${c.courseTypeName}` : ''}
                        </span>
                      </div>
                    </label>
                  )
                })
              )}
            </div>

            <div className="flex items-center justify-between gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50 shrink-0">
              <span className="text-xxs font-bold text-slate-500 uppercase tracking-wide select-none">
                {tf(ASSIGNMENT_LABELS.coursesSelected, pendingCourseIds.length)}
              </span>
              <div className="flex items-center gap-2">
                <AppButton
                  variant="ghost"
                  onClick={() => setAddingCourses(false)}
                >
                  {t(ASSIGNMENT_LABELS.cancel)}
                </AppButton>
                <AppButton
                  variant="primary"
                  icon={Plus}
                  onClick={handleAddCourses}
                  loading={savingCourses}
                  disabled={pendingCourseIds.length === 0}
                >
                  {t(ASSIGNMENT_LABELS.addCourses)}
                </AppButton>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Add Learners Modal */}
      {addingLearners && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center z-50 p-4 transition-all animate-fade-in" onClick={closeAddLearnersModal}>
          <div className="bg-white border border-slate-200 rounded-xl shadow-2xl w-full max-w-5xl h-[85vh] flex flex-col p-6 gap-4 animate-scale-up" onClick={(e) => e.stopPropagation()}>

            {/* Modal Header */}
            <div className="flex items-center justify-between border-b border-slate-200/60 pb-3 shrink-0 select-none">
              <div className="flex items-center gap-2">
                <UserPlus className="h-5 w-5 text-indigo-500" />
                <h2 className="font-extrabold text-slate-800 text-sm uppercase tracking-wider">{t(ASSIGNMENT_LABELS.addMoreLearners)}</h2>
              </div>

              <div className="flex items-center gap-4 bg-slate-50 p-1.5 rounded border border-slate-100">
                <SegmentedToggle
                  options={[
                    { value: 'picker', label: t(ASSIGNMENT_LABELS.directorySearch) },
                    { value: 'bulk', label: t(ASSIGNMENT_LABELS.bulkImportEmployeeIds) },
                  ]}
                  value={memberAddTab}
                  onChange={setMemberAddTab}
                />
              </div>

              <IconButton
                onClick={closeAddLearnersModal}
                icon={X}
                title={t(ASSIGNMENT_LABELS.close)}
                tone="neutral"
                size="sm"
              />
            </div>

            {/* Modal Body */}
            <div className="flex-1 min-h-0 flex flex-col">
              {memberAddTab === 'picker' ? (
                <div className="flex-1 flex flex-col min-h-0">
                  <LearnerDirectorySelector
                    selectedLearners={pendingAddLearners}
                    onChange={setPendingAddLearners}
                  />
                </div>
              ) : (
                <div className="space-y-4 h-full flex flex-col justify-start overflow-y-auto custom-scrollbar pr-1">
                  <p className="text-xs font-medium text-slate-500">
                    {t(ASSIGNMENT_LABELS.bulkImportEmployeeIdsNote)}
                  </p>
                  <div className="grid grid-cols-1 gap-3 md:grid-cols-[minmax(0,1fr)_auto] shrink-0">
                    <textarea
                      id="learnerCodes"
                      rows={5}
                      value={learnerCodesInput}
                      onChange={(e) => setLearnerCodesInput(e.target.value)}
                      placeholder={t(ASSIGNMENT_LABELS.employeeIdsPlaceholder)}
                      className="w-full px-3 py-2 border border-slate-200 rounded text-sm font-mono text-slate-850 focus:outline-none focus:border-indigo-500 bg-slate-50/50"
                    />
                    <AppButton
                      type="button"
                      variant="primary"
                      icon={Plus}
                      onClick={handleImportCodes}
                      loading={importingCodes}
                      disabled={!learnerCodesInput.trim()}
                      className="self-start"
                    >
                      {t(ASSIGNMENT_LABELS.addToQueue)}
                    </AppButton>
                  </div>

                  {/* Queued codes view */}
                  <div className="border border-slate-200 rounded-lg overflow-hidden flex flex-col flex-1 min-h-0">
                    <div className="bg-slate-50 px-4 py-2 border-b border-slate-200 flex justify-between items-center text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none shrink-0">
                      <span>{tf(ASSIGNMENT_LABELS.queuedForAssignmentAdditions, pendingAddLearners.length)}</span>
                      <div className="flex items-center gap-3">
                        {pendingAddLearners.some(l => unverifiedCodes.has(l.code)) && (
                          <button
                            type="button"
                            onClick={() => {
                              setPendingAddLearners(prev => prev.filter(l => !unverifiedCodes.has(l.code)))
                              setUnverifiedCodes(new Set())
                            }}
                            className="text-amber-600 hover:text-amber-800 font-bold cursor-pointer"
                          >
                            {t(ASSIGNMENT_LABELS.removeNotFound)}
                          </button>
                        )}
                        {pendingAddLearners.length > 0 && (
                          <button
                            type="button"
                            onClick={() => { setPendingAddLearners([]); setUnverifiedCodes(new Set()) }}
                            className="text-red-500 hover:text-red-700 font-bold cursor-pointer"
                          >
                            {t(ASSIGNMENT_LABELS.clearQueue)}
                          </button>
                        )}
                      </div>
                    </div>
                    <div className="flex-1 overflow-y-auto custom-scrollbar divide-y divide-slate-100 bg-white min-h-0">
                      {pendingAddLearners.length === 0 ? (
                        <div className="text-center py-12 text-slate-400 text-xs font-semibold">{t(ASSIGNMENT_LABELS.queueIsEmpty)}</div>
                      ) : (
                        pendingAddLearners.map((l, idx) => (
                          <div key={l.code} className="px-4 py-2.5 flex justify-between items-center text-xs font-medium">
                            <div className="flex items-center gap-4">
                              <span className="font-bold text-slate-400 w-8">{idx + 1}</span>
                              <span className="font-mono text-slate-850 font-semibold">{l.code}</span>
                              {l.name !== l.code && <span className="text-slate-500 text-xxs">({l.name})</span>}
                              {unverifiedCodes.has(l.code) && (
                                <StatusBadge size="xxs" tone="warning">{t(COMMON_LABELS.notFoundInDirectory)}</StatusBadge>
                              )}
                            </div>
                            <button
                              type="button"
                              onClick={() => setPendingAddLearners(prev => prev.filter(x => x.code !== l.code))}
                              className="text-red-500 hover:text-red-700 font-bold text-xxs cursor-pointer"
                            >
                              {t(ASSIGNMENT_LABELS.remove)}
                            </button>
                          </div>
                        ))
                      )}
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Modal Footer */}
            <div className="shrink-0 border-t border-slate-100 pt-4 flex justify-end gap-2 select-none">
              <AppButton
                variant="ghost"
                onClick={closeAddLearnersModal}
              >
                {t(ASSIGNMENT_LABELS.cancel)}
              </AppButton>
              <AppButton
                variant="primary"
                onClick={handleAddLearners}
                loading={savingLearners}
                disabled={pendingAddLearners.length === 0}
              >
                {t(ASSIGNMENT_LABELS.addLearners)}
              </AppButton>
            </div>
          </div>
        </div>
      )}

      {/* Learner Courses Modal */}
      {courseModalCode && modalLearner && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-fade-in" onClick={() => setCourseModalCode(null)}>
          <div className="bg-white border border-slate-200 rounded-xl shadow-2xl w-full max-w-xl max-h-[85vh] flex flex-col overflow-hidden animate-scale-up" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none shrink-0">
              <div className="flex items-center gap-2.5 min-w-0">
                <BookOpen className="h-5 w-5 text-indigo-600 shrink-0" />
                <div className="flex flex-col min-w-0">
                  <h3 className="text-sm font-extrabold text-slate-800 uppercase tracking-wider truncate">
                    {modalLearner.learnerName || modalLearner.learnerCode}
                  </h3>
                  <span className="text-xxs font-mono text-slate-400">{modalLearner.learnerCode}</span>
                </div>
              </div>
              <IconButton
                onClick={() => setCourseModalCode(null)}
                icon={X}
                title={t(ASSIGNMENT_LABELS.close)}
                tone="neutral"
              />
            </div>

            <div className="flex-1 overflow-y-auto custom-scrollbar p-6 space-y-3 min-h-0">
              {modalLearner.courses.length === 0 ? (
                <div className="text-center py-8 text-slate-400 text-xs font-semibold italic">
                  {t(ASSIGNMENT_LABELS.noCoursesAssigned)}
                </div>
              ) : (
                modalLearner.courses.map((c) => (
                  <div key={c.assignmentRuleId ?? c.courseCode ?? ''} className="flex items-center justify-between gap-4 p-3 rounded-lg border border-slate-100 bg-slate-50/50">
                    <div className="flex flex-col min-w-0 flex-1">
                      <span className="font-semibold text-slate-800 text-xs truncate" title={c.courseTitle || ''}>
                        {c.courseTitle}
                      </span>
                      <span className="font-mono text-slate-400 text-xxs mt-0.5">{c.courseCode}</span>
                    </div>
                    <div className="flex items-center gap-3 shrink-0">
                      <ProgressBar value={c.progress} completed={c.isCompleted} maxWidthClass="max-w-20" />
                      <StatusBadge size="xxs">{learnerStatusLabel(c.status)}</StatusBadge>
                      {typeof c.assignmentRuleId === 'number' && (
                        <IconButton
                          onClick={() => handleResetLearnerCourse(modalLearner.learnerCode, c.assignmentRuleId as number, c.courseTitle || c.courseCode || '')}
                          icon={RotateCcw}
                          tone="neutral"
                          size="sm"
                          title={t(ASSIGNMENT_LABELS.resetThisCourseOnly)}
                        />
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>

            <div className="flex items-center justify-end px-6 py-4 border-t border-slate-100 bg-slate-50/50 shrink-0">
              <AppButton
                variant="ghost"
                onClick={() => setCourseModalCode(null)}
              >
                {t(ASSIGNMENT_LABELS.close)}
              </AppButton>
            </div>
          </div>
        </div>
      )}

      {confirmDialog}
    </>
  )
}
