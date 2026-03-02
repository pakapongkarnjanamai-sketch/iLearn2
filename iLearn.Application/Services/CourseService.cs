using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IGenericRepository<CourseResource> _courseResourceRepository;
        private readonly IGenericRepository<CourseVersion> _courseVersionRepository;
        private readonly ICourseAssignmentService _assignmentService;

        public CourseService(
            ICourseRepository courseRepository,
            IGenericRepository<CourseResource> courseResourceRepository,
            IGenericRepository<CourseVersion> courseVersionRepository,
            ICourseAssignmentService assignmentService)
        {
            _courseRepo = courseRepository;
            _courseResourceRepository = courseResourceRepository;
            _courseVersionRepository = courseVersionRepository;
            _assignmentService = assignmentService;
        }

        public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync(bool isActive = true)
        {
            var courses = await _courseRepo.GetAsync(
                filter: c => c.IsActive == isActive,
                includeProperties: "Category,Versions"
            );

            return courses.Select(c => c.ToDto()).ToList();
        }

        public async Task<CourseDetailDto> GetCourseByIdAsync(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                return null;

            var versions = await _courseVersionRepository.GetAllAsync();
            var targetVersion = versions.FirstOrDefault(v => v.CourseId == id && v.IsActive)
                ?? versions.Where(v => v.CourseId == id)
                           .OrderByDescending(v => v.VersionNumber)
                           .FirstOrDefault();

            var resourceList = new List<CourseResourceDto>();
            if (targetVersion != null)
            {
                var courseResources = await _courseResourceRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == targetVersion.Id,
                    includeProperties: "Resource"
                );

                resourceList = courseResources.Select(cr => new CourseResourceDto
                {
                    Id = cr.Resource.Id,
                    Name = cr.Resource.Name,
                    TypeId = cr.Resource.TypeId,
                    TypeName = cr.Resource.TypeId == 2 ? "Exam" : "Learn",
                    IsActive = cr.Resource.IsActive,
                    URL = cr.Resource.URL
                }).ToList();
            }

            return new CourseDetailDto
            {
                Id = course.Id,
                CourseCode = course.Code,
                CourseName = course.Title,
                Description = course.Description,
                CourseType = (int)course.Type,
                CategoryId = course.CategoryId,
                IsActive = course.IsActive,
                Resources = resourceList
            };
        }

        public async Task<CourseDto> CreateCourseAsync(CourseCreateDto model)
        {
            if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
            {
                throw new InvalidOperationException($"???????? '{model.CourseCode}' ????????????????");
            }

            var course = new Course
            {
                Code = model.CourseCode,
                Title = model.CourseName,
                CategoryId = model.CategoryId,
                Description = model.Description,
                Type = (CourseType)model.CourseType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _courseRepo.AddAsync(course);

            // Create initial version
            var courseVersion = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = 1,
                Note = "Initial Create",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _courseVersionRepository.AddAsync(courseVersion);

            // Add resources
            if (model.ResourceIds?.Count > 0)
            {
                await AddResourcesToCourseVersionAsync(courseVersion.Id, model.ResourceIds);
            }

            // Process assignment if needed
            if (course.Type == CourseType.General)
            {
                await _assignmentService.ProcessAssignmentForCourseAsync(course.Id);
            }

            return course.ToDto();
        }

        public async Task<CourseDto> CreateCourseWithScormAsync(CourseCreateDto model)
        {
            if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
            {
                throw new InvalidOperationException($"???????? '{model.CourseCode}' ????????????????");
            }

            var course = new Course
            {
                Code = model.CourseCode,
                Title = model.CourseName,
                Description = model.Description,
                CategoryId = model.CategoryId,
                Type = (CourseType)model.CourseType,
                IsActive = false, // Draft status
                CreatedAt = DateTime.UtcNow
            };

            await _courseRepo.AddAsync(course);

            var version = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = 1,
                Note = "Draft (Initial Upload)",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            await _courseVersionRepository.AddAsync(version);

            // Add resources if provided
            if (model.ResourceIds?.Count > 0)
            {
                await AddResourcesToCourseVersionAsync(version.Id, model.ResourceIds);
            }

            return course.ToDto();
        }

        public async Task<CourseDto> UpdateCourseAsync(int id, CourseCreateDto dto)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"???????? ID: {id} ???????????");

            course.Title = dto.CourseName;
            course.Description = dto.Description;
            course.Code = dto.CourseCode;
            course.CategoryId = dto.CategoryId;
            course.Type = (CourseType)dto.CourseType;

            await _courseRepo.UpdateAsync(course);

            // Update course resources in active version
            var versions = await _courseVersionRepository.GetAllAsync();
            var activeVersion = versions.FirstOrDefault(v => v.CourseId == id && v.IsActive);

            if (activeVersion != null)
            {
                await ReplaceVersionResourcesAsync(activeVersion.Id, dto.ResourceIds);
            }

            return course.ToDto();
        }

        public async Task DeleteCourseAsync(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"???????? ID: {id} ???????????");

            await _courseRepo.DeleteAsync(course);
        }

        public async Task TriggerAssignmentAsync(int courseId)
        {
            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null)
                throw new KeyNotFoundException($"???????? ID: {courseId} ???????????");

            await _assignmentService.ProcessAssignmentForCourseAsync(courseId);
        }

        // Helper Methods
        private async Task AddResourcesToCourseVersionAsync(int versionId, List<int> resourceIds)
        {
            if (resourceIds?.Count > 0)
            {
                foreach (var resourceId in resourceIds)
                {
                    var courseResource = new CourseResource
                    {
                        CourseVersionId = versionId,
                        ResourceId = resourceId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _courseResourceRepository.AddAsync(courseResource);
                }
            }
        }

        private async Task ReplaceVersionResourcesAsync(int versionId, List<int> newResourceIds)
        {
            // Get current resources
            var allCourseResources = await _courseResourceRepository.GetAllAsync();
            var currentResources = allCourseResources
                .Where(cr => cr.CourseVersionId == versionId)
                .ToList();

            // Delete old resources
            foreach (var item in currentResources)
            {
                await _courseResourceRepository.DeleteAsync(item);
            }

            // Add new resources
            await AddResourcesToCourseVersionAsync(versionId, newResourceIds);
        }
    }
}
