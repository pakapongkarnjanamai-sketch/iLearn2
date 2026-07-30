using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
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
        private readonly IGenericRepository<CourseContentItem> _courseContentItemRepository;
        private readonly IGenericRepository<ContentItem> _contentItemRepository;
        private readonly IGenericRepository<FileStorage> _fileStorageRepository;
        private readonly IGenericRepository<Enrollment> _enrollmentRepository;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepository;
        private readonly IGenericRepository<LearningLog> _learningLogRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IScormService _scormService;
        private readonly IAdminActivityService _adminActivityService;
        private readonly ICurrentUserService _currentUser;
        private readonly IScormRuntimeStateService _scormRuntimeStateService;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;

        public CourseVersionService(
            IGenericRepository<CourseVersion> versionRepository,
            IGenericRepository<CourseContentItem> courseContentItemRepository,
            IGenericRepository<ContentItem> contentItemRepository,
            IGenericRepository<FileStorage> fileStorageRepository,
            IGenericRepository<Enrollment> enrollmentRepository,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepository,
            IGenericRepository<LearningLog> learningLogRepository,
            ICourseRepository courseRepository,
            IScormService scormService,
            IAdminActivityService adminActivityService,
            ICurrentUserService currentUser,
            IScormRuntimeStateService scormRuntimeStateService,
            IDateTime dateTime,
            IUnitOfWork unitOfWork)
        {
            _versionRepository = versionRepository;
            _courseContentItemRepository = courseContentItemRepository;
            _contentItemRepository = contentItemRepository;
            _fileStorageRepository = fileStorageRepository;
            _enrollmentRepository = enrollmentRepository;
            _enrollmentAssignmentRepository = enrollmentAssignmentRepository;
            _learningLogRepository = learningLogRepository;
            _courseRepository = courseRepository;
            _scormService = scormService;
            _adminActivityService = adminActivityService;
            _currentUser = currentUser;
            _scormRuntimeStateService = scormRuntimeStateService;
            _dateTime = dateTime;
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateCourseVersionDto> GetVersionByIdAsync(int versionId)
        {
            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null)
                throw new KeyNotFoundException($"Version ID: {versionId} not found.");

            var courseContentItems = await _courseContentItemRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId
            );

            var sortedContentItems = courseContentItems.OrderBy(cr => cr.Order).ToList();

            return new CreateCourseVersionDto
            {
                CourseId = version.CourseId,
                Note = version.Note,
                IsActive = version.IsActive,
                ContentItemIds = sortedContentItems.Select(cr => cr.ContentItemId).ToList()
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
                var courseContentItems = await _courseContentItemRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == version.Id,
                    includeProperties: "ContentItem"
                );

                var sortedCourseContentItems = courseContentItems.OrderBy(cr => cr.Order).ToList();

                var versionDto = new CourseVersionDto
                {
                    Id = version.Id,
                    CourseId = version.CourseId,
                    VersionNumber = version.VersionNumber,
                    Note = version.Note,
                    IsActive = version.IsActive,
                    CreatedAt = version.CreatedAt,
                    ContentItems = sortedCourseContentItems.Select(cr => new CourseContentItemDto
                    {
                        Id = cr.ContentItem?.Id ?? 0,
                        Name = cr.ContentItem?.Name ?? "Unknown",
                        TypeId = cr.ContentItem?.TypeId ?? 0,
                        TypeName = cr.ContentItem?.TypeId == 1 ? "Learn" : "Exam",
                        IsActive = cr.ContentItem?.IsActive ?? false,
                        URL = cr.ContentItem?.URL
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

            var courseContentItems = await _courseContentItemRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId,
                includeProperties: "ContentItem"
            );

            var sortedContentItems = courseContentItems.OrderBy(cr => cr.Order).ToList();
            var issues = await GetContentItemReadinessIssuesAsync(sortedContentItems);

            return new CourseVersionReadinessDto
            {
                VersionId = versionId,
                ContentItemCount = sortedContentItems.Count,
                IsReady = sortedContentItems.Count > 0 && issues.Count == 0,
                Issues = issues.Select(issue => new CourseVersionReadinessIssueDto
                {
                    ContentItemId = issue.ContentItemId,
                    ContentItemName = issue.ContentItemName,
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
                CreatedAt = _dateTime.Now
            };

            await _versionRepository.AddAsync(newVersion);

            var orderedContentItemIds = await BuildOrderedContentItemIdsAsync(model, files);
            await ReplaceCourseVersionContentItemsAsync(newVersion.Id, orderedContentItemIds);

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

            var courseContentItemsForNew = await _courseContentItemRepository.GetAsync(
                filter: cr => cr.CourseVersionId == newVersion.Id,
                includeProperties: "ContentItem"
            );

            var sortedContentItems = courseContentItemsForNew.OrderBy(cr => cr.Order).ToList();

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
                ContentItems = sortedContentItems.Select(cr => new CourseContentItemDto
                {
                    Id = cr.ContentItem?.Id ?? 0,
                    Name = cr.ContentItem?.Name ?? "Unknown",
                    TypeId = cr.ContentItem?.TypeId ?? 0,
                    TypeName = cr.ContentItem?.TypeId == 1 ? "Learn" : "Exam",
                    IsActive = cr.ContentItem?.IsActive ?? false,
                    URL = cr.ContentItem?.URL
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
            var orderedContentItemIds = await BuildOrderedContentItemIdsAsync(model, files);

            if (model.IsActive)
            {
                await EnsureContentItemIdsReadyForActivationAsync(orderedContentItemIds);
            }

            version.Note = model.Note;

            await ReplaceCourseVersionContentItemsAsync(versionId, orderedContentItemIds);

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

            var courseContentItems = await _courseContentItemRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId,
                includeProperties: "ContentItem"
            );

            var sortedContentItems = courseContentItems.OrderBy(cr => cr.Order).ToList();

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
                ContentItems = sortedContentItems.Select(cr => new CourseContentItemDto
                {
                    Id = cr.ContentItem?.Id ?? 0,
                    Name = cr.ContentItem?.Name ?? "Unknown",
                    TypeId = cr.ContentItem?.TypeId ?? 0,
                    TypeName = cr.ContentItem?.TypeId == 1 ? "Learn" : "Exam",
                    IsActive = cr.ContentItem?.IsActive ?? false,
                    URL = cr.ContentItem?.URL
                }).ToList()
            };
        }

        public async Task DeleteVersionAsync(int versionId)
        {
            var version = await _versionRepository.GetByIdAsync(versionId);
            if (version == null)
                throw new KeyNotFoundException($"Version ID: {versionId} not found.");

            var courseContentItems = await _courseContentItemRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId
            );

            foreach (var cr in courseContentItems)
            {
                await _courseContentItemRepository.DeleteAsync(cr);
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

            await _scormRuntimeStateService.ClearForEnrollmentsAsync(
                targetEnrollments.Select(enrollment => enrollment.Id).ToList(),
                saveChanges: false);
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
                : $"{enrollment.LearnerCode}:{enrollment.CourseId}";
        }

        private async Task<List<int>> BuildOrderedContentItemIdsAsync(CreateCourseVersionDto model, List<IFormFile>? files)
        {
            var orderedContentItemIds = new List<int>();
            if (model.ContentItemIds == null || model.ContentItemIds.Count == 0)
            {
                return orderedContentItemIds;
            }

            var uploadedFiles = files ?? new List<IFormFile>();
            int fileIndex = 0;

            for (int index = 0; index < model.ContentItemIds.Count; index++)
            {
                var contentItemId = model.ContentItemIds[index];
                var currentTypeId = model.ContentTypeIds != null && model.ContentTypeIds.Count > index
                    ? model.ContentTypeIds[index]
                    : 1;

                if (contentItemId == 0)
                {
                    if (fileIndex >= uploadedFiles.Count)
                    {
                        throw new InvalidOperationException("Cannot save this course version because an uploaded contentItem file is missing.");
                    }

                    var newContentItem = await ProcessNewContentItemAsync(uploadedFiles[fileIndex], currentTypeId);
                    orderedContentItemIds.Add(newContentItem.Id);
                    fileIndex++;
                }
                else if (contentItemId > 0)
                {
                    orderedContentItemIds.Add(contentItemId);
                }
            }

            return orderedContentItemIds;
        }

        private async Task ReplaceCourseVersionContentItemsAsync(int versionId, IReadOnlyList<int> contentItemIds)
        {
            var oldContentItems = await _courseContentItemRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId
            );

            foreach (var oldContentItem in oldContentItems)
            {
                await _courseContentItemRepository.DeleteAsync(oldContentItem);
            }

            int orderIndex = 1;
            foreach (var contentItemId in contentItemIds)
            {
                var courseContentItem = new CourseContentItem
                {
                    CourseVersionId = versionId,
                    ContentItemId = contentItemId,
                    Order = orderIndex++,
                    CreatedAt = _dateTime.Now
                };
                await _courseContentItemRepository.AddAsync(courseContentItem);
            }
        }

        private async Task EnsureVersionReadyForActivationAsync(int versionId)
        {
            var courseContentItems = await _courseContentItemRepository.GetAsync(
                filter: cr => cr.CourseVersionId == versionId,
                includeProperties: "ContentItem"
            );

            var sortedContentItems = courseContentItems.OrderBy(cr => cr.Order).ToList();
            var issues = await GetContentItemReadinessIssuesAsync(sortedContentItems, attemptAutoPrepare: true);

            if (sortedContentItems.Count == 0 || issues.Count > 0)
            {
                throw new InvalidOperationException(CourseContentReadiness.BuildActivationErrorMessage(sortedContentItems.Count, issues));
            }
        }

        private async Task EnsureContentItemIdsReadyForActivationAsync(IReadOnlyList<int> contentItemIds)
        {
            if (contentItemIds.Count == 0)
            {
                throw new InvalidOperationException(CourseContentReadiness.BuildActivationErrorMessage(0, []));
            }

            var issues = await GetContentItemReadinessIssuesAsync(contentItemIds.Distinct().ToList(), attemptAutoPrepare: true);

            if (issues.Count > 0)
            {
                throw new InvalidOperationException(CourseContentReadiness.BuildActivationErrorMessage(contentItemIds.Count, issues));
            }
        }

        private async Task<List<ContentItemReadinessIssue>> GetContentItemReadinessIssuesAsync(
            IEnumerable<CourseContentItem> courseContentItems,
            bool attemptAutoPrepare = false)
        {
            var issues = new List<ContentItemReadinessIssue>();

            foreach (var courseContentItem in courseContentItems)
            {
                var contentItem = courseContentItem.ContentItem ?? await _contentItemRepository.GetByIdAsync(courseContentItem.ContentItemId);
                var issue = await GetContentItemReadinessIssueAsync(contentItem, courseContentItem.ContentItemId, attemptAutoPrepare);
                if (issue != null)
                {
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private async Task<List<ContentItemReadinessIssue>> GetContentItemReadinessIssuesAsync(
            IReadOnlyCollection<int> contentItemIds,
            bool attemptAutoPrepare = false)
        {
            var issues = new List<ContentItemReadinessIssue>();

            foreach (var contentItemId in contentItemIds)
            {
                var contentItem = await _contentItemRepository.GetByIdAsync(contentItemId);
                var issue = await GetContentItemReadinessIssueAsync(contentItem, contentItemId, attemptAutoPrepare);
                if (issue != null)
                {
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private async Task<ContentItemReadinessIssue?> GetContentItemReadinessIssueAsync(
            ContentItem? contentItem,
            int contentItemId,
            bool attemptAutoPrepare)
        {
            var issue = CourseContentReadiness.GetContentItemIssue(contentItem, contentItemId);
            if (issue == null || !attemptAutoPrepare || contentItem == null)
            {
                return issue;
            }

            var preparationIssue = await TryPrepareContentItemForActivationAsync(contentItem);
            if (preparationIssue != null)
            {
                return preparationIssue;
            }

            return CourseContentReadiness.GetContentItemIssue(contentItem, contentItemId);
        }

        private async Task SetCourseActiveIfNeededAsync(Course course)
        {
            if (course.Status == CourseStatus.Draft)
            {
                course.Status = CourseStatus.Open;
                course.IsActive = true;
                await _courseRepository.UpdateAsync(course);
                return;
            }

            if (course.Status == CourseStatus.Open && !course.IsActive)
            {
                course.IsActive = true;
                await _courseRepository.UpdateAsync(course);
                return;
            }

            if (course.Status == CourseStatus.Closed && course.IsActive)
            {
                course.IsActive = false;
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
            if (course != null && course.Status == CourseStatus.Open)
            {
                course.Status = CourseStatus.Draft;
                course.IsActive = false;
                await _courseRepository.UpdateAsync(course);
            }
        }

        private async Task<ContentItemReadinessIssue?> TryPrepareContentItemForActivationAsync(ContentItem contentItem)
        {
            if (CourseContentReadiness.IsContentItemReady(contentItem))
            {
                return null;
            }

            if (!contentItem.FileStorageId.HasValue)
            {
                return null;
            }

            var fileStorage = await _fileStorageRepository.GetByIdAsync(contentItem.FileStorageId.Value);
            if (fileStorage == null)
            {
                return new ContentItemReadinessIssue(contentItem.Id, contentItem.Name, "original SCORM package is missing");
            }

            // Determine source: prefer StoragePath (disk), fallback to Data (legacy DB blob)
            bool useStoragePath = !string.IsNullOrWhiteSpace(fileStorage.StoragePath);
            if (!useStoragePath && (fileStorage.Data == null || fileStorage.Data.Length == 0))
            {
                return new ContentItemReadinessIssue(contentItem.Id, contentItem.Name, "original SCORM package is missing");
            }

            var previousFolderName = contentItem.URL;

            try
            {
                var folderName = Guid.NewGuid().ToString();
                ScormManifestDto scormInfo;

                if (useStoragePath)
                {
                    var archiveFullPath = _scormService.GetArchiveFullPath(fileStorage.StoragePath!);
                    scormInfo = await _scormService.ExtractAndParseScormFromFileAsync(archiveFullPath, folderName);
                }
                else
                {
                    scormInfo = await _scormService.ExtractAndParseScormAsync(fileStorage.Data!, folderName);
                }

                contentItem.LaunchHref = scormInfo.LaunchHref;
                contentItem.SchemaVersion = scormInfo.SchemaVersion;
                contentItem.URL = scormInfo.FolderName;
                contentItem.IsActive = true;

                await _contentItemRepository.UpdateAsync(contentItem);

                if (!string.IsNullOrWhiteSpace(previousFolderName)
                    && !string.Equals(previousFolderName, scormInfo.FolderName, StringComparison.OrdinalIgnoreCase))
                {
                    _scormService.DeleteScormFolder(previousFolderName);
                }

                return null;
            }
            catch (InvalidScormPackageException ex)
            {
                contentItem.IsActive = false;
                await _contentItemRepository.UpdateAsync(contentItem);
                return new ContentItemReadinessIssue(contentItem.Id, contentItem.Name, ex.Message);
            }
            catch (Exception ex)
            {
                contentItem.IsActive = false;
                await _contentItemRepository.UpdateAsync(contentItem);
                return new ContentItemReadinessIssue(contentItem.Id, contentItem.Name, $"automatic content preparation failed: {ex.Message}");
            }
        }

        private async Task<ContentItem> ProcessNewContentItemAsync(IFormFile file, int typeId)
        {
            if (file == null || file.Length == 0)
                throw new InvalidScormPackageException("A SCORM package file is required.");

            ScormUploadValidation.EnsureValidScormPackageUpload(file);

            var safeFileName = ScormUploadValidation.NormalizeUploadedFileName(file.FileName);
            var archiveGuid = Guid.NewGuid().ToString();
            var archiveFileName = $"{archiveGuid}.zip";

            // Stream file directly to disk archive (no memory buffer)
            string storagePath;
            using (var stream = file.OpenReadStream())
            {
                storagePath = await _scormService.SavePackageToArchiveAsync(stream, archiveFileName);
            }

            var fileStorage = new FileStorage
            {
                Name = safeFileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/zip" : file.ContentType,
                Length = file.Length,
                StoragePath = storagePath,
                Data = null
            };

            var savedFile = await _fileStorageRepository.AddAsync(fileStorage);

            var contentItem = new ContentItem
            {
                Name = safeFileName,
                TypeId = typeId,
                IsActive = false,
                FileStorageId = savedFile.Id,
                CachedFileLength = file.Length
            };

            var savedContentItem = await _contentItemRepository.AddAsync(contentItem);

            try
            {
                string folderName = Guid.NewGuid().ToString();
                var archiveFullPath = _scormService.GetArchiveFullPath(storagePath);

                var scormInfo = await _scormService.ExtractAndParseScormFromFileAsync(
                    archiveFullPath,
                    folderName
                );

                savedContentItem.LaunchHref = scormInfo.LaunchHref;
                savedContentItem.SchemaVersion = scormInfo.SchemaVersion;
                savedContentItem.URL = scormInfo.FolderName;
                savedContentItem.IsActive = true;

                await _contentItemRepository.UpdateAsync(savedContentItem);
            }
            catch (InvalidScormPackageException)
            {
                // Upload failed validation — roll back everything we created so no orphaned
                // archive file (up to 1 GB) or dangling DB rows are left behind. The archive
                // was written to disk before extraction, and both rows were already committed
                // (AddAsync saves immediately), so we must clean them up explicitly.
                _scormService.DeleteArchiveFile(storagePath);
                try
                {
                    await _contentItemRepository.HardDeleteAsync(savedContentItem);
                    await _fileStorageRepository.HardDeleteAsync(savedFile);
                }
                catch (Exception cleanupEx)
                {
                    // Don't let a cleanup failure mask the original InvalidScormPackageException.
                    Console.WriteLine($"Warning: cleanup after failed SCORM upload incomplete: {cleanupEx.Message}");
                }
                throw;
            }

            return savedContentItem;
        }
    }
}