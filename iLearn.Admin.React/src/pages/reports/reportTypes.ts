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
  assignmentCount: number
  enrolledLearners: number
  completedCount: number
  overdueCount: number
  avgProgress: number
  completionRate: number
  avgScore?: number | null
}

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
