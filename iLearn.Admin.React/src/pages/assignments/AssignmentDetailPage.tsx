import { useState, useEffect } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { 
  ArrowLeft, 
  Users, 
  BookOpen, 
  AlertTriangle,
  RefreshCw,
  Settings,
  Trash2,
  UserPlus,
  FileBarChart,
  CalendarClock,
  X
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'

type AssignmentDetail = {
  id: number
  assignmentNo: string
  description: string
  startDate: string
  dueDate: string
  status: string
  completedEnrollmentCount: number
  totalEnrollmentCount: number
  completionPct: number
  courseNames: string
  courses: Array<{
    id: number
    ruleId: number
    title: string
    code: string
  }>
  learners: Array<{
    id: number
    learnerCode: string
    learnerName: string
    division?: string
    department?: string
    progress: number
    isCompleted: boolean
    completedDate: string | null
    status: string
  }>
}

export function AssignmentDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { setLabel } = useBreadcrumbs()

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
    if (!window.confirm(`Reset progress attempt for learner ${learnerCode}? This will clear test history.`)) return
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
    if (!window.confirm(`Remove learner ${learnerCode} from this assignment? Enrollment will be deleted.`)) return
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
    if (!window.confirm('Delete this course rule? All linked learner enrollments will be deleted.')) return
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
    if (!window.confirm('Delete this entire assignment batch? This deletes ALL course rules and linked user records.')) return
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
    return (
      <div className="flex h-96 items-center justify-center">
        <RefreshCw className="h-8 w-8 animate-spin text-indigo-500" />
      </div>
    )
  }

  if (!assignment) {
    return (
      <div className="text-center py-12">
        <AlertTriangle className="h-12 w-12 text-amber-500 mx-auto" />
        <h2 className="text-lg font-bold text-slate-700 mt-4">Assignment Batch Not Found</h2>
        <p className="text-slate-400 mt-2">The requested operational batch identity could not be verified.</p>
        <Link to="/assignments" className="mt-6 inline-flex items-center text-indigo-500 font-semibold hover:underline">
          <ArrowLeft className="h-4 w-4 mr-1" /> Back to registry
        </Link>
      </div>
    )
  }

  return (
    <>
      <header className="mb-3">
        <div className="text-xxs font-extrabold uppercase text-slate-400">Assignment</div>
        <h1 className="text-2xl font-extrabold text-slate-900">{assignment.assignmentNo}</h1>
      </header>

      {/* KPI Cards Strip */}
      <div className="grid auto-cols-fr grid-flow-col gap-0 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs mb-6">
        <div className="min-w-0 border-r border-slate-200 p-4 last:border-r-0">
          <span className="block text-[11px] font-extrabold uppercase text-slate-400">Learners</span>
          <span className="block mt-1 text-[22px] font-extrabold leading-tight text-indigo-600">{assignment.totalEnrollmentCount}</span>
        </div>

        <div className="min-w-0 border-r border-slate-200 p-4 last:border-r-0">
          <span className="block text-[11px] font-extrabold uppercase text-slate-400">Completed</span>
          <span className="block mt-1 text-[22px] font-extrabold leading-tight text-indigo-600">{assignment.completedEnrollmentCount}</span>
        </div>

        <div className="min-w-0 border-r border-slate-200 p-4 last:border-r-0">
          <span className="block text-[11px] font-extrabold uppercase text-slate-400">Completion</span>
          <span className="block mt-1 text-[22px] font-extrabold leading-tight text-indigo-600">{assignment.completionPct}%</span>
        </div>

        <div className="min-w-0 border-r border-slate-200 p-4 last:border-r-0">
          <span className="block text-[11px] font-extrabold uppercase text-slate-400">Status</span>
          <span className="mt-1 block">
            <span className={`inline-flex px-2 py-0.5 rounded text-xxs font-bold ${
              assignment.status === 'Completed' ? 'bg-emerald-100 text-emerald-800'
                : assignment.status === 'InProgress' || assignment.status === 'Active' ? 'bg-blue-100 text-blue-800'
                : 'bg-slate-100 text-slate-700'
            }`}>{assignment.status}</span>
          </span>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start">
        
        {/* Main Left Side Panels */}
        <div className="space-y-8 min-w-0">
          
          {/* Linked courses */}
          <section>
            <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3">
              <h2 className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600"><BookOpen aria-hidden="true" />Courses</h2>
            </div>
            
            <ul className="divide-y divide-slate-100">
              {assignment.courses.map(c => (
                <li key={c.id} className="py-2.5 flex items-center justify-between">
                  <div className="flex flex-col">
                    <span className="text-sm font-bold text-slate-800">{c.title}</span>
                    <span className="text-xxs font-mono text-slate-400 mt-0.5">{c.code}</span>
                  </div>
                  <button
                    onClick={() => handleRemoveCourse(c.ruleId)}
                    className="p-1 text-slate-400 hover:text-red-600 rounded transition"
                    title="Remove course from assignment"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </li>
              ))}
            </ul>
          </section>

          {/* Active Registered Learners grid */}
          <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
            <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3 p-4">
              <h2 className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600"><Users aria-hidden="true" />Learners</h2>
            </div>

            <div className="overflow-x-auto max-h-105 custom-scrollbar">
              <table className="w-full text-left text-sm border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                    <th className="p-3">Learner</th>
                    <th className="p-3">Department</th>
                    <th className="p-3">Progress</th>
                    <th className="p-3">Status</th>
                    <th className="p-3 text-center">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {assignment.learners.map(l => (
                    <tr key={l.id} className="hover:bg-slate-50/60 transition">
                      <td className="p-3">
                        <div className="flex flex-col">
                          <span className="font-bold text-slate-800 leading-tight">{l.learnerName}</span>
                          <span className="text-xxs font-mono text-slate-400 mt-0.5">{l.learnerCode}</span>
                        </div>
                      </td>
                      <td className="p-3 text-slate-500 text-xxs">{l.department || '-'}</td>
                      <td className="p-3">
                        <div className="flex items-center gap-2 max-w-20">
                          <div className="w-full bg-slate-100 rounded-full h-1.5">
                            <div 
                              className={`h-1.5 rounded-full ${l.isCompleted ? 'bg-emerald-500' : 'bg-blue-600'}`} 
                              style={{ width: `${l.progress}%` }}
                            ></div>
                          </div>
                          <span className="font-bold text-xxs text-slate-500 shrink-0">{Math.round(l.progress)}%</span>
                        </div>
                      </td>
                      <td className="p-3">
                        <span className={`inline-flex px-2 py-0.5 rounded text-xxs font-bold ${
                          l.status === 'Completed' ? 'bg-emerald-100 text-emerald-800'
                            : l.status === 'In Progress' ? 'bg-blue-100 text-blue-800'
                            : 'bg-slate-100 text-slate-700'
                        }`}>{l.status}</span>
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

        {/* Right Sidebar controls */}
        <aside className="lg:sticky lg:top-5 rounded-lg border border-slate-200 bg-white p-4 space-y-2">
          <div className="flex items-center gap-2 pb-2 mb-1 border-b border-slate-200">
            <Settings className="h-4 w-4 text-indigo-600" aria-hidden="true" />
            <h2 className="text-sm font-bold text-slate-800">Controls</h2>
          </div>

          <AssignCtrlLink to={`/assignments/${id}/report`} icon={FileBarChart}>Open Report</AssignCtrlLink>
          <AssignCtrlBtn icon={UserPlus} onClick={() => setAddingLearners(true)}>Add More Learners</AssignCtrlBtn>
          <AssignCtrlBtn icon={CalendarClock} onClick={() => setShowDueDateModal(true)}>Extend Due Date</AssignCtrlBtn>
          <AssignCtrlBtn icon={Trash2} onClick={handleDeleteBatch} variant="danger">Delete Batch</AssignCtrlBtn>

          {/* Schedule info */}
          <div className="pt-2 border-t border-slate-100">
            <dl className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <span className="text-slate-400 font-bold text-xs uppercase block">Start Date</span>
                <span className="block font-semibold text-slate-700 mt-1">{new Date(assignment.startDate).toLocaleDateString()}</span>
              </div>
              <div>
                <span className="text-slate-400 font-bold text-xs uppercase block">Due Date</span>
                <span className="block font-semibold text-slate-700 mt-1">{new Date(assignment.dueDate).toLocaleDateString()}</span>
              </div>
            </dl>
          </div>

          <div className="pt-2 border-t border-slate-100">
            <Link to="/assignments" className="w-full flex items-center justify-center gap-1.5 text-slate-400 hover:text-slate-700 transition font-semibold text-xs py-1.5">
              <ArrowLeft className="h-3.5 w-3.5" />
              <span>Back to Assignments</span>
            </Link>
          </div>
        </aside>

      </div>

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
                <span className="font-bold text-slate-800">{new Date(assignment.dueDate).toLocaleDateString()}</span>
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

    </>
  )
}

/* ── Uniform control buttons ── */

function AssignCtrlLink({ to, icon: Icon, children }: {
  to: string; icon: React.ComponentType<{ className?: string }>; children: React.ReactNode
}) {
  return (
    <Link
      to={to}
      className="group w-full flex items-center gap-2.5 rounded-md border border-slate-200 bg-white p-2 text-slate-700 hover:border-indigo-300 hover:bg-indigo-50/40 transition cursor-pointer text-left"
    >
      <div className="h-7 w-7 rounded bg-slate-100 group-hover:bg-indigo-100 flex items-center justify-center shrink-0 text-slate-500 group-hover:text-indigo-600 transition-colors">
        <Icon className="h-3.5 w-3.5 shrink-0" />
      </div>
      <span className="text-[13px] font-bold text-slate-800 group-hover:text-indigo-800 transition-colors">{children}</span>
    </Link>
  )
}

function AssignCtrlBtn({ icon: Icon, children, disabled, onClick, variant = 'default' }: {
  icon: React.ComponentType<{ className?: string }>; children: React.ReactNode; disabled?: boolean; onClick: () => void; variant?: 'default' | 'danger'
}) {
  if (disabled) {
    return (
      <button
        type="button"
        disabled
        className="w-full flex items-center gap-2.5 rounded-md border border-slate-100 bg-slate-50 p-2 text-slate-300 cursor-not-allowed text-left focus:outline-none"
      >
        <div className="h-7 w-7 rounded bg-slate-100/50 flex items-center justify-center shrink-0 text-slate-300">
          <Icon className="h-3.5 w-3.5 shrink-0" />
        </div>
        <span className="text-[13px] font-bold">{children}</span>
      </button>
    )
  }

  if (variant === 'danger') {
    return (
      <button
        type="button"
        onClick={onClick}
        className="group w-full flex items-center gap-2.5 rounded-md border border-red-200 bg-white p-2 text-red-600 hover:border-red-300 hover:bg-red-50/50 transition cursor-pointer text-left"
      >
        <div className="h-7 w-7 rounded bg-red-50 group-hover:bg-red-100 flex items-center justify-center shrink-0 text-red-500 group-hover:text-red-600 transition-colors">
          <Icon className="h-3.5 w-3.5 shrink-0" />
        </div>
        <span className="text-[13px] font-bold text-red-700 group-hover:text-red-800 transition-colors">{children}</span>
      </button>
    )
  }

  return (
    <button
      type="button"
      onClick={onClick}
      className="group w-full flex items-center gap-2.5 rounded-md border border-slate-200 bg-white p-2 text-slate-700 hover:border-indigo-300 hover:bg-indigo-50/40 transition cursor-pointer text-left"
    >
      <div className="h-7 w-7 rounded bg-slate-100 group-hover:bg-indigo-100 flex items-center justify-center shrink-0 text-slate-500 group-hover:text-indigo-600 transition-colors">
        <Icon className="h-3.5 w-3.5 shrink-0" />
      </div>
      <span className="text-[13px] font-bold text-slate-800 group-hover:text-indigo-800 transition-colors">{children}</span>
    </button>
  )
}
