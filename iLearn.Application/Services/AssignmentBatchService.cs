using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;

namespace iLearn.Application.Services
{
    public class AssignmentBatchService : IAssignmentBatchService
    {
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly ICurrentUserService _currentUser;

        public AssignmentBatchService(
            IGenericRepository<Assignment> assignmentRepo,
            ICurrentUserService currentUser)
        {
            _assignmentRepo = assignmentRepo;
            _currentUser = currentUser;
        }

        public string GetBatchKey(Assignment assignment)
        {
            return string.IsNullOrWhiteSpace(assignment.AssignmentNo)
                ? $"assignment:{assignment.Id}"
                : assignment.AssignmentNo;
        }

        public async Task<IReadOnlyList<Assignment>> LoadBatchAsync(
            Assignment assignment,
            string? includeProperties = null,
            bool ignoreQueryFilters = false)
        {
            var divisionId = _currentUser.DivisionId;

            if (string.IsNullOrWhiteSpace(assignment.AssignmentNo))
            {
                return await _assignmentRepo.GetAsync(
                    r => r.Id == assignment.Id && (!divisionId.HasValue || r.DivisionId == divisionId.Value),
                    includeProperties: includeProperties,
                    ignoreQueryFilters: ignoreQueryFilters);
            }

            return await _assignmentRepo.GetAsync(
                r => r.AssignmentNo == assignment.AssignmentNo && (!divisionId.HasValue || r.DivisionId == divisionId.Value),
                includeProperties: includeProperties,
                ignoreQueryFilters: ignoreQueryFilters);
        }
    }
}
