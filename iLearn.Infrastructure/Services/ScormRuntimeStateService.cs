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
                .OrderBy(state => state.ResourceId)
                .ToList();
        }

        public async Task<IReadOnlyList<ScormRuntimeStateDto>> UpsertAsync(
            int enrollmentId,
            IReadOnlyCollection<ScormRuntimeResourceCommitDto> resources,
            CancellationToken cancellationToken = default)
        {
            if (resources.Count == 0)
            {
                return [];
            }

            var existingStates = (await _runtimeStateRepo.GetAsync(state => state.EnrollmentId == enrollmentId))
                .ToDictionary(state => state.ResourceId);
            var touchedStates = new Dictionary<int, ScormRuntimeState>();

            foreach (var resource in resources)
            {
                if (resource.ResourceId <= 0)
                {
                    continue;
                }

                if (!existingStates.TryGetValue(resource.ResourceId, out var state))
                {
                    state = new ScormRuntimeState
                    {
                        EnrollmentId = enrollmentId,
                        ResourceId = resource.ResourceId
                    };

                    ApplyCommit(state, resource);
                    await _runtimeStateRepo.AddWithoutSaveAsync(state);
                    existingStates[resource.ResourceId] = state;
                }
                else
                {
                    ApplyCommit(state, resource);
                    _runtimeStateRepo.UpdateWithoutSave(state);
                }

                touchedStates[resource.ResourceId] = state;
            }

            if (touchedStates.Count == 0)
            {
                return [];
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return touchedStates.Values
                .Select(MapToDto)
                .OrderBy(state => state.ResourceId)
                .ToList();
        }

        private void ApplyCommit(ScormRuntimeState state, ScormRuntimeResourceCommitDto resource)
        {
            var normalizedVersion = ScormRuntimeFieldMap.NormalizeVersion(resource.ScormVersion);
            var normalizedLessonStatus = PreferIncoming(resource.LessonStatus, null);
            var normalizedCompletionStatus = IsScorm12(normalizedVersion)
                ? ScormRuntimeFieldMap.NormalizeCompletionStatus(resource.LessonStatus, null)
                : ScormRuntimeFieldMap.NormalizeCompletionStatus(resource.LessonStatus, resource.CompletionStatus);
            var normalizedSuccessStatus = IsScorm12(normalizedVersion)
                ? NormalizeScorm12SuccessStatus(resource.LessonStatus)
                : ScormRuntimeFieldMap.NormalizeSuccessStatus(resource.LessonStatus, resource.SuccessStatus);
            var isPlaceholderProgressCommit = IsPlaceholderProgressCommit(
                resource,
                normalizedLessonStatus,
                normalizedCompletionStatus,
                normalizedSuccessStatus);

            state.ScormVersion = PreferIncoming(normalizedVersion, state.ScormVersion) ?? string.Empty;
            state.LessonLocation = PreferIncoming(resource.LessonLocation, state.LessonLocation);
            state.SuspendData = PreferIncoming(resource.SuspendData, state.SuspendData);
            state.LessonStatus = PreferStatus(normalizedLessonStatus, state.LessonStatus, isPlaceholderProgressCommit);
            state.CompletionStatus = PreferStatus(normalizedCompletionStatus, state.CompletionStatus, isPlaceholderProgressCommit);
            state.SuccessStatus = PreferSuccessStatus(normalizedSuccessStatus, state.SuccessStatus);
            state.SessionTime = PreferDuration(resource.SessionTime, state.SessionTime);
            state.TotalTime = PreferDuration(resource.TotalTime, state.TotalTime);
            state.Entry = PreferEntry(resource.Entry, state.Entry, state.LessonLocation, state.SuspendData);
            state.Exit = PreferIncoming(resource.Exit, state.Exit);
            state.CmiSnapshotJson = PreferIncoming(resource.CmiSnapshotJson, state.CmiSnapshotJson);
            state.LastCommittedAtUtc = resource.LastCommittedAtUtc ?? _dateTime.Now.ToUniversalTime();

            state.RawScore = PreferRawScore(
                resource.RawScore,
                state.RawScore,
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
                ResourceId = state.ResourceId,
                ScormVersion = state.ScormVersion,
                LessonLocation = state.LessonLocation,
                SuspendData = state.SuspendData,
                LessonStatus = state.LessonStatus,
                CompletionStatus = state.CompletionStatus,
                SuccessStatus = state.SuccessStatus,
                RawScore = state.RawScore,
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
            ScormRuntimeResourceCommitDto resource,
            string? lessonStatus,
            string? completionStatus,
            string? successStatus)
        {
            return LooksLikePlaceholderOutcome(lessonStatus, completionStatus, successStatus) &&
                   (!resource.RawScore.HasValue || resource.RawScore.Value == 0m) &&
                   string.IsNullOrWhiteSpace(resource.LessonLocation) &&
                   string.IsNullOrWhiteSpace(resource.SuspendData) &&
                   (string.IsNullOrWhiteSpace(resource.SessionTime) || IsZeroLikeDuration(resource.SessionTime)) &&
                   (string.IsNullOrWhiteSpace(resource.TotalTime) || IsZeroLikeDuration(resource.TotalTime));
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
