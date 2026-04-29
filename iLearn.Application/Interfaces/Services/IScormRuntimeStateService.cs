using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IScormRuntimeStateService
    {
        Task<IReadOnlyList<ScormRuntimeStateDto>> GetActiveStatesAsync(int enrollmentId, DateTime? resetAt = null);

        Task<IReadOnlyList<ScormRuntimeStateDto>> UpsertAsync(
            int enrollmentId,
            IReadOnlyCollection<ScormRuntimeContentItemCommitDto> contentItems,
            CancellationToken cancellationToken = default);
    }
}