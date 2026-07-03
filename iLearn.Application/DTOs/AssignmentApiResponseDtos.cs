using iLearn.Application.Interfaces.Services;

namespace iLearn.Application.DTOs
{
    public class AssignmentHistoryResponseDto
    {
        public List<AssignmentHistoryDto> Data { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public AssignmentHistorySummaryDto Summary { get; set; } = new();
    }

    public class AssignmentHistorySummaryDto
    {
        public int All { get; set; }
        public int InProgress { get; set; }
        public int Upcoming { get; set; }
        public int Expired { get; set; }
        public int Completed { get; set; }
    }

    public class AssignmentGanttTaskDto
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string AssignmentNo { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public int Progress { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class AssignmentCourseReferenceDto
    {
        public int Id { get; set; }
        public int? CourseId { get; set; }
    }

    public class AssignmentDashboardResponseDto
    {
        public bool Success { get; set; }
        public AssignmentDashboardDto Data { get; set; } = new();
    }

    public class AssignmentResolveResponseDto
    {
        public bool Success { get; set; }
        public int Data { get; set; }
    }

    public class AssignmentReassignDataResponseDto
    {
        public bool Success { get; set; }
        public AssignmentReassignDataDto Data { get; set; } = new();
    }

    public class AssignmentReassignDataDto
    {
        public List<int> CourseIds { get; set; } = [];
        public int? LearnerGroupId { get; set; }
    }

    public class AssignmentResetEnrollmentsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ResetCount { get; set; }
    }

    public class ValidateBeforeAssignResponseDto
    {
        public bool Success { get; set; }
        public List<ConflictDto> InProgressConflicts { get; set; } = [];
        public List<CompletedConflictDto> CompletedConflicts { get; set; } = [];
        public int ResolvedCount { get; set; }
    }

    public class AssignmentExtendDueDateResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime NewDueDate { get; set; }
    }

    public class AssignmentMutationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AddedCount { get; set; }
    }

    public class AssignmentRemoveCourseResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool AssignmentDeleted { get; set; }
    }

    public class AssignmentActionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AssignmentRemoveLearnersResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RemovedCount { get; set; }
    }

    public class AssignmentGroupHistoryResponseDto
    {
        public bool Success { get; set; }
        public List<AssignmentGroupHistoryDto> Data { get; set; } = [];
    }
}
