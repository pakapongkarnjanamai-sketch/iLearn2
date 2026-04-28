using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.API.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LearningLogsController : ControllerBase
    {
        private readonly IGenericRepository<LearningLog> _logRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly IMemoryCache _cache;
        private readonly ILearnerProxyIdentityResolver _learnerProxyIdentityResolver;
        private readonly IScormRuntimeStateService _scormRuntimeStateService;
        private readonly IDateTime _dateTime;
        public LearningLogsController(
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<CourseVersion> versionRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            ICurrentUserService currentUserService,
            IMemoryCache cache,
            ILearnerProxyIdentityResolver learnerProxyIdentityResolver,
            IScormRuntimeStateService scormRuntimeStateService,
            IDateTime dateTime)
        {
            _logRepo = logRepo;
            _enrollmentRepo = enrollmentRepo;
            _versionRepo = versionRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _currentUser = currentUserService;
            _cache = cache;
            _learnerProxyIdentityResolver = learnerProxyIdentityResolver;
            _scormRuntimeStateService = scormRuntimeStateService;
            _dateTime = dateTime;
        }

        [AllowAnonymous]
        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressDto input)
        {
            if (!TryResolveTrustedLearnerStudentCode(out var studentCode, out var errorResult))
            {
                return errorResult;
            }

            var validation = await ValidateEnrollmentForStudentAsync(input.EnrollmentId, studentCode);
            if (validation.ErrorResult != null)
            {
                return validation.ErrorResult;
            }

            var updates = input.Resources
                .Select(resource => new ResourceProgressUpdate(
                    resource.ResourceId,
                    resource.Status,
                    resource.Progress,
                    resource.Score,
                    resource.SessionTime))
                .ToList();

            await UpsertLearningLogsAsync(input.EnrollmentId, validation.VersionId, studentCode, updates, resetAt: validation.Enrollment!.ResetAt);
            await UpdateEnrollmentRollupAsync(validation.Enrollment!, validation.VersionId);

            InvalidateLearningCaches();

            return Ok(new ApiResponse<string> { Success = true, Message = "Progress saved." });
        }

        [AllowAnonymous]
        [HttpPost("commit-runtime")]
        public async Task<IActionResult> CommitRuntime([FromBody] ScormRuntimeCommitRequestDto input)
        {
            if (!TryResolveTrustedLearnerStudentCode(out var studentCode, out var errorResult))
            {
                return errorResult;
            }

            var payloadValidationMessage = ValidateRuntimeCommitRequest(input);
            if (!string.IsNullOrWhiteSpace(payloadValidationMessage))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = payloadValidationMessage });
            }

            if (input.Resources.Count == 0)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "No runtime resources were supplied." });
            }

            var validation = await ValidateEnrollmentForStudentAsync(input.EnrollmentId, studentCode);
            if (validation.ErrorResult != null)
            {
                return validation.ErrorResult;
            }

            var version = (await _versionRepo.GetAsync(v => v.Id == validation.VersionId, includeProperties: "CourseResources")).FirstOrDefault();
            if (version?.CourseResources == null)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Course version resources were not found." });
            }

            var validResourceIds = version.CourseResources.Select(cr => cr.ResourceId).ToHashSet();
            var runtimeResources = input.Resources
                .Where(resource => validResourceIds.Contains(resource.ResourceId))
                .ToList();

            if (runtimeResources.Count == 0)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "No valid course resources were supplied for runtime commit." });
            }

            var persistedStates = await _scormRuntimeStateService.UpsertAsync(input.EnrollmentId, runtimeResources);
            var progressUpdates = runtimeResources
                .Select(MapRuntimeCommitToProgress)
                .ToList();

            await UpsertLearningLogsAsync(input.EnrollmentId, validation.VersionId, studentCode, progressUpdates, incrementAttemptCount: false, resetAt: validation.Enrollment!.ResetAt);
            await UpdateEnrollmentRollupAsync(validation.Enrollment!, validation.VersionId);

            InvalidateLearningCaches();

            return Ok(new ApiResponse<IReadOnlyList<ScormRuntimeStateDto>>
            {
                Success = true,
                Message = "Runtime committed.",
                Data = persistedStates
            });
        }

        [AllowAnonymous]
        [HttpPost("reset-progress")]
        public async Task<IActionResult> ResetProgress([FromBody] ResetProgressDto input)
        {
            if (!TryResolveTrustedLearnerStudentCode(out var studentCode, out var errorResult))
            {
                return errorResult;
            }

            if (input == null || input.EnrollmentId <= 0)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Invalid enrollment id." });
            }

            var validation = await ValidateEnrollmentForStudentAsync(input.EnrollmentId, studentCode, allowCompleted: true);
            if (validation.ErrorResult != null)
            {
                return validation.ErrorResult;
            }

            var enrollment = validation.Enrollment!;
            enrollment.IsCompleted = false;
            enrollment.CompletedDate = null;
            enrollment.Progress = 0;
            enrollment.TotalScore = 0;
            enrollment.TotalTimeSpent = 0;
            enrollment.ResetAt = _dateTime.Now;

            await _enrollmentRepo.UpdateAsync(enrollment);

            var assignmentLinks = await _enrollmentAssignmentRepo.GetAsync(link => link.EnrollmentId == enrollment.Id);
            foreach (var link in assignmentLinks)
            {
                link.SnapshotCompleted = false;
                link.SnapshotCompletedDate = null;
                link.SnapshotProgress = 0;
                await _enrollmentAssignmentRepo.UpdateAsync(link);
            }

            InvalidateLearningCaches();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Progress reset.",
                Data = new
                {
                    enrollment.Id,
                    enrollment.ResetAt,
                    enrollment.Progress,
                    enrollment.IsCompleted
                }
            });
        }

        private static string? ValidateRuntimeCommitRequest(ScormRuntimeCommitRequestDto input)
        {
            if (input.EnrollmentId <= 0)
            {
                return "Invalid enrollment id for runtime commit.";
            }

            foreach (var resource in input.Resources)
            {
                if (resource.ResourceId <= 0)
                {
                    return "Invalid resource id for runtime commit.";
                }

                if (ExceedsLimit(resource.ScormVersion, ScormRuntimeLimits.ScormVersionMaxLength))
                {
                    return $"SCORM version exceeds the supported limit of {ScormRuntimeLimits.ScormVersionMaxLength} characters.";
                }

                if (ExceedsLimit(resource.LessonLocation, ScormRuntimeLimits.LessonLocationMaxLength))
                {
                    return $"Lesson location exceeds the supported limit of {ScormRuntimeLimits.LessonLocationMaxLength} characters.";
                }

                if (ExceedsLimit(resource.SuspendData, ScormRuntimeLimits.SuspendDataMaxLength))
                {
                    return $"Suspend data exceeds the supported limit of {ScormRuntimeLimits.SuspendDataMaxLength} characters.";
                }

                if (ExceedsLimit(resource.LessonStatus, ScormRuntimeLimits.StatusMaxLength) ||
                    ExceedsLimit(resource.CompletionStatus, ScormRuntimeLimits.StatusMaxLength) ||
                    ExceedsLimit(resource.SuccessStatus, ScormRuntimeLimits.StatusMaxLength))
                {
                    return $"Runtime status fields exceed the supported limit of {ScormRuntimeLimits.StatusMaxLength} characters.";
                }

                if (ExceedsLimit(resource.SessionTime, ScormRuntimeLimits.SessionTimeMaxLength) ||
                    ExceedsLimit(resource.TotalTime, ScormRuntimeLimits.TotalTimeMaxLength))
                {
                    return $"Runtime time fields exceed the supported limit of {ScormRuntimeLimits.SessionTimeMaxLength} characters.";
                }

                if (ExceedsLimit(resource.Entry, ScormRuntimeLimits.EntryMaxLength) ||
                    ExceedsLimit(resource.Exit, ScormRuntimeLimits.ExitMaxLength))
                {
                    return $"Runtime entry or exit fields exceed the supported limit of {ScormRuntimeLimits.EntryMaxLength} characters.";
                }

                if (ExceedsLimit(resource.CmiSnapshotJson, ScormRuntimeLimits.CmiSnapshotJsonMaxLength))
                {
                    return $"Runtime snapshot exceeds the supported limit of {ScormRuntimeLimits.CmiSnapshotJsonMaxLength} characters.";
                }
            }

            return null;
        }

        private static bool ExceedsLimit(string? value, int maxLength)
        {
            return !string.IsNullOrEmpty(value) && value.Length > maxLength;
        }

        private int ParseSessionTime(string? timeStr)
        {
            if (string.IsNullOrEmpty(timeStr)) return 0;
            if (TimeSpan.TryParse(timeStr, out var ts))
            {
                return (int)ts.TotalSeconds;
            }
            return 0;
        }

        private bool TryResolveTrustedLearnerStudentCode(out string studentCode, out IActionResult errorResult)
        {
            if (_learnerProxyIdentityResolver.TryResolveStudentCode(HttpContext, out studentCode, out var statusCode, out var errorMessage))
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

        private async Task<(Enrollment? Enrollment, int VersionId, IActionResult? ErrorResult)> ValidateEnrollmentForStudentAsync(
            int enrollmentId,
            string studentCode,
            bool allowCompleted = false)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(enrollmentId);
            if (enrollment == null)
            {
                return (null, 0, NotFound(new ApiResponse<string> { Success = false, Message = "Enrollment not found" }));
            }

            if (!string.Equals(enrollment.StudentCode, studentCode, StringComparison.OrdinalIgnoreCase))
            {
                return (null, 0, Unauthorized(new ApiResponse<string> { Success = false, Message = "StudentDto code mismatch" }));
            }

            if (enrollment.IsCompleted && !allowCompleted)
            {
                return (null, 0, Ok(new ApiResponse<string> { Success = true, Message = "Course is completed." }));
            }

            if (!enrollment.EnrolledCourseVersion.HasValue)
            {
                return (null, 0, BadRequest(new ApiResponse<string> { Success = false, Message = "ไม่พบเวอร์ชันของหลักสูตรในระบบ (หลักสูตรอาจถูกลบไปแล้ว)" }));
            }

            return (enrollment, enrollment.EnrolledCourseVersion.Value, null);
        }

        private async Task UpsertLearningLogsAsync(
            int enrollmentId,
            int versionId,
            string studentCode,
            IReadOnlyCollection<ResourceProgressUpdate> updates,
            bool incrementAttemptCount = true,
            DateTime? resetAt = null)
        {
            var existingLogs = await _logRepo.GetAsync(log =>
                log.EnrollmentId == enrollmentId &&
                (!resetAt.HasValue || log.CreatedAt >= resetAt.Value));

            foreach (var update in updates)
            {
                var log = existingLogs.FirstOrDefault(item => item.ResourceId == update.ResourceId);
                bool isInputPassed = string.Equals(update.Status, "passed", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(update.Status, "completed", StringComparison.OrdinalIgnoreCase);
                string newStatus = isInputPassed ? "passed" : (update.Status ?? "incomplete");
                int sessionSeconds = ParseSessionTime(update.SessionTime);

                if (log != null)
                {
                    log.TotalSecondsPlayed += sessionSeconds;

                    if (!string.IsNullOrEmpty(update.SessionTime))
                    {
                        log.SessionTime = update.SessionTime;
                    }

                    log.Status = newStatus;
                    log.Progress = isInputPassed ? 100 : (update.Progress ?? 0);

                    if (update.Score.HasValue)
                    {
                        log.Score = update.Score;
                    }

                    if (incrementAttemptCount)
                    {
                        log.AttemptCount++;
                    }

                    await _logRepo.UpdateAsync(log);
                }
                else
                {
                    var newLog = new LearningLog
                    {
                        EnrollmentId = enrollmentId,
                        StudentCode = studentCode,
                        ResourceId = update.ResourceId,
                        CourseVersionId = versionId,
                        Status = newStatus,
                        Progress = isInputPassed ? 100 : (update.Progress ?? 0),
                        Score = update.Score,
                        SessionTime = update.SessionTime,
                        TotalSecondsPlayed = sessionSeconds,
                        AttemptCount = 1,
                        CreatedAt = _dateTime.Now
                    };
                    await _logRepo.AddAsync(newLog);
                }
            }
        }

        private async Task UpdateEnrollmentRollupAsync(Enrollment enrollment, int versionId)
        {
            var version = (await _versionRepo.GetAsync(v => v.Id == versionId, includeProperties: "CourseResources")).FirstOrDefault();
            if (version?.CourseResources == null)
            {
                return;
            }

            var updatedLogs = await _logRepo.GetAsync(log =>
                log.EnrollmentId == enrollment.Id &&
                (!enrollment.ResetAt.HasValue || log.CreatedAt >= enrollment.ResetAt.Value));
            var allResourceIds = version.CourseResources.Select(cr => cr.ResourceId).ToList();
            int passedCount = updatedLogs.Count(log =>
                allResourceIds.Contains(log.ResourceId ?? 0) &&
                (log.Status == "passed" || log.Status == "completed"));

            if (passedCount >= allResourceIds.Count && allResourceIds.Count > 0)
            {
                enrollment.IsCompleted = true;
                enrollment.CompletedDate = _dateTime.Now;
                enrollment.Progress = 100;
            }
            else
            {
                enrollment.Progress = allResourceIds.Count > 0
                    ? ((double)passedCount / allResourceIds.Count) * 100
                    : 0;
            }

            enrollment.TotalTimeSpent = updatedLogs.Sum(log => log.TotalSecondsPlayed);
            enrollment.TotalScore = updatedLogs
                .Where(log => allResourceIds.Contains(log.ResourceId ?? 0))
                .Max(log => (int?)log.Score ?? 0);

            await _enrollmentRepo.UpdateAsync(enrollment);

            var assignmentLinks = await _enrollmentAssignmentRepo.GetAsync(link => link.EnrollmentId == enrollment.Id);
            foreach (var link in assignmentLinks)
            {
                link.SnapshotCompleted = enrollment.IsCompleted;
                link.SnapshotCompletedDate = enrollment.CompletedDate;
                link.SnapshotProgress = enrollment.Progress;
                await _enrollmentAssignmentRepo.UpdateAsync(link);
            }
        }

        private void InvalidateLearningCaches()
        {
            AdminSummaryStatsCache.InvalidateLearningLogs(_cache);
            AdminSummaryStatsCache.InvalidateEnrollments(_cache);
        }

        private static ResourceProgressUpdate MapRuntimeCommitToProgress(ScormRuntimeResourceCommitDto resource)
        {
            var scormVersion = ScormRuntimeFieldMap.NormalizeVersion(resource.ScormVersion);
            var normalizedCompletionStatus = IsScorm12(scormVersion)
                ? ScormRuntimeFieldMap.NormalizeCompletionStatus(resource.LessonStatus, null)
                : ScormRuntimeFieldMap.NormalizeCompletionStatus(resource.LessonStatus, resource.CompletionStatus);
            var normalizedSuccessStatus = IsScorm12(scormVersion)
                ? NormalizeScorm12SuccessStatus(resource.LessonStatus)
                : ScormRuntimeFieldMap.NormalizeSuccessStatus(resource.LessonStatus, resource.SuccessStatus);

            return new ResourceProgressUpdate(
                resource.ResourceId,
                DeriveLegacyStatus(resource.LessonStatus, normalizedCompletionStatus, normalizedSuccessStatus),
                DeriveLegacyProgress(resource.LessonStatus, normalizedCompletionStatus, normalizedSuccessStatus),
                resource.RawScore.HasValue ? (int)Math.Round(resource.RawScore.Value, MidpointRounding.AwayFromZero) : null,
                resource.SessionTime);
        }

        private static bool IsScorm12(string? scormVersion)
        {
            return string.Equals(scormVersion, ScormRuntimeFieldMap.Scorm12, StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizeScorm12SuccessStatus(string? lessonStatus)
        {
            return lessonStatus?.Trim().ToLowerInvariant() switch
            {
                "passed" => "passed",
                "failed" => "failed",
                null => null,
                "" => null,
                _ => "unknown"
            };
        }

        private static string DeriveLegacyStatus(string? lessonStatus, string? completionStatus, string? successStatus)
        {
            if (string.Equals(successStatus, "failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "failed", StringComparison.OrdinalIgnoreCase))
            {
                return "failed";
            }

            if (string.Equals(successStatus, "passed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "passed", StringComparison.OrdinalIgnoreCase))
            {
                return "passed";
            }

            if (string.Equals(completionStatus, "completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "browsed", StringComparison.OrdinalIgnoreCase))
            {
                return "completed";
            }

            return "incomplete";
        }

        private static double? DeriveLegacyProgress(string? lessonStatus, string? completionStatus, string? successStatus)
        {
            if (string.Equals(successStatus, "failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "failed", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (string.Equals(completionStatus, "completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "passed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "browsed", StringComparison.OrdinalIgnoreCase))
            {
                return 100;
            }

            if (string.Equals(completionStatus, "incomplete", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "incomplete", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lessonStatus, "not attempted", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            return null;
        }

        private sealed record ResourceProgressUpdate(
            int ResourceId,
            string? Status,
            double? Progress,
            int? Score,
            string? SessionTime);
    }
}