import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { 
  ArrowLeft, 
  Users, 
  Settings, 
  UserPlus, 
  UserMinus, 
  RefreshCw, 
  AlertTriangle,
  Layers
} from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { AppButton } from '../../components/ui/AppButton'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

type LearnerGroupMember = {
  id: number
  learnerCode: string
  learnerName: string
  division?: string
  department?: string
  section?: string
  position?: string
}

type LearnerGroupDetail = {
  id: number
  name: string
  description: string
  createdBy: string
  categoryId?: number
  categoryName?: string
  members: LearnerGroupMember[]
}

type PreviewAddResult = {
  groupId: number
  groupName: string
  selectedLearnerCount: number
  newMemberCount: number
  existingMemberCount: number
  selectedAssignmentCount: number
  estimatedEnrollmentCount: number
  learners: Array<{
    learnerCode: string
    learnerName: string
    division?: string
    department?: string
    isAlreadyMember: boolean
  }>
  assignments: Array<{
    id: number
    assignmentNo: string
    description: string
    courseNames: string
    status: string
    estimatedEnrollmentCount: number
  }>
}

type PreviewRemoveResult = {
  groupId: number
  groupName: string
  selectedMemberCount: number
  estimatedUnenrollmentCount: number
  members: Array<{
    memberId: number
    learnerCode: string
    learnerName: string
    division?: string
    department?: string
    currentAssignmentEnrollmentCount: number
  }>
}

