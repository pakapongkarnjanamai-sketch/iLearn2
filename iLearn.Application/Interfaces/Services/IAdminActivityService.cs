using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IAdminActivityService
    {
        Task LogAsync(
            string actionType,
            string entityType,
            int? entityId,
            string title,
            string? description = null,
            int? divisionId = null,
            string? dataJson = null);

        Task<IReadOnlyList<AdminActivityDto>> GetRecentActivitiesAsync(int take = 20, int? divisionId = null);
    }
}
