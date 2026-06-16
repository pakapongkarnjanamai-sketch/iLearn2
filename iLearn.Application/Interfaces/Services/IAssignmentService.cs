using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Domain.Entities;

namespace iLearn.Application.Interfaces.Services
{
    public interface IAssignmentService
    {
        Task<AssignmentHistoryResponseDto> GetHistoryAsync(
            PaginationParams p,
            int? divisionId,
            DateTime currentDate,
            CancellationToken cancellationToken = default);

        Task<List<AssignmentGanttTaskDto>> GetGanttTasksAsync(
            int? divisionId,
            DateTime currentDate,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AssignmentCourseReferenceDto>> GetByCourseAsync(
            int courseId,
            int? divisionId,
            CancellationToken cancellationToken = default);

        Task<AssignmentDashboardDto?> GetDashboardAsync(
            int assignmentId,
            int? divisionId,
            CancellationToken cancellationToken = default);

        Task<int?> ResolveAssignmentIdByNoAsync(string assignmentNo, CancellationToken cancellationToken = default);

        Task<AssignmentReassignDataDto?> GetReassignDataAsync(
            int assignmentId,
            int? divisionId,
            CancellationToken cancellationToken = default);

        Task DeleteAssignmentAsync(
            int assignmentId,
            int? divisionId,
            CancellationToken cancellationToken = default);

        Task<AssignmentResetEnrollmentsResponseDto> ResetEnrollmentsAsync(
            int assignmentId,
            ResetEnrollmentsDto dto,
            int? divisionId,
            CancellationToken cancellationToken = default);

        Task<AssignmentExtendDueDateResponseDto> ExtendDueDateAsync(
            int assignmentId,
            DateTime newDueDate,
            CancellationToken cancellationToken = default);

        Task<AssignmentMutationResponseDto> AddCoursesToAssignmentAsync(
            int assignmentId,
            ManageAssignmentCoursesDto dto,
            int? divisionId,
            CancellationToken cancellationToken = default);

        Task<AssignmentRemoveCourseResponseDto> RemoveCourseFromAssignmentAsync(
            int assignmentId,
            int ruleId,
            int? divisionId,
            CancellationToken cancellationToken = default);

        Task<AssignmentMutationResponseDto> AddLearnersToAssignmentAsync(
            int assignmentId,
            ManageAssignmentLearnersDto dto,
            int? divisionId,
            CancellationToken cancellationToken = default);

        Task<AssignmentActionResponseDto> RemoveLearnerFromAssignmentAsync(
            int assignmentId,
            string learnerCode,
            int? divisionId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Course>> GetAccessibleCoursesAsync(
            IEnumerable<int> courseIds,
            int? divisionId,
            bool includeCourseType = false);

        bool HasUnauthorizedCourses(IEnumerable<int> requestedCourseIds, IEnumerable<Course> accessibleCourses);

        Task<List<string>> GetBatchLearnerCodesAsync(
            List<int> ruleIds,
            IEnumerable<Assignment> batchRules,
            CancellationToken cancellationToken = default);

        List<string> NormalizeLearnerCodes(IEnumerable<string>? learnerCodes);
    }
}
