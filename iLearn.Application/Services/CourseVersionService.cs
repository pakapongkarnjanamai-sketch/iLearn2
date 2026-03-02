using iLearn.Application.DTOs;
using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class CourseVersionService : ICourseVersionService
    {
        private readonly IGenericRepository<CourseVersion> _versionRepository;
        private readonly IGenericRepository<CourseResource> _courseResourceRepository;
        private readonly IGenericRepository<Resource> _resourceRepository;
        private readonly IGenericRepository<FileStorage> _fileStorageRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IScormService _scormService;

        public CourseVersionService(
            IGenericRepository<CourseVersion> versionRepository,
            IGenericRepository<CourseResource> courseResourceRepository,
            IGenericRepository<Resource> resourceRepository,
            IGenericRepository<FileStorage> fileStorageRepository,
            ICourseRepository courseRepository,
            IScormService scormService)
        {
            _versionRepository = versionRepository;
            _courseResourceRepository = courseResourceRepository;
            _resourceRepository = resourceRepository;
            _fileStorageRepository = fileStorageRepository;
            _courseRepository = courseRepository;
            _scormService = scormService;
        }

        public async Task<CreateCourseVersionDto> GetVersionByIdAsync(int versionId)
        {
            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null)
                throw new KeyNotFoundException($"Version ID: {versionId} ???????????");

            var courseResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId
            );

            return new CreateCourseVersionDto
            {
                CourseId = version.CourseId,
                Note = version.Note,
                IsActive = version.IsActive,
                ResourceIds = courseResources.Select(cr => cr.ResourceId).ToList()
            };
        }

        public async Task<IEnumerable<CourseVersionDto>> GetCourseVersionsAsync(int courseId)
        {
            var versions = await _versionRepository.GetAsync(
                filter: v => v.CourseId == courseId
            );

            // Sort by VersionNumber descending
            var sortedVersions = versions.OrderByDescending(v => v.VersionNumber).ToList();
            
            var result = new List<CourseVersionDto>();

            foreach (var version in sortedVersions)
            {
                var courseResources = await _courseResourceRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == version.Id,
                    includeProperties: "Resource"
                );

                var versionDto = new CourseVersionDto
                {
                    Id = version.Id,
                    CourseId = version.CourseId,
                    VersionNumber = version.VersionNumber,
                    Note = version.Note,
                    IsActive = version.IsActive,
                    CreatedAt = version.CreatedAt,
                    Resources = courseResources.Select(cr => new CourseResourceDto
                    {
                        Id = cr.Resource?.Id ?? 0,
                        Name = cr.Resource?.Name ?? "Unknown",
                        TypeId = cr.Resource?.TypeId ?? 0,
                        TypeName = cr.Resource?.TypeId == 1 ? "Learn" : "Exam",
                        IsActive = cr.Resource?.IsActive ?? false,
                        URL = cr.Resource?.URL
                    }).ToList()
                };

                result.Add(versionDto);
            }

            return result;
        }

        public async Task<CourseVersionDto> CreateVersionAsync(int courseId, CreateCourseVersionDto model, List<IFormFile> files)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
                throw new KeyNotFoundException($"Course ID: {courseId} ???????????");

            // ??????? IsActive = true ?????? version ??????? active ????
            if (model.IsActive)
            {
                var activeVersions = await _versionRepository.GetAsync(
                    filter: v => v.CourseId == courseId && v.IsActive
                );

                foreach (var oldVersion in activeVersions)
                {
                    oldVersion.IsActive = false;
                    await _versionRepository.UpdateAsync(oldVersion);
                }
            }

            // ????? version number ????
            var existingVersions = await _versionRepository.GetAsync(
                filter: v => v.CourseId == courseId
            );
            int nextVersionNumber = existingVersions.Any()
                ? existingVersions.Max(v => v.VersionNumber) + 1
                : 1;

            // ????? Version ????
            var newVersion = new CourseVersion
            {
                CourseId = courseId,
                VersionNumber = nextVersionNumber,
                Note = model.Note,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _versionRepository.AddAsync(newVersion);

            // ?????? Resources
            if (model.ResourceIds != null && model.ResourceIds.Count > 0)
            {
                int fileIndex = 0;
                foreach (var resourceId in model.ResourceIds)
                {
                    if (resourceId == 0 && fileIndex < (files?.Count ?? 0))
                    {
                        // ???????? - ??????????????????
                        var file = files[fileIndex];
                        var newResource = await ProcessNewResourceAsync(file);
                        
                        if (newResource != null)
                        {
                            var courseResource = new CourseResource
                            {
                                CourseVersionId = newVersion.Id,
                                ResourceId = newResource.Id,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _courseResourceRepository.AddAsync(courseResource);
                        }

                        fileIndex++;
                    }
                    else if (resourceId > 0)
                    {
                        // Resource ?????????????
                        var courseResource = new CourseResource
                        {
                            CourseVersionId = newVersion.Id,
                            ResourceId = resourceId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _courseResourceRepository.AddAsync(courseResource);
                    }
                }
            }

            var courseResourcesForNew = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == newVersion.Id,
                includeProperties: "Resource"
            );

            return new CourseVersionDto
            {
                Id = newVersion.Id,
                CourseId = newVersion.CourseId,
                VersionNumber = newVersion.VersionNumber,
                Note = newVersion.Note,
                IsActive = newVersion.IsActive,
                CreatedAt = newVersion.CreatedAt,
                Resources = courseResourcesForNew.Select(cr => new CourseResourceDto
                {
                    Id = cr.Resource?.Id ?? 0,
                    Name = cr.Resource?.Name ?? "Unknown",
                    TypeId = cr.Resource?.TypeId ?? 0,
                    TypeName = cr.Resource?.TypeId == 1 ? "Learn" : "Exam",
                    IsActive = cr.Resource?.IsActive ?? false,
                    URL = cr.Resource?.URL
                }).ToList()
            };
        }

        public async Task<CourseVersionDto> UpdateVersionAsync(int versionId, CreateCourseVersionDto model, List<IFormFile> files)
        {
            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null)
                throw new KeyNotFoundException($"Version ID: {versionId} ???????????");

            // ?????? Note ??? IsActive
            version.Note = model.Note;

            // ??????? IsActive = true ?????? version ??????? active
            if (model.IsActive && !version.IsActive)
            {
                var activeVersions = await _versionRepository.GetAsync(
                    filter: v => v.CourseId == version.CourseId && v.IsActive
                );

                foreach (var oldVersion in activeVersions)
                {
                    oldVersion.IsActive = false;
                    await _versionRepository.UpdateAsync(oldVersion);
                }

                version.IsActive = true;
            }

            await _versionRepository.UpdateAsync(version);

            // ?????? Resources - ???????????????????
            var oldResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId
            );

            foreach (var oldResource in oldResources)
            {
                await _courseResourceRepository.DeleteAsync(oldResource);
            }

            // ????? Resources ????
            if (model.ResourceIds != null && model.ResourceIds.Count > 0)
            {
                int fileIndex = 0;
                foreach (var resourceId in model.ResourceIds)
                {
                    if (resourceId == 0 && fileIndex < (files?.Count ?? 0))
                    {
                        // ???????? - ??????????????????
                        var file = files[fileIndex];
                        var newResource = await ProcessNewResourceAsync(file);
                        
                        if (newResource != null)
                        {
                            var courseResource = new CourseResource
                            {
                                CourseVersionId = versionId,
                                ResourceId = newResource.Id,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _courseResourceRepository.AddAsync(courseResource);
                        }

                        fileIndex++;
                    }
                    else if (resourceId > 0)
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

            var courseResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId,
                includeProperties: "Resource"
            );

            return new CourseVersionDto
            {
                Id = version.Id,
                CourseId = version.CourseId,
                VersionNumber = version.VersionNumber,
                Note = version.Note,
                IsActive = version.IsActive,
                CreatedAt = version.CreatedAt,
                Resources = courseResources.Select(cr => new CourseResourceDto
                {
                    Id = cr.Resource?.Id ?? 0,
                    Name = cr.Resource?.Name ?? "Unknown",
                    TypeId = cr.Resource?.TypeId ?? 0,
                    TypeName = cr.Resource?.TypeId == 1 ? "Learn" : "Exam",
                    IsActive = cr.Resource?.IsActive ?? false,
                    URL = cr.Resource?.URL
                }).ToList()
            };
        }

        public async Task DeleteVersionAsync(int versionId)
        {
            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null)
                throw new KeyNotFoundException($"Version ID: {versionId} ???????????");

            // ?? CourseResources ?????????????
            var courseResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId
            );

            foreach (var cr in courseResources)
            {
                await _courseResourceRepository.DeleteAsync(cr);
            }

            // ?? Version
            await _versionRepository.DeleteAsync(version);
        }

        public async Task SetActiveVersionAsync(int courseId, int versionId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
                throw new KeyNotFoundException($"Course ID: {courseId} ???????????");

            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null || version.CourseId != courseId)
                throw new KeyNotFoundException($"Version ID: {versionId} ???????????");

            // ??? versions ??????????? active
            var activeVersions = await _versionRepository.GetAsync(
                filter: v => v.CourseId == courseId && v.IsActive && v.Id != versionId
            );

            foreach (var oldVersion in activeVersions)
            {
                oldVersion.IsActive = false;
                await _versionRepository.UpdateAsync(oldVersion);
            }

            // ???? version ????
            version.IsActive = true;
            await _versionRepository.UpdateAsync(version);
        }

        /// <summary>
        /// Helper method - Process new uploaded file as Resource
        /// Handles SCORM extraction and activation
        /// </summary>
        private async Task<Resource> ProcessNewResourceAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            // Step 1: Save file to FileStorage
            var fileStorage = new FileStorage
            {
                Name = file.FileName,
                ContentType = file.ContentType,
                Length = file.Length
            };

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileStorage.Data = ms.ToArray();
            }

            var savedFile = await _fileStorageRepository.AddAsync(fileStorage);

            // Step 2: Create Resource (inactive initially)
            var resource = new Resource
            {
                Name = file.FileName,
                TypeId = 1, // Default to "Learn" type
                IsActive = false,
                FileStorageId = savedFile.Id
            };

            var savedResource = await _resourceRepository.AddAsync(resource);

            // Step 3: If SCORM file (.zip), extract and parse immediately
            string extension = Path.GetExtension(file.FileName).ToLower();
            if (extension == ".zip")
            {
                try
                {
                    string folderName = Guid.NewGuid().ToString();
                    
                    var scormInfo = await _scormService.ExtractAndParseScormAsync(
                        fileStorage.Data,
                        folderName
                    );

                    // Update resource with SCORM info
                    savedResource.ResourceHref = scormInfo.ResourceHref;
                    savedResource.SchemaVersion = scormInfo.SchemaVersion;
                    savedResource.URL = scormInfo.FolderName;
                    savedResource.IsActive = true;

                    await _resourceRepository.UpdateAsync(savedResource);
                }
                catch (InvalidScormPackageException ex)
                {
                    // If SCORM parsing fails, leave resource inactive but don't throw
                    // This allows partial processing
                    savedResource.IsActive = false;
                    await _resourceRepository.UpdateAsync(savedResource);
                    
                    // Re-throw to notify the caller
                    throw;
                }
            }
            else
            {
                // For non-SCORM files, just activate immediately
                savedResource.IsActive = true;
                await _resourceRepository.UpdateAsync(savedResource);
            }

            return savedResource;
        }
    }
}
