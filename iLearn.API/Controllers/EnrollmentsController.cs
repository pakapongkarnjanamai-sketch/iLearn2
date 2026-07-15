using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.API.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IEnrollmentService _enrollmentAdminService;
        private readonly IGenericRepository<LearningLog> _logRepo;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly IScormService _scormService;
        private readonly IDateTime _dateTime;
        private readonly IMemoryCache _cache;
        private readonly ILearnerProxyIdentityResolver _learnerProxyIdentityResolver;
        private readonly IScormRuntimeStateService _scormRuntimeStateService;
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUser;

        public EnrollmentsController(
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<Course> courseRepo,
            IEnrollmentService enrollmentAdminService,
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<CourseVersion> versionRepo,
            IScormService scormService,
            IDateTime dateTime,
            IMemoryCache cache,
            ILearnerProxyIdentityResolver learnerProxyIdentityResolver,
            IScormRuntimeStateService scormRuntimeStateService,
            INotificationService notificationService,
            ICurrentUserService currentUser)
        {
            _enrollmentRepo = enrollmentRepo;
            _courseRepo = courseRepo;
            _enrollmentAdminService = enrollmentAdminService;
            _logRepo = logRepo;
            _versionRepo = versionRepo;
            _scormService = scormService;
            _dateTime = dateTime;
            _cache = cache;
            _learnerProxyIdentityResolver = learnerProxyIdentityResolver;
            _scormRuntimeStateService = scormRuntimeStateService;
            _notificationService = notificationService;
            _currentUser = currentUser;
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("ResetStatus")]
        public async Task<IActionResult> ResetStatus([FromQuery] int key)
        {
            var result = await _enrollmentAdminService.ResetStatusAsync(key);
            if (result == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            AdminSummaryStatsCache.InvalidateEnrollments(_cache);
            return Ok(new { success = true });
        }

        [AllowAnonymous]
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            if (!TryGetTrustedLearnerLearnerCode(out var learnerCode, out var errorResult))
            {
                return errorResult;
            }

            var currentDate = _dateTime.Now;

            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => !e.IsDeleted && e.LearnerCode == learnerCode && e.Course != null
                    && (e.Course.Status == CourseStatus.Open || e.Course.Status == CourseStatus.Closed),
                includeProperties: "Course.CourseType,Course.Versions.CourseContentItems.ContentItem,AssignmentLinks.Assignment",
                ignoreQueryFilters: true
            );

            var filtered = enrollments.Where(e =>
            {
                if (!IsEnrollmentContentReady(e))
                {
                    return false;
                }

                var schedule = GetEffectiveSchedule(e);
                if (!schedule.ShouldBeVisible)
                {
                    return false;
                }

                if (e.IsCompleted)
                {
                    return EnrollmentVisibilityPolicy.ShouldShowCompletedEnrollment(e.CompletedDate, currentDate);
                }

                bool startOk = !schedule.StartDate.HasValue || schedule.StartDate <= currentDate;
                bool dueOk   = !schedule.DueDate.HasValue || schedule.DueDate >= currentDate;
                return startOk && dueOk;
            }).ToList();

            var dtos = filtered
                .OrderBy(e => e.IsCompleted)
                .ThenBy(e => GetEffectiveSchedule(e).DueDate)
                .Select(e =>
                {
                    var dto = e.ToDto();
                    var schedule = GetEffectiveSchedule(e);
                    return new LearnerEnrollmentDto
                    {
                        Id = dto.Id,
                        CourseId = dto.CourseId,
                        CourseCode = dto.CourseCode,
                        CourseTitle = dto.CourseTitle,
                        EnrolledCourseVersion = dto.EnrolledCourseVersion,
                        IsCompleted = dto.IsCompleted,
                        StartDate = schedule.StartDate,
                        DueDate = schedule.DisplayDueDate,
                        CompletedDate = dto.CompletedDate,
                        Progress = dto.Progress,
                        CourseTypeName = dto.CourseTypeName
                    };
                })
                .ToList();

            return Ok(new ApiResponse<IEnumerable<LearnerEnrollmentDto>>
            {
                Success = true,
                Data    = dtos
            });
        }

        [AllowAnonymous]
        [HttpGet("course-catalog")]
        public async Task<IActionResult> GetCourseCatalog([FromQuery] string? divisionName = null)
        {
            if (!TryGetTrustedLearnerLearnerCode(out _, out var errorResult))
            {
                return errorResult;
            }

            if (string.IsNullOrWhiteSpace(divisionName))
            {
                return Ok(new ApiResponse<IEnumerable<LearnerCourseCatalogDto>>
                {
                    Success = true,
                    Data = Array.Empty<LearnerCourseCatalogDto>()
                });
            }

            var normalizedDivisionName = divisionName.Trim();

            var courses = await _courseRepo.GetAsync(
                filter: c => !c.IsDeleted
                    && c.Status == CourseStatus.Open
                    && c.Category != null
                    && c.Category.Division != null
                    && c.Category.Division.Name == normalizedDivisionName,
                includeProperties: "Category,Category.Division,CourseType"
            );

            var catalogItems = courses
                .OrderBy(c => c.Code)
                .ThenBy(c => c.Title)
                .Select(c => new LearnerCourseCatalogDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Title = c.Title,
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category?.Name ?? "ไม่ระบุหมวดหมู่",
                    CourseTypeId = c.CourseTypeId,
                    CourseTypeName = c.CourseType?.Name ?? "ไม่ระบุประเภท",
                    CoverImageUrl = null
                })
                .ToList();

            return Ok(new ApiResponse<IEnumerable<LearnerCourseCatalogDto>>
            {
                Success = true,
                Data = catalogItems
            });
        }

        [AllowAnonymous]
        [HttpGet("player-info/{courseId}")]
        public async Task<IActionResult> GetPlayerInfoByCourse(int courseId)
        {
            if (!TryGetTrustedLearnerLearnerCode(out var learnerCode, out var errorResult))
            {
                return errorResult;
            }

            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.CourseId == courseId && e.LearnerCode == learnerCode,
                includeProperties: "Course.CourseType,Course.Category"
            );
            var enrollment = enrollments.FirstOrDefault();

            CourseVersion? targetVersion = null;
            bool isReadOnly = false;
            bool isCompleted = false;
            List<LearningLog> userLogs = new();
            Dictionary<int, ScormRuntimeStateDto> runtimeStateMap = new();

            if (enrollment != null)
            {
                var targetVersionId = enrollment.EnrolledCourseVersion;
                isCompleted = enrollment.IsCompleted;

                var versions = await _versionRepo.GetAsync(
                    filter: v => v.CourseId == courseId && v.Id == targetVersionId,
                    includeProperties: "CourseContentItems.ContentItem,Course.CourseType,Course.Category"
                );
                targetVersion = versions.FirstOrDefault();

                if (targetVersion != null)
                {
                    userLogs = (await _logRepo.GetAsync(l =>
                        l.LearnerCode     == learnerCode       &&
                        l.CourseVersionId == targetVersion.Id  &&
                        l.EnrollmentId    == enrollment.Id     &&
                        (enrollment.ResetAt == null || l.CreatedAt >= enrollment.ResetAt)
                    )).ToList();

                    runtimeStateMap = (await _scormRuntimeStateService.GetActiveStatesAsync(enrollment.Id, enrollment.ResetAt))
                        .ToDictionary(state => state.ContentItemId);
                }
            }
            else
            {
                isReadOnly = true;

                var activeVersions = await _versionRepo.GetAsync(
                    filter: v => v.CourseId == courseId && v.IsActive && v.Course != null && v.Course.Status == CourseStatus.Open,
                    includeProperties: "CourseContentItems.ContentItem,Course.CourseType,Course.Category"
                );
                targetVersion = activeVersions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            }

            if (targetVersion == null)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = "Content not found or Course is not active" });
            }

            var canAccessCourse = enrollment != null
                ? targetVersion.Course?.Status == CourseStatus.Open || targetVersion.Course?.Status == CourseStatus.Closed
                : targetVersion.Course?.Status == CourseStatus.Open;

            if (!canAccessCourse || !CourseContentReadiness.IsVersionReady(targetVersion.CourseContentItems))
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = "Content is not ready for learning." });
            }

            var contentItems = targetVersion.CourseContentItems
                .Where(cr => CourseContentReadiness.IsContentItemReady(cr.ContentItem))
                .OrderBy(cr => cr.ContentItem!.TypeId == 1 ? 0 : 1) // Learn first
                .ThenBy(cr => cr.ContentItem!.Name)
                .Select(cr => {
                    var contentItem = cr.ContentItem!;
                    var log = userLogs.FirstOrDefault(l => l.ContentItemId == contentItem.Id);
                    runtimeStateMap.TryGetValue(contentItem.Id, out var runtimeState);
                    var contentItemType = contentItem.TypeId == 2 ? "Exam" : "Learn";
                    var scormVersion = runtimeState?.ScormVersion ?? ScormRuntimeFieldMap.NormalizeVersion(contentItem.SchemaVersion);
                    bool isDone = log != null && (
                        log.Status.ToLower() == "completed" ||
                        log.Status.ToLower() == "passed"
                    );
                    var status = ResolvePlayerContentItemStatus(contentItem.TypeId, log, runtimeState, isDone);

                    return new PlayerContentItemDto
                    {
                        Id = contentItem.Id,
                        Name = contentItem.Name,
                        Type = contentItemType,
                        LaunchUrl = !string.IsNullOrEmpty(contentItem.URL) && !string.IsNullOrEmpty(contentItem.LaunchHref)
                            ? _scormService.GetScormUrl(contentItem.URL, contentItem.LaunchHref)
                            : contentItem.URL ?? string.Empty,
                        ScormVersion = scormVersion,
                        Status = status,
                        Progress = ResolvePlayerContentItemCompletionProgress(status),
                        ActivityProgress = ResolvePlayerContentItemActivityProgress(contentItemType, status, log, runtimeState, isDone),
                        IsCompleted = isDone,
                        Score = ResolvePlayerContentItemScore(log, runtimeState),
                        Time = ResolvePlayerContentItemTime(log, runtimeState),
                        TotalSecondsPlayed = log?.TotalSecondsPlayed ?? 0,
                        RuntimeState = runtimeState
                    };
                })
                .ToList();

            var dto = new PlayerInfoDto
            {
                CourseVersionId = targetVersion.Id,
                CourseTitle = targetVersion.Course?.Title ?? "Unknown Course",
                CategoryName = targetVersion.Course?.Category?.Name ?? "ไม่ระบุหมวดหมู่",
                CourseTypeName = targetVersion.Course?.CourseType?.Name ?? "ไม่ระบุประเภท",
                Progress = enrollment?.Progress ?? 0,
                IsCompleted = isCompleted,
                IsReadOnly = isReadOnly,
                EnrollmentId = enrollment?.Id,
                ContentItems = contentItems
            };

            return Ok(new ApiResponse<PlayerInfoDto> { Success = true, Data = dto });
        }

        private static string ResolvePlayerContentItemStatus(
            int contentItemTypeId,
            LearningLog? log,
            ScormRuntimeStateDto? runtimeState,
            bool isDone)
        {
            return ScormContentStatusPolicy.ResolveStatus(
                contentItemTypeId,
                runtimeState?.LessonStatus,
                runtimeState?.CompletionStatus,
                runtimeState?.SuccessStatus,
                log?.Status,
                isDone);
        }

        private static double ResolvePlayerContentItemCompletionProgress(string status)
        {
            return ScormContentStatusPolicy.ResolveCompletionProgress(status);
        }

        private static double ResolvePlayerContentItemActivityProgress(
            string contentItemType,
            string status,
            LearningLog? log,
            ScormRuntimeStateDto? runtimeState,
            bool isDone)
        {
            if (status is "passed" or "completed" or "failed")
            {
                return 100;
            }

            if (string.Equals(contentItemType, "Learn", StringComparison.OrdinalIgnoreCase) && runtimeState?.RawScore is decimal rawScore && rawScore > 0)
            {
                return ClampProgress((double)rawScore);
            }

            if (log?.Progress > 0)
            {
                return ClampProgress(log.Progress);
            }

            return isDone ? 100 : 0;
        }

        private static decimal? ResolvePlayerContentItemScore(LearningLog? log, ScormRuntimeStateDto? runtimeState)
        {
            if (runtimeState?.RawScore != null)
            {
                return runtimeState.RawScore.Value;
            }

            return log?.Score;
        }

        private static string ResolvePlayerContentItemTime(LearningLog? log, ScormRuntimeStateDto? runtimeState)
        {
            if (!string.IsNullOrWhiteSpace(runtimeState?.SessionTime))
            {
                return runtimeState.SessionTime;
            }

            if (!string.IsNullOrWhiteSpace(log?.SessionTime))
            {
                return log.SessionTime;
            }

            return "00:00:00";
        }

        private static double ClampProgress(double value)
        {
            return Math.Round(Math.Max(0, Math.Min(100, value)), 2);
        }

        private bool TryGetTrustedLearnerLearnerCode(out string learnerCode, out IActionResult errorResult)
        {
            if (_learnerProxyIdentityResolver.TryResolveLearnerCode(HttpContext, out learnerCode, out var statusCode, out var errorMessage))
            {
                errorResult = null!;
                return true;
            }

            errorResult = StatusCode(statusCode, new ApiResponse<string>
            {
                Success = false,
                Message = errorMessage
            });

            return false;
        }

        private static bool IsEnrollmentContentReady(Enrollment enrollment)
        {
            if (enrollment.Course?.Status != CourseStatus.Open && enrollment.Course?.Status != CourseStatus.Closed)
            {
                return false;
            }

            var targetVersion = ResolveEnrollmentTargetVersion(enrollment);
            return targetVersion != null && CourseContentReadiness.IsVersionReady(targetVersion.CourseContentItems);
        }

        private static CourseVersion? ResolveEnrollmentTargetVersion(Enrollment enrollment)
        {
            var versions = enrollment.Course?.Versions;
            if (versions == null || versions.Count == 0)
            {
                return null;
            }

            if (enrollment.EnrolledCourseVersion.HasValue)
            {
                return versions.FirstOrDefault(v => v.Id == enrollment.EnrolledCourseVersion.Value);
            }

            return versions
                .Where(v => v.IsActive)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dto = await _enrollmentAdminService.GetByIdAsync(id);
            if (dto == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Not Found" });
            return Ok(new ApiResponse<EnrollmentDto> { Success = true, Data = dto });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id}/completion")]
        public async Task<IActionResult> UpdateCompletion(int id, [FromBody] bool isComplete)
        {
            var dto = await _enrollmentAdminService.UpdateCompletionAsync(id, isComplete);
            if (dto == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Not Found" });

            AdminSummaryStatsCache.InvalidateEnrollments(_cache);
            return Ok(new ApiResponse<EnrollmentDto> { Success = true, Data = dto });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("BulkAssign")]
        public async Task<IActionResult> BulkAssign([FromBody] BulkAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var result = await _enrollmentAdminService.BulkAssignAsync(dto);
            if (!result.Success)
            {
                if (result.ErrorType == "Forbid")
                {
                    return Forbid();
                }
                if (result.ErrorType == "Conflict")
                {
                    return Conflict(new
                    {
                        message = result.ErrorMessage,
                        inProgressConflicts = result.InProgressConflicts,
                        completedConflicts = result.CompletedConflicts
                    });
                }
                return BadRequest(new { message = result.ErrorMessage });
            }

            AdminSummaryStatsCache.InvalidateEnrollments(_cache);

            await _notificationService.NotifyAsync(
                _currentUser.UserId,
                NotificationTypes.BulkAssignCompleted,
                NotificationLevels.Success,
                "มอบหมายคอร์สสำเร็จ",
                message: $"มอบหมาย {dto.CourseIds?.Count ?? 0} คอร์สให้ผู้เรียน (เลขที่ {result.AssignmentNo})",
                linkPath: $"/assignments/{result.AssignmentId}",
                entityType: "Assignment",
                entityId: result.AssignmentId);

            return Ok(new
            {
                message = "Courses assigned successfully!",
                assignmentNo = result.AssignmentNo,
                assignmentId = result.AssignmentId
            });
        }

        private static List<EnrollmentAssignment> GetActiveLinks(Enrollment enrollment)
        {
            return enrollment.AssignmentLinks
                .Where(ea => !ea.IsDeleted && ea.Assignment != null && !ea.Assignment.IsDeleted)
                .ToList();
        }

        private static EnrollmentSchedule GetEffectiveSchedule(Enrollment enrollment)
        {
            var activeLinks = GetActiveLinks(enrollment);
            var hadDeletedAssignmentOnly = enrollment.AssignmentLinks.Any() && activeLinks.Count == 0;

            if (hadDeletedAssignmentOnly)
            {
                return new EnrollmentSchedule
                {
                    ShouldBeVisible = false,
                    StartDate = enrollment.StartDate,
                    DueDate = enrollment.DueDate,
                    DisplayDueDate = enrollment.DueDate
                };
            }

            if (activeLinks.Count == 0)
            {
                return new EnrollmentSchedule
                {
                    ShouldBeVisible = true,
                    StartDate = enrollment.StartDate,
                    DueDate = enrollment.DueDate,
                    DisplayDueDate = enrollment.DueDate
                };
            }

            return new EnrollmentSchedule
            {
                ShouldBeVisible = true,
                StartDate = activeLinks.Min(a => a.StartDate),
                DueDate = activeLinks.Max(a => a.DueDate),
                DisplayDueDate = activeLinks.Min(a => a.DueDate)
            };
        }

        private sealed class EnrollmentSchedule
        {
            public bool ShouldBeVisible { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? DisplayDueDate { get; set; }
        }
    }
}