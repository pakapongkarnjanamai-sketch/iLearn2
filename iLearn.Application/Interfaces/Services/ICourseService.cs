using iLearn.Application.DTOs;
using iLearn.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface ICourseService
    {
        // Read Operations
        Task<IEnumerable<CourseDto>> GetAllCoursesAsync(bool isActive = true);
        Task<IEnumerable<CourseDto>> GetCoursesByDivisionNameAsync(string divisionName, bool isActive = true);
        Task<CourseDetailDto> GetCourseByIdAsync(int id);
        
        // Create Operations
        Task<CourseDto> CreateCourseAsync(CourseCreateDto model);
        Task<CourseDto> CreateCourseWithScormAsync(CourseCreateDto model);

        // Update Operations
        Task<CourseDto> UpdateCourseAsync(int id, CourseCreateDto model);
        Task<bool> UpdateCourseStatusAsync(int id, bool isActive);
        Task<CourseStatusResultDto> UpdateCourseStatusAsync(int id, CourseStatus status);
        Task<CourseStatusImpactDto> GetCourseStatusImpactAsync(int id);

        // Delete Operations
        Task DeleteCourseAsync(int id, bool force = false);

        // Dashboard / Aggregation Operations
        Task<List<CourseLearnerDto>> GetCourseLearnersAsync(int courseId);
        Task<List<CourseAssignmentHistoryDto>> GetCourseAssignmentsAsync(int courseId);
        Task<CourseDashboardDto> GetCourseDashboardAsync(int courseId);
    }
}
