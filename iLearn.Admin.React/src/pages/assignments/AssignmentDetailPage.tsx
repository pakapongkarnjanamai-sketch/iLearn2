import { useState, useEffect } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { 
  ArrowLeft, 
  Calendar, 
  Users, 
  BookOpen, 
  AlertTriangle,
  RefreshCw,
  Trash2,
  UserPlus,
  FileBarChart
} from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { AppButton } from '../../components/ui/AppButton'
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
      <PageHeader
        actions={
          <div className="flex items-center gap-2">
            <Link to={`/assignments/${id}/report`}>
              <AppButton variant="ghost" icon={FileBarChart}>
                Open Report
              </AppButton>
            </Link>
            <AppButton variant="secondary" icon={UserPlus} onClick={() => setAddingLearners(true)}>
              Add More Learners
            </AppButton>
            <button
              onClick={handleDeleteBatch}
              className="inline-flex items-center gap-1.5 px-3 py-2 bg-red-50 text-red-600 border border-red-200 hover:bg-red-100 rounded text-xs font-bold transition shadow-xs"
            >
              <Trash2 className="h-4 w-4" />
              <span>Delete Batch</span>
            </button>
          </div>
        }
      />

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

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
        
        {/* Main Left Side Panels */}
        <div className="lg:col-span-2 space-y-8">
          
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
        <aside className="space-y-8">
          
          {/* Add Learners Dialog overlay */}
          {addingLearners && (
            <section className="space-y-4 border-l-2 border-blue-500 p-4">
              <div className="flex items-center justify-between border-b border-slate-200/60 pb-2">
                <h2 className="font-extrabold text-slate-700 text-sm uppercase">Add Cohort Learners</h2>
                <button onClick={() => setAddingLearners(false)} className="text-slate-400 hover:text-slate-600 text-xs font-semibold">Close</button>
              </div>

              <div className="space-y-4">
                <div className="space-y-1.5">
                  <label htmlFor="newCodes" className="block text-xxs font-extrabold text-slate-400 uppercase">Employee NID Codes</label>
                  <textarea
                    id="newCodes"
                    rows={4}
                    value={newLearnersInput}
                    onChange={(e) => setNewLearnersInput(e.target.value)}
                    placeholder="Enter codes separated by comma or newlines..."
                    className="w-full px-3 py-2 border border-slate-200 rounded text-sm font-mono focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 bg-slate-50/50"
                  />
                </div>
                
                <div className="flex gap-2">
                  <button onClick={() => setAddingLearners(false)} className="flex-1 py-1.5 text-center border border-slate-200 rounded text-xs font-semibold text-slate-600 hover:bg-slate-50 transition">Cancel</button>
                  <button onClick={handleAddLearners} disabled={savingLearners || !newLearnersInput.trim()} className="flex-1 py-1.5 text-center bg-blue-600 hover:bg-blue-700 text-white rounded text-xs font-bold disabled:opacity-55 shadow-xs transition">
                    {savingLearners ? 'Saving...' : 'Add Learners'}
                  </button>
                </div>
              </div>
            </section>
          )}

          {/* Schedule extend controls */}
          <section className="space-y-4">
            <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3">
              <h2 className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600"><Calendar aria-hidden="true" />Schedule</h2>
            </div>
            
            <dl className="grid grid-cols-2 gap-3 text-sm py-1.5">
              <div className="border-b border-slate-100/50 pb-2">
                <span className="text-slate-400 font-extrabold text-xxs uppercase block">Start Date</span>
                <span className="block font-semibold text-slate-700 mt-1">{new Date(assignment.startDate).toLocaleDateString()}</span>
              </div>
              <div className="border-b border-slate-100/50 pb-2">
                <span className="text-slate-400 font-extrabold text-xxs uppercase block">Due Date</span>
                <span className="block font-semibold text-slate-700 mt-1">{new Date(assignment.dueDate).toLocaleDateString()}</span>
              </div>
            </dl>

            <div className="space-y-3 pt-2">
              <div className="space-y-1.5">
                <label htmlFor="newDue" className="block text-xxs font-extrabold text-slate-400 uppercase">Extend Due Date</label>
                <input
                  type="date"
                  id="newDue"
                  value={newDueDateInput}
                  onChange={(e) => setNewDueDateInput(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-200 rounded text-sm text-slate-800 bg-slate-50/50 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400"
                />
              </div>

              <button
                type="button"
                onClick={handleExtendDueDate}
                disabled={extendingDate}
                className="w-full text-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded text-xs font-bold transition disabled:opacity-55 shadow-xs"
              >
                {extendingDate ? 'Extending...' : 'Extend Due Date'}
              </button>
            </div>
          </section>

        </aside>

      </div>
    </>
  )
}
