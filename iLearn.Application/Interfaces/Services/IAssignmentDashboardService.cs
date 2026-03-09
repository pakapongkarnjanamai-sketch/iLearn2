using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    /// <summary>
    /// Encapsulates all dashboard / reporting logic for Assignments.
    /// Keeps the controller thin and enables unit testing.
    /// </summary>
    public interface IAssignmentDashboardService
    {
        /// <summary>Get the full dashboard data for a specific assignment group.</summary>
        Task<AssignmentDashboardDto?> GetDashboardAsync(int assignmentId);

        /// <summary>Validate potential conflicts before a bulk-assign operation.</summary>
        Task<ValidateBeforeAssignResult> ValidateBeforeAssignAsync(BulkAssignDto dto);

        /// <summary>Paginated + filterable assignment history.</summary>
        Task<PagedResult<AssignmentHistoryDto>> GetAssignmentHistoryPagedAsync(PaginationParams p);
    }

    public class ValidateBeforeAssignResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<ConflictDto> InProgressConflicts { get; set; } = [];
        public List<CompletedConflictDto> CompletedConflicts { get; set; } = [];
        public int ResolvedCount { get; set; }
    }

    public class ConflictDto
    {
        public string StudentCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }

    public class CompletedConflictDto
    {
        public string StudentCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime? CompletedDate { get; set; }
    }
}
