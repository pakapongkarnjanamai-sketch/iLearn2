using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IScormRuntimeStateService
    {
        Task<IReadOnlyList<ScormRuntimeStateDto>> GetActiveStatesAsync(int enrollmentId, DateTime? resetAt = null);

        /// <summary>Soft-deletes all runtime states for an enrollment when its progress is reset.</summary>
        Task<int> ClearForEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default);

        /// <summary>Soft-deletes runtime states for multiple enrollments. Set saveChanges to false when the caller owns the commit.</summary>
        Task<int> ClearForEnrollmentsAsync(
            IReadOnlyCollection<int> enrollmentIds,
            bool saveChanges = true,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ScormRuntimeStateDto>> UpsertAsync(
            int enrollmentId,
            IReadOnlyCollection<ScormRuntimeContentItemCommitDto> contentItems,
            CancellationToken cancellationToken = default);
    }
}