export function GroupDetailPage() {
  const { id } = useParams()

  const [loading, setLoading] = useState(true)
  const [group, setGroup] = useState<LearnerGroupDetail | null>(null)
  
  // Member selection states (for removal)
  const [selectedMemberIds, setSelectedMemberIds] = useState<number[]>([])

  // Modal / Operations drawers
  const [managerMode, setManagerMode] = useState<'none' | 'add' | 'remove'>('none')
  
  // Bulk Add form state
  const [learnerCodesInput, setLearnerCodesInput] = useState('')
  const [enrollToAssignments, setEnrollToAssignments] = useState(true)
  const [addPreview, setAddPreview] = useState<PreviewAddResult | null>(null)
  const [loadingPreview, setLoadingPreview] = useState(false)

  // Bulk Remove form state
  const [unenrollFromAssignments, setUnenrollFromAssignments] = useState(true)
  const [removePreview, setRemovePreview] = useState<PreviewRemoveResult | null>(null)

  const loadGroupDetails = async () => {
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; data: LearnerGroupDetail }>(`LearnerGroups/${id}`)
      if (resp.success) {
        setGroup(resp.data)
        setSelectedMemberIds([])
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to load learner group details')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadGroupDetails()
  }, [id])

  // Bulk Add Preview Handler
  const handlePreviewAdd = async () => {
    const codes = learnerCodesInput.split(/[\n,]+/).map(c => c.trim()).filter(c => c.length > 0)
    if (codes.length === 0) {
      toast.error('Please input at least one NID employee code')
      return
    }

    setLoadingPreview(true)
    try {
      const previewData = await fetchWithAccessControl<any>(`LearnerGroups/${id}/members/preview`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          learnerCodes: codes,
          enrollToRelatedAssignments: enrollToAssignments,
          assignmentStatuses: ['Upcoming', 'InProgress'] // Default active windows
        })
      })
      if (previewData.success) {
        setAddPreview(previewData.data)
      }
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || 'Failed to analyze member addition impact')
    } finally {
      setLoadingPreview(false)
    }
  }

  // Bulk Add Commit
  const handleConfirmAdd = async () => {
    if (!addPreview) return
    try {
      const resp = await fetchWithAccessControl<any>(`LearnerGroups/${id}/members/confirm`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          learnerCodes: addPreview.learners.map(l => l.learnerCode),
          enrollToRelatedAssignments: enrollToAssignments,
          assignmentStatuses: ['Upcoming', 'InProgress']
        })
      })
      if (resp.success) {
        toast.success(resp.message || 'Group membership updated successfully!')
        setManagerMode('none')
        setLearnerCodesInput('')
        setAddPreview(null)
        loadGroupDetails()
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to save group members')
    }
  }

  // Single Delete member operation
  const handleRemoveSingleMember = async (memberId: number) => {
    if (!window.confirm('Remove this learner from group? Related assignments remain active.')) return
    try {
      await fetchWithAccessControl(`LearnerGroups/${id}/members/${memberId}`, {
        method: 'DELETE'
      })
      toast.success('Learner removed from group')
      loadGroupDetails()
    } catch (err) {
      console.error(err)
      toast.error('Unable to remove group member')
    }
  }

  // Bulk Remove Preview
  const handlePreviewRemove = async () => {
    if (selectedMemberIds.length === 0) {
      toast.error('Please select at least one member from the table to remove.')
      return
    }

    setLoadingPreview(true)
    try {
      const resp = await fetchWithAccessControl<any>(`LearnerGroups/${id}/members/remove/preview`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          memberIds: selectedMemberIds,
          unenrollFromRelatedAssignments: unenrollFromAssignments,
          assignmentStatuses: ['Upcoming', 'InProgress']
        })
      })
      if (resp.success) {
        setRemovePreview(resp.data)
        setManagerMode('remove')
      }
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || 'Failed to preview removal impact')
    } finally {
      setLoadingPreview(false)
    }
  }

  // Bulk Remove Commit
  const handleConfirmRemove = async () => {
    if (!removePreview) return
    try {
      const resp = await fetchWithAccessControl<any>(`LearnerGroups/${id}/members/remove/confirm`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          memberIds: selectedMemberIds,
          unenrollFromRelatedAssignments: unenrollFromAssignments,
          assignmentStatuses: ['Upcoming', 'InProgress']
        })
      })
      if (resp.success) {
        toast.success(resp.message || 'Selected group members removed successfully!')
        setManagerMode('none')
        setRemovePreview(null)
        loadGroupDetails()
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to commit members removal')
    }
  }

  const handleToggleSelectMember = (memberId: number) => {
    setSelectedMemberIds(prev => 
      prev.includes(memberId) ? prev.filter(x => x !== memberId) : [...prev, memberId]
    )
  }

  if (loading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <RefreshCw className="h-8 w-8 animate-spin text-blue-600" />
      </div>
    )
  }

  if (!group) {
    return (
      <div className="text-center py-12">
        <AlertTriangle className="h-12 w-12 text-amber-500 mx-auto" />
        <h2 className="text-lg font-bold text-slate-700 mt-4">Group Not Found</h2>
        <p className="text-slate-400 mt-2">The requested student cohort does not exist.</p>
        <Link to="/learner-groups" className="mt-6 inline-flex items-center text-blue-600 font-semibold hover:underline">
          <ArrowLeft className="h-4 w-4 mr-1" /> Back to groups
        </Link>
      </div>
    )
  }

  return (
    <>
      <PageHeader
        title={group.name}
        eyebrow="Student Groups"
        actions={
          <div className="flex items-center gap-2">
            <Link to={`/learner-groups/${id}/edit`}>
              <AppButton variant="secondary" icon={Settings}>
                Edit Group Properties
              </AppButton>
            </Link>
            <AppButton variant="primary" icon={UserPlus} onClick={() => setManagerMode('add')}>
              Add Members
            </AppButton>
            {selectedMemberIds.length > 0 && (
              <AppButton variant="danger" icon={UserMinus} onClick={handlePreviewRemove}>
                Remove Selected ({selectedMemberIds.length})
              </AppButton>
            )}
          </div>
        }
      />

      <header className="mb-3">
        <div className="text-xxs font-extrabold uppercase text-slate-400">Student Group</div>
        <h1 className="text-2xl font-extrabold text-slate-900">{group.name}</h1>
      </header>

      {/* Main Grid display vs Overlay Operations Drawer */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
        
        {/* Members List Table Grid */}
        <section className="admin-card admin-table-card lg:col-span-2">
          <div className="admin-card-head">
            <h2 className="admin-card-head-title"><Users aria-hidden="true" />Members ({group.members.length})</h2>
          </div>

          <div className="admin-table-card-scroll max-h-140 custom-scrollbar">
            <table className="w-full text-left text-sm border-collapse">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                  <th className="p-3 w-10">Select</th>
                  <th className="p-3">Learner Code / NID</th>
                  <th className="p-3">Name</th>
                  <th className="p-3">Division / Department</th>
                  <th className="p-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-700">
                {group.members.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="p-8 text-center text-slate-400">
                      No members.
                    </td>
                  </tr>
                ) : (
                  group.members.map(m => {
                    const isChecked = selectedMemberIds.includes(m.id)
                    return (
                      <tr key={m.id} className={`hover:bg-slate-50/60 transition ${isChecked ? 'bg-blue-50/20' : ''}`}>
                        <td className="p-3 w-10">
                          <input
                            type="checkbox"
                            checked={isChecked}
                            onChange={() => handleToggleSelectMember(m.id)}
                            className="h-4 w-4 text-blue-600 rounded border-slate-300 focus:ring-blue-500"
                          />
                        </td>
                        <td className="p-3 font-mono font-bold text-slate-800">{m.learnerCode}</td>
                        <td className="p-3 font-semibold text-slate-900">{m.learnerName}</td>
                        <td className="p-3 text-slate-500 text-xs">
                          {m.division || '-'} {m.department ? `/ ${m.department}` : ''}
                        </td>
                        <td className="p-3 text-center">
                          <button
                            onClick={() => handleRemoveSingleMember(m.id)}
                            className="p-1 text-slate-400 hover:text-red-600 rounded transition"
                            title="Remove member"
                          >
                            <UserMinus className="h-4 w-4" />
                          </button>
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </table>
          </div>
        </section>

        {/* Dynamic Sidebar Controls based on mode selection */}
        <aside className="lg:col-span-1 space-y-6">
          
          {managerMode === 'none' && (
            <section className="admin-card space-y-4">
              <div className="admin-card-head">
                <h2 className="admin-card-head-title"><Layers aria-hidden="true" />Properties</h2>
              </div>
              <dl className="space-y-3 text-sm">
                <div className="border-b border-slate-100/50 pb-2">
                  <dt className="text-slate-400 font-extrabold text-xxs uppercase">LMS Category</dt>
                  <dd className="text-slate-800 font-bold mt-1">{group.categoryName || '-'}</dd>
                </div>
                <div className="border-b border-slate-100/50 pb-2">
                  <dt className="text-slate-400 font-extrabold text-xxs uppercase">Owner / Creator</dt>
                  <dd className="text-slate-800 font-mono font-bold mt-1">{group.createdBy || 'System Admin'}</dd>
                </div>
              </dl>
            </section>
          )}

          {/* Add Members Overlay drawer Panel */}
          {managerMode === 'add' && (
            <section className="admin-card space-y-4 border-l-2 border-blue-500">
              <div className="flex items-center justify-between border-b border-slate-200/60 pb-2">
                <h2 className="font-extrabold text-slate-700 text-sm uppercase">Add Group Members</h2>
                <button onClick={() => { setManagerMode('none'); setAddPreview(null); }} className="text-slate-400 hover:text-slate-600 text-xs font-semibold">
                  Close
                </button>
              </div>

              {!addPreview ? (
                <div className="space-y-4">
                  <div className="space-y-1.5">
                    <label htmlFor="learnerCodes" className="block text-xxs font-extrabold text-slate-400 uppercase">
                      Employee NIDs / Codes
                    </label>
                    <textarea
                      id="learnerCodes"
                      rows={5}
                      value={learnerCodesInput}
                      onChange={(e) => setLearnerCodesInput(e.target.value)}
                      placeholder="Enter one code per line or comma-separated:&#10;500124&#10;500125"
                      className="w-full px-3 py-2 border border-slate-200 rounded text-sm font-mono text-slate-800 focus:outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-500 bg-slate-50/50"
                    />
                  </div>

                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="enrollToAssignments"
                      checked={enrollToAssignments}
                      onChange={(e) => setEnrollToAssignments(e.target.checked)}
                      className="h-4 w-4 rounded text-blue-600 border-slate-300 focus:ring-blue-500"
                    />
                    <label htmlFor="enrollToAssignments" className="text-xs font-semibold text-slate-700 select-none cursor-pointer">
                      Auto-enroll to Active Assignments
                    </label>
                  </div>

                  <button
                    onClick={handlePreviewAdd}
                    disabled={loadingPreview || !learnerCodesInput.trim()}
                    className="w-full text-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded text-xs font-bold transition disabled:opacity-55 shadow-xs"
                  >
                    {loadingPreview ? 'Analyzing...' : 'Analyze & Preview'}
                  </button>
                </div>
              ) : (
                <div className="space-y-4 text-sm">
                  <div className="bg-slate-50/60 border border-slate-100 p-3 rounded space-y-2 text-xs">
                    <div className="flex justify-between">
                      <span className="text-slate-400 font-semibold uppercase text-xxs">Selected Count:</span>
                      <span className="font-bold text-slate-800">{addPreview.selectedLearnerCount}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-400 font-semibold uppercase text-xxs">New Members:</span>
                      <span className="font-bold text-emerald-600">+{addPreview.newMemberCount}</span>
                    </div>
                    <div className="flex justify-between border-b border-slate-200/60 pb-1.5 mb-1.5">
                      <span className="text-slate-400 font-semibold uppercase text-xxs">Existing Members:</span>
                      <span className="font-bold text-slate-600">{addPreview.existingMemberCount}</span>
                    </div>
                    <div className="flex justify-between font-bold text-slate-700">
                      <span className="text-slate-400 font-semibold uppercase text-xxs">Course Enrollments:</span>
                      <span className="text-indigo-600">{addPreview.estimatedEnrollmentCount}</span>
                    </div>
                  </div>

                  {addPreview.assignments.length > 0 && (
                    <div className="space-y-2">
                      <span className="block text-xxs font-extrabold text-slate-400 uppercase">Enrolled Active Batches</span>
                      <ul className="space-y-1.5 max-h-35 overflow-y-auto custom-scrollbar">
                        {addPreview.assignments.map(a => (
                          <li key={a.id} className="text-xs bg-slate-50/60 p-2 rounded border border-slate-200/50 flex flex-col">
                            <span className="font-bold text-indigo-600">{a.assignmentNo}</span>
                            <span className="text-slate-500 text-xxs mt-0.5 truncate">{a.courseNames}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  <div className="flex gap-2">
                    <button
                      onClick={() => setAddPreview(null)}
                      className="flex-1 text-center py-2 border border-slate-200 hover:bg-slate-50 text-slate-600 rounded text-xs font-semibold transition"
                    >
                      Back
                    </button>
                    <button
                      onClick={handleConfirmAdd}
                      className="flex-1 text-center py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded text-xs font-bold transition shadow-xs"
                    >
                      Commit Changes
                    </button>
                  </div>
                </div>
              )}
            </section>
          )}

          {/* Remove Members Preview drawer Panel */}
          {managerMode === 'remove' && removePreview && (
            <section className="admin-card space-y-4 border-l-2 border-red-500">
              <div className="flex items-center justify-between border-b border-slate-200/60 pb-2">
                <h2 className="font-extrabold text-slate-700 text-sm uppercase">Remove Group Members</h2>
                <button onClick={() => { setManagerMode('none'); setRemovePreview(null); }} className="text-slate-400 hover:text-slate-600 text-xs font-semibold">
                  Close
                </button>
              </div>

              <div className="space-y-4 text-sm">
                <p className="text-xs text-slate-500 leading-relaxed">Remove selected members and optional enrollments.</p>

                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="unenrollFromAssignments"
                    checked={unenrollFromAssignments}
                    onChange={(e) => setUnenrollFromAssignments(e.target.checked)}
                    className="h-4 w-4 rounded text-red-600 border-slate-300 focus:ring-red-500"
                  />
                  <label htmlFor="unenrollFromAssignments" className="text-xs font-semibold text-slate-700 select-none cursor-pointer">
                    Unenroll from Group Assignments
                  </label>
                </div>

                <div className="bg-slate-50/60 border border-slate-100 p-3 rounded space-y-2 text-xs">
                  <div className="flex justify-between">
                    <span className="text-slate-400 font-semibold uppercase text-xxs">Selected for Removal:</span>
                    <span className="font-bold text-red-600">{removePreview.selectedMemberCount} Users</span>
                  </div>
                  <div className="flex justify-between font-bold text-slate-700 border-t border-slate-200/40 pt-2 mt-2">
                    <span className="text-slate-400 font-semibold uppercase text-xxs">Stripped Course Enrollments:</span>
                    <span className="text-red-600">{removePreview.estimatedUnenrollmentCount}</span>
                  </div>
                </div>

                <div className="flex gap-2 pt-2">
                  <button
                    onClick={() => { setManagerMode('none'); setRemovePreview(null); }}
                    className="flex-1 text-center py-2 border border-slate-200 hover:bg-slate-50 text-slate-600 rounded text-xs font-semibold transition"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleConfirmRemove}
                    className="flex-1 text-center py-2 bg-red-600 hover:bg-red-700 text-white rounded text-xs font-bold transition shadow-xs"
                  >
                    Commit Removal
                  </button>
                </div>
              </div>
            </section>
          )}

        </aside>

      </div>
    </>
  )
}
