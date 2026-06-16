import { useState, useEffect, useMemo, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Users,
  BookOpen,
  RotateCcw,
  Trash2,
  UserPlus,
  FileBarChart,
  CalendarClock,
  X,
  Plus
} from 'lucide-react'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { StatusText } from '../../components/ui/StatusText'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { DetailLayout, Fact, FactGrid } from '../../components/ui/detail'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { AppButton } from '../../components/ui/AppButton'
import { LearnerDirectorySelector, type LearnerSelection } from '../../components/shared/LearnerDirectorySelector'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { formatDate } from '../../lib/format'
import { DetailTabs } from '../../components/ui/DetailTabs'

// Mirrors AssignmentDashboardDto returned by GET Assignments/dashboard/{id}
type AssignmentDetail = {
  assignmentNo: string
  description: string
  createdBy?: string | null
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
  learners: Array<{
    learnerCode: string
    learnerName?: string | null
    assignmentRuleId?: number | null
    courseCode?: string | null
    courseTitle?: string | null
    progress: number
    isCompleted: boolean
    status: string
    completedDate?: string | null
    startDate?: string | null
    dueDate?: string | null
  }>
  learnerGroupId?: number | null
  learnerGroupName?: string | null
  hasDeletedCourse: boolean
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
  
  const [addingLearners, setAddingLearners] = useState(false)
  const [memberAddTab, setMemberAddTab] = useState<'picker' | 'bulk'>('picker')
  const [pendingAddLearners, setPendingAddLearners] = useState<LearnerSelection[]>([])
  const [learnerCodesInput, setLearnerCodesInput] = useState('')
  const [savingLearners, setSavingLearners] = useState(false)
  const [activeDetailTab, setActiveDetailTab] = useState<'courses' | 'learners'>('courses')

  const groupedLearners = useMemo(() => {
    if (!assignment?.learners) return []

    const map = new Map<string, {
      learnerCode: string
      learnerName: string | null | undefined
      courses: Array<{
        courseCode: string | null | undefined
        courseTitle: string | null | undefined
        progress: number
        isCompleted: boolean
        status: string
      }>
    }>()

    assignment.learners.forEach(l => {
      let entry = map.get(l.learnerCode)
      if (!entry) {
        entry = {
          learnerCode: l.learnerCode,
          learnerName: l.learnerName,
          courses: []
        }
        map.set(l.learnerCode, entry)
      }
      if (l.courseCode || l.courseTitle) {
        entry.courses.push({
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
      toast.error('Failed to load assignment batch details')
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void loadAssignmentDetails()
  }, [loadAssignmentDetails])

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
      toast.error(err.message || 'Failed to extend due date')
    } finally {
      setExtendingDate(false)
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

  const handleImportCodes = () => {
    const parsedCodes = parseLearnerCodes(learnerCodesInput)
    if (parsedCodes.length === 0) {
      toast.error('Enter at least one EId code')
      return
    }

    const newSelections = parsedCodes.map(code => ({
      code,
      name: code, // fallback to code
      division: '',
      department: ''
    }))

    setPendingAddLearners(prev => {
      const existingCodes = new Set(prev.map(l => l.code))
      const currentCodes = new Set(assignment?.learners.map(m => m.learnerCode) || [])
      
      const uniqueNew = newSelections.filter(l => !existingCodes.has(l.code) && !currentCodes.has(l.code))
      const duplicateCount = parsedCodes.length - uniqueNew.length
      if (duplicateCount > 0) {
        toast.info(`${duplicateCount} code(s) were skipped (already selected or in the assignment)`)
      }
      return [...prev, ...uniqueNew]
    })
    setLearnerCodesInput('')
    toast.success(`Imported ${parsedCodes.length} learner code(s) to queue`)
  }

  // Add more learners to this existing batch
  const handleAddLearners = async () => {
    const codes = pendingAddLearners.map(l => l.code)
    if (codes.length === 0) {
      toast.error('Please select or import at least one learner')
      return
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
        setAddingLearners(false)
        setPendingAddLearners([])
        setLearnerCodesInput('')
        loadAssignmentDetails()
      }
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || 'Failed to add learners')
    } finally {
      setSavingLearners(false)
    }
  }

  // Reset progress attempt
  const handleResetLearner = async (learnerCode: string) => {
    if (!(await confirm({
      title: 'Reset Progress',
      message: `Reset progress attempt for learner ${learnerCode}? This will clear test history.`,
      confirmLabel: 'Reset',
    }))) return
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message: string }>(`Assignments/${id}/reset-enrollments`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ learnerCodes: [learnerCode] })
      })
      if (resp.success) {
        toast.success(resp.message)
        loadAssignmentDetails()
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to reset progress attempt')
    }
  }

  // Remove learner from assignment
  const handleRemoveLearner = async (learnerCode: string) => {
    if (!(await confirm({
      title: 'Remove Learner',
      message: `Remove learner ${learnerCode} from this assignment? Enrollment will be deleted.`,
      confirmLabel: 'Remove',
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
      toast.error('Failed to delete learner enrollment')
    }
  }

  // Remove Course rule from batch
  const handleRemoveCourse = async (ruleId: number) => {
    if (!(await confirm({
      title: 'Delete Course Rule',
      message: 'Delete this course rule? All linked learner enrollments will be deleted.',
      confirmLabel: 'Delete',
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
      toast.error('Unable to delete assigned course')
    }
  }

  // Delete entire assignment batch
  const handleDeleteBatch = async () => {
    if (!(await confirm({
      title: 'Delete Assignment Batch',
      message: 'Delete this entire assignment batch? This deletes ALL course rules and linked user records.',
      confirmLabel: 'Delete Batch',
      danger: true,
    }))) return
    try {
      await fetchWithAccessControl(`Assignments/${id}`, {
        method: 'DELETE'
      })
      toast.success('Assignment batch deleted successfully')
      navigate('/assignments')
    } catch (err) {
      console.error(err)
      toast.error('Failed to delete assignment rules')
    }
  }

  if (loading) {
    return <LoadingState />
  }

  if (!assignment) {
    return (
      <NotFoundState
        title="Assignment Batch Not Found"
        message="The requested operational batch identity could not be verified."
        backTo="/assignments"
        backLabel="Back to registry"
      />
    )
  }

  const assignmentStatus = deriveAssignmentStatus(assignment)
  const detailTabs: Array<{ key: 'courses' | 'learners'; label: string }> = [
    { key: 'courses', label: 'Courses' },
    { key: 'learners', label: 'Learners' },
  ]

  return (
    <>
      <DetailLayout
        sidebar={
          <ControlsSidebar>
            <ControlAction to={`/assignments/${id}/report`} icon={FileBarChart}>Open Report</ControlAction>
            <ControlAction icon={UserPlus} onClick={() => setAddingLearners(true)}>Add More Learners</ControlAction>
            <ControlAction icon={CalendarClock} onClick={() => setShowDueDateModal(true)}>Extend Due Date</ControlAction>
            <ControlAction icon={Trash2} onClick={handleDeleteBatch} variant="danger">Delete Batch</ControlAction>
          </ControlsSidebar>
        }
      >
        <main className="space-y-6">
          <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            <SectionHeader icon={FileBarChart} variant="card">Overview</SectionHeader>

            <div className="p-5">
              <FactGrid className="pt-2">
                <Fact label="Learners" valueClassName="font-bold text-slate-800">
                  {assignment.totalEmployees}
                </Fact>
                <Fact label="Completed" valueClassName="font-bold text-slate-800">
                  {assignment.chartData.completed}
                </Fact>
                <Fact label="Completion Rate" valueClassName="font-bold text-slate-800">
                  {Math.round(assignment.completionRate)}%
                </Fact>
                <Fact label="Status">
                  <StatusText
                    tone={
                      assignmentStatus === 'Completed'
                        ? 'success'
                        : assignmentStatus === 'Upcoming'
                        ? 'warning'
                        : assignmentStatus === 'Overdue'
                        ? 'danger'
                        : 'neutral'
                    }
                  >
                    {assignmentStatus}
                  </StatusText>
                </Fact>
                <Fact label="Start Date" valueClassName="font-semibold">
                  {formatDate(assignment.startDate)}
                </Fact>
                <Fact label="Due Date" valueClassName="font-semibold">
                  {formatDate(assignment.dueDate)}
                </Fact>
                {assignment.learnerGroupName && (
                  <Fact label="Learner Group" colSpan="full" valueClassName="font-semibold">
                    {assignment.learnerGroupName}
                  </Fact>
                )}
              </FactGrid>
            </div>
          </section>

          <DetailTabs
            tabs={detailTabs}
            active={activeDetailTab}
            onChange={setActiveDetailTab}
          />

          {activeDetailTab === 'courses' && (
            <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
              <SectionHeader icon={BookOpen} variant="card">Courses</SectionHeader>

              <ul className="divide-y divide-slate-100 px-4">
                {assignment.courses.map((c) => (
                  <li key={c.assignmentRuleId} className="py-2.5 flex items-center justify-between">
                    <div className="flex flex-col">
                      <span className={`text-sm font-bold ${c.isCourseDeleted ? 'text-slate-400 line-through' : 'text-slate-800'}`}>
                        {c.courseTitle}
                        {c.isCourseDeleted && <span className="ml-1.5 text-xxs font-semibold no-underline">(deleted)</span>}
                      </span>
                      <span className="text-xxs font-mono text-slate-400 mt-0.5">{c.courseCode}</span>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="text-xxs font-bold text-slate-500">
                        {c.completedLearners} / {c.totalLearners} completed
                      </span>
                      <button
                        onClick={() => handleRemoveCourse(c.assignmentRuleId)}
                        className="p-1 text-red-500 hover:bg-rose-50 rounded-md transition cursor-pointer"
                        title="Remove course from assignment"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {activeDetailTab === 'learners' && (
            <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
              <SectionHeader icon={Users} variant="card">Learners</SectionHeader>
 
              <div className="overflow-x-auto max-h-105 custom-scrollbar">
                <table className="w-full text-left text-sm border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none">
                      <th className="p-3">Learner</th>
                      <th className="p-3">Assigned Courses & Progress</th>
                      <th className="p-3">Summary</th>
                      <th className="p-3 text-center">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-slate-700">
                    {groupedLearners.map((l) => {
                      const completedCount = l.courses.filter(c => c.isCompleted).length
                      const totalCount = l.courses.length
                      const allCompleted = totalCount > 0 && completedCount === totalCount

                      return (
                        <tr key={l.learnerCode} className="hover:bg-slate-50/60 transition">
                          <td className="p-3 align-top">
                            <div className="flex flex-col">
                              <span className="font-bold text-slate-800 leading-tight">{l.learnerName || l.learnerCode}</span>
                              <span className="text-xxs font-mono text-slate-400 mt-0.5">{l.learnerCode}</span>
                            </div>
                          </td>
                          <td className="p-3">
                            <div className="flex flex-col gap-3">
                              {l.courses.length === 0 ? (
                                <span className="text-slate-400 text-xs italic">No courses assigned</span>
                              ) : (
                                l.courses.map((c) => (
                                  <div key={c.courseCode || ''} className="flex items-center justify-between gap-6 border-b border-slate-100/50 last:border-0 pb-1.5 last:pb-0">
                                    <div className="flex flex-col min-w-0 flex-1">
                                      <span className="font-semibold text-slate-700 text-xs truncate" title={c.courseTitle || ''}>
                                        {c.courseTitle}
                                      </span>
                                      <span className="font-mono text-slate-400 text-xxs mt-0.5">{c.courseCode}</span>
                                    </div>
                                    <div className="flex items-center gap-3 shrink-0">
                                      <ProgressBar value={c.progress} completed={c.isCompleted} maxWidthClass="max-w-16" />
                                      <StatusBadge size="xxs">{c.status}</StatusBadge>
                                    </div>
                                  </div>
                                ))
                              )}
                            </div>
                          </td>
                          <td className="p-3 align-top">
                            <div className="flex flex-col gap-1">
                              <span className="text-xs font-bold text-slate-700">
                                {completedCount} / {totalCount} Completed
                              </span>
                              <span className={`text-xxs font-extrabold w-max px-1.5 py-0.5 rounded ${
                                allCompleted ? 'bg-emerald-50 text-emerald-700 border border-emerald-100' : 'bg-slate-50 text-slate-500 border border-slate-150'
                              }`}>
                                {allCompleted ? 'Completed' : 'In Progress'}
                              </span>
                            </div>
                          </td>
                          <td className="p-3 text-center align-top">
                            <div className="inline-flex items-center gap-1.5">
                              <button
                                onClick={() => handleResetLearner(l.learnerCode)}
                                className="p-1 text-indigo-500 hover:bg-indigo-50 rounded-md transition cursor-pointer"
                                title="Reset attempts"
                              >
                                <RotateCcw className="h-3.5 w-3.5" />
                              </button>
                              <button
                                onClick={() => handleRemoveLearner(l.learnerCode)}
                                className="p-1 text-red-500 hover:bg-rose-50 rounded-md transition cursor-pointer"
                                title="Remove learner"
                              >
                                <Trash2 className="h-3.5 w-3.5" />
                              </button>
                            </div>
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            </section>
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
                <h3 className="text-base font-extrabold text-slate-800 uppercase tracking-wide">Extend Due Date</h3>
              </div>
              <button onClick={() => setShowDueDateModal(false)} className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer">
                <X className="h-5 w-5" />
              </button>
            </div>

            <div className="px-6 py-5 space-y-4">
              <div className="flex items-center gap-3 text-sm text-slate-600">
                <span className="text-slate-400 font-semibold uppercase text-xs">Current Due Date:</span>
                <span className="font-bold text-slate-800">{formatDate(assignment.dueDate)}</span>
              </div>

              <div className="space-y-1.5">
                <label htmlFor="newDue" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">New Due Date</label>
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
              <button
                type="button"
                onClick={() => setShowDueDateModal(false)}
                className="px-4 py-2 text-sm font-bold text-slate-500 hover:text-slate-700 rounded-lg hover:bg-slate-100 transition cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={extendingDate || !newDueDateInput}
                onClick={async () => {
                  await handleExtendDueDate()
                  setShowDueDateModal(false)
                }}
                className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-sm font-bold transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1.5 cursor-pointer shadow-xs"
              >
                {extendingDate ? 'Extending...' : 'Confirm'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Add Learners Modal */}
      {addingLearners && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center z-50 p-4 transition-all animate-fade-in" onClick={() => { setAddingLearners(false); setPendingAddLearners([]); setLearnerCodesInput(''); }}>
          <div className="bg-white border border-slate-200 rounded-xl shadow-2xl w-full max-w-5xl h-[85vh] flex flex-col p-6 gap-4 animate-scale-up" onClick={(e) => e.stopPropagation()}>
            
            {/* Modal Header */}
            <div className="flex items-center justify-between border-b border-slate-200/60 pb-3 shrink-0 select-none">
              <div className="flex items-center gap-2">
                <UserPlus className="h-5 w-5 text-indigo-500" />
                <h2 className="font-extrabold text-slate-800 text-sm uppercase tracking-wider">Add More Learners</h2>
              </div>
              
              <div className="flex items-center gap-4 bg-slate-50 p-1.5 rounded border border-slate-100">
                <button
                  type="button"
                  onClick={() => setMemberAddTab('picker')}
                  className={`px-3 py-1 text-center text-xs font-bold rounded transition cursor-pointer ${
                    memberAddTab === 'picker' ? 'bg-white text-blue-700 shadow-xs' : 'text-slate-500 hover:text-slate-800'
                  }`}
                >
                  Directory Search
                </button>
                <button
                  type="button"
                  onClick={() => setMemberAddTab('bulk')}
                  className={`px-3 py-1 text-center text-xs font-bold rounded transition cursor-pointer ${
                    memberAddTab === 'bulk' ? 'bg-white text-blue-700 shadow-xs' : 'text-slate-500 hover:text-slate-800'
                  }`}
                >
                  Bulk Import (EIds)
                </button>
              </div>

              <button
                onClick={() => { setAddingLearners(false); setPendingAddLearners([]); setLearnerCodesInput(''); }}
                className="text-slate-400 hover:text-slate-600 rounded-full hover:bg-slate-50 p-1 transition cursor-pointer"
                title="Close"
              >
                <X className="h-5 w-5" />
              </button>
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
                    Bulk import employee EIds separated by commas, spaces, or new lines. Duplicate or current assignment codes will be skipped automatically.
                  </p>
                  <div className="grid grid-cols-1 gap-3 md:grid-cols-[minmax(0,1fr)_auto] shrink-0">
                    <textarea
                      id="learnerCodes"
                      rows={5}
                      value={learnerCodesInput}
                      onChange={(e) => setLearnerCodesInput(e.target.value)}
                      placeholder="Paste employee EIds here (e.g. N130812, N142715)..."
                      className="w-full px-3 py-2 border border-slate-200 rounded text-sm font-mono text-slate-850 focus:outline-none focus:border-indigo-500 bg-slate-50/50"
                    />
                    <AppButton
                      type="button"
                      variant="primary"
                      icon={Plus}
                      onClick={handleImportCodes}
                      disabled={!learnerCodesInput.trim()}
                      className="self-start"
                    >
                      Add to Queue
                    </AppButton>
                  </div>

                  {/* Queued codes view */}
                  <div className="border border-slate-200 rounded-lg overflow-hidden flex flex-col flex-1 min-h-0">
                    <div className="bg-slate-50 px-4 py-2 border-b border-slate-200 flex justify-between items-center text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none shrink-0">
                      <span>Queued for Assignment Additions ({pendingAddLearners.length})</span>
                      {pendingAddLearners.length > 0 && (
                        <button
                          type="button"
                          onClick={() => setPendingAddLearners([])}
                          className="text-red-500 hover:text-red-700 font-bold cursor-pointer"
                        >
                          Clear Queue
                        </button>
                      )}
                    </div>
                    <div className="flex-1 overflow-y-auto custom-scrollbar divide-y divide-slate-100 bg-white min-h-0">
                      {pendingAddLearners.length === 0 ? (
                        <div className="text-center py-12 text-slate-400 text-xs font-semibold">Queue is empty. Paste codes and click Add to Queue.</div>
                      ) : (
                        pendingAddLearners.map((l, idx) => (
                          <div key={l.code} className="px-4 py-2.5 flex justify-between items-center text-xs font-medium">
                            <div className="flex items-center gap-4">
                              <span className="font-bold text-slate-400 w-8">{idx + 1}</span>
                              <span className="font-mono text-slate-850 font-semibold">{l.code}</span>
                              {l.name !== l.code && <span className="text-slate-500 text-xxs">({l.name})</span>}
                            </div>
                            <button
                              type="button"
                              onClick={() => setPendingAddLearners(prev => prev.filter(x => x.code !== l.code))}
                              className="text-red-500 hover:text-red-700 font-bold text-xxs cursor-pointer"
                            >
                              Remove
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
              <button
                onClick={() => { setAddingLearners(false); setPendingAddLearners([]); setLearnerCodesInput(''); }}
                className="px-4 py-2 border border-slate-200 hover:bg-slate-50 text-slate-600 rounded text-xs font-bold transition cursor-pointer"
              >
                Cancel
              </button>
              <button
                onClick={handleAddLearners}
                disabled={savingLearners || pendingAddLearners.length === 0}
                className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded text-xs font-bold transition disabled:opacity-55 cursor-pointer shadow-xs flex items-center gap-1.5"
              >
                {savingLearners ? 'Saving...' : 'Add Learners'}
              </button>
            </div>
          </div>
        </div>
      )}

      {confirmDialog}
    </>
  )
}

