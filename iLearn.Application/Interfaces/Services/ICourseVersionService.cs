using iLearn.Application.DTOs;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface ICourseVersionService
    {
        // Read Operations
        Task<CreateCourseVersionDto> GetVersionByIdAsync(int versionId);
        Task<IEnumerable<CourseVersionDto>> GetCourseVersionsAsync(int courseId);
        Task<CourseVersionLearnerImpactDto> GetVersionLearnerImpactAsync(int courseId);
        Task<CourseVersionReadinessDto> GetVersionReadinessAsync(int versionId);

        // Create Operations
        Task<CourseVersionDto> CreateVersionAsync(int courseId, CreateCourseVersionDto model, List<IFormFile> files);

        // Update Operations
        Task<CourseVersionDto> UpdateVersionAsync(int versionId, CreateCourseVersionDto model, List<IFormFile> files);

        // Delete Operations
        Task DeleteVersionAsync(int versionId);

        // Version Management
        Task SetActiveVersionAsync(int courseId, int versionId, CourseVersionLearnerPolicy learnerPolicy = CourseVersionLearnerPolicy.NewLearnersOnly);
    }
}
