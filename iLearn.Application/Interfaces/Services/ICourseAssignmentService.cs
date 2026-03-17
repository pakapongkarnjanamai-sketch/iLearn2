using iLearn.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface ICourseAssignmentService
    {
        Task AssignGeneralCoursesToNewUserAsync(string employeeId);

        Task AssignCourseToEmployees(int courseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, int? assignmentRuleId = null, bool forceReset = false);

        Task AssignCoursesToEmployees(IReadOnlyDictionary<int, int> assignmentRuleIdsByCourseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, bool forceReset = false);

        /// <summary>Returns all assignment history (unpaginated). Prefer IAssignmentDashboardService.GetAssignmentHistoryPagedAsync for pagination.</summary>
        Task<List<AssignmentHistoryDto>> GetAssignmentHistoryAsync();

        /// <summary>
        /// Checks assignment conflicts before assigning a course, including version-aware reassignment rules.
        /// </summary>
        Task<AssignmentConflictDto> CheckAssignmentConflictsAsync(int courseId, List<string> employeeCodes, DateTime startDate, DateTime dueDate);
    }
}