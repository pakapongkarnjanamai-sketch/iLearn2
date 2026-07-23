// Mirrors ComplianceReportDto (iLearn.Application/DTOs/ReportDtos.cs)
export interface ComplianceReportDto {
  generatedAt: string
  totalLearners: number
  openEnrollments: number
  completedEnrollments: number
  overdueEnrollments: number
  overdueLearners: number
  complianceRate: number
  byDivision: ComplianceGroupRow[]
  byDepartment: ComplianceGroupRow[]
  overdueRows: ComplianceOverdueRow[]
}

// Mirrors ComplianceGroupRow (iLearn.Application/DTOs/ReportDtos.cs)
export interface ComplianceGroupRow {
  groupName: string
  parentDivision?: string | null
  learners: number
  enrollments: number
  completed: number
  overdue: number
  completionRate: number
}

// Mirrors ComplianceOverdueRow (iLearn.Application/DTOs/ReportDtos.cs)
export interface ComplianceOverdueRow {
  learnerCode: string
  learnerName?: string | null
  division?: string | null
  department?: string | null
  courseCode?: string | null
  courseTitle?: string | null
  assignmentNo?: string | null
  dueDate?: string | null
  daysOverdue: number
  progress: number
}

// Mirrors TranscriptReportDto (iLearn.Application/DTOs/ReportDtos.cs)
export interface TranscriptReportDto {
  generatedAt: string
  learnerCode: string
  learnerName?: string | null
  division?: string | null
  department?: string | null
  learnerGroups: string[]
  totalCourses: number
  completedCourses: number
  rows: TranscriptRow[]
}

// Mirrors TranscriptRow (iLearn.Application/DTOs/ReportDtos.cs)
export interface TranscriptRow {
  enrollmentId: number
  courseCode?: string | null
  courseTitle?: string | null
  assignmentNo?: string | null
  status: string
  progress: number
  totalScore: number
  totalTimeSpentSeconds: number
  startDate?: string | null
  dueDate?: string | null
  completedDate?: string | null
}

// Mirrors CourseSummaryReportDto (iLearn.Application/DTOs/ReportDtos.cs)
export interface CourseSummaryReportDto {
  generatedAt: string
  rows: CourseSummaryRow[]
}

// Mirrors CourseSummaryRow (iLearn.Application/DTOs/ReportDtos.cs)
export interface CourseSummaryRow {
  courseId: number
  code?: string | null
  title?: string | null
  categoryName?: string | null
  divisionName?: string | null
  courseTypeName?: string | null
  assignmentCount: number
  enrolledLearners: number
  completedCount: number
  overdueCount: number
  avgProgress: number
  completionRate: number
  avgScore?: number | null
}

// Mirrors AssignmentSummaryReportDto (iLearn.Application/DTOs/ReportDtos.cs)
export interface AssignmentSummaryReportDto {
  generatedAt: string
  totalAssignments: number
  activeAssignments: number
  completedAssignments: number
  overdueAssignments: number
  totalLearners: number
  totalEnrollments: number
  completionRate: number
  rows: AssignmentSummaryRow[]
}

// Mirrors AssignmentSummaryRow (iLearn.Application/DTOs/ReportDtos.cs)
export interface AssignmentSummaryRow {
  assignmentId: number
  assignmentNo: string
  description?: string | null
  divisionName?: string | null
  startDate?: string | null
  dueDate?: string | null
  createdAt: string
  courseCount: number
  learnerCount: number
  enrollmentCount: number
  completedCount: number
  overdueCount: number
  completionRate: number
  status: string
}

// Mirrors LearnerGroupSummaryReportDto (iLearn.Application/DTOs/ReportDtos.cs)
export interface LearnerGroupSummaryReportDto {
  generatedAt: string
  totalGroups: number
  totalMembers: number
  groupsWithAssignments: number
  totalAssignments: number
  totalEnrollments: number
  completionRate: number
  rows: LearnerGroupSummaryRow[]
}

// Mirrors LearnerGroupSummaryRow (iLearn.Application/DTOs/ReportDtos.cs)
export interface LearnerGroupSummaryRow {
  learnerGroupId: number
  name: string
  description?: string | null
  divisionName?: string | null
  categoryName?: string | null
  createdAt: string
  dueDate?: string | null
  memberCount: number
  assignmentCount: number
  courseCount: number
  enrollmentCount: number
  completedCount: number
  overdueCount: number
  avgProgress: number
  completionRate: number
}

// Export endpoints return binary .xlsx files, not JSON envelopes:
// GET Reports/assignments/export?from&to&lang
// GET Reports/learner-groups/export?from&to&lang


// Mirrors ActivityReportDto (iLearn.Application/DTOs/ReportDtos.cs)
export interface ActivityReportDto {
  generatedAt: string
  months: ActivityMonthRow[]
}

// Mirrors ActivityMonthRow (iLearn.Application/DTOs/ReportDtos.cs)
export interface ActivityMonthRow {
  month: string // "yyyy-MM"
  completions: number
  activeLearners: number
  newEnrollments: number
  totalHoursPlayed: number
}
