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

        /// <summary>Returns all assignment history (unpaginated). Prefer IAssignmentDashboardService.GetAssignmentHistoryPagedAsync for pagination.</summary>
        Task<List<AssignmentHistoryDto>> GetAssignmentHistoryAsync();

        /// <summary>
        /// ตรวจสอบความขัดแย้งก่อนมอบหมายคอร์ส โดยรองรับการอัปเดตเวอร์ชัน (Version-Aware Conflict Validation)
        /// </summary>
        Task<AssignmentConflictDto> CheckAssignmentConflictsAsync(int courseId, List<string> employeeCodes, DateTime startDate, DateTime dueDate);
    }
}