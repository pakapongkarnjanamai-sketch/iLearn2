import { useCallback, useState, useEffect, useMemo } from 'react'
import { useParams } from 'react-router-dom'
import {
  Users,
  Settings,
  UserPlus,
  UserMinus,
  X,
  Check,
  Plus,
  Folder,
  FolderOpen,
} from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { Badge } from '../../components/ui/Badge'
import { IconButton } from '../../components/ui/IconButton'
import { SegmentedToggle } from '../../components/ui/SegmentedToggle'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { DetailLayout, Fact, FactGrid } from '../../components/ui/detail'
import { Card } from '../../components/ui/Card'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { LearnerDirectorySelector, type LearnerSelection } from '../../components/shared/LearnerDirectorySelector'
import { AppTreeView, type TreeViewNode } from '../../components/ui/AppTreeView'
import { DetailTabs } from '../../components/ui/DetailTabs'
import { DETAIL_TABLE_CHUNK_SIZE } from '../../lib/tableStandards'
import { LEARNER_LABELS, UI_LABELS, t, tf } from '../../lib/labels'

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
  categoryAncestors?: Array<{ id: number; name: string }>
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

type GroupCategoryLookup = {
  id: number
  name: string
  parentId?: number | null
  depth?: number
}

export function LearnerGroupDetailPage() {
  const { id } = useParams()
  const { setLabel } = useBreadcrumbs()
  const { confirm, confirmDialog } = useConfirm()

  // Edit Group Properties states
  const [isEditingProperties, setIsEditingProperties] = useState(false)
  const [editName, setEditName] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [editCategoryId, setEditCategoryId] = useState(0)
  const [categories, setCategories] = useState<GroupCategoryLookup[]>([])
  const [isExplorerOpen, setIsExplorerOpen] = useState(false)
  const [tempCategoryId, setTempCategoryId] = useState<number>(0)
  const [savingProperties, setSavingProperties] = useState(false)

  const selectedCategoryPath = useMemo(() => {
    if (!editCategoryId) return t(LEARNER_LABELS.noCategoryRootFolder)

    const path: string[] = []
    const visited = new Set<number>()
    let current: GroupCategoryLookup | undefined = categories.find(c => c.id === editCategoryId)

    while (current && !visited.has(current.id)) {
      visited.add(current.id)
      path.unshift(current.name)
      const pId: number | null | undefined = current.parentId
      current = pId ? categories.find(c => c.id === pId) : undefined
    }

    return path.length > 0 ? path.join(' / ') : t(LEARNER_LABELS.noCategoryRootFolder)
  }, [categories, editCategoryId])

  const tempCategoryPath = useMemo(() => {
    if (tempCategoryId === 0) return t(LEARNER_LABELS.rootFolder)

    const path: string[] = []
    const visited = new Set<number>()
    let current: GroupCategoryLookup | undefined = categories.find(c => c.id === tempCategoryId)

    while (current && !visited.has(current.id)) {
      visited.add(current.id)
      path.unshift(current.name)
      const pId: number | null | undefined = current.parentId
      current = pId ? categories.find(c => c.id === pId) : undefined
    }

    return path.length > 0 ? path.join(' / ') : t(LEARNER_LABELS.rootFolder)
  }, [categories, tempCategoryId])

  const treeNodes = useMemo<TreeViewNode[]>(() => {
    const byParent: Record<number, GroupCategoryLookup[]> = {}
    const roots: GroupCategoryLookup[] = []

    categories.forEach(category => {
      const pId = category.parentId || 0
      if (pId === 0) {
        roots.push(category)
      } else {
        if (!byParent[pId]) byParent[pId] = []
        byParent[pId].push(category)
      }
    })

    roots.sort((a, b) => a.name.localeCompare(b.name))
    Object.values(byParent).forEach(children => {
      children.sort((a, b) => a.name.localeCompare(b.name))
    })

    const mapNode = (category: GroupCategoryLookup): TreeViewNode => {
      const children = byParent[category.id] || []
      return {
        id: `cat-${category.id}`,
        text: category.name,
        categoryId: category.id,
        items: children.map(mapNode)
      }
    }

    return [
      {
        id: 'root',
        text: t(LEARNER_LABELS.rootFolderNoCategory),
        isRoot: true,
        categoryId: 0,
        items: roots.map(mapNode)
      }
    ]
  }, [categories])

  const openEditPropertiesModal = async () => {
    if (!group) return
    setEditName(group.name)
    setEditDescription(group.description || '')
    setEditCategoryId(group.categoryId || 0)
    setIsEditingProperties(true)
    
    try {
      const response = await fetchWithAccessControl<GroupCategoryLookup[] | { data?: GroupCategoryLookup[] }>('LearnerGroupCategories')
      const list = Array.isArray(response) ? response : response.data ?? []
      setCategories(list)
    } catch (error) {
      console.error(error)
      toast.error(t(LEARNER_LABELS.failedToLoadGroupCategories))
    }
  }

  const validateInformation = () => {
    if (!editName.trim()) {
      toast.error(t(LEARNER_LABELS.groupNameRequired))
      return false
    }
    if (!editDescription.trim()) {
      toast.error(t(LEARNER_LABELS.groupDescriptionRequired))
      return false
    }
    return true
  }

  const handleSaveProperties = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validateInformation()) return

    setSavingProperties(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; message?: string }>(`LearnerGroups/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: editName.trim(),
          description: editDescription.trim(),
          categoryId: editCategoryId > 0 ? editCategoryId : null
        })
      })
      if (resp.success) {
        toast.success(resp.message || t(LEARNER_LABELS.groupUpdated))
        setIsEditingProperties(false)
        await loadGroupDetails()
      }
    } catch (error: unknown) {
      console.error(error)
      toast.error(t(LEARNER_LABELS.failedToUpdateGroupProperties))
    } finally {
      setSavingProperties(false)
    }
  }

  const renderCategoryExplorerModal = () => {
    if (!isExplorerOpen) return null

    return (
      <div
        className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-fade-in"
        onClick={() => setIsExplorerOpen(false)}
      >
        <div
          className="bg-white border border-slate-100 rounded-xl shadow-2xl w-full max-w-md overflow-hidden flex flex-col animate-scale-up"
          onClick={event => event.stopPropagation()}
        >
          <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100 select-none">
            <div className="flex items-center gap-2">
              <FolderOpen className="h-5 w-5 text-indigo-500" />
              <h3 className="text-sm font-extrabold text-slate-800 uppercase tracking-wide">{t(LEARNER_LABELS.categoryFolderExplorer)}</h3>
            </div>

            <IconButton
              type="button"
              onClick={() => setIsExplorerOpen(false)}
              icon={X}
              title={t(LEARNER_LABELS.close)}
              tone="neutral"
            />
          </div>

          <div className="px-5 py-2.5 bg-slate-50 border-b border-slate-100 text-xxs font-semibold text-slate-400 uppercase select-none">
            {t(LEARNER_LABELS.categoryExplorerInstruction)}
          </div>

          <div className="p-4 flex-1 overflow-y-auto max-h-80 min-h-60 bg-slate-50/30 border-b border-slate-100">
            <div className="bg-white border border-slate-200/60 rounded-lg p-2 shadow-3xs max-h-72 overflow-y-auto custom-scrollbar">
              <AppTreeView
                items={treeNodes}
                onItemClick={event => {
                  const idVal = event.itemData.categoryId ?? 0
                  setTempCategoryId(idVal)
                }}
              />
            </div>
          </div>

          <div className="px-5 py-4 bg-slate-50 flex flex-col gap-3 select-none">
            <div className="flex items-center gap-1.5 text-xs">
              <span className="text-slate-400 font-bold uppercase text-xxs">{t(LEARNER_LABELS.selectedLabel)}</span>
              <Badge tone="neutral" variant="soft" className="truncate flex-1 font-bold">
                {tempCategoryPath}
              </Badge>
            </div>

            <div className="flex justify-end gap-2">
              <AppButton
                variant="ghost"
                onClick={() => setIsExplorerOpen(false)}
              >
                {t(UI_LABELS.cancel)}
              </AppButton>
              <AppButton
                variant="primary"
                onClick={() => {
                  setEditCategoryId(tempCategoryId)
                  setIsExplorerOpen(false)
                }}
              >
                {t(LEARNER_LABELS.confirmSelection)}
              </AppButton>
            </div>
          </div>
        </div>
      </div>
    )
  }

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
  const [activeDetailTab, setActiveDetailTab] = useState<'members'>('members')
  const [visibleMemberRows, setVisibleMemberRows] = useState(DETAIL_TABLE_CHUNK_SIZE)
  
  // Member additions workspace state
  const [memberAddTab, setMemberAddTab] = useState<'picker' | 'bulk'>('picker')
  const [pendingAddLearners, setPendingAddLearners] = useState<LearnerSelection[]>([])
  
  // Bulk Add form state
  const [learnerCodesInput, setLearnerCodesInput] = useState('')
  const [enrollToAssignments, setEnrollToAssignments] = useState(true)
  const [addPreview, setAddPreview] = useState<PreviewAddResult | null>(null)
  const [loadingPreview, setLoadingPreview] = useState(false)

  // Bulk Remove form state
  const [unenrollFromAssignments, setUnenrollFromAssignments] = useState(true)
  const [removePreview, setRemovePreview] = useState<PreviewRemoveResult | null>(null)

  const loadGroupDetails = useCallback(async () => {
    setLoading(true)
    try {
      const resp = await fetchWithAccessControl<{ success: boolean; data: LearnerGroupDetail }>(`LearnerGroups/${id}`)
      if (resp.success) {
        setGroup(resp.data)
        setSelectedMemberIds([])
      }
    } catch (err) {
      console.error(err)
      toast.error(t(LEARNER_LABELS.failedToLoadGroupDetails))
    } finally {
      setLoading(false)
    }
  }, [id, setSelectedMemberIds])

  useEffect(() => {
    void loadGroupDetails()
  }, [loadGroupDetails])

  useEffect(() => {
    setVisibleMemberRows(DETAIL_TABLE_CHUNK_SIZE)
  }, [id])

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
      toast.error(t(LEARNER_LABELS.learnerCodeRequired))
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
        toast.info(tf(LEARNER_LABELS.codeSkipped, duplicateCount))
      }
      return [...prev, ...uniqueNew]
    })
    setLearnerCodesInput('')
    toast.success(tf(LEARNER_LABELS.importedCodesToQueue, parsedCodes.length))
  }

  // Bulk Add Preview Handler
  const handlePreviewAdd = async () => {
    const codes = pendingAddLearners.map(l => l.code)
    if (codes.length === 0) {
      toast.error(t(LEARNER_LABELS.addLearnerToQueue))
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
      toast.error(err.message || t(LEARNER_LABELS.failedToAnalyzeAddition))
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
        toast.success(resp.message || t(LEARNER_LABELS.membershipUpdated))
        setManagerMode('none')
        setPendingAddLearners([])
        setAddPreview(null)
        loadGroupDetails()
      }
    } catch (err) {
      console.error(err)
      toast.error(t(LEARNER_LABELS.failedToSaveMembers))
    }
  }

  // Single Delete member operation
  const handleRemoveSingleMember = async (memberId: number) => {
    if (!(await confirm({
      title: t(LEARNER_LABELS.removeMember),
      message: t(LEARNER_LABELS.removeMemberConfirm),
      confirmLabel: t(LEARNER_LABELS.remove),
      danger: true,
    }))) return
    try {
      await fetchWithAccessControl(`LearnerGroups/${id}/members/${memberId}`, {
        method: 'DELETE'
      })
      toast.success(t(LEARNER_LABELS.learnerRemoved))
      loadGroupDetails()
    } catch (err) {
      console.error(err)
      toast.error(t(LEARNER_LABELS.failedToRemoveMember))
    }
  }

  // Bulk Remove Preview
  const handlePreviewRemove = async () => {
    if (selectedMemberIds.length === 0) {
      toast.error(t(LEARNER_LABELS.selectMemberToRemove))
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
      toast.error(err.message || t(LEARNER_LABELS.failedToPreviewRemoval))
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
        toast.success(resp.message || t(LEARNER_LABELS.membersRemoved))
        setManagerMode('none')
        setRemovePreview(null)
        loadGroupDetails()
      }
    } catch (err) {
      console.error(err)
      toast.error(t(LEARNER_LABELS.failedToCommitRemoval))
    }
  }

  const handleToggleSelectMember = (memberId: number) => {
    setSelectedMemberIds(prev => 
      prev.includes(memberId) ? prev.filter(x => x !== memberId) : [...prev, memberId]
    )
  }

  if (loading) {
    return <LoadingState />
  }

  if (!group) {
    return (
      <NotFoundState
        title={t(LEARNER_LABELS.groupNotFound)}
        message={t(LEARNER_LABELS.groupNotFoundMessage)}
        backTo="/learner-groups"
        backLabel={t(LEARNER_LABELS.backToLearnerGroups)}
      />
    )
  }

  const visibleMembers = group.members.slice(0, visibleMemberRows)

  return (
    <>
      <DetailLayout
        sidebar={
          <ControlsSidebar>
            <ControlAction onClick={openEditPropertiesModal} icon={Settings}>{t(LEARNER_LABELS.editGroupProperties)}</ControlAction>
            <ControlAction icon={UserPlus} onClick={() => { setManagerMode('add'); setAddPreview(null); }}>{t(LEARNER_LABELS.addMembers)}</ControlAction>
            <ControlAction icon={UserMinus} disabled={selectedMemberIds.length === 0} onClick={handlePreviewRemove} variant="danger">
              {selectedMemberIds.length > 0 ? tf(LEARNER_LABELS.removeSelectedWithCount, selectedMemberIds.length) : t(LEARNER_LABELS.removeSelected)}
            </ControlAction>
          </ControlsSidebar>
        }
      >
        <main className="space-y-6">
          <Card icon={Settings} title={t(LEARNER_LABELS.overview)} bodyClassName="p-5 space-y-5">
            {group.description && (
              <p className="text-sm text-slate-500 leading-relaxed max-w-2xl border-l-2 border-slate-200 pl-3 whitespace-pre-wrap">
                {group.description}
              </p>
            )}
            <FactGrid>
              <Fact label={t(LEARNER_LABELS.groupName)} valueClassName="font-semibold">
                {group.name}
              </Fact>
              <Fact label={t(LEARNER_LABELS.members)} valueClassName="font-bold text-slate-800">
                {group.members.length}
              </Fact>
              <Fact
                label={t(LEARNER_LABELS.lmsCategory)}
                colSpan="full"
                valueClassName="font-semibold"
              >
                {group.categoryAncestors && group.categoryAncestors.length > 0 ? (
                  <div className="flex flex-wrap items-center gap-1 text-xs text-slate-500">
                    {group.categoryAncestors.map((ancestor) => (
                      <span key={ancestor.id} className="flex items-center gap-1">
                        <span className="text-slate-600">{ancestor.name}</span>
                        <span className="text-slate-300 font-normal">/</span>
                      </span>
                    ))}
                    <span className="text-slate-800 font-extrabold">{group.categoryName || t(LEARNER_LABELS.emptyValue)}</span>
                  </div>
                ) : (
                  group.categoryName || t(LEARNER_LABELS.emptyValue)
                )}
              </Fact>
              <Fact
                label={t(LEARNER_LABELS.ownerCreator)}
                colSpan="full"
                valueClassName="font-semibold"
              >
                {group.createdBy || t(LEARNER_LABELS.systemAdmin)}
              </Fact>
            </FactGrid>
          </Card>

          <DetailTabs
            tabs={[{ key: 'members', label: tf(LEARNER_LABELS.membersWithCount, group.members.length) }]}
            active={activeDetailTab}
            onChange={setActiveDetailTab}
          />

          {activeDetailTab === 'members' && (
            <Card
              icon={Users}
              title={tf(LEARNER_LABELS.membersWithCount, group.members.length)}
              className="min-w-0"
            >
              <div className="overflow-x-auto max-h-140 custom-scrollbar">
                <table className="w-full text-left text-sm border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs">
                      <th className="p-3 w-10">{t(LEARNER_LABELS.select)}</th>
                      <th className="p-3">{t(LEARNER_LABELS.learnerCode)}</th>
                      <th className="p-3">{t(LEARNER_LABELS.name)}</th>
                      <th className="p-3">{t(LEARNER_LABELS.divisionDepartment)}</th>
                      <th className="p-3 text-center">{t(LEARNER_LABELS.action)}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-slate-700">
                    {group.members.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="p-8 text-center text-slate-400">
                          {t(LEARNER_LABELS.noMembers)}
                        </td>
                      </tr>
                    ) : (
                      visibleMembers.map((m) => {
                        const isChecked = selectedMemberIds.includes(m.id)
                        return (
                          <tr key={m.id} className={`hover:bg-slate-50/60 transition ${isChecked ? 'bg-indigo-50/20' : ''}`}>
                            <td className="p-3 w-10">
                              <input
                                type="checkbox"
                                checked={isChecked}
                                onChange={() => handleToggleSelectMember(m.id)}
                                className="h-4 w-4 text-indigo-500 rounded border-slate-300 focus:ring-indigo-400 cursor-pointer"
                              />
                            </td>
                            <td className="p-3 font-mono font-bold text-slate-800">{m.learnerCode}</td>
                            <td className="p-3 font-semibold text-slate-900">{m.learnerName}</td>
                            <td className="p-3 text-slate-500 text-xs font-semibold">
                              {m.division || t(LEARNER_LABELS.emptyValue)} {m.department ? `/ ${m.department}` : ''}
                            </td>
                            <td className="p-3 text-center">
                              <IconButton
                                onClick={() => handleRemoveSingleMember(m.id)}
                                icon={UserMinus}
                                tone="danger"
                                size="sm"
                                title={t(LEARNER_LABELS.removeMember)}
                              />
                            </td>
                          </tr>
                        )
                      })
                    )}
                  </tbody>
                </table>
              </div>

              {group.members.length > 0 && (
                <div className="flex items-center justify-between gap-2 border-t border-slate-100 bg-slate-50/40 px-3 py-2">
                  <span className="text-xxs font-semibold uppercase tracking-wide text-slate-500">
                    {tf(LEARNER_LABELS.showingOf, visibleMembers.length, group.members.length)}
                  </span>
                  {group.members.length > visibleMembers.length && (
                    <AppButton
                      variant="ghost"
                      onClick={() => setVisibleMemberRows(prev => prev + DETAIL_TABLE_CHUNK_SIZE)}
                      className="px-3 py-1 text-xxs font-bold"
                    >
                      {t(LEARNER_LABELS.loadMore)}
                    </AppButton>
                  )}
                </div>
              )}
            </Card>
          )}
        </main>

      </DetailLayout>

      {/* Remove Members Confirmation Modal */}
      {managerMode === 'remove' && removePreview && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-fade-in" onClick={() => { setManagerMode('none'); setRemovePreview(null); }}>
          <div className="bg-white border border-slate-100 rounded-xl shadow-2xl w-full max-w-sm overflow-hidden flex flex-col animate-scale-up duration-200" onClick={(e) => e.stopPropagation()}>
            
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <div className="flex items-center gap-2">
                <UserMinus className="h-5 w-5 text-red-500" />
                <h3 className="text-base font-extrabold text-slate-800 uppercase tracking-wide">{t(LEARNER_LABELS.confirmRemoval)}</h3>
              </div>
              <IconButton
                onClick={() => { setManagerMode('none'); setRemovePreview(null); }}
                icon={X}
                title={t(LEARNER_LABELS.close)}
                tone="neutral"
              />
            </div>

            <div className="px-6 py-5 space-y-4">
              <p className="text-xs font-medium text-slate-500 leading-relaxed">
                {t(LEARNER_LABELS.removeMembersMessage)}
              </p>

              <div className="flex items-center gap-3 py-1">
                <input
                  type="checkbox"
                  id="unenrollFromAssignments"
                  checked={unenrollFromAssignments}
                  onChange={(e) => setUnenrollFromAssignments(e.target.checked)}
                  className="h-4.5 w-4.5 text-red-600 rounded border-slate-300 focus:ring-red-400 cursor-pointer"
                />
                <label htmlFor="unenrollFromAssignments" className="text-xs sm:text-[13px] font-bold text-slate-700 select-none cursor-pointer">
                  {t(LEARNER_LABELS.unenrollGroupAssignments)}
                </label>
              </div>

              <div className="bg-slate-50/70 border border-slate-100 p-4 rounded-lg space-y-2.5 text-xs font-semibold">
                <div className="flex justify-between">
                  <span className="text-slate-400 font-bold uppercase text-[11px] tracking-wider">{t(LEARNER_LABELS.selectedMembers)}</span>
                  <span className="font-extrabold text-red-600">{tf(LEARNER_LABELS.usersCount, removePreview.selectedMemberCount)}</span>
                </div>
                <div className="flex justify-between font-bold text-slate-700 border-t border-slate-200/50 pt-2.5">
                  <span className="text-slate-400 font-bold uppercase text-[11px] tracking-wider">{t(LEARNER_LABELS.assignmentsAffected)}</span>
                  <span className="text-red-600 font-extrabold">{tf(LEARNER_LABELS.enrollmentsCount, removePreview.estimatedUnenrollmentCount)}</span>
                </div>
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <AppButton
                variant="ghost"
                onClick={() => { setManagerMode('none'); setRemovePreview(null); }}
              >
                {t(UI_LABELS.cancel)}
              </AppButton>
              <AppButton
                variant="danger"
                icon={Check}
                onClick={handleConfirmRemove}
              >
                {t(LEARNER_LABELS.confirmRemoval)}
              </AppButton>
            </div>

          </div>
        </div>
      )}

      {/* Add Members Overlay Drawer Premium Modal dialog */}
      {managerMode === 'add' && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center z-50 p-4 transition-all animate-fade-in">
          <div className="bg-white border border-slate-200 rounded-xl shadow-2xl w-full max-w-5xl h-[85vh] flex flex-col p-6 gap-4 animate-scale-up">
            
            {/* Modal Header */}
            <div className="flex items-center justify-between border-b border-slate-200/60 pb-3 shrink-0 select-none">
              <div className="flex items-center gap-2">
                <UserPlus className="h-5 w-5 text-indigo-500" />
                <h2 className="font-extrabold text-slate-800 text-sm uppercase tracking-wider">{t(LEARNER_LABELS.addGroupMembers)}</h2>
              </div>
              
              {!addPreview && (
                <SegmentedToggle
                  options={[
                    { value: 'picker', label: t(LEARNER_LABELS.directorySearch) },
                    { value: 'bulk', label: t(LEARNER_LABELS.bulkImportEids) },
                  ]}
                  value={memberAddTab}
                  onChange={setMemberAddTab}
                />
              )}

              <IconButton
                onClick={() => { setManagerMode('none'); setAddPreview(null); setPendingAddLearners([]); }}
                icon={X}
                title={t(LEARNER_LABELS.close)}
                tone="neutral"
                size="sm"
              />
            </div>

            {/* Modal Body */}
            <div className="flex-1 min-h-0 flex flex-col">
              {!addPreview ? (
                memberAddTab === 'picker' ? (
                  <div className="flex-1 flex flex-col min-h-0">
                    <LearnerDirectorySelector
                      selectedLearners={pendingAddLearners}
                      onChange={setPendingAddLearners}
                    />
                  </div>
                ) : (
                  <div className="space-y-4 h-full flex flex-col justify-start overflow-y-auto custom-scrollbar pr-1">
                    <p className="text-xs font-medium text-slate-500">
                      {t(LEARNER_LABELS.bulkImportInstruction)}
                    </p>
                    <div className="grid grid-cols-1 gap-3 md:grid-cols-[minmax(0,1fr)_auto] shrink-0">
                      <textarea
                        id="learnerCodes"
                        rows={5}
                        value={learnerCodesInput}
                        onChange={(e) => setLearnerCodesInput(e.target.value)}
                        placeholder={t(LEARNER_LABELS.learnerCodesPlaceholder)}
                        className="w-full px-3 py-2 border border-slate-200 rounded text-sm font-mono text-slate-800 focus:outline-none focus:border-indigo-500 bg-slate-50/50"
                      />
                      <AppButton
                        type="button"
                        variant="primary"
                        icon={Plus}
                        onClick={handleImportCodes}
                        disabled={!learnerCodesInput.trim()}
                        className="self-start"
                      >
                        {t(LEARNER_LABELS.addToQueue)}
                      </AppButton>
                    </div>

                    {/* Queued codes view */}
                    <div className="border border-slate-200 rounded-lg overflow-hidden flex flex-col flex-1 min-h-0">
                      <div className="bg-slate-50 px-4 py-2 border-b border-slate-200 flex justify-between items-center text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none shrink-0">
                        <span>{tf(LEARNER_LABELS.queuedAdditions, pendingAddLearners.length)}</span>
                        {pendingAddLearners.length > 0 && (
                          <button
                            type="button"
                            onClick={() => setPendingAddLearners([])}
                            className="text-red-500 hover:text-red-700 font-bold cursor-pointer"
                          >
                            {t(LEARNER_LABELS.clearQueue)}
                          </button>
                        )}
                      </div>
                      <div className="flex-1 overflow-y-auto custom-scrollbar divide-y divide-slate-100 bg-white min-h-0">
                        {pendingAddLearners.length === 0 ? (
                          <div className="text-center py-12 text-slate-400 text-xs font-semibold">{t(LEARNER_LABELS.emptyQueue)}</div>
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
                                {t(LEARNER_LABELS.remove)}
                              </button>
                            </div>
                          ))
                        )}
                      </div>
                    </div>
                  </div>
                )
              ) : (
                /* Add Preview Impact metrics */
                <div className="space-y-4 text-sm max-w-2xl mx-auto py-4">
                  <div className="bg-emerald-50/50 border border-emerald-100 p-4 rounded-lg flex flex-col gap-3">
                    <div className="flex items-center gap-2 border-b border-emerald-100/50 pb-2 mb-1">
                      <Check className="h-5 w-5 text-emerald-600 shrink-0" />
                      <span className="font-extrabold uppercase text-xs text-emerald-800 tracking-wider">{t(LEARNER_LABELS.analysisSummary)}</span>
                    </div>
                    
                    <div className="grid grid-cols-2 gap-4 text-xs font-semibold">
                      <div className="flex justify-between border-b border-emerald-100/40 pb-1.5">
                        <span className="text-slate-500">{t(LEARNER_LABELS.selectedCount)}</span>
                        <span className="font-bold text-slate-800">{addPreview.selectedLearnerCount}</span>
                      </div>
                      <div className="flex justify-between border-b border-emerald-100/40 pb-1.5">
                        <span className="text-slate-500">{t(LEARNER_LABELS.newMembers)}</span>
                        <span className="font-bold text-emerald-600">{tf(LEARNER_LABELS.addedMembersCount, addPreview.newMemberCount)}</span>
                      </div>
                      <div className="flex justify-between border-b border-emerald-100/40 pb-1.5">
                        <span className="text-slate-500">{t(LEARNER_LABELS.existingMembers)}</span>
                        <span className="font-bold text-slate-600">{addPreview.existingMemberCount}</span>
                      </div>
                      <div className="flex justify-between border-b border-emerald-100/40 pb-1.5">
                        <span className="text-slate-500">{t(LEARNER_LABELS.estimatedEnrollments)}</span>
                        <span className="font-bold text-indigo-600">{addPreview.estimatedEnrollmentCount}</span>
                      </div>
                    </div>
                  </div>

                  {addPreview.assignments.length > 0 && (
                    <div className="space-y-2">
                      <span className="block text-xxs font-extrabold text-slate-400 uppercase tracking-wider select-none">{t(LEARNER_LABELS.activeAssignmentsImpacted)}</span>
                      <ul className="space-y-1.5 max-h-56 overflow-y-auto custom-scrollbar">
                        {addPreview.assignments.map(a => (
                          <li key={a.id} className="text-xs bg-slate-50 border border-slate-200/60 p-3 rounded-lg flex flex-col gap-0.5">
                            <span className="font-extrabold text-indigo-600">{a.assignmentNo}</span>
                            <span className="text-slate-500 text-xxs mt-0.5 font-medium truncate">{a.courseNames}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Modal Footer */}
            <div className="shrink-0 border-t border-slate-100 pt-4 flex justify-between items-center select-none">
              {!addPreview ? (
                <>
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="enrollToAssignmentsModal"
                      checked={enrollToAssignments}
                      onChange={(e) => setEnrollToAssignments(e.target.checked)}
                      className="h-4 w-4 rounded text-indigo-500 border-slate-300 focus:ring-indigo-400 cursor-pointer"
                    />
                    <label htmlFor="enrollToAssignmentsModal" className="text-xs font-semibold text-slate-700 select-none cursor-pointer">
                      {t(LEARNER_LABELS.autoEnrollActiveAssignments)}
                    </label>
                  </div>

                  <div className="flex gap-2">
                    <AppButton
                      variant="ghost"
                      onClick={() => { setManagerMode('none'); setPendingAddLearners([]); }}
                    >
                      {t(UI_LABELS.cancel)}
                    </AppButton>
                    <AppButton
                      variant="primary"
                      onClick={handlePreviewAdd}
                      loading={loadingPreview}
                      disabled={pendingAddLearners.length === 0}
                    >
                      {t(LEARNER_LABELS.analyzePreview)}
                    </AppButton>
                  </div>
                </>
              ) : (
                <>
                  <div className="text-xxs font-extrabold text-slate-400 uppercase tracking-wide">
                    {t(LEARNER_LABELS.reviewCommitAdditions)}
                  </div>
                  <div className="flex gap-2">
                    <AppButton variant="secondary" onClick={() => setAddPreview(null)}>
                      {t(UI_LABELS.previous)}
                    </AppButton>
                    <AppButton variant="primary" icon={Check} onClick={handleConfirmAdd}>
                      {t(LEARNER_LABELS.commitChanges)}
                    </AppButton>
                  </div>
                </>
              )}
            </div>

          </div>
        </div>
      )}

      {isEditingProperties && (
        <div className="modal-overlay" onClick={() => setIsEditingProperties(false)}>
          <div className="modal-window p-5 relative animate-scale-in" onClick={e => e.stopPropagation()}>
            <button
              type="button"
              onClick={() => setIsEditingProperties(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 hover:bg-slate-100 rounded transition cursor-pointer"
              aria-label={t(LEARNER_LABELS.closeModal)}
            >
              <X className="h-4 w-4" />
            </button>

            <div className="mb-4 flex items-center gap-2 border-b border-slate-100 pb-3 pr-8 select-none">
              <Settings className="h-5 w-5 text-indigo-600" />
              <div>
                <h3 className="text-sm font-extrabold text-slate-800">{t(LEARNER_LABELS.editLearnerGroup)}</h3>
                <p className="text-xxs font-semibold text-slate-400">{t(LEARNER_LABELS.updateGroupDetails)}</p>
              </div>
            </div>

            <form onSubmit={handleSaveProperties} className="space-y-4">
              <div className="space-y-1.5">
                <label htmlFor="editName" className="wiz-label">
                  {t(LEARNER_LABELS.groupName)} <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  id="editName"
                  value={editName}
                  onChange={e => setEditName(e.target.value)}
                  placeholder={t(LEARNER_LABELS.groupNamePlaceholder)}
                  className="wiz-input"
                  required
                />
              </div>

              <div className="space-y-1.5">
                <label className="wiz-label">{t(LEARNER_LABELS.categoryFolder)}</label>
                <div className="flex gap-2 items-center">
                  <div className="flex-1 flex items-center gap-2 px-3 py-2 border border-slate-200 rounded-md bg-slate-50/50 text-slate-700 min-w-0 select-none">
                    <Folder className="h-4 w-4 text-indigo-500 shrink-0" />
                    <span className="text-sm font-semibold truncate">
                      {selectedCategoryPath}
                    </span>
                  </div>

                  <AppButton
                    variant="secondary"
                    icon={FolderOpen}
                    onClick={() => {
                      setTempCategoryId(editCategoryId || 0)
                      setIsExplorerOpen(true)
                    }}
                  >
                    {t(LEARNER_LABELS.selectFolder)}
                  </AppButton>
                </div>
              </div>

              <div className="space-y-1.5">
                <label htmlFor="editDescription" className="wiz-label">
                  {t(LEARNER_LABELS.description)} <span className="text-red-500">*</span>
                </label>
                <textarea
                  id="editDescription"
                  value={editDescription}
                  onChange={e => setEditDescription(e.target.value)}
                  rows={3}
                  placeholder={t(LEARNER_LABELS.groupDescriptionPlaceholder)}
                  className="wiz-input resize-y"
                  required
                />
              </div>

              <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
                <AppButton
                  variant="ghost"
                  onClick={() => setIsEditingProperties(false)}
                  disabled={savingProperties}
                >
                  {t(UI_LABELS.cancel)}
                </AppButton>
                <AppButton
                  type="submit"
                  variant="primary"
                  icon={Check}
                  loading={savingProperties}
                  className="px-4 py-2 text-xs font-bold shadow-3xs"
                >
                  {savingProperties ? t(LEARNER_LABELS.saving) : t(LEARNER_LABELS.saveChanges)}
                </AppButton>
              </div>
            </form>
          </div>
        </div>
      )}

      {renderCategoryExplorerModal()}

      {confirmDialog}
    </>
  )
}

