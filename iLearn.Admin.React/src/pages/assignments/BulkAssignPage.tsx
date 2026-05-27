import { useState, useEffect } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { 
  ArrowLeft, 
  Check, 
  ChevronRight, 
  ChevronLeft, 
  BookOpen, 
  Users, 
  Calendar, 
  ShieldCheck, 
  RefreshCw
} from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { SelectionTray } from '../../components/ui/SelectionTray'

type LookupCourse = {
  id: number
  code: string
  title: string
  courseTypeName?: string
}

type StudentGroupLookup = {
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
  const [searchParams] = useSearchParams()
  
  // URL context defaults
  const queryCourseId = searchParams.get('courseId')
  const queryGroupId = searchParams.get('groupId')

  // Step state
  const [step, setStep] = useState<1 | 2 | 3 | 4 | 5>(1)

  // System options state
  const [courses, setCourses] = useState<LookupCourse[]>([])
  const [groups, setGroups] = useState<StudentGroupLookup[]>([])
  const [loadingLookups, setLoadingLookups] = useState(true)

  // Selection states
  const [selectedCourseIds, setSelectedCourseIds] = useState<number[]>([])
  const [targetMode, setTargetMode] = useState<'group' | 'custom'>('group')
  const [selectedGroupId, setSelectedGroupId] = useState<number>(0)
  const [customNidsInput, setCustomNidsInput] = useState('')
  
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

  const loadLookups = async () => {
    setLoadingLookups(true)
    try {
      // Fetch open courses lookup
      const coursesData = await fetchWithAccessControl<any>('Assignments/lookup-courses')
      const coursesList = Array.isArray(coursesData) ? coursesData : (coursesData.data || [])
      setCourses(coursesList)

      // Fetch Student Groups
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
      return customNidsInput
        .split(/[\n,]+/)
        .map(c => c.trim())
        .filter(c => c.length > 0)
    }
    return [] // API resolves Student Group members server-side when groupId is sent
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
        setStep(4)
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
        setStep(5)
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

  if (loadingLookups) {
    return (
      <div className="flex h-96 items-center justify-center">
        <RefreshCw className="h-8 w-8 animate-spin text-blue-600" />
      </div>
    )
  }

  return (
    <>
      <header className="mb-3">
        <div className="text-xxs font-extrabold uppercase text-slate-400">Assignments</div>
        <h1 className="text-2xl font-extrabold text-slate-900">Bulk Assign</h1>
      </header>

      {/* Stepper Progress Bar */}
      <div className="max-w-4xl border-b border-slate-200 pb-5 mb-8 flex items-center justify-between">
        {[
          { num: 1, label: 'Choose Courses' },
          { num: 2, label: 'Target Scope' },
          { num: 3, label: 'Schedule' },
          { num: 4, label: 'Conflict Preview' },
          { num: 5, label: 'Dispatched' }
        ].map((s) => (
          <div key={s.num} className="flex items-center gap-2">
            <span className={`h-7 w-7 rounded-full flex items-center justify-center font-bold text-xs border ${
              step === s.num
                ? 'bg-blue-600 border-blue-600 text-white shadow-xs'
                : step > s.num
                  ? 'bg-emerald-500 border-emerald-500 text-white'
                  : 'bg-white border-slate-200 text-slate-400'
            }`}>
              {step > s.num ? <Check className="h-3.5 w-3.5" /> : s.num}
            </span>
            <span className={`text-xs font-semibold select-none hidden md:inline ${
              step === s.num ? 'text-slate-800 font-bold' : 'text-slate-400'
            }`}>{s.label}</span>
            {s.num < 5 && <ChevronRight className="h-4 w-4 text-slate-300 hidden md:block" />}
          </div>
        ))}
      </div>

      {/* Stepper Panels */}
      <div className="admin-card max-w-4xl min-h-105 flex flex-col justify-between">
        
        {/* Step 1: Choose Courses */}
        {step === 1 && (
          <div className="space-y-4 flex-1">
            <div className="flex items-center gap-2 border-b border-slate-100 pb-3 mb-2">
              <BookOpen className="h-5 w-5 text-blue-600" />
              <h2 className="text-sm font-bold text-slate-800">Select Syllabus Courses</h2>
            </div>
            
            <p className="text-xs text-slate-500">Choose one or more active catalog courses to assign to your target audience.</p>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-3 max-h-75 overflow-y-auto custom-scrollbar pt-2 pr-1">
              {courses.map(c => {
                const isSelected = selectedCourseIds.includes(c.id)
                return (
                  <button
                    key={c.id}
                    type="button"
                    onClick={() => handleToggleCourse(c.id)}
                    className={`p-3 text-left rounded border transition flex items-center justify-between ${
                      isSelected
                        ? 'border-blue-600 bg-blue-50/20 shadow-xs'
                        : 'border-slate-200 hover:border-slate-300'
                    }`}
                  >
                    <div className="flex flex-col min-w-0 pr-2">
                      <span className="text-slate-800 font-bold text-sm leading-tight truncate">{c.title}</span>
                      <span className="text-slate-400 font-mono text-xxs mt-0.5">{c.code}</span>
                    </div>
                    <span className={`h-4 w-4 shrink-0 rounded border flex items-center justify-center ${
                      isSelected ? 'bg-blue-600 border-blue-600 text-white' : 'border-slate-300 bg-white'
                    }`}>
                      {isSelected && <Check className="h-3 w-3" />}
                    </span>
                  </button>
                )
              })}
            </div>
            <div className="mt-4">
              <SelectionTray
                selectedItems={courses.filter(c => selectedCourseIds.includes(c.id))}
                getId={c => c.id}
                getLabel={c => `${c.title} (${c.code})`}
                onRemove={id => handleToggleCourse(Number(id))}
                onClear={() => setSelectedCourseIds([])}
                title="Selected Courses"
              />
            </div>
          </div>
        )}

        {/* Step 2: Choose Target Scope */}
        {step === 2 && (
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
                Student Group
              </button>
              <button
                type="button"
                onClick={() => setTargetMode('custom')}
                className={`flex-1 py-1.5 text-center text-xs font-bold rounded transition ${
                  targetMode === 'custom' ? 'bg-white text-slate-800 shadow-xs' : 'text-slate-500 hover:text-slate-800'
                }`}
              >
                Custom NIDs List
              </button>
            </div>

            {targetMode === 'group' ? (
              <div className="space-y-2 max-w-md">
                <label htmlFor="groupId" className="block text-xs font-bold text-slate-500 uppercase">
                  Select Student Group
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
                <label htmlFor="customNids" className="block text-xs font-bold text-slate-500 uppercase">
                  Custom Employee NIDs
                </label>
                <textarea
                  id="customNids"
                  rows={5}
                  value={customNidsInput}
                  onChange={(e) => setCustomNidsInput(e.target.value)}
                  placeholder="Enter employee codes separated by comma or new lines:&#10;500124&#10;500125"
                  className="w-full px-3 py-2 border border-slate-200 rounded text-sm font-mono text-slate-800 focus:outline-none focus:border-blue-600"
                />
              </div>
            )}
            
            {targetMode === 'group' && selectedGroupId > 0 && (
              <div className="mt-4 max-w-md">
                <SelectionTray
                  selectedItems={groups.filter(g => g.id === selectedGroupId)}
                  getId={g => g.id}
                  getLabel={g => `${g.name} (${g.memberCount} members)`}
                  onRemove={() => setSelectedGroupId(0)}
                  onClear={() => setSelectedGroupId(0)}
                  title="Selected Group"
                />
              </div>
            )}
          </div>
        )}

        {/* Step 3: Date Schedules */}
        {step === 3 && (
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
        {step === 4 && validationResult && (
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
        {step === 5 && (
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

        {/* Action Panel Footer */}
        <div className="flex items-center justify-between border-t border-slate-100 pt-4 mt-6">
          {step > 1 && step < 5 ? (
            <AppButton
              variant="secondary"
              icon={ChevronLeft}
              onClick={() => setStep(prev => (prev - 1) as any)}
            >
              Back
            </AppButton>
          ) : (
            <div></div>
          )}

          {step < 3 ? (
            <button
              onClick={() => setStep(prev => (prev + 1) as any)}
              disabled={
                (step === 1 && selectedCourseIds.length === 0) ||
                (step === 2 && targetMode === 'group' && selectedGroupId === 0) ||
                (step === 2 && targetMode === 'custom' && getTargetCodes().length === 0)
              }
              className="inline-flex items-center gap-1 px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded text-sm font-bold transition disabled:opacity-55 shadow"
            >
              <span>Continue</span>
              <ChevronRight className="h-4 w-4" />
            </button>
          ) : step === 3 ? (
            <button
              onClick={runConflictValidation}
              disabled={validating}
              className="inline-flex items-center gap-1.5 px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded text-sm font-bold transition disabled:opacity-55 shadow"
            >
              {validating ? <RefreshCw className="h-4 w-4 animate-spin" /> : <ChevronRight className="h-4 w-4" />}
              <span>Analyze Conflicts</span>
            </button>
          ) : step === 4 ? (
            <button
              onClick={handleCommitAssignment}
              disabled={submitting}
              className="inline-flex items-center gap-1.5 px-5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded text-sm font-bold transition disabled:opacity-55 shadow"
            >
              {submitting ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}
              <span>Dispatch Assignment</span>
            </button>
          ) : (
            <Link to="/assignments" className="mx-auto">
              <AppButton variant="secondary" icon={ArrowLeft}>
                Back to Assignment Registry
              </AppButton>
            </Link>
          )}
        </div>

      </div>
    </>
  )
}
