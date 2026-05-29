import { useState, useEffect, useMemo } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { 
  ArrowLeft,
  Check, 
  BookOpen, 
  Users, 
  Calendar, 
  ShieldCheck,
  X,
  Plus,
  Loader2,
  Search
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { LearnerDirectorySelector, type LearnerSelection } from '../../components/shared/LearnerDirectorySelector'

type LookupCourse = {
  id: number
  code: string
  title: string
  courseTypeName?: string
}

type LearnerGroupLookup = {
  id: number
  name: string
  memberCount: number
}

type ConflictItem = {
  learnerCode: string
  learnerName: string
  courseTitle: string
}

type ValidateResult = {
  inProgressConflicts: ConflictItem[]
  completedConflicts: ConflictItem[]
  resolvedCount: number
}

export function BulkAssignPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  
  // URL context defaults
  const queryCourseId = searchParams.get('courseId')
  const queryGroupId = searchParams.get('groupId')

  // Step state
  const [currentStep, setCurrentStep] = useState(1)

  // System options state
  const [courses, setCourses] = useState<LookupCourse[]>([])
  const [groups, setGroups] = useState<LearnerGroupLookup[]>([])
  const [loadingLookups, setLoadingLookups] = useState(true)

  // Selection states
  const [selectedCourseIds, setSelectedCourseIds] = useState<number[]>([])
  const [courseSearch, setCourseSearch] = useState('')
  const [targetMode, setTargetMode] = useState<'group' | 'custom'>('group')
  const [selectedGroupId, setSelectedGroupId] = useState<number>(0)
  const [customEidsInput] = useState('')
  const [groupSearch, setGroupSearch] = useState('')
  const [selectedLearners, setSelectedLearners] = useState<LearnerSelection[]>([])
  
  // Date scheduling states
  const [startDate, setStartDate] = useState(new Date().toISOString().split('T')[0])
  const [dueDate, setDueDate] = useState(new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0])
  const [description, setDescription] = useState('')

  // Validation / Conflict states
  const [validating, setValidating] = useState(false)
  const [validationResult, setValidationResult] = useState<ValidateResult | null>(null)
  const [confirmReassignInProgress, setConfirmReassignInProgress] = useState(false)
  const [confirmReassignCompleted, setConfirmReassignCompleted] = useState(false)

  // Submit result
  const [submitting, setSubmitting] = useState(false)
  const [assignmentNo, setAssignmentNo] = useState('')

  const availableCourses = useMemo(() => (
    courses.filter(c => !selectedCourseIds.includes(c.id))
  ), [courses, selectedCourseIds])

  const visibleAvailableCourses = useMemo(() => {
    const q = courseSearch.trim().toLowerCase()
    if (!q) return availableCourses
    return availableCourses.filter(c => 
      c.title.toLowerCase().includes(q) || 
      c.code.toLowerCase().includes(q)
    )
  }, [availableCourses, courseSearch])

  const selectedCourses = useMemo(() => (
    courses.filter(c => selectedCourseIds.includes(c.id))
  ), [courses, selectedCourseIds])

  const filteredGroups = useMemo(() => {
    const q = groupSearch.trim().toLowerCase()
    if (!q) return groups
    return groups.filter(g => g.name.toLowerCase().includes(q))
  }, [groups, groupSearch])

  const loadLookups = async () => {
    setLoadingLookups(true)
    try {
      // Fetch open courses lookup
      const coursesData = await fetchWithAccessControl<any>('Assignments/lookup-courses')
      const coursesList = Array.isArray(coursesData) ? coursesData : (coursesData.data || [])
      setCourses(coursesList)

      // Fetch Learner Groups
      const groupsData = await fetchWithAccessControl<any>('LearnerGroups')
      const groupsList = Array.isArray(groupsData) ? groupsData : (groupsData.data || [])
      setGroups(groupsList)

      // Pre-select from query search params if present
      if (queryCourseId) {
        setSelectedCourseIds([Number(queryCourseId)])
      }
      if (queryGroupId) {
        setSelectedGroupId(Number(queryGroupId))
        setTargetMode('group')
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to load lookup items')
    } finally {
      setLoadingLookups(false)
    }
  }

  useEffect(() => {
    loadLookups()
  }, [])

  const getTargetCodes = (): string[] => {
    if (targetMode === 'custom') {
      // Use selected learners from the directory search
      if (selectedLearners.length > 0) {
        return selectedLearners.map(l => l.code)
      }
      // Fallback: parse customEidsInput if any
      return customEidsInput
        .split(/[\n,]+/)
        .map(c => c.trim())
        .filter(c => c.length > 0)
    }
    return []
  }

  // Step 4: Validate Conflicts before sending BulkAssign
  const runConflictValidation = async (): Promise<boolean> => {
    setValidating(true)
    const codes = getTargetCodes()
    
    try {
      const resp = await fetchWithAccessControl<{ success: boolean } & ValidateResult>('Assignments/validate-before-assign', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          courseIds: selectedCourseIds,
          employeeCodes: codes,
          groupId: targetMode === 'group' ? selectedGroupId : null,
          startDate: `${startDate}T00:00:00`,
          dueDate: `${dueDate}T23:59:59`
        })
      })
      if (resp.success) {
        setValidationResult(resp)
        return true
      }
      return false
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || 'Validation failed. Check date parameters.')
      return false
    } finally {
      setValidating(false)
    }
  }

  // Step 5: Submit Bulk Assignment
  const handleCommitAssignment = async () => {
    setSubmitting(true)
    const codes = getTargetCodes()

    try {
      const resp = await fetchWithAccessControl<any>('Enrollments/BulkAssign', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          courseIds: selectedCourseIds,
          employeeCodes: codes,
          groupId: targetMode === 'group' ? selectedGroupId : null,
          description,
          startDate: `${startDate}T00:00:00`,
          dueDate: `${dueDate}T23:59:59`,
          confirmReassignInProgress,
          confirmReassignCompleted
        })
      })
      
      if (resp.assignmentNo) {
        setAssignmentNo(resp.assignmentNo)
        toast.success(resp.message || 'Courses assigned successfully!')
      }
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || 'Conflict occurred. Check reassign confirmation overrides.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleToggleCourse = (id: number) => {
    setSelectedCourseIds(prev => 
      prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
    )
  }

  const validateCourses = () => {
    if (selectedCourseIds.length === 0) {
      toast.error('Select at least one course to assign')
      return false
    }
    return true
  }

  const validateScope = () => {
    if (targetMode === 'group' && selectedGroupId === 0) {
      toast.error('Select a target Learner Group')
      return false
    }
    if (targetMode === 'custom' && selectedLearners.length === 0 && getTargetCodes().length === 0) {
      toast.error('Select at least one learner')
      return false
    }
    return true
  }

  const renderChooseCoursesStep = () => (
    <div className="flex flex-col gap-4 h-[calc(100vh-270px)] min-h-105">
      <div className="wiz-section">
        <BookOpen />
        <h2 className="wiz-section-title">Select Syllabus Courses</h2>
      </div>
      
      <div className="flex-1 flex flex-col sm:flex-row gap-3 min-h-0">
        {/* Left Column: Available Catalog */}
        <div className="flex-1 flex flex-col border border-slate-200 rounded bg-white min-h-0">
          <div className="p-2.5 bg-slate-50 border-b border-slate-200 flex items-center justify-between shrink-0 select-none">
            <span className="font-extrabold text-xxs text-slate-400 uppercase tracking-wider">Syllabus Catalog</span>
            <span className="px-2 py-0.5 rounded-full bg-slate-200 text-xxs font-extrabold text-slate-600">{availableCourses.length}</span>
          </div>
          
          <div className="p-1.5 border-b border-slate-100 shrink-0 select-none">
            <input
              type="text"
              placeholder="Search catalog by title or code..."
              value={courseSearch}
              onChange={e => setCourseSearch(e.target.value)}
              className="w-full px-2.5 py-1.5 text-xs border border-slate-200 rounded bg-white focus:outline-none focus:border-indigo-500"
            />
          </div>
          
          <div className="flex-1 overflow-y-auto custom-scrollbar p-1.5 space-y-1.5 min-h-0">
            {visibleAvailableCourses.length === 0 ? (
              <div className="flex h-full items-center justify-center text-xs font-semibold text-slate-400 py-12 text-center select-none">
                {courseSearch ? 'No matching courses found' : 'All courses selected'}
              </div>
            ) : (
              visibleAvailableCourses.map(c => (
                <div
                  key={c.id}
                  onClick={() => handleToggleCourse(c.id)}
                  className="p-2.5 text-left rounded border border-slate-200 hover:border-blue-500 hover:bg-indigo-50/5 cursor-pointer transition flex items-center justify-between group"
                >
                  <div className="flex flex-col min-w-0 pr-2">
                    <span className="text-slate-855 font-bold text-xs leading-tight truncate">{c.title}</span>
                    <span className="text-slate-400 font-mono text-xxs mt-0.5 font-bold">{c.code}</span>
                  </div>
                  <span className="h-5 w-5 shrink-0 rounded border border-slate-300 flex items-center justify-center text-slate-400 group-hover:border-blue-500 group-hover:bg-indigo-50/5 transition">
                    <Plus className="h-3 w-3" />
                  </span>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Right Column: Selected Courses */}
        <div className="flex-1 flex flex-col border border-slate-200 rounded-lg bg-white min-h-0">
          <div className="p-2.5 bg-slate-50 border-b border-slate-200 flex items-center justify-between shrink-0 select-none">
            <span className="font-extrabold text-xxs text-slate-400 uppercase tracking-wider">Selected Courses</span>
            <div className="flex items-center gap-2">
              <span className="px-2 py-0.5 rounded bg-blue-100 text-xxs font-extrabold text-blue-700">{selectedCourseIds.length}</span>
              {selectedCourseIds.length > 0 && (
                <button
                  type="button"
                  onClick={() => setSelectedCourseIds([])}
                  className="text-xxs font-extrabold text-red-600 hover:text-red-700 cursor-pointer"
                >
                  Clear
                </button>
              )}
            </div>
          </div>
          
          <div className="flex-1 overflow-y-auto custom-scrollbar p-1.5 space-y-1.5 min-h-0">
            {selectedCourses.length === 0 ? (
              <div className="flex h-full items-center justify-center text-xs font-semibold text-slate-400 py-12 text-center select-none">
                No courses selected.
              </div>
            ) : (
              selectedCourses.map(c => (
                <div
                  key={c.id}
                  className="p-2.5 text-left rounded border border-indigo-100 bg-indigo-50/5 flex items-center justify-between"
                >
                  <div className="flex flex-col min-w-0 pr-2">
                    <span className="text-slate-855 font-bold text-xs leading-tight truncate">{c.title}</span>
                    <span className="text-slate-400 font-mono text-xxs mt-0.5 font-bold">{c.code}</span>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleToggleCourse(c.id)}
                    className="h-5 w-5 shrink-0 rounded border border-red-100 text-red-600 flex items-center justify-center hover:bg-red-50 hover:border-red-200 transition cursor-pointer"
                    aria-label="Remove course"
                  >
                    <X className="h-3.5 w-3.5" />
                  </button>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  )

  const renderTargetScopeStep = () => (
    <div className="flex flex-col gap-4 h-[calc(100vh-270px)] min-h-105">
      <div className="wiz-section">
        <Users />
        <h2 className="wiz-section-title">Define Target Scope</h2>
      </div>

      {/* Mode toggle */}
      <div className="flex items-center gap-1 bg-slate-100 p-1 rounded-lg max-w-xs select-none shrink-0">
        <button
          type="button"
          onClick={() => setTargetMode('group')}
          className={`flex-1 py-2 px-3 text-center text-sm font-bold rounded-md transition cursor-pointer ${
            targetMode === 'group' ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          Learner Group
        </button>
        <button
          type="button"
          onClick={() => setTargetMode('custom')}
          className={`flex-1 py-2 px-3 text-center text-sm font-bold rounded-md transition cursor-pointer ${
            targetMode === 'custom' ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          Individual Learners
        </button>
      </div>

      {/* Dual-panel content */}
      {targetMode === 'group' ? (
        <div className="flex-1 flex flex-col border border-slate-200 rounded bg-white min-h-0">
          <div className="p-3 bg-slate-50 border-b border-slate-200 flex items-center justify-between shrink-0 select-none">
            <span className="font-bold text-xs text-slate-500 uppercase tracking-wide">Available Learner Groups</span>
            <span className="px-2 py-0.5 rounded-full bg-slate-200 text-xs font-bold text-slate-600">{groups.length}</span>
          </div>

          <div className="p-2 border-b border-slate-100 shrink-0">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                placeholder="Search groups..."
                value={groupSearch}
                onChange={e => setGroupSearch(e.target.value)}
                className="w-full pl-9 pr-3 py-2 text-sm border border-slate-200 rounded-md bg-white focus:outline-none focus:border-indigo-500"
              />
            </div>
          </div>

          <div className="flex-1 overflow-y-auto custom-scrollbar p-2 space-y-1 min-h-0">
            {filteredGroups.length === 0 ? (
              <div className="flex h-full items-center justify-center text-sm text-slate-400 py-12 text-center select-none">
                {groupSearch ? 'No matching groups' : 'No groups available'}
              </div>
            ) : (
              filteredGroups.map(g => (
                <div
                  key={g.id}
                  onClick={() => setSelectedGroupId(g.id)}
                  className={`p-3 rounded-md border cursor-pointer transition flex items-center justify-between group ${
                    selectedGroupId === g.id
                      ? 'border-indigo-300 bg-indigo-50/50 ring-1 ring-indigo-200'
                      : 'border-slate-200 hover:border-indigo-300 hover:bg-indigo-50/20'
                  }`}
                >
                  <div className="flex flex-col min-w-0 pr-2">
                    <span className="text-sm font-semibold text-slate-800 leading-tight truncate">{g.name}</span>
                    <span className="text-xs text-slate-400 mt-0.5">{g.memberCount} members</span>
                  </div>
                  <div className={`h-5 w-5 shrink-0 rounded-full border-2 flex items-center justify-center transition ${
                    selectedGroupId === g.id
                      ? 'border-indigo-500 bg-indigo-500'
                      : 'border-slate-300 group-hover:border-indigo-400'
                  }`}>
                    {selectedGroupId === g.id && <Check className="h-3 w-3 text-white" />}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      ) : (
        <div className="flex-1 flex flex-col min-h-0">
          <LearnerDirectorySelector selectedLearners={selectedLearners} onChange={setSelectedLearners} />
        </div>
      )}
    </div>
  )

  const renderScheduleStep = () => (
    <div className="space-y-4">
      <div className="wiz-section">
        <Calendar />
        <h2 className="wiz-section-title">Set Dates & Schedules</h2>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 max-w-md">
        <div className="space-y-1.5">
          <label htmlFor="startDate" className="wiz-label">
            Start Date <span className="text-red-500">*</span>
          </label>
          <input
            type="date"
            id="startDate"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            className="wiz-input cursor-pointer"
          />
        </div>

        <div className="space-y-1.5">
          <label htmlFor="dueDate" className="wiz-label">
            Due / Expiry Date <span className="text-red-500">*</span>
          </label>
          <input
            type="date"
            id="dueDate"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
            className="wiz-input cursor-pointer"
          />
        </div>
      </div>

      <div className="space-y-1.5">
        <label htmlFor="desc" className="wiz-label">
          Batch Description / Memo
        </label>
        <textarea
          id="desc"
          rows={3}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="e.g. Mandatory Cybersecurity training 2026"
          className="wiz-input resize-y"
        />
      </div>
    </div>
  )

  const renderConflictPreviewStep = () => {
    if (!validationResult) return null
    return (
      <div className="space-y-4">
        <div className="wiz-section">
          <ShieldCheck />
          <h2 className="wiz-section-title">Conflict Validation Preview</h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-3.5 select-none">
          <div className="bg-emerald-50/30 border border-emerald-100 p-3 rounded text-center">
            <span className="block text-xxs font-extrabold text-emerald-600 uppercase">Ready to Enroll</span>
            <span className="block text-xl font-extrabold text-emerald-700 mt-0.5">{validationResult.resolvedCount}</span>
          </div>
          <div className="bg-amber-50/30 border border-amber-100 p-3 rounded text-center">
            <span className="block text-xxs font-extrabold text-amber-600 uppercase">In Progress Attempts</span>
            <span className="block text-xl font-extrabold text-amber-700 mt-0.5">{validationResult.inProgressConflicts.length}</span>
          </div>
          <div className="bg-slate-50/40 border border-slate-200 p-3 rounded text-center">
            <span className="block text-xxs font-extrabold text-slate-400 uppercase">Already Completed</span>
            <span className="block text-xl font-extrabold text-slate-700 mt-0.5">{validationResult.completedConflicts.length}</span>
          </div>
        </div>

        {/* Overrides */}
        <div className="space-y-3 bg-slate-50/15 p-4 rounded border border-slate-200 select-none">
          <span className="block text-xxs font-extrabold text-slate-400 uppercase">Conflict Overrides Required</span>
          
          <div className="flex items-center gap-2.5">
            <input
              type="checkbox"
              id="confirmReassignInProgress"
              checked={confirmReassignInProgress}
              onChange={(e) => setConfirmReassignInProgress(e.target.checked)}
              className="h-4 w-4 rounded text-indigo-500 focus:ring-indigo-400 cursor-pointer"
            />
            <label htmlFor="confirmReassignInProgress" className="text-xs font-semibold text-slate-500 cursor-pointer">
              Force reset and reassign learners with active in-progress attempts.
            </label>
          </div>

          <div className="flex items-center gap-2.5">
            <input
              type="checkbox"
              id="confirmReassignCompleted"
              checked={confirmReassignCompleted}
              onChange={(e) => setConfirmReassignCompleted(e.target.checked)}
              className="h-4 w-4 rounded text-indigo-500 focus:ring-indigo-400 cursor-pointer"
            />
            <label htmlFor="confirmReassignCompleted" className="text-xs font-semibold text-slate-500 cursor-pointer">
              Force reassign and reset learners who already completed the course catalog before.
            </label>
          </div>
        </div>
      </div>
    )
  }

  const steps: WizardStep[] = useMemo(() => [
    { label: 'Choose Courses', validate: () => validateCourses(), render: () => renderChooseCoursesStep() },
    { label: 'Target Scope', validate: () => validateScope(), render: () => renderTargetScopeStep() },
    { 
      label: 'Schedule', 
      validate: async () => {
        if (!startDate || !dueDate) {
          toast.error('Start and Due dates are required')
          return false
        }
        return await runConflictValidation()
      }, 
      render: () => renderScheduleStep() 
    },
    { label: 'Conflict Preview', render: () => renderConflictPreviewStep() }
  ], [
    courses, 
    groups, 
    selectedCourseIds, 
    courseSearch, 
    targetMode, 
    selectedGroupId, 
    selectedLearners,
    groupSearch,
    customEidsInput, 
    startDate, 
    dueDate, 
    description, 
    confirmReassignInProgress, 
    confirmReassignCompleted, 
    validationResult,
    validating
  ])

  if (loadingLookups) {
    return (
      <div className="flex h-96 items-center justify-center">
        <div className="flex flex-col items-center gap-3 select-none">
          <Loader2 className="h-6 w-6 animate-spin text-indigo-500" />
          <span className="text-xs text-slate-500 font-bold">Loading assignment configurations...</span>
        </div>
      </div>
    )
  }

  if (assignmentNo) {
    return (
      <div className="wizard-surface flex min-h-0 flex-1 flex-col overflow-hidden bg-white pt-5 px-6 justify-center items-center py-12 text-center">
        <div className="h-12 w-12 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mb-4 shadow-3xs select-none">
          <Check className="h-6 w-6" />
        </div>
        <h1 className="text-base font-extrabold text-slate-800 tracking-tight leading-tight select-none">Deployment Successful!</h1>
        <p className="text-xxs font-semibold text-slate-400 mt-1 max-w-sm select-none leading-relaxed">
          Your courses have been successfully dispatched. Enrolled learners can now access training logs immediately.
        </p>
        <div className="mt-5 bg-slate-50 border border-slate-200 p-3 rounded font-mono text-xs max-w-xs w-full select-none">
          <span className="block text-xxs text-slate-400 font-sans uppercase font-extrabold mb-1">Assignment Batch No.</span>
          <span className="font-bold text-indigo-500 text-sm">{assignmentNo}</span>
        </div>
        <button
          type="button"
          onClick={() => navigate('/assignments')}
          className="inline-flex items-center gap-1.75 rounded-md border border-slate-200 bg-white px-3 py-1.5 text-slate-900 hover:border-slate-300 hover:bg-slate-50 cursor-pointer mt-6 text-xxs font-extrabold shadow-3xs"
        >
          <ArrowLeft className="h-3.5 w-3.5" aria-hidden="true" />
          <span>Back to Assignment Registry</span>
        </button>
      </div>
    )
  }

  return (
    <>
      <AppWizard
        title="Bulk Assign"
        description="Choose catalog courses, define target audience scope, then review and dispatch."
        eyebrow="Assignments"
        steps={steps}
        currentStep={currentStep}
        onStepChange={setCurrentStep}
        onCancel={() => navigate('/assignments')}
        onSubmit={handleCommitAssignment}
        submitLabel="Dispatch Assignment"
        isSubmitting={submitting}
        submitIcon={<Check className="h-3.5 w-3.5" />}
      />

      {/* Show full blocking screen validation indicator when running async validation between step 3 and 4 */}
      {validating && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-xs flex items-center justify-center z-9999 animate-fade-in select-none">
          <div className="bg-white p-5 rounded-lg border border-slate-100 flex flex-col items-center gap-3 shadow-lg max-w-xs">
            <Loader2 className="h-7 w-7 animate-spin text-indigo-500" />
            <div className="text-center">
              <p className="text-xs font-bold text-slate-800">Analyzing Syllabus Scope</p>
              <p className="text-xxs font-semibold text-slate-400 mt-0.5 leading-relaxed">
                Checking for existing completion logs, in-progress attempts, and enrollments...
              </p>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
