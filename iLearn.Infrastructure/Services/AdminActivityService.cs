using iLearn.Application.DTOs;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace iLearn.Infrastructure.Services
{
    public class AdminActivityService : IAdminActivityService
    {
        private readonly IGenericRepository<AdminActivity> _adminActivityRepository;
        private readonly ILogger<AdminActivityService> _logger;

        public AdminActivityService(
            IGenericRepository<AdminActivity> adminActivityRepository,
            ILogger<AdminActivityService> logger)
        {
            _adminActivityRepository = adminActivityRepository;
            _logger = logger;
        }

        public async Task LogAsync(
            string actionType,
            string entityType,
            int? entityId,
            string title,
            string? description = null,
            int? divisionId = null,
            string? dataJson = null)
        {
            var activity = new AdminActivity
            {
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                Title = title,
                Description = description,
                DivisionId = divisionId,
                DataJson = dataJson,
                IsActive = true
            };

            try
            {
                await _adminActivityRepository.AddAsync(activity);
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                _logger.LogWarning(ex, "AdminActivities table was not found while writing activity '{ActionType}'. Skipping activity logging.", actionType);
            }
        }

        public async Task<IReadOnlyList<AdminActivityDto>> GetRecentActivitiesAsync(int take = 20, int? divisionId = null)
        {
            try
            {
                var query = _adminActivityRepository.GetQuery().AsQueryable();

                if (divisionId.HasValue)
                    query = query.Where(x => x.DivisionId == divisionId.Value);

                return await query
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(take)
                    .Select(x => x.ToDto())
                    .ToListAsync();
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                _logger.LogWarning(ex, "AdminActivities table was not found. Returning an empty activity list.");
                return [];
            }
        }
    }
}
