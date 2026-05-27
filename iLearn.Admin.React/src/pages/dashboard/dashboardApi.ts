import { fetchWithAccessControl } from '../../lib/apiClient'

export type DashboardScope = {
  isGlobal: boolean
  divisionId: number | null
  divisionName: string | null
}

export type DashboardKpi = {
  activeCourses: number
  draftCourses: number
  newCourses: number
  contentItemCount: number
  learnerGroupCount: number
  activeAssignmentBatches: number
  assignedLearners: number
  completionRate: number
  totalLearningTasks: number
  completedLearningTasks: number
  overdueTasks: number
  dueSoonTasks: number
  learningSessionsLast30: number
  learningSessionsPrevious30: number
  learningSessionDelta: number
}

export type TaskStatusPoint = { status: string; count: number }
export type LearningActivityPoint = { month: string; sessions: number }
export type CategoryMixPoint = {
  categoryId: number | null
  categoryName: string
  courseCount: number
}

export type PriorityAssignment = {
  assignmentId: number
  assignmentNo: string
  description: string | null
  divisionName: string | null
  startDate: string | null
  dueDate: string | null
  courseCount: number
  learnerCount: number
  totalTasks: number
  completedTasks: number
  overdueTasks: number
  dueSoonTasks: number
  completionRate: number
  status: string
}

export type CourseAttention = {
  courseId: number
  courseCode: string
  courseTitle: string
  categoryName: string | null
  learnerTasks: number
  completedTasks: number
  overdueTasks: number
  completionRate: number
}

export type DashboardOverview = {
  generatedAt: string
  scope: DashboardScope
  kpi: DashboardKpi
  taskStatus: TaskStatusPoint[]
  learningActivity: LearningActivityPoint[]
  categoryMix: CategoryMixPoint[]
  priorityAssignments: PriorityAssignment[]
  courseAttention: CourseAttention[]
}

export type AdminActivity = {
  id: number
  actionType: string
  entityType: string
  entityId?: number | null
  title: string
  description?: string | null
  divisionId?: number | null
  createdAt: string
  createdBy?: string | null
}

export type MaintenanceOperation = {
  operationId: string
  operationName: string
  currentStep?: string | null
  progress?: number | null
  startedAt?: string | null
}

export type MaintenanceStatus = {
  hasActiveMaintenance: boolean
  operations: MaintenanceOperation[]
}

type ApiResponse<T> = { success: boolean; data: T; message?: string }

const unwrap = async <T>(path: string): Promise<T> => {
  const resp = await fetchWithAccessControl<ApiResponse<T>>(path)
  if (!resp || resp.success !== true) {
    throw new Error(resp?.message ?? 'API call failed')
  }
  return resp.data
}

export const fetchDashboardOverview = () =>
  unwrap<DashboardOverview>('admin/Dashboard/Overview')

export const fetchMaintenanceStatus = () =>
  unwrap<MaintenanceStatus>('admin/Dashboard/MaintenanceStatus')

export const fetchRecentAdminActivities = (take = 10) =>
  unwrap<AdminActivity[]>(`admin/Dashboard/RecentAdminActivities?take=${take}`)
