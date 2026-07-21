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
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<LearningLogsController> _logger;

        private const int MaxSessionSecondsDeltaPerCommit = 14400;

        public LearningLogsController(
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<CourseVersion> versionRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            ICurrentUserService currentUserService,
            IMemoryCache cache,
            ILearnerProxyIdentityResolver learnerProxyIdentityResolver,
            IScormRuntimeStateService scormRuntimeStateService,
            IDateTime dateTime,
            ILogger<LearningLogsController> logger)
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
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressDto input)
        {
            if (!TryResolveTrustedLearnerLearnerCode(out var learnerCode, out var errorResult))
            {
                return errorResult;
            }

            var validation = await ValidateEnrollmentForLearnerAsync(input.EnrollmentId, learnerCode);
            if (validation.ErrorResult != null)
            {
                return validation.ErrorResult;
            }

            var updates = input.ContentItems
                .Select(contentItem => new ContentItemProgressUpdate(
                    contentItem.ContentItemId,
                    contentItem.Status,
                    contentItem.Progress,
                    contentItem.Score,
                    contentItem.SessionTime))
                .ToList();

            await UpsertLearningLogsAsync(input.EnrollmentId, validation.VersionId, learnerCode, updates, resetAt: validation.Enrollment!.ResetAt);
            await UpdateEnrollmentRollupAsync(validation.Enrollment!, validation.VersionId);

            InvalidateLearningCaches();

            return Ok(new ApiResponse<string> { Success = true, Message = "Progress saved." });
        }

        [AllowAnonymous]
        [HttpPost("commit-runtime")]
        public async Task<IActionResult> CommitRuntime([FromBody] ScormRuntimeCommitRequestDto input)
        {
            if (!TryResolveTrustedLearnerLearnerCode(out var learnerCode, out var errorResult))
            {
                return errorResult;
            }

            var payloadValidationMessage = ValidateRuntimeCommitRequest(input);
            if (!string.IsNullOrWhiteSpace(payloadValidationMessage))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = payloadValidationMessage });
            }

            if (input.ContentItems.Count == 0)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "No runtime contentItems were supplied." });
            }

            var validation = await ValidateEnrollmentForLearnerAsync(input.EnrollmentId, learnerCode, allowCompleted: true);
            if (validation.ErrorResult != null)
            {
                return validation.ErrorResult;
            }

            var version = (await _versionRepo.GetAsync(v => v.Id == validation.VersionId, includeProperties: "CourseContentItems.ContentItem")).FirstOrDefault();
            if (version?.CourseContentItems == null)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Course version contentItems were not found." });
            }

            var validContentItemIds = version.CourseContentItems.Select(cr => cr.ContentItemId).ToHashSet();
            var contentTypeByContentItemId = version.CourseContentItems
                .GroupBy(courseContentItem => courseContentItem.ContentItemId)
                .ToDictionary(group => group.Key, group => group.First().ContentItem?.TypeId);
            var runtimeContentItems = input.ContentItems
                .Where(contentItem => validContentItemIds.Contains(contentItem.ContentItemId))
                .ToList();

            if (runtimeContentItems.Count == 0)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "No valid course contentItems were supplied for runtime commit." });
            }

            var persistedStates = await _scormRuntimeStateService.UpsertAsync(input.EnrollmentId, runtimeContentItems);
            var progressUpdates = runtimeContentItems
                .Select(contentItem =>
                {
                    contentTypeByContentItemId.TryGetValue(contentItem.ContentItemId, out var contentTypeId);
                    return MapRuntimeCommitToProgress(contentItem, contentTypeId);
                })
                .ToList();

            await UpsertLearningLogsAsync(input.EnrollmentId, validation.VersionId, learnerCode, progressUpdates, incrementAttemptCount: false, resetAt: validation.Enrollment!.ResetAt);
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
            if (!TryResolveTrustedLearnerLearnerCode(out var learnerCode, out var errorResult))
            {
                return errorResult;
            }

            if (input == null || input.EnrollmentId <= 0)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Invalid enrollment id." });
            }

            var validation = await ValidateEnrollmentForLearnerAsync(input.EnrollmentId, learnerCode, allowCompleted: true);
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

            await _scormRuntimeStateService.ClearForEnrollmentAsync(enrollment.Id);

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

            foreach (var contentItem in input.ContentItems)
            {
                if (contentItem.ContentItemId <= 0)
                {
                    return "Invalid contentItem id for runtime commit.";
                }

                if (ExceedsLimit(contentItem.ScormVersion, ScormRuntimeLimits.ScormVersionMaxLength))
                {
                    return $"SCORM version exceeds the supported limit of {ScormRuntimeLimits.ScormVersionMaxLength} characters.";
                }

                if (ExceedsLimit(contentItem.LessonLocation, ScormRuntimeLimits.LessonLocationMaxLength))
                {
                    return $"Lesson location exceeds the supported limit of {ScormRuntimeLimits.LessonLocationMaxLength} characters.";
                }

                if (ExceedsLimit(contentItem.SuspendData, ScormRuntimeLimits.SuspendDataMaxLength))
                {
                    return $"Suspend data exceeds the supported limit of {ScormRuntimeLimits.SuspendDataMaxLength} characters.";
                }

                if (ExceedsLimit(contentItem.LessonStatus, ScormRuntimeLimits.StatusMaxLength) ||
                    ExceedsLimit(contentItem.CompletionStatus, ScormRuntimeLimits.StatusMaxLength) ||
                    ExceedsLimit(contentItem.SuccessStatus, ScormRuntimeLimits.StatusMaxLength))
                {
                    return $"Runtime status fields exceed the supported limit of {ScormRuntimeLimits.StatusMaxLength} characters.";
                }

                if (ExceedsLimit(contentItem.SessionTime, ScormRuntimeLimits.SessionTimeMaxLength) ||
                    ExceedsLimit(contentItem.TotalTime, ScormRuntimeLimits.TotalTimeMaxLength))
                {
                    return $"Runtime time fields exceed the supported limit of {ScormRuntimeLimits.SessionTimeMaxLength} characters.";
                }

                if (ExceedsLimit(contentItem.Entry, ScormRuntimeLimits.EntryMaxLength) ||
                    ExceedsLimit(contentItem.Exit, ScormRuntimeLimits.ExitMaxLength))
                {
                    return $"Runtime entry or exit fields exceed the supported limit of {ScormRuntimeLimits.EntryMaxLength} characters.";
                }

                if (ExceedsLimit(contentItem.CmiSnapshotJson, ScormRuntimeLimits.CmiSnapshotJsonMaxLength))
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
            return ScormDurationParser.ToSeconds(timeStr);
        }

        private bool TryResolveTrustedLearnerLearnerCode(out string learnerCode, out IActionResult errorResult)
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

        private async Task<(Enrollment? Enrollment, int VersionId, IActionResult? ErrorResult)> ValidateEnrollmentForLearnerAsync(
            int enrollmentId,
            string learnerCode,
            bool allowCompleted = false)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(enrollmentId);
            if (enrollment == null)
            {
                return (null, 0, NotFound(new ApiResponse<string> { Success = false, Message = "Enrollment not found" }));
            }

            if (!string.Equals(enrollment.LearnerCode, learnerCode, StringComparison.OrdinalIgnoreCase))
            {
                return (null, 0, Unauthorized(new ApiResponse<string> { Success = false, Message = "LearnerDto code mismatch" }));
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
            string learnerCode,
            IReadOnlyCollection<ContentItemProgressUpdate> updates,
            bool incrementAttemptCount = true,
            DateTime? resetAt = null)
        {
            var existingLogs = await _logRepo.GetAsync(log =>
                log.EnrollmentId == enrollmentId &&
                (!resetAt.HasValue || log.CreatedAt >= resetAt.Value));

            foreach (var update in updates)
            {
                var log = existingLogs.FirstOrDefault(item => item.ContentItemId == update.ContentItemId);
                bool isInputPassed = string.Equals(update.Status, "passed", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(update.Status, "completed", StringComparison.OrdinalIgnoreCase);
                string newStatus = isInputPassed ? "passed" : (update.Status ?? "incomplete");
                int sessionSeconds = ParseSessionTime(update.SessionTime);

                if (log != null)
                {
                    int previousSessionSeconds = ParseSessionTime(log.SessionTime);
                    int sessionSecondsDelta = sessionSeconds >= previousSessionSeconds
                        ? sessionSeconds - previousSessionSeconds
                        : sessionSeconds;
                    var shouldApplySessionSecondsDelta = sessionSecondsDelta <= MaxSessionSecondsDeltaPerCommit;
                    if (shouldApplySessionSecondsDelta)
                    {
                        log.TotalSecondsPlayed += sessionSecondsDelta;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Ignored suspicious SCORM session time delta {SessionSecondsDelta}s for enrollment {EnrollmentId}, content item {ContentItemId}. Previous session time: {PreviousSessionTime}; incoming session time: {IncomingSessionTime}.",
                            sessionSecondsDelta,
                            enrollmentId,
                            update.ContentItemId,
                            log.SessionTime,
                            update.SessionTime);
                    }

                    if (!string.IsNullOrEmpty(update.SessionTime))
                    {
                        log.SessionTime = update.SessionTime;
                    }

                    var shouldPreserveTerminalOutcome = HasTerminalProgress(log.Status) && IsPlaceholderProgress(newStatus);
                    if (shouldPreserveTerminalOutcome)
                    {
                        newStatus = log.Status;
                        isInputPassed = IsCompletionStatus(newStatus);
                    }

                    log.Status = newStatus;
                    log.Progress = isInputPassed ? 100 : (update.Progress ?? 0);

                    if (update.Score.HasValue)
                    {
                        if (!shouldPreserveTerminalOutcome)
                        {
                            log.Score = update.Score;
                        }
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
                        LearnerCode = learnerCode,
                        ContentItemId = update.ContentItemId,
                        CourseVersionId = versionId,
                        Status = newStatus,
                        Progress = isInputPassed ? 100 : (update.Progress ?? 0),
                        Score = update.Score,
                        SessionTime = update.SessionTime,
                        TotalSecondsPlayed = sessionSeconds <= MaxSessionSecondsDeltaPerCommit ? sessionSeconds : 0,
                        AttemptCount = 1,
                        CreatedAt = _dateTime.Now
                    };

                    if (sessionSeconds > MaxSessionSecondsDeltaPerCommit)
                    {
                        _logger.LogWarning(
                            "Ignored suspicious initial SCORM session time {SessionSeconds}s for enrollment {EnrollmentId}, content item {ContentItemId}. Incoming session time: {IncomingSessionTime}.",
                            sessionSeconds,
                            enrollmentId,
                            update.ContentItemId,
                            update.SessionTime);
                    }

                    await _logRepo.AddAsync(newLog);
                }
            }
        }

        private async Task UpdateEnrollmentRollupAsync(Enrollment enrollment, int versionId)
        {
            var version = (await _versionRepo.GetAsync(v => v.Id == versionId, includeProperties: "CourseContentItems")).FirstOrDefault();
            if (version?.CourseContentItems == null)
            {
                return;
            }

            var updatedLogs = await _logRepo.GetAsync(log =>
                log.EnrollmentId == enrollment.Id &&
                (!enrollment.ResetAt.HasValue || log.CreatedAt >= enrollment.ResetAt.Value));
            var allContentItemIds = version.CourseContentItems.Select(cr => cr.ContentItemId).ToList();
            int passedCount = updatedLogs.Count(log =>
                allContentItemIds.Contains(log.ContentItemId ?? 0) &&
                (log.Status == "passed" || log.Status == "completed"));

            if (passedCount >= allContentItemIds.Count && allContentItemIds.Count > 0)
            {
                enrollment.IsCompleted = true;
                enrollment.CompletedDate = _dateTime.Now;
                enrollment.Progress = 100;
            }
            else
            {
                enrollment.IsCompleted = false;
                enrollment.CompletedDate = null;
                enrollment.Progress = allContentItemIds.Count > 0
                    ? ((double)passedCount / allContentItemIds.Count) * 100
                    : 0;
            }

            enrollment.TotalTimeSpent = updatedLogs.Sum(log => log.TotalSecondsPlayed);
            enrollment.TotalScore = updatedLogs
                .Where(log => allContentItemIds.Contains(log.ContentItemId ?? 0))
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

        private static ContentItemProgressUpdate MapRuntimeCommitToProgress(ScormRuntimeContentItemCommitDto contentItem, int? contentTypeId)
        {
            var scormVersion = ScormRuntimeFieldMap.NormalizeVersion(contentItem.ScormVersion);
            var normalizedCompletionStatus = IsScorm12(scormVersion)
                ? ScormRuntimeFieldMap.NormalizeCompletionStatus(contentItem.LessonStatus, null)
                : ScormRuntimeFieldMap.NormalizeCompletionStatus(contentItem.LessonStatus, contentItem.CompletionStatus);
            var normalizedSuccessStatus = IsScorm12(scormVersion)
                ? NormalizeScorm12SuccessStatus(contentItem.LessonStatus)
                : ScormRuntimeFieldMap.NormalizeSuccessStatus(contentItem.LessonStatus, contentItem.SuccessStatus);
            var resolvedStatus = ScormContentStatusPolicy.ResolveStatus(
                contentTypeId,
                contentItem.LessonStatus,
                normalizedCompletionStatus,
                normalizedSuccessStatus);

            return new ContentItemProgressUpdate(
                contentItem.ContentItemId,
                resolvedStatus,
                ScormContentStatusPolicy.ResolveCompletionProgress(resolvedStatus),
                contentItem.RawScore.HasValue
                    ? (int)Math.Round(contentItem.RawScore.Value, MidpointRounding.AwayFromZero)
                    : (contentItem.ScaledScore.HasValue
                        ? (int)Math.Round(contentItem.ScaledScore.Value * 100, MidpointRounding.AwayFromZero)
                        : null),
                contentItem.SessionTime);
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

        private static bool HasTerminalProgress(string? status)
        {
            return NormalizeStatus(status) switch
            {
                "passed" => true,
                "completed" => true,
                "failed" => true,
                "browsed" => true,
                _ => false
            };
        }

        private static bool IsCompletionStatus(string? status)
        {
            return NormalizeStatus(status) is "passed" or "completed" or "browsed";
        }

        private static bool IsPlaceholderProgress(string? status)
        {
            return NormalizeStatus(status) switch
            {
                "incomplete" => true,
                "not attempted" => true,
                "unknown" => true,
                _ => false
            };
        }

        private static string? NormalizeStatus(string? status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? null
                : status.Trim().ToLowerInvariant();
        }

        private sealed record ContentItemProgressUpdate(
            int ContentItemId,
            string? Status,
            double? Progress,
            int? Score,
            string? SessionTime);
    }
}