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
