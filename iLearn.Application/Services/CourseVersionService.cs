using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Application.Interfaces;
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
        private readonly IGenericRepository<Enrollment> _enrollmentRepository;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepository;
        private readonly IGenericRepository<LearningLog> _learningLogRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IScormService _scormService;
        private readonly IAdminActivityService _adminActivityService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;

        public CourseVersionService(
            IGenericRepository<CourseVersion> versionRepository,
            IGenericRepository<CourseResource> courseResourceRepository,
            IGenericRepository<Resource> resourceRepository,
            IGenericRepository<FileStorage> fileStorageRepository,
            IGenericRepository<Enrollment> enrollmentRepository,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepository,
            IGenericRepository<LearningLog> learningLogRepository,
            ICourseRepository courseRepository,
            IScormService scormService,
            IAdminActivityService adminActivityService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            IUnitOfWork unitOfWork)
        {
            _versionRepository = versionRepository;
            _courseResourceRepository = courseResourceRepository;
            _resourceRepository = resourceRepository;
            _fileStorageRepository = fileStorageRepository;
            _enrollmentRepository = enrollmentRepository;
            _enrollmentAssignmentRepository = enrollmentAssignmentRepository;
            _learningLogRepository = learningLogRepository;
            _courseRepository = courseRepository;
            _scormService = scormService;
            _adminActivityService = adminActivityService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateCourseVersionDto> GetVersionByIdAsync(int versionId)
        {
            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null)
                throw new KeyNotFoundException($"Version ID: {versionId} not found.");

            var courseResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId
            );

            var sortedResources = courseResources.OrderBy(cr => cr.Order).ToList();

            return new CreateCourseVersionDto
            {
                CourseId = version.CourseId,
                Note = version.Note,
                IsActive = version.IsActive,
                ResourceIds = sortedResources.Select(cr => cr.ResourceId).ToList()
            };
        }

        public async Task<IEnumerable<CourseVersionDto>> GetCourseVersionsAsync(int courseId)
        {
            var versions = await _versionRepository.GetAsync(
                filter: v => v.CourseId == courseId
            );

            var sortedVersions = versions.OrderByDescending(v => v.VersionNumber).ToList();
            var result = new List<CourseVersionDto>();

            foreach (var version in sortedVersions)
            {
                var courseResources = await _courseResourceRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == version.Id,
                    includeProperties: "Resource"
                );

                var sortedCourseResources = courseResources.OrderBy(cr => cr.Order).ToList();

                var versionDto = new CourseVersionDto
                {
                    Id = version.Id,
                    CourseId = version.CourseId,
                    VersionNumber = version.VersionNumber,
                    Note = version.Note,
                    IsActive = version.IsActive,
                    CreatedAt = version.CreatedAt,
                    Resources = sortedCourseResources.Select(cr => new CourseResourceDto
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

        public async Task<CourseVersionLearnerImpactDto> GetVersionLearnerImpactAsync(int courseId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
                throw new KeyNotFoundException($"Course ID: {courseId} not found.");

            var enrollments = await _enrollmentRepository.GetAsync(e => e.CourseId == courseId);
            var eligibleEnrollments = await GetPolicyEligibleOpenEnrollmentsAsync(courseId);
            var startedEnrollmentIds = await GetStartedEnrollmentIdsAsync(eligibleEnrollments);
            var eligibleKeys = eligibleEnrollments.Select(GetEnrollmentKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

            return new CourseVersionLearnerImpactDto
            {
                CourseId = courseId,
                NotStartedCount = eligibleEnrollments.Count(e => !IsStarted(e, startedEnrollmentIds)),
                InProgressCount = eligibleEnrollments.Count(e => IsStarted(e, startedEnrollmentIds)),
                CompletedCount = enrollments.Count(e => e.IsCompleted),
                OtherOpenCount = enrollments.Count(e => !e.IsCompleted && !eligibleKeys.Contains(GetEnrollmentKey(e)))
            };
        }

        public async Task<CourseVersionReadinessDto> GetVersionReadinessAsync(int versionId)
        {
            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null)
                throw new KeyNotFoundException($"Version ID: {versionId} not found.");

            var courseResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId,
                includeProperties: "Resource"
            );

            var sortedResources = courseResources.OrderBy(cr => cr.Order).ToList();
            var issues = await GetResourceReadinessIssuesAsync(sortedResources);

            return new CourseVersionReadinessDto
            {
                VersionId = versionId,
                ResourceCount = sortedResources.Count,
                IsReady = sortedResources.Count > 0 && issues.Count == 0,
                Issues = issues.Select(issue => new CourseVersionReadinessIssueDto
                {
                    ResourceId = issue.ResourceId,
                    ResourceName = issue.ResourceName,
                    Reason = issue.Reason
                }).ToList()
            };
        }

        public async Task<CourseVersionDto> CreateVersionAsync(int courseId, CreateCourseVersionDto model, List<IFormFile> files)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
                throw new KeyNotFoundException($"Course ID: {courseId} not found.");

            var existingVersions = await _versionRepository.GetAsync(
                filter: v => v.CourseId == courseId
            );

            int nextVersionNumber = existingVersions.Any()
                ? existingVersions.Max(v => v.VersionNumber) + 1
                : 1;

            var newVersion = new CourseVersion
            {
                CourseId = courseId,
                VersionNumber = nextVersionNumber,
                Note = model.Note,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            await _versionRepository.AddAsync(newVersion);

            var orderedResourceIds = await BuildOrderedResourceIdsAsync(model, files);
            await ReplaceCourseVersionResourcesAsync(newVersion.Id, orderedResourceIds);

            if (model.IsActive)
            {
                await EnsureVersionReadyForActivationAsync(newVersion.Id);

                var activeVersions = await _versionRepository.GetAsync(
                    filter: v => v.CourseId == courseId && v.IsActive
                );

                foreach (var oldVersion in activeVersions)
                {
                    oldVersion.IsActive = false;
                    await _versionRepository.UpdateAsync(oldVersion);
                }

                newVersion.IsActive = true;
                await _versionRepository.UpdateAsync(newVersion);
                await SetCourseActiveIfNeededAsync(course);

                await ApplyLearnerVersionPolicyAsync(newVersion.CourseId, newVersion.Id, model.LearnerPolicy);
            }

            var courseResourcesForNew = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == newVersion.Id,
                includeProperties: "Resource"
            );

            var sortedResources = courseResourcesForNew.OrderBy(cr => cr.Order).ToList();

            await _adminActivityService.LogAsync(
                actionType: "CreateCourseVersion",
                entityType: nameof(CourseVersion),
                entityId: newVersion.Id,
                title: $"Created course version v{newVersion.VersionNumber}",
                description: $"Created version v{newVersion.VersionNumber} for course '{course.Code}'.",
                divisionId: _currentUser.DivisionId);

            return new CourseVersionDto
            {
                Id = newVersion.Id,
                CourseId = newVersion.CourseId,
                VersionNumber = newVersion.VersionNumber,
                Note = newVersion.Note,
                IsActive = newVersion.IsActive,
                CreatedAt = newVersion.CreatedAt,
                Resources = sortedResources.Select(cr => new CourseResourceDto
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
                throw new KeyNotFoundException($"Version ID: {versionId} not found.");

            var activatesVersion = model.IsActive && !version.IsActive;
            var deactivatesVersion = !model.IsActive && version.IsActive;
            var orderedResourceIds = await BuildOrderedResourceIdsAsync(model, files);

            if (model.IsActive)
            {
                await EnsureResourceIdsReadyForActivationAsync(orderedResourceIds);
            }

            version.Note = model.Note;

            await ReplaceCourseVersionResourcesAsync(versionId, orderedResourceIds);

            if (model.IsActive)
            {
                var activeVersions = await _versionRepository.GetAsync(
                    filter: v => v.CourseId == version.CourseId && v.IsActive && v.Id != versionId
                );

                foreach (var oldVersion in activeVersions)
                {
                    oldVersion.IsActive = false;
                    await _versionRepository.UpdateAsync(oldVersion);
                }

                version.IsActive = true;
            }
            else if (deactivatesVersion)
            {
                version.IsActive = false;
            }

            await _versionRepository.UpdateAsync(version);

            if (model.IsActive)
            {
                await SetCourseActiveIfNeededAsync(version.CourseId);
            }
            else if (deactivatesVersion)
            {
                await DeactivateCourseIfNoActiveVersionAsync(version.CourseId);
            }

            var courseResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId,
                includeProperties: "Resource"
            );

            var sortedResources = courseResources.OrderBy(cr => cr.Order).ToList();

            if (activatesVersion)
            {
                await ApplyLearnerVersionPolicyAsync(version.CourseId, version.Id, model.LearnerPolicy);
            }

            return new CourseVersionDto
            {
                Id = version.Id,
                CourseId = version.CourseId,
                VersionNumber = version.VersionNumber,
                Note = version.Note,
                IsActive = version.IsActive,
                CreatedAt = version.CreatedAt,
                Resources = sortedResources.Select(cr => new CourseResourceDto
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
                throw new KeyNotFoundException($"Version ID: {versionId} not found.");

            var courseResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId
            );

            foreach (var cr in courseResources)
            {
                await _courseResourceRepository.DeleteAsync(cr);
            }

            await _versionRepository.DeleteAsync(version);
        }

        public async Task SetActiveVersionAsync(
            int courseId,
            int versionId,
            CourseVersionLearnerPolicy learnerPolicy = CourseVersionLearnerPolicy.NewLearnersOnly)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
                throw new KeyNotFoundException($"Course ID: {courseId} not found.");

            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null || version.CourseId != courseId)
                throw new KeyNotFoundException($"Version ID: {versionId} not found.");

            var wasAlreadyActive = version.IsActive;

            await EnsureVersionReadyForActivationAsync(versionId);

            var activeVersions = await _versionRepository.GetAsync(
                filter: v => v.CourseId == courseId && v.IsActive && v.Id != versionId
            );

            foreach (var oldVersion in activeVersions)
            {
                oldVersion.IsActive = false;
                await _versionRepository.UpdateAsync(oldVersion);
            }

            version.IsActive = true;
            await _versionRepository.UpdateAsync(version);
            await SetCourseActiveIfNeededAsync(course);

            if (!wasAlreadyActive)
            {
                await ApplyLearnerVersionPolicyAsync(courseId, versionId, learnerPolicy);
            }
        }

        private async Task ApplyLearnerVersionPolicyAsync(
            int courseId,
            int versionId,
            CourseVersionLearnerPolicy learnerPolicy)
        {
            if (learnerPolicy == CourseVersionLearnerPolicy.NewLearnersOnly)
            {
                return;
            }

            var eligibleEnrollments = await GetPolicyEligibleOpenEnrollmentsAsync(courseId);
            if (eligibleEnrollments.Count == 0)
            {
                return;
            }

            var startedEnrollmentIds = await GetStartedEnrollmentIdsAsync(eligibleEnrollments);
            var targetEnrollments = learnerPolicy == CourseVersionLearnerPolicy.MoveNotStarted
                ? eligibleEnrollments.Where(e => !IsStarted(e, startedEnrollmentIds)).ToList()
                : eligibleEnrollments;

            if (targetEnrollments.Count == 0)
            {
                return;
            }

            var now = _dateTime.Now;
            foreach (var enrollment in targetEnrollments)
            {
                enrollment.ResetAt = now;
                enrollment.EnrolledCourseVersion = versionId;
                enrollment.IsCompleted = false;
                enrollment.CompletedDate = null;
                enrollment.Progress = 0;
                enrollment.TotalScore = 0;
                enrollment.TotalTimeSpent = 0;
                enrollment.UpdatedAt = now;

                _enrollmentRepository.UpdateWithoutSave(enrollment);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<List<Enrollment>> GetPolicyEligibleOpenEnrollmentsAsync(int courseId)
        {
            var assignmentLinks = await _enrollmentAssignmentRepository.GetAsync(
                link => link.Enrollment != null && link.Enrollment.CourseId == courseId,
                includeProperties: "Enrollment,Assignment");

            return assignmentLinks
                .Where(IsInProgressAssignmentLink)
                .Select(link => link.Enrollment!)
                .Where(enrollment => !enrollment.IsCompleted)
                .GroupBy(GetEnrollmentKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private bool IsInProgressAssignmentLink(EnrollmentAssignment link)
        {
            if (link.IsDeleted || link.SnapshotCompleted || link.Assignment == null || link.Assignment.IsDeleted)
            {
                return false;
            }

            if (_currentUser.DivisionId.HasValue && link.Assignment.DivisionId != _currentUser.DivisionId.Value)
            {
                return false;
            }

            var now = _dateTime.Now;
            if (link.Assignment.StartDate.HasValue && link.Assignment.StartDate.Value > now)
            {
                return false;
            }

            if (link.Assignment.DueDate.HasValue && link.Assignment.DueDate.Value < now)
            {
                return false;
            }

            return true;
        }

        private async Task<HashSet<int>> GetStartedEnrollmentIdsAsync(IEnumerable<Enrollment> enrollments)
        {
            var enrollmentList = enrollments.Where(e => e.Id > 0).ToList();
            var enrollmentIds = enrollmentList.Select(e => e.Id).Distinct().ToList();
            if (enrollmentIds.Count == 0)
            {
                return [];
            }

            var resetMap = enrollmentList.ToDictionary(e => e.Id, e => e.ResetAt);
            var logs = await _learningLogRepository.GetAsync(log => enrollmentIds.Contains(log.EnrollmentId));

            return logs
                .Where(log => !log.IsDeleted)
                .Where(log => !resetMap.TryGetValue(log.EnrollmentId, out var resetAt)
                    || !resetAt.HasValue
                    || log.CreatedAt >= resetAt.Value)
                .Select(log => log.EnrollmentId)
                .ToHashSet();
        }

        private static bool IsStarted(Enrollment enrollment, HashSet<int> startedEnrollmentIds)
        {
            return enrollment.Progress > 0
                || enrollment.TotalScore > 0
                || enrollment.CompletedDate.HasValue
                || startedEnrollmentIds.Contains(enrollment.Id);
        }

        private static string GetEnrollmentKey(Enrollment enrollment)
        {
            return enrollment.Id > 0
                ? enrollment.Id.ToString()
                : $"{enrollment.StudentCode}:{enrollment.CourseId}";
        }

        private async Task<List<int>> BuildOrderedResourceIdsAsync(CreateCourseVersionDto model, List<IFormFile>? files)
        {
            var orderedResourceIds = new List<int>();
            if (model.ResourceIds == null || model.ResourceIds.Count == 0)
            {
                return orderedResourceIds;
            }

            var uploadedFiles = files ?? new List<IFormFile>();
            int fileIndex = 0;

            for (int index = 0; index < model.ResourceIds.Count; index++)
            {
                var resourceId = model.ResourceIds[index];
                var currentTypeId = model.ResourceTypes != null && model.ResourceTypes.Count > index
                    ? model.ResourceTypes[index]
                    : 1;

                if (resourceId == 0)
                {
                    if (fileIndex >= uploadedFiles.Count)
                    {
                        throw new InvalidOperationException("Cannot save this course version because an uploaded resource file is missing.");
                    }

                    var newResource = await ProcessNewResourceAsync(uploadedFiles[fileIndex], currentTypeId);
                    orderedResourceIds.Add(newResource.Id);
                    fileIndex++;
                }
                else if (resourceId > 0)
                {
                    orderedResourceIds.Add(resourceId);
                }
            }

            return orderedResourceIds;
        }

        private async Task ReplaceCourseVersionResourcesAsync(int versionId, IReadOnlyList<int> resourceIds)
        {
            var oldResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId
            );

            foreach (var oldResource in oldResources)
            {
                await _courseResourceRepository.DeleteAsync(oldResource);
            }

            int orderIndex = 1;
            foreach (var resourceId in resourceIds)
            {
                var courseResource = new CourseResource
                {
                    CourseVersionId = versionId,
                    ResourceId = resourceId,
                    Order = orderIndex++,
                    CreatedAt = DateTime.UtcNow
                };
                await _courseResourceRepository.AddAsync(courseResource);
            }
        }

        private async Task EnsureVersionReadyForActivationAsync(int versionId)
        {
            var courseResources = await _courseResourceRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId,
                includeProperties: "Resource"
            );

            var sortedResources = courseResources.OrderBy(cr => cr.Order).ToList();
            var issues = await GetResourceReadinessIssuesAsync(sortedResources, attemptAutoPrepare: true);

            if (sortedResources.Count == 0 || issues.Count > 0)
            {
                throw new InvalidOperationException(CourseContentReadiness.BuildActivationErrorMessage(sortedResources.Count, issues));
            }
        }

        private async Task EnsureResourceIdsReadyForActivationAsync(IReadOnlyList<int> resourceIds)
        {
            if (resourceIds.Count == 0)
            {
                throw new InvalidOperationException(CourseContentReadiness.BuildActivationErrorMessage(0, []));
            }

            var issues = await GetResourceReadinessIssuesAsync(resourceIds.Distinct().ToList(), attemptAutoPrepare: true);

            if (issues.Count > 0)
            {
                throw new InvalidOperationException(CourseContentReadiness.BuildActivationErrorMessage(resourceIds.Count, issues));
            }
        }

        private async Task<List<ResourceReadinessIssue>> GetResourceReadinessIssuesAsync(
            IEnumerable<CourseResource> courseResources,
            bool attemptAutoPrepare = false)
        {
            var issues = new List<ResourceReadinessIssue>();

            foreach (var courseResource in courseResources)
            {
                var resource = courseResource.Resource ?? await _resourceRepository.GetByIdAsync(courseResource.ResourceId);
                var issue = await GetResourceReadinessIssueAsync(resource, courseResource.ResourceId, attemptAutoPrepare);
                if (issue != null)
                {
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private async Task<List<ResourceReadinessIssue>> GetResourceReadinessIssuesAsync(
            IReadOnlyCollection<int> resourceIds,
            bool attemptAutoPrepare = false)
        {
            var issues = new List<ResourceReadinessIssue>();

            foreach (var resourceId in resourceIds)
            {
                var resource = await _resourceRepository.GetByIdAsync(resourceId);
                var issue = await GetResourceReadinessIssueAsync(resource, resourceId, attemptAutoPrepare);
                if (issue != null)
                {
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private async Task<ResourceReadinessIssue?> GetResourceReadinessIssueAsync(
            Resource? resource,
            int resourceId,
            bool attemptAutoPrepare)
        {
            var issue = CourseContentReadiness.GetResourceIssue(resource, resourceId);
            if (issue == null || !attemptAutoPrepare || resource == null)
            {
                return issue;
            }

            var preparationIssue = await TryPrepareResourceForActivationAsync(resource);
            if (preparationIssue != null)
            {
                return preparationIssue;
            }

            return CourseContentReadiness.GetResourceIssue(resource, resourceId);
        }

        private async Task SetCourseActiveIfNeededAsync(Course course)
        {
            if (!course.IsActive)
            {
                course.IsActive = true;
                await _courseRepository.UpdateAsync(course);
            }
        }

        private async Task SetCourseActiveIfNeededAsync(int courseId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course != null)
            {
                await SetCourseActiveIfNeededAsync(course);
            }
        }

        private async Task DeactivateCourseIfNoActiveVersionAsync(int courseId)
        {
            var activeVersions = await _versionRepository.GetAsync(v => v.CourseId == courseId && v.IsActive);
            if (activeVersions.Any())
            {
                return;
            }

            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course != null && course.IsActive)
            {
                course.IsActive = false;
                await _courseRepository.UpdateAsync(course);
            }
        }

        private async Task<ResourceReadinessIssue?> TryPrepareResourceForActivationAsync(Resource resource)
        {
            if (CourseContentReadiness.IsResourceReady(resource))
            {
                return null;
            }

            if (!resource.FileStorageId.HasValue)
            {
                return null;
            }

            var fileStorage = await _fileStorageRepository.GetByIdAsync(resource.FileStorageId.Value);
            if (fileStorage?.Data == null || fileStorage.Data.Length == 0)
            {
                return new ResourceReadinessIssue(resource.Id, resource.Name, "original SCORM package is missing");
            }

            var previousFolderName = resource.URL;

            try
            {
                var scormInfo = await _scormService.ExtractAndParseScormAsync(
                    fileStorage.Data,
                    Guid.NewGuid().ToString());

                resource.ResourceHref = scormInfo.ResourceHref;
                resource.SchemaVersion = scormInfo.SchemaVersion;
                resource.URL = scormInfo.FolderName;
                resource.IsActive = true;

                await _resourceRepository.UpdateAsync(resource);

                if (!string.IsNullOrWhiteSpace(previousFolderName)
                    && !string.Equals(previousFolderName, scormInfo.FolderName, StringComparison.OrdinalIgnoreCase))
                {
                    _scormService.DeleteScormFolder(previousFolderName);
                }

                return null;
            }
            catch (InvalidScormPackageException ex)
            {
                resource.IsActive = false;
                await _resourceRepository.UpdateAsync(resource);
                return new ResourceReadinessIssue(resource.Id, resource.Name, ex.Message);
            }
            catch (Exception ex)
            {
                resource.IsActive = false;
                await _resourceRepository.UpdateAsync(resource);
                return new ResourceReadinessIssue(resource.Id, resource.Name, $"automatic content preparation failed: {ex.Message}");
            }
        }

        private async Task<Resource> ProcessNewResourceAsync(IFormFile file, int typeId)
        {
            if (file == null || file.Length == 0)
                throw new InvalidScormPackageException("A SCORM package file is required.");

            ScormUploadValidation.EnsureValidScormPackageUpload(file);

            var safeFileName = ScormUploadValidation.NormalizeUploadedFileName(file.FileName);

            var fileStorage = new FileStorage
            {
                Name = safeFileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/zip" : file.ContentType,
                Length = file.Length
            };

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileStorage.Data = ms.ToArray();
            }

            var savedFile = await _fileStorageRepository.AddAsync(fileStorage);

            var resource = new Resource
            {
                Name = safeFileName,
                TypeId = typeId,
                IsActive = false,
                FileStorageId = savedFile.Id
            };

            var savedResource = await _resourceRepository.AddAsync(resource);

            try
            {
                string folderName = Guid.NewGuid().ToString();

                var scormInfo = await _scormService.ExtractAndParseScormAsync(
                    fileStorage.Data,
                    folderName
                );

                savedResource.ResourceHref = scormInfo.ResourceHref;
                savedResource.SchemaVersion = scormInfo.SchemaVersion;
                savedResource.URL = scormInfo.FolderName;
                savedResource.IsActive = true;

                await _resourceRepository.UpdateAsync(savedResource);
            }
            catch (InvalidScormPackageException)
            {
                savedResource.IsActive = false;
                await _resourceRepository.UpdateAsync(savedResource);
                throw;
            }

            return savedResource;
        }
    }
}