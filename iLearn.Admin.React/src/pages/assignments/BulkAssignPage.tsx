import { useState, useEffect, useMemo } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { 
  ArrowLeft,
  ArrowRight,
  Check, 
  BookOpen, 
  Users, 
  Calendar, 
  ShieldCheck,
  RefreshCw,
  X,
  Plus
} from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

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

const stepLabels = ['Choose Courses', 'Target Scope', 'Schedule', 'Conflict Preview', 'Dispatched']

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
  const [customEidsInput, setCustomEidsInput] = useState('')
  
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
      return customEidsInput
        .split(/[\n,]+/)
        .map(c => c.trim())
        .filter(c => c.length > 0)
    }
    return [] // API resolves Learner Group members server-side when groupId is sent
  }

  // Step 4: Validate Conflicts before sending BulkAssign
  const runConflictValidation = async () => {
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
        setCurrentStep(4)
      }
    } catch (err: any) {
      console.error(err)
      toast.error(err.message || 'Validation failed. Check date parameters.')
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
        setCurrentStep(5)
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

  const renderStepButton = (label: string, index: number) => {
    const stepNum = index + 1
    const isActive = currentStep === stepNum
    const isComplete = currentStep > stepNum

    return (
      <button
        key={label}
        type="button"
        onClick={() => {
          if (stepNum <= currentStep || (
            (stepNum === 2 && selectedCourseIds.length > 0) ||
            (stepNum === 3 && selectedCourseIds.length > 0 && (targetMode === 'custom' ? getTargetCodes().length > 0 : selectedGroupId > 0)) ||
            (stepNum === 4 && selectedCourseIds.length > 0 && (targetMode === 'custom' ? getTargetCodes().length > 0 : selectedGroupId > 0) && startDate && dueDate)
          )) {
            if (stepNum === 4) {
              void runConflictValidation()
            } else {
              setCurrentStep(stepNum)
            }
          }
        }}
        className={`flex min-w-31 items-center gap-2 border px-3 py-2 text-left text-xs font-bold ${isActive ? 'border-blue-500 bg-blue-50 text-blue-700' : isComplete ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-slate-200 bg-white text-slate-500'}`}
        aria-current={isActive ? 'step' : undefined}
      >
        <span className="flex h-5 w-5 items-center justify-center rounded-sm border border-current text-xxs">{stepNum}</span>
        <span>{label}</span>
      </button>
    )
  }

  if (loadingLookups) {
    return (
      <div className="flex h-96 items-center justify-center">
        <RefreshCw className="h-8 w-8 animate-spin text-blue-600" />
      </div>
    )
  }

  return (
    <div className="admin-grid-surface">
      <div className="flex min-h-0 flex-1 flex-col gap-4">
      {/* Header and Stepper Progress */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="text-xxs font-extrabold uppercase text-slate-400">Assignments</div>
          <h1 className="text-xl font-extrabold text-slate-800">Bulk Assign</h1>
          <p className="text-sm font-medium text-slate-500">Choose catalog courses, define target audience scope, then review and dispatch.</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {stepLabels.map(renderStepButton)}
        </div>
      </div>

      {/* Stepper Panels */}
      <div className="min-h-0 flex-1">
        <div className="admin-card p-5">
          {/* Step 1: Choose Courses */}
          {currentStep === 1 && (
            <div className="flex flex-col gap-4 h-[calc(100vh-280px)] min-h-[480px]">
              <div className="flex items-center justify-between border-b border-slate-100 pb-3 shrink-0">
                <div className="flex items-center gap-2">
                  <BookOpen className="h-5 w-5 text-blue-600" />
                  <h2 className="text-sm font-bold text-slate-800">Select Syllabus Courses</h2>
                </div>
                <p className="text-xs text-slate-500 hidden sm:block">Choose catalog courses to assign to your target audience.</p>
              </div>
              
              <div className="flex-1 flex flex-col md:flex-row gap-4 min-h-0">
                {/* Left Column: Available Catalog */}
                <div className="flex-1 flex flex-col border border-slate-200 rounded-lg bg-white min-h-0">
                  <div className="p-3 bg-slate-50 border-b border-slate-200 flex items-center justify-between shrink-0">
                    <span className="font-bold text-xs text-slate-500 uppercase tracking-wider">Syllabus Catalog</span>
                    <span className="px-2 py-0.5 rounded-full bg-slate-200 text-xxs font-bold text-slate-600">{availableCourses.length}</span>
                  </div>
                  
                  <div className="p-2 border-b border-slate-100 shrink-0">
                    <input
                      type="text"
                      placeholder="Search catalog by title or code..."
                      value={courseSearch}
                      onChange={e => setCourseSearch(e.target.value)}
                      className="w-full px-3 py-1.5 text-xs border border-slate-200 rounded bg-white focus:outline-none focus:border-blue-600"
                    />
                  </div>
                  
                  <div className="flex-1 overflow-y-auto custom-scrollbar p-2 space-y-2 min-h-0">
                    {visibleAvailableCourses.length === 0 ? (
                      <div className="flex h-full items-center justify-center text-xs font-semibold text-slate-400 py-12 text-center">
                        {courseSearch ? 'No matching courses found' : 'All courses selected'}
                      </div>
                    ) : (
                      visibleAvailableCourses.map(c => (
                        <div
                          key={c.id}
                          onClick={() => handleToggleCourse(c.id)}
                          className="p-3 text-left rounded border border-slate-200 hover:border-blue-500 hover:bg-blue-50/10 cursor-pointer transition flex items-center justify-between group"
                        >
                          <div className="flex flex-col min-w-0 pr-2">
                            <span className="text-slate-800 font-bold text-sm leading-tight truncate">{c.title}</span>
                            <span className="text-slate-400 font-mono text-xxs mt-0.5">{c.code}</span>
                          </div>
                          <span className="h-5 w-5 shrink-0 rounded border border-slate-300 flex items-center justify-center text-slate-400 group-hover:border-blue-500 group-hover:bg-blue-50 transition">
                            <Plus className="h-3 w-3" />
                          </span>
                        </div>
                      ))
                    )}
                  </div>
                </div>

                {/* Right Column: Selected Courses */}
                <div className="flex-1 flex flex-col border border-slate-200 rounded-lg bg-white min-h-0">
                  <div className="p-3 bg-slate-50 border-b border-slate-200 flex items-center justify-between shrink-0">
                    <span className="font-bold text-xs text-slate-500 uppercase tracking-wider">Selected Courses</span>
                    <div className="flex items-center gap-3">
                      <span className="px-2 py-0.5 rounded-full bg-blue-100 text-xxs font-bold text-blue-700">{selectedCourseIds.length}</span>
                      {selectedCourseIds.length > 0 && (
                        <button
                          type="button"
                          onClick={() => setSelectedCourseIds([])}
                          className="text-xxs font-bold text-red-600 hover:text-red-700 cursor-pointer"
                        >
                          Clear All
                        </button>
                      )}
                    </div>
                  </div>
                  
                  <div className="flex-1 overflow-y-auto custom-scrollbar p-2 space-y-2 min-h-0">
                    {selectedCourses.length === 0 ? (
                      <div className="flex h-full items-center justify-center text-xs font-semibold text-slate-400 py-12 text-center">
                        No courses selected yet. Click items in the catalog to select them.
                      </div>
                    ) : (
                      selectedCourses.map(c => (
                        <div
                          key={c.id}
                          className="p-3 text-left rounded border border-blue-100 bg-blue-50/5 flex items-center justify-between"
                        >
                          <div className="flex flex-col min-w-0 pr-2">
                            <span className="text-slate-800 font-bold text-sm leading-tight truncate">{c.title}</span>
                            <span className="text-slate-400 font-mono text-xxs mt-0.5">{c.code}</span>
                          </div>
                          <button
                            type="button"
                            onClick={() => handleToggleCourse(c.id)}
                            className="h-5 w-5 shrink-0 rounded border border-red-200 text-red-600 flex items-center justify-center hover:bg-red-50 hover:border-red-300 transition cursor-pointer"
                            aria-label="Remove course"
                          >
                            <X className="h-3 w-3" />
                          </button>
                        </div>
                      ))
                    )}
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Step 2: Choose Target Scope */}
          {currentStep === 2 && (
            <div className="space-y-5 flex-1">
              <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                <Users className="h-5 w-5 text-blue-600" />
                <h2 className="text-sm font-bold text-slate-800">Define Target Scope</h2>
              </div>

              <div className="flex items-center gap-4 bg-slate-50 p-2.5 rounded border border-slate-100 max-w-sm">
                <button
                  type="button"
                  onClick={() => setTargetMode('group')}
                  className={`flex-1 py-1.5 text-center text-xs font-bold rounded transition ${
                    targetMode === 'group' ? 'bg-white text-slate-800 shadow-xs' : 'text-slate-500 hover:text-slate-800'
                  }`}
                >
                  Learner Group
                </button>
                <button
                  type="button"
                  onClick={() => setTargetMode('custom')}
                  className={`flex-1 py-1.5 text-center text-xs font-bold rounded transition ${
                    targetMode === 'custom' ? 'bg-white text-slate-800 shadow-xs' : 'text-slate-500 hover:text-slate-800'
                  }`}
                >
                  Custom EIds List
                </button>
              </div>

              {targetMode === 'group' ? (
                <div className="space-y-2 max-w-md">
                  <label htmlFor="groupId" className="block text-xs font-bold text-slate-500 uppercase">
                    Select Learner Group
                  </label>
                  <select
                    id="groupId"
                    value={selectedGroupId}
                    onChange={(e) => setSelectedGroupId(Number(e.target.value))}
                    className="w-full px-3 py-2 border border-slate-200 rounded text-sm text-slate-800 bg-white focus:outline-none focus:border-blue-600"
                  >
                    <option value={0}>-- Select Group --</option>
                    {groups.map(g => (
                      <option key={g.id} value={g.id}>
                        {g.name} ({g.memberCount} members)
                      </option>
                    ))}
                  </select>
                </div>
              ) : (
                <div className="space-y-1.5">
                  <label htmlFor="customEids" className="block text-xs font-bold text-slate-500 uppercase">
                    Custom Employee EIds
                  </label>
                  <textarea
                    id="customEids"
                    rows={5}
                    value={customEidsInput}
                    onChange={(e) => setCustomEidsInput(e.target.value)}
                    placeholder="Enter employee EIds separated by comma or new lines:&#10;N130812&#10;N142715"
                    className="w-full px-3 py-2 border border-slate-200 rounded text-sm font-mono text-slate-800 focus:outline-none focus:border-blue-600"
                  />
                </div>
              )}
            </div>
          )}

          {/* Step 3: Date Schedules */}
          {currentStep === 3 && (
            <div className="space-y-5 flex-1">
              <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                <Calendar className="h-5 w-5 text-blue-600" />
                <h2 className="text-sm font-bold text-slate-800">Set Dates & Schedules</h2>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-5 max-w-xl">
                <div className="space-y-1.5">
                  <label htmlFor="startDate" className="block text-xs font-bold text-slate-500 uppercase">
                    Start Date <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="date"
                    id="startDate"
                    value={startDate}
                    onChange={(e) => setStartDate(e.target.value)}
                    className="w-full px-3 py-2 border border-slate-200 rounded text-sm text-slate-800 bg-white"
                  />
                </div>

                <div className="space-y-1.5">
                  <label htmlFor="dueDate" className="block text-xs font-bold text-slate-500 uppercase">
                    Due / Expiry Date <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="date"
                    id="dueDate"
                    value={dueDate}
                    onChange={(e) => setDueDate(e.target.value)}
                    className="w-full px-3 py-2 border border-slate-200 rounded text-sm text-slate-800 bg-white"
                  />
                </div>
              </div>

              {/* Description */}
              <div className="space-y-1.5">
                <label htmlFor="desc" className="block text-xs font-bold text-slate-500 uppercase">
                  Batch Description / Memo
                </label>
                <textarea
                  id="desc"
                  rows={2}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="e.g. Mandatory Cybersecurity training 2026"
                  className="w-full px-3 py-2 border border-slate-200 rounded text-sm text-slate-800 bg-white focus:outline-none focus:border-blue-600 resize-none"
                />
              </div>
            </div>
          )}

          {/* Step 4: Preview & Conflict Handling */}
          {currentStep === 4 && validationResult && (
            <div className="space-y-5 flex-1">
              <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                <ShieldCheck className="h-5 w-5 text-blue-600" />
                <h2 className="text-sm font-bold text-slate-800">Conflict Validation Preview</h2>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="bg-emerald-50 border border-emerald-100 p-4 rounded text-center">
                  <span className="block text-xxs font-bold text-emerald-600 uppercase">Ready to Enroll</span>
                  <span className="block text-3xl font-extrabold text-emerald-700 mt-1">{validationResult.resolvedCount}</span>
                </div>
                <div className="bg-amber-50 border border-amber-100 p-4 rounded text-center">
                  <span className="block text-xxs font-bold text-amber-600 uppercase">In Progress Attempts</span>
                  <span className="block text-3xl font-extrabold text-amber-700 mt-1">{validationResult.inProgressConflicts.length}</span>
                </div>
                <div className="bg-slate-50 border border-slate-100 p-4 rounded text-center">
                  <span className="block text-xxs font-bold text-slate-500 uppercase">Already Completed</span>
                  <span className="block text-3xl font-extrabold text-slate-700 mt-1">{validationResult.completedConflicts.length}</span>
                </div>
              </div>

              {/* Overrides */}
              <div className="space-y-3 bg-slate-50 p-4 rounded border border-slate-200">
                <span className="block text-xs font-bold text-slate-700 uppercase mb-1">Conflict Overrides Required</span>
                
                <div className="flex items-center gap-2.5">
                  <input
                    type="checkbox"
                    id="confirmReassignInProgress"
                    checked={confirmReassignInProgress}
                    onChange={(e) => setConfirmReassignInProgress(e.target.checked)}
                    className="h-4 w-4 rounded text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="confirmReassignInProgress" className="text-xs font-semibold text-slate-600 select-none cursor-pointer">
                    Force reset and reassign learners with active in-progress attempts.
                  </label>
                </div>

                <div className="flex items-center gap-2.5">
                  <input
                    type="checkbox"
                    id="confirmReassignCompleted"
                    checked={confirmReassignCompleted}
                    onChange={(e) => setConfirmReassignCompleted(e.target.checked)}
                    className="h-4 w-4 rounded text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="confirmReassignCompleted" className="text-xs font-semibold text-slate-600 select-none cursor-pointer">
                    Force reassign and reset learners who already completed the course catalog before.
                  </label>
                </div>
              </div>
            </div>
          )}

          {/* Step 5: Completed Screen */}
          {currentStep === 5 && (
            <div className="text-center py-8 space-y-4 flex-1 flex flex-col justify-center items-center">
              <div className="h-14 w-14 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mb-2 shadow-xs">
                <Check className="h-8 w-8" />
              </div>
              <h2 className="text-lg font-bold text-slate-800">Deployment Successful!</h2>
              <p className="text-sm text-slate-500 max-w-sm">
                Your courses have been successfully dispatched. Enrolled learners can now access training logs immediately.
              </p>
              <div className="bg-slate-50 border border-slate-200 p-4 rounded-lg font-mono text-sm max-w-xs w-full">
                <span className="block text-xxs text-slate-400 font-sans uppercase font-bold mb-1">Assignment Batch No.</span>
                <span className="font-extrabold text-blue-600 text-base">{assignmentNo}</span>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Footer Navigation Buttons */}
      <div className="flex items-center justify-end gap-3 border-t border-slate-200 pt-3 shrink-0">
        {currentStep < 5 && (
          <button
            type="button"
            onClick={() => navigate('/assignments')}
            className="admin-button admin-button--secondary"
          >
            <X aria-hidden="true" />
            <span>Cancel</span>
          </button>
        )}

        {currentStep > 1 && currentStep < 5 && (
          <button
            type="button"
            onClick={() => setCurrentStep(prev => Math.max(1, prev - 1))}
            className="admin-button admin-button--secondary"
          >
            <ArrowLeft aria-hidden="true" />
            <span>Previous</span>
          </button>
        )}

        {currentStep < 3 ? (
          <button
            type="button"
            onClick={() => setCurrentStep(prev => Math.min(stepLabels.length, prev + 1))}
            disabled={
              (currentStep === 1 && selectedCourseIds.length === 0) ||
              (currentStep === 2 && targetMode === 'group' && selectedGroupId === 0) ||
              (currentStep === 2 && targetMode === 'custom' && getTargetCodes().length === 0)
            }
            className="admin-button admin-button--primary"
          >
            <ArrowRight aria-hidden="true" />
            <span>Continue</span>
          </button>
        ) : currentStep === 3 ? (
          <button
            type="button"
            onClick={runConflictValidation}
            disabled={validating}
            className="admin-button admin-button--primary disabled:opacity-55"
          >
            {validating ? <RefreshCw className="h-4 w-4 animate-spin" /> : <ArrowRight className="h-4 w-4" />}
            <span>Analyze Conflicts</span>
          </button>
        ) : currentStep === 4 ? (
          <button
            type="button"
            onClick={handleCommitAssignment}
            disabled={submitting}
            className="admin-button admin-button--primary bg-emerald-600 hover:bg-emerald-700 border-emerald-600 disabled:opacity-55"
          >
            {submitting ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}
            <span>Dispatch Assignment</span>
          </button>
        ) : (
          <button
            type="button"
            onClick={() => navigate('/assignments')}
            className="admin-button admin-button--secondary"
          >
            <ArrowLeft aria-hidden="true" />
            <span>Back to Assignment Registry</span>
          </button>
        )}
      </div>
    </div>
  </div>
  )
}
