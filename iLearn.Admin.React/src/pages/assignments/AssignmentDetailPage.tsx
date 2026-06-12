import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Users,
  BookOpen,
  Trash2,
  UserPlus,
  FileBarChart,
  CalendarClock,
  X
} from 'lucide-react'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { DetailLayout, DetailPageHeader, Fact, FactGrid } from '../../components/ui/detail'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { ControlsSidebar, ControlsDivider, ControlAction } from '../../components/ui/ControlsSidebar'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { formatDate } from '../../lib/format'

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
  const [newLearnersInput, setNewLearnersInput] = useState('')
  const [savingLearners, setSavingLearners] = useState(false)

  const loadAssignmentDetails = async () => {
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
  }

  useEffect(() => {
    loadAssignmentDetails()
  }, [id])

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

  // Add more learners to this existing batch
  const handleAddLearners = async () => {
    const codes = newLearnersInput.split(/[\n,]+/).map(c => c.trim()).filter(c => c.length > 0)
    if (codes.length === 0) {
      toast.error('Please input at least one employee NID code')
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
        setNewLearnersInput('')
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

  return (
    <>
      <DetailPageHeader
        eyebrow="Assignments"
        title={assignment.assignmentNo}
        meta={<StatusBadge size="xxs">{assignmentStatus}</StatusBadge>}
      />

      {/* KPI Cards Strip */}
      <div className="grid auto-cols-fr grid-flow-col gap-0 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs mb-6">
        <div className="min-w-0 border-r border-slate-200 p-4 last:border-r-0">
          <span className="block text-[11px] font-extrabold uppercase text-slate-400">Learners</span>
          <span className="block mt-1 text-[22px] font-extrabold leading-tight text-indigo-600">{assignment.totalEmployees}</span>
        </div>

        <div className="min-w-0 border-r border-slate-200 p-4 last:border-r-0">
          <span className="block text-[11px] font-extrabold uppercase text-slate-400">Completed</span>
          <span className="block mt-1 text-[22px] font-extrabold leading-tight text-indigo-600">{assignment.chartData.completed}</span>
        </div>

        <div className="min-w-0 border-r border-slate-200 p-4 last:border-r-0">
          <span className="block text-[11px] font-extrabold uppercase text-slate-400">Completion</span>
          <span className="block mt-1 text-[22px] font-extrabold leading-tight text-indigo-600">{Math.round(assignment.completionRate)}%</span>
        </div>

        <div className="min-w-0 border-r border-slate-200 p-4 last:border-r-0">
          <span className="block text-[11px] font-extrabold uppercase text-slate-400">Status</span>
          <span className="mt-1 block">
            <StatusBadge size="xxs">{assignmentStatus}</StatusBadge>
          </span>
        </div>
      </div>

      <DetailLayout
        sidebar={
          <ControlsSidebar backTo="/assignments" backLabel="Back to Assignments">
            <ControlAction to={`/assignments/${id}/report`} icon={FileBarChart}>Open Report</ControlAction>
            <ControlAction icon={UserPlus} onClick={() => setAddingLearners(true)}>Add More Learners</ControlAction>
            <ControlAction icon={CalendarClock} onClick={() => setShowDueDateModal(true)}>Extend Due Date</ControlAction>
            <ControlAction icon={Trash2} onClick={handleDeleteBatch} variant="danger">Delete Batch</ControlAction>

            {/* Schedule info */}
            <ControlsDivider>
              <FactGrid cols={2} className="gap-3 text-sm">
                <Fact
                  label="Start Date"
                  labelClassName="text-slate-400 font-bold text-xs uppercase"
                  valueClassName="font-semibold"
                >
                  {formatDate(assignment.startDate)}
                </Fact>
                <Fact
                  label="Due Date"
                  labelClassName="text-slate-400 font-bold text-xs uppercase"
                  valueClassName="font-semibold"
                >
                  {formatDate(assignment.dueDate)}
                </Fact>
                {assignment.learnerGroupName && (
                  <Fact
                    label="Learner Group"
                    colSpan="full"
                    labelClassName="text-slate-400 font-bold text-xs uppercase"
                    valueClassName="font-semibold"
                  >
                    {assignment.learnerGroupName}
                  </Fact>
                )}
              </FactGrid>
            </ControlsDivider>
          </ControlsSidebar>
        }
      >
        
        {/* Main Left Side Panels */}
        <div className="space-y-8">
          
          {/* Linked courses */}
          <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            <SectionHeader icon={BookOpen} variant="card">Courses</SectionHeader>

            <ul className="divide-y divide-slate-100 px-4">
              {assignment.courses.map(c => (
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
                      className="p-1 text-slate-400 hover:text-red-600 rounded transition"
                      title="Remove course from assignment"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          </section>

          {/* Active Registered Learners grid */}
          <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            <SectionHeader icon={Users} variant="card">Learners</SectionHeader>

            <div className="overflow-x-auto max-h-105 custom-scrollbar">
              <table className="w-full text-left text-sm border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                    <th className="p-3">Learner</th>
                    <th className="p-3">Course</th>
                    <th className="p-3">Progress</th>
                    <th className="p-3">Status</th>
                    <th className="p-3 text-center">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {assignment.learners.map(l => (
                    <tr key={`${l.learnerCode}-${l.assignmentRuleId ?? 'x'}`} className="hover:bg-slate-50/60 transition">
                      <td className="p-3">
                        <div className="flex flex-col">
                          <span className="font-bold text-slate-800 leading-tight">{l.learnerName || l.learnerCode}</span>
                          <span className="text-xxs font-mono text-slate-400 mt-0.5">{l.learnerCode}</span>
                        </div>
                      </td>
                      <td className="p-3 text-slate-500 text-xxs">
                        {l.courseTitle ? (
                          <div className="flex flex-col">
                            <span className="font-semibold text-slate-600">{l.courseTitle}</span>
                            <span className="font-mono text-slate-400 mt-0.5">{l.courseCode}</span>
                          </div>
                        ) : '-'}
                      </td>
                      <td className="p-3">
                        <ProgressBar value={l.progress} completed={l.isCompleted} maxWidthClass="max-w-20" />
                      </td>
                      <td className="p-3">
                        <StatusBadge size="xxs">{l.status}</StatusBadge>
                      </td>
                      <td className="p-3 text-center">
                        <div className="inline-flex items-center gap-1.5">
                          <button
                            onClick={() => handleResetLearner(l.learnerCode)}
                            className="px-2 py-1 bg-slate-50 text-slate-600 border border-slate-200 rounded text-xxs font-semibold hover:bg-slate-100 transition"
                            title="Reset attempts"
                          >
                            Reset
                          </button>
                          <button
                            onClick={() => handleRemoveLearner(l.learnerCode)}
                            className="p-1 text-slate-400 hover:text-red-600 rounded transition"
                            title="Remove learner"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

        </div>

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
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-fade-in" onClick={() => setAddingLearners(false)}>
          <div className="bg-white border border-slate-100 rounded-xl shadow-2xl w-full max-w-md overflow-hidden flex flex-col animate-scale-up duration-200" onClick={(e) => e.stopPropagation()}>
            
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <UserPlus className="h-5 w-5 text-indigo-600" />
                <h3 className="text-base font-extrabold text-slate-800 uppercase tracking-wide">Add More Learners</h3>
              </div>
              <button onClick={() => setAddingLearners(false)} className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer">
                <X className="h-5 w-5" />
              </button>
            </div>

            <div className="px-6 py-5 space-y-3.5">
              <p className="text-xs font-medium text-slate-500 leading-relaxed">
                Bulk add employee EId codes (e.g., N130812, N142715) separated by commas, spaces, or new lines.
              </p>
              <div className="space-y-1.5">
                <label htmlFor="newCodes" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">Employee Codes</label>
                <textarea
                  id="newCodes"
                  rows={4}
                  value={newLearnersInput}
                  onChange={(e) => setNewLearnersInput(e.target.value)}
                  placeholder="Paste employee codes here..."
                  className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm font-mono text-slate-800 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 bg-slate-50/50 transition duration-150"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <button
                type="button"
                onClick={() => setAddingLearners(false)}
                className="px-4 py-2 text-sm font-bold text-slate-500 hover:text-slate-700 rounded-lg hover:bg-slate-100 transition cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={savingLearners || !newLearnersInput.trim()}
                onClick={async () => {
                  await handleAddLearners()
                }}
                className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-sm font-bold transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1.5 cursor-pointer shadow-xs"
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

