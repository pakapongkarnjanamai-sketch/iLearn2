using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;

namespace iLearn.Infrastructure.Services
{
    public class ScormRuntimeStateService : IScormRuntimeStateService
    {
        private readonly IGenericRepository<ScormRuntimeState> _runtimeStateRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTime _dateTime;

        public ScormRuntimeStateService(
            IGenericRepository<ScormRuntimeState> runtimeStateRepo,
            IUnitOfWork unitOfWork,
            IDateTime dateTime)
        {
            _runtimeStateRepo = runtimeStateRepo;
            _unitOfWork = unitOfWork;
            _dateTime = dateTime;
        }

        public async Task<IReadOnlyList<ScormRuntimeStateDto>> GetActiveStatesAsync(int enrollmentId, DateTime? resetAt = null)
        {
            var states = await _runtimeStateRepo.GetAsync(state =>
                state.EnrollmentId == enrollmentId &&
                (!resetAt.HasValue ||
                 state.UpdatedAt >= resetAt.Value ||
                 (state.UpdatedAt == null && state.CreatedAt >= resetAt.Value)));

            return states
                .Select(MapToDto)
                .OrderBy(state => state.ContentItemId)
                .ToList();
        }

        public async Task<IReadOnlyList<ScormRuntimeStateDto>> UpsertAsync(
            int enrollmentId,
            IReadOnlyCollection<ScormRuntimeContentItemCommitDto> contentItems,
            CancellationToken cancellationToken = default)
        {
            if (contentItems.Count == 0)
            {
                return [];
            }

            var existingStates = (await _runtimeStateRepo.GetAsync(state => state.EnrollmentId == enrollmentId))
                .ToDictionary(state => state.ContentItemId);
            var touchedStates = new Dictionary<int, ScormRuntimeState>();

            foreach (var contentItem in contentItems)
            {
                if (contentItem.ContentItemId <= 0)
                {
                    continue;
                }

                if (!existingStates.TryGetValue(contentItem.ContentItemId, out var state))
                {
                    state = new ScormRuntimeState
                    {
                        EnrollmentId = enrollmentId,
                        ContentItemId = contentItem.ContentItemId
                    };

                    ApplyCommit(state, contentItem);
                    await _runtimeStateRepo.AddWithoutSaveAsync(state);
                    existingStates[contentItem.ContentItemId] = state;
                }
                else
                {
                    ApplyCommit(state, contentItem);
                    _runtimeStateRepo.UpdateWithoutSave(state);
                }

                touchedStates[contentItem.ContentItemId] = state;
            }

            if (touchedStates.Count == 0)
            {
                return [];
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return touchedStates.Values
                .Select(MapToDto)
                .OrderBy(state => state.ContentItemId)
                .ToList();
        }

        private void ApplyCommit(ScormRuntimeState state, ScormRuntimeContentItemCommitDto contentItem)
        {
            var normalizedVersion = ScormRuntimeFieldMap.NormalizeVersion(contentItem.ScormVersion);
            var normalizedLessonStatus = PreferIncoming(contentItem.LessonStatus, null);
            var normalizedCompletionStatus = IsScorm12(normalizedVersion)
                ? ScormRuntimeFieldMap.NormalizeCompletionStatus(contentItem.LessonStatus, null)
                : ScormRuntimeFieldMap.NormalizeCompletionStatus(contentItem.LessonStatus, contentItem.CompletionStatus);
            var normalizedSuccessStatus = IsScorm12(normalizedVersion)
                ? NormalizeScorm12SuccessStatus(contentItem.LessonStatus)
                : ScormRuntimeFieldMap.NormalizeSuccessStatus(contentItem.LessonStatus, contentItem.SuccessStatus);
            var isPlaceholderProgressCommit = IsPlaceholderProgressCommit(
                contentItem,
                normalizedLessonStatus,
                normalizedCompletionStatus,
                normalizedSuccessStatus);

            state.ScormVersion = PreferIncoming(normalizedVersion, state.ScormVersion) ?? string.Empty;
            state.LessonLocation = PreferIncoming(contentItem.LessonLocation, state.LessonLocation);
            state.SuspendData = PreferIncoming(contentItem.SuspendData, state.SuspendData);
            state.LessonStatus = PreferStatus(normalizedLessonStatus, state.LessonStatus, isPlaceholderProgressCommit);
            state.CompletionStatus = PreferStatus(normalizedCompletionStatus, state.CompletionStatus, isPlaceholderProgressCommit);
            state.SuccessStatus = PreferSuccessStatus(normalizedSuccessStatus, state.SuccessStatus);
            state.SessionTime = PreferDuration(contentItem.SessionTime, state.SessionTime);
            state.TotalTime = PreferDuration(contentItem.TotalTime, state.TotalTime);
            state.Entry = PreferEntry(contentItem.Entry, state.Entry, state.LessonLocation, state.SuspendData);
            state.Exit = PreferIncoming(contentItem.Exit, state.Exit);
            state.CmiSnapshotJson = PreferIncoming(contentItem.CmiSnapshotJson, state.CmiSnapshotJson);
            state.LastCommittedAtUtc = contentItem.LastCommittedAtUtc ?? _dateTime.Now.ToUniversalTime();

            state.RawScore = PreferRawScore(
                contentItem.RawScore,
                state.RawScore,
                state.LessonStatus,
                state.CompletionStatus,
                state.SuccessStatus,
                isPlaceholderProgressCommit);

            state.ScaledScore = PreferRawScore(
                contentItem.ScaledScore,
                state.ScaledScore,
                state.LessonStatus,
                state.CompletionStatus,
                state.SuccessStatus,
                isPlaceholderProgressCommit);
        }

        private static ScormRuntimeStateDto MapToDto(ScormRuntimeState state)
        {
            return new ScormRuntimeStateDto
            {
                EnrollmentId = state.EnrollmentId,
                ContentItemId = state.ContentItemId,
                ScormVersion = state.ScormVersion,
                LessonLocation = state.LessonLocation,
                SuspendData = state.SuspendData,
                LessonStatus = state.LessonStatus,
                CompletionStatus = state.CompletionStatus,
                SuccessStatus = state.SuccessStatus,
                RawScore = state.RawScore,
                ScaledScore = state.ScaledScore,
                SessionTime = state.SessionTime,
                TotalTime = state.TotalTime,
                Entry = state.Entry,
                Exit = state.Exit,
                LastCommittedAtUtc = state.LastCommittedAtUtc,
                CmiSnapshotJson = state.CmiSnapshotJson
            };
        }

        private static string? PreferIncoming(string? incoming, string? existing)
        {
            return string.IsNullOrWhiteSpace(incoming)
                ? existing
                : incoming.Trim();
        }

        private static string? PreferStatus(string? incoming, string? existing, bool isPlaceholderProgressCommit)
        {
            var normalizedIncoming = PreferIncoming(incoming, null);
            if (normalizedIncoming == null)
            {
                return existing;
            }

            return isPlaceholderProgressCommit && HasTerminalProgress(existing) && IsPlaceholderProgress(normalizedIncoming)
                ? existing
                : normalizedIncoming;
        }

        private static string? PreferSuccessStatus(string? incoming, string? existing)
        {
            var normalizedIncoming = PreferIncoming(incoming, null);
            if (normalizedIncoming == null)
            {
                return existing;
            }

            return HasFinalSuccess(existing) && IsUnknownSuccess(normalizedIncoming)
                ? existing
                : normalizedIncoming;
        }

        private static string? PreferDuration(string? incoming, string? existing)
        {
            var normalizedIncoming = PreferIncoming(incoming, null);
            if (normalizedIncoming == null)
            {
                return existing;
            }

            return IsMeaningfulDuration(existing) && IsZeroLikeDuration(normalizedIncoming)
                ? existing
                : normalizedIncoming;
        }

        private static string? PreferEntry(string? incoming, string? existing, string? lessonLocation, string? suspendData)
        {
            var normalizedIncoming = PreferIncoming(incoming, null);
            if (normalizedIncoming == null)
            {
                return existing;
            }

            return string.Equals(existing, "resume", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(normalizedIncoming, "ab-initio", StringComparison.OrdinalIgnoreCase) &&
                   HasResumeContext(lessonLocation, suspendData)
                ? existing
                : normalizedIncoming;
        }

        private static decimal? PreferRawScore(
            decimal? incoming,
            decimal? existing,
            string? lessonStatus,
            string? completionStatus,
            string? successStatus,
            bool isPlaceholderProgressCommit)
        {
            if (!incoming.HasValue)
            {
                return existing;
            }

            return existing.HasValue &&
                   existing.Value > 0 &&
                   incoming.Value == 0m &&
                   (isPlaceholderProgressCommit || LooksLikePlaceholderOutcome(lessonStatus, completionStatus, successStatus))
                ? existing
                : incoming.Value;
        }

        private static bool HasTerminalProgress(string? status)
        {
            return Normalize(status) switch
            {
                "passed" => true,
                "completed" => true,
                "failed" => true,
                "browsed" => true,
                _ => false
            };
        }

        private static bool IsPlaceholderProgress(string? status)
        {
            return Normalize(status) switch
            {
                "incomplete" => true,
                "not attempted" => true,
                "unknown" => true,
                _ => false
            };
        }

        private static bool HasFinalSuccess(string? status)
        {
            return Normalize(status) switch
            {
                "passed" => true,
                "failed" => true,
                _ => false
            };
        }

        private static bool IsUnknownSuccess(string? status)
        {
            return string.Equals(Normalize(status), "unknown", StringComparison.Ordinal);
        }

        private static bool LooksLikePlaceholderOutcome(string? lessonStatus, string? completionStatus, string? successStatus)
        {
            return IsPlaceholderProgress(lessonStatus) &&
                   IsPlaceholderProgress(completionStatus) &&
                   (string.IsNullOrWhiteSpace(successStatus) || IsUnknownSuccess(successStatus));
        }

        private static bool IsPlaceholderProgressCommit(
            ScormRuntimeContentItemCommitDto contentItem,
            string? lessonStatus,
            string? completionStatus,
            string? successStatus)
        {
            return LooksLikePlaceholderOutcome(lessonStatus, completionStatus, successStatus) &&
                   (!contentItem.RawScore.HasValue || contentItem.RawScore.Value == 0m) &&
                   string.IsNullOrWhiteSpace(contentItem.LessonLocation) &&
                   string.IsNullOrWhiteSpace(contentItem.SuspendData) &&
                   (string.IsNullOrWhiteSpace(contentItem.SessionTime) || IsZeroLikeDuration(contentItem.SessionTime)) &&
                   (string.IsNullOrWhiteSpace(contentItem.TotalTime) || IsZeroLikeDuration(contentItem.TotalTime));
        }

        private static bool HasResumeContext(string? lessonLocation, string? suspendData)
        {
            return !string.IsNullOrWhiteSpace(lessonLocation) || !string.IsNullOrWhiteSpace(suspendData);
        }

        private static bool IsMeaningfulDuration(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && !IsZeroLikeDuration(value);
        }

        private static bool IsZeroLikeDuration(string value)
        {
            var normalized = value.Trim();
            if (normalized.Length == 0)
            {
                return false;
            }

            if (TimeSpan.TryParse(normalized, out var parsedDuration))
            {
                return parsedDuration == TimeSpan.Zero;
            }

            var digits = new string(normalized.Where(char.IsDigit).ToArray());
            return digits.Length > 0 && digits.All(digit => digit == '0');
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim().ToLowerInvariant();
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
    }
}
