using iLearn.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface ICourseService
    {
        // Read Operations
        Task<IEnumerable<CourseDto>> GetAllCoursesAsync(bool isActive = true);
        Task<CourseDetailDto> GetCourseByIdAsync(int id);
        
        // Create Operations
        Task<CourseDto> CreateCourseAsync(CourseCreateDto model);
        Task<CourseDto> CreateCourseWithScormAsync(CourseCreateDto model);
        
        // Update Operations
        Task<CourseDto> UpdateCourseAsync(int id, CourseCreateDto model);
        
        // Delete Operations
        Task DeleteCourseAsync(int id);
        
        // Assignment
        Task TriggerAssignmentAsync(int courseId);
    }
}
