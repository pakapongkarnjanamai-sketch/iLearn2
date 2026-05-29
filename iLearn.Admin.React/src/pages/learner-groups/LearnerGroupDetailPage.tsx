import { useState, useEffect, useMemo } from 'react'
import { useParams, Link } from 'react-router-dom'
import { 
  ArrowLeft, 
  Users, 
  Settings, 
  UserPlus, 
  UserMinus, 
  RefreshCw, 
  AlertTriangle,
  Layers,
  Search,
  Plus,
  X
} from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { AppButton } from '../../components/ui/AppButton'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'

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

type LearnerSelection = {
  code: string
  name: string
  division?: string
  department?: string
  section?: string
  position?: string
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

export function LearnerGroupDetailPage() {
  const { id } = useParams()
  const { setLabel } = useBreadcrumbs()

  const [loading, setLoading] = useState(true)
  const [group, setGroup] = useState<LearnerGroupDetail | null>(null)

  useEffect(() => {
    if (group?.name) {
      setLabel(String(id), group.name)
    }
  }, [group, id, setLabel])
  
  // Member selection states (for removal)
  const [selectedMemberIds, setSelectedMemberIds] = useState<number[]>([])

  // Modal / Operations drawers
  const [managerMode, setManagerMode] = useState<'none' | 'add' | 'remove'>('none')
  
  // Member additions workspace state
  const [activeTab, setActiveTab] = useState<'picker' | 'bulk'>('picker')
  const [searchQuery, setSearchQuery] = useState('')
  const [searching, setSearching] = useState(false)
  const [searchResults, setSearchResults] = useState<LearnerSelection[]>([])
  const [pendingAddLearners, setPendingAddLearners] = useState<LearnerSelection[]>([])
  
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

  // Search directory API caller
  const handleSearch = async () => {
    const query = searchQuery.trim()
    if (!query) {
      toast.error('Please enter a name or NID to search')
      return
    }

    setSearching(true)
    try {
      const filter = [
        ['name', 'contains', query],
        'or',
        ['code', 'contains', query]
      ]
      const url = `Learners/Get?take=20&filter=${encodeURIComponent(JSON.stringify(filter))}`
      const response = await fetchWithAccessControl<any>(url)

      let list: any[] = []
      if (response) {
        if (Array.isArray(response)) {
          list = response
        } else if (response.data && Array.isArray(response.data)) {
          list = response.data
        }
      }

      const formattedResults = list.map(item => {
        const code = String(item.code || item.eId || item.eid || item.nid || item.EId || '').trim()
        const name = String(item.name || item.fullName || (item.englishFirstName ? `${item.englishFirstName} ${item.englishLastName || ''}`.trim() : '') || item.thaiFirstName || '').trim()
        const division = item.division || ''
        const department = item.department || ''
        const section = item.section || ''
        const position = item.position || ''

        return { code, name, division, department, section, position }
      }).filter(item => item.code)

      setSearchResults(formattedResults)
      if (formattedResults.length === 0) {
        toast.info('No learners found matching search criteria')
      }
    } catch (error) {
      console.error(error)
      toast.error('Failed to search learners directory')
    } finally {
      setSearching(false)
    }
  }

  const addPendingLearner = (learner: LearnerSelection) => {
    if (group?.members.some(m => m.learnerCode === learner.code)) {
      toast.warning(`${learner.name} is already a member of this group`)
      return
    }
    setPendingAddLearners(prev => {
      if (prev.some(item => item.code === learner.code)) {
        toast.warning(`${learner.name} is already in the queue`)
        return prev
      }
      return [...prev, learner]
    })
  }

  const removePendingLearner = (code: string) => {
    setPendingAddLearners(prev => prev.filter(item => item.code !== code))
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
      toast.error('Enter at least one NID code')
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
      const groupCodes = new Set(group?.members.map(m => m.learnerCode) || [])
      
      const uniqueNew = newSelections.filter(l => !existingCodes.has(l.code) && !groupCodes.has(l.code))
      const duplicateCount = parsedCodes.length - uniqueNew.length
      if (duplicateCount > 0) {
        toast.info(`${duplicateCount} code(s) were skipped (already selected or in the group)`)
      }
      return [...prev, ...uniqueNew]
    })
    setLearnerCodesInput('')
    toast.success(`Imported ${parsedCodes.length} learner code(s) to queue`)
  }

  // Bulk Add Preview Handler
  const handlePreviewAdd = async () => {
    const codes = pendingAddLearners.map(l => l.code)
    if (codes.length === 0) {
      toast.error('Please add at least one learner to the queue')
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
        setPendingAddLearners([])
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
        <h2 className="text-lg font-bold text-slate-700 mt-4">Learner Group Not Found</h2>
        <p className="text-slate-400 mt-2">The requested learner group does not exist.</p>
        <Link to="/learner-groups" className="mt-6 inline-flex items-center text-blue-600 font-semibold hover:underline">
          <ArrowLeft className="h-4 w-4 mr-1" /> Back to Learner Groups
        </Link>
      </div>
    )
  }

  return (
    <>
      <PageHeader
        title={group.name}
        eyebrow="Learner Groups"
        actions={
          <div className="flex items-center gap-2">
            <Link to={`/learner-groups/${id}/edit`}>
              <AppButton variant="secondary" icon={Settings}>
                Edit Group Properties
              </AppButton>
            </Link>
            <AppButton variant="primary" icon={UserPlus} onClick={() => { setManagerMode('add'); setAddPreview(null); }}>
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
        <div className="text-xxs font-extrabold uppercase text-slate-400">Learner Group</div>
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
                            className="p-1 text-slate-400 hover:text-red-650 rounded transition"
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
                <button onClick={() => { setManagerMode('none'); setAddPreview(null); setPendingAddLearners([]); }} className="text-slate-400 hover:text-slate-600 text-xs font-semibold">
                  Close
                </button>
              </div>

              {!addPreview ? (
                <div className="space-y-4">
                  {/* Tabs */}
                  <div className="flex bg-slate-50 p-1 rounded border border-slate-100 shrink-0">
                    <button
                      type="button"
                      onClick={() => setActiveTab('picker')}
                      className={`flex-1 py-1.5 text-center text-xs font-bold rounded transition ${
                        activeTab === 'picker' ? 'bg-white text-blue-750 shadow-xs' : 'text-slate-500 hover:text-slate-800'
                      }`}
                    >
                      Directory Search
                    </button>
                    <button
                      type="button"
                      onClick={() => setActiveTab('bulk')}
                      className={`flex-1 py-1.5 text-center text-xs font-bold rounded transition ${
                        activeTab === 'bulk' ? 'bg-white text-blue-750 shadow-xs' : 'text-slate-500 hover:text-slate-800'
                      }`}
                    >
                      Bulk Import
                    </button>
                  </div>

                  {activeTab === 'picker' ? (
                    <div className="space-y-3">
                      <div className="flex gap-1.5">
                        <div className="relative flex-1">
                          <Search className="absolute left-2 top-2 h-3.5 w-3.5 text-slate-400" />
                          <input
                            type="text"
                            placeholder="Name or NID..."
                            value={searchQuery}
                            onChange={e => setSearchQuery(e.target.value)}
                            onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); void handleSearch(); } }}
                            className="w-full pl-7 pr-2 py-1.5 text-xs border border-slate-200 rounded focus:outline-none focus:border-blue-600"
                          />
                        </div>
                        <button
                          type="button"
                          onClick={handleSearch}
                          disabled={searching}
                          className="px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded text-xs font-bold transition disabled:opacity-55"
                        >
                          {searching ? <RefreshCw className="h-3 w-3 animate-spin" /> : 'Search'}
                        </button>
                      </div>

                      {/* Directory results */}
                      <div className="max-h-50 overflow-y-auto custom-scrollbar border border-slate-100 rounded bg-slate-50/20 p-1.5 space-y-1.5">
                        {searchResults.length === 0 ? (
                          <div className="text-center py-6 text-slate-400 text-xxs font-medium">Search to see directory results</div>
                        ) : (
                          searchResults.map(l => {
                            const isAlreadySelected = pendingAddLearners.some(p => p.code === l.code)
                            const isGroupMember = group.members.some(gm => gm.learnerCode === l.code)
                            const isDisabled = isAlreadySelected || isGroupMember

                            return (
                              <div key={l.code} className="p-2 rounded bg-white border border-slate-200 flex justify-between items-center text-xs">
                                <div className="min-w-0 pr-1 flex flex-col">
                                  <span className="font-bold text-slate-800 truncate leading-tight">{l.name}</span>
                                  <span className="text-slate-400 font-mono text-xxs mt-0.5">{l.code} {l.division ? `• ${l.division}` : ''}</span>
                                </div>
                                <button
                                  type="button"
                                  disabled={isDisabled}
                                  onClick={() => addPendingLearner(l)}
                                  className={`h-6 px-2 rounded font-bold text-xxs transition ${
                                    isDisabled
                                      ? 'bg-slate-100 text-slate-400 cursor-default'
                                      : 'bg-blue-50 text-blue-750 hover:bg-blue-100'
                                  }`}
                                >
                                  {isGroupMember ? 'In Group' : isAlreadySelected ? 'Queued' : 'Add'}
                                </button>
                              </div>
                            )
                          })
                        )}
                      </div>
                    </div>
                  ) : (
                    <div className="space-y-3">
                      <textarea
                        id="learnerCodes"
                        rows={4}
                        value={learnerCodesInput}
                        onChange={(e) => setLearnerCodesInput(e.target.value)}
                        placeholder="NID codes separated by comma or new lines..."
                        className="w-full px-3 py-2 border border-slate-200 rounded text-xs font-mono text-slate-800 focus:outline-none focus:border-blue-600 bg-slate-50/50"
                      />
                      <button
                        type="button"
                        onClick={handleImportCodes}
                        disabled={!learnerCodesInput.trim()}
                        className="w-full py-1.5 bg-slate-200 hover:bg-slate-300 text-slate-700 rounded text-xs font-bold transition disabled:opacity-55"
                      >
                        Add to Queue
                      </button>
                    </div>
                  )}

                  {/* Queued Learners (Pending additions) */}
                  <div className="border border-slate-200 rounded overflow-hidden">
                    <div className="bg-slate-50 px-3 py-1.5 border-b border-slate-200 flex justify-between items-center text-xxs font-extrabold text-slate-500 uppercase tracking-wider">
                      <span>Add Queue ({pendingAddLearners.length})</span>
                      {pendingAddLearners.length > 0 && (
                        <button
                          type="button"
                          onClick={() => setPendingAddLearners([])}
                          className="text-red-500 hover:text-red-750 font-bold"
                        >
                          Clear
                        </button>
                      )}
                    </div>
                    <div className="max-h-40 overflow-y-auto custom-scrollbar divide-y divide-slate-100 bg-white">
                      {pendingAddLearners.length === 0 ? (
                        <div className="text-center py-6 text-slate-400 text-xxs font-semibold">Queue is empty</div>
                      ) : (
                        pendingAddLearners.map(l => (
                          <div key={l.code} className="px-3 py-1.5 flex justify-between items-center text-xs font-medium">
                            <div className="min-w-0 flex items-center gap-2">
                              <span className="font-mono text-slate-800 font-semibold">{l.code}</span>
                              {l.name !== l.code && <span className="text-slate-500 truncate text-xxs">({l.name})</span>}
                            </div>
                            <button
                              type="button"
                              onClick={() => removePendingLearner(l.code)}
                              className="text-red-500 hover:text-red-700 font-bold text-xxs"
                            >
                              Remove
                            </button>
                          </div>
                        ))
                      )}
                    </div>
                  </div>

                  <div className="flex items-center gap-2 border-t border-slate-100 pt-2.5">
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
                    disabled={loadingPreview || pendingAddLearners.length === 0}
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
