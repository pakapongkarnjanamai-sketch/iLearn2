namespace iLearn.Application.DTOs
{
    public sealed record DashboardApiResponseDto<T>(bool Success, T Data);

    public sealed record DashboardOverviewDto(
        DateTime GeneratedAt,
        DashboardScopeDto Scope,
        DashboardKpiDto Kpi,
        IReadOnlyList<DashboardTaskStatusPointDto> TaskStatus,
        IReadOnlyList<DashboardLearningActivityPointDto> LearningActivity,
        IReadOnlyList<DashboardCategoryMixPointDto> CategoryMix,
        IReadOnlyList<DashboardPriorityAssignmentDto> PriorityAssignments,
        IReadOnlyList<DashboardCourseAttentionDto> CourseAttention);

    public sealed record DashboardScopeDto(bool IsGlobal, int? DivisionId, string? DivisionName);

    public sealed record DashboardKpiDto(
        int ActiveCourses,
        int DraftCourses,
        int NewCourses,
        int ContentItemCount,
        int LearnerGroupCount,
        int ActiveAssignmentBatches,
        int AssignedLearners,
        double CompletionRate,
        int TotalLearningTasks,
        int CompletedLearningTasks,
        int OverdueTasks,
        int DueSoonTasks,
        int LearningSessionsLast30,
        int LearningSessionsPrevious30,
        int LearningSessionDelta);

    public sealed record DashboardTaskStatusPointDto(string Status, int Count);

    public sealed record DashboardLearningActivityPointDto(string Month, int Sessions);

    public sealed record DashboardCategoryMixPointDto(int? CategoryId, string CategoryName, int CourseCount);

    public sealed record DashboardPriorityAssignmentDto(
        int AssignmentId,
        string AssignmentNo,
        string Description,
        string? DivisionName,
        DateTime? StartDate,
        DateTime? DueDate,
        int CourseCount,
        int LearnerCount,
        int TotalTasks,
        int CompletedTasks,
        int OverdueTasks,
        int DueSoonTasks,
        double CompletionRate,
        string Status);

    public sealed record DashboardCourseAttentionDto(
        int CourseId,
        string CourseCode,
        string CourseTitle,
        string CategoryName,
        int LearnerTasks,
        int CompletedTasks,
        int OverdueTasks,
        double CompletionRate);

    public sealed record DashboardStatsDto(
        int ActiveCourses,
        int DraftCourses,
        int InProgressAssignments,
        int TotalContentItems);

    public sealed record DashboardEnrollmentTrendPointDto(string Month, int Enrollments);

    public sealed record DashboardMaintenanceStatusDto(
        bool HasActiveMaintenance,
        IReadOnlyList<DashboardMaintenanceOperationDto> Operations);

    public sealed record DashboardMaintenanceOperationDto(
        Guid OperationId,
        string OperationName,
        string CurrentStep,
        string? CurrentItemName,
        int CurrentItem,
        int TotalItems,
        int SuccessCount,
        int FailureCount,
        string InitiatedBy,
        DateTimeOffset StartedAt,
        DateTimeOffset? LastUpdatedAt);
}