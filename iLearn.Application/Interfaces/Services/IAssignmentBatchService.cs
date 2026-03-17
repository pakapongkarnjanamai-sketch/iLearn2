using iLearn.Domain.Entities;

namespace iLearn.Application.Interfaces.Services
{
    public interface IAssignmentBatchService
    {
        string GetBatchKey(Assignment assignment);

        Task<IReadOnlyList<Assignment>> LoadBatchAsync(
            Assignment assignment,
            string? includeProperties = null,
            bool ignoreQueryFilters = false);
    }
}
