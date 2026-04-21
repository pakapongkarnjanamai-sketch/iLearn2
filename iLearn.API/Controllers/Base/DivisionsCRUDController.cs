using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class DivisionsCRUDController : GenericController<Division>
    {
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly IGenericRepository<Role> _roleRepo;

        public DivisionsCRUDController(
            IGenericRepository<Division> repository,
            ICurrentUserService currentUser,
            IGenericRepository<Category> categoryRepo,
            IGenericRepository<Role> roleRepo) : base(repository, currentUser)
        {
            _categoryRepo = categoryRepo;
            _roleRepo = roleRepo;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery().AsQueryable();

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(d => d.Id == _currentUser.DivisionId.Value);

            // Load counts per division in two small queries
            var categoryCounts = await _categoryRepo.GetQuery()
                .Where(c => c.DivisionId != null)
                .GroupBy(c => c.DivisionId!.Value)
                .Select(g => new { DivisionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DivisionId, x => x.Count);

            var roleCounts = await _roleRepo.GetQuery()
                .Where(r => r.DivisionId != null)
                .GroupBy(r => r.DivisionId!.Value)
                .Select(g => new { DivisionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DivisionId, x => x.Count);

            // Project flat fields for DataSourceLoader (server-side paging/sorting)
            var projected = query.Select(d => new
            {
                d.Id,
                d.Name,
                d.IsActive,
                d.CreatedAt
            });

            var loadResult = DataSourceLoader.Load(projected, loadOptions);

            // Enrich with counts in memory
            if (loadResult.data is IEnumerable<object> items)
            {
                var enriched = items.Cast<dynamic>().Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.IsActive,
                    d.CreatedAt,
                    categoryCount = categoryCounts.GetValueOrDefault((int)d.Id, 0),
                    roleCount = roleCounts.GetValueOrDefault((int)d.Id, 0)
                }).ToList();

                return Ok(new
                {
                    loadResult.totalCount,
                    loadResult.groupCount,
                    loadResult.summary,
                    data = enriched
                });
            }

            return Ok(loadResult);
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats()
        {
            var totalDivisions = await _repository.CountAsync();
            var totalCategories = await _categoryRepo.CountAsync();
            var totalRoles = await _roleRepo.CountAsync();

            var usedByCategoryIds = await _categoryRepo.GetQuery()
                .Where(c => c.DivisionId != null)
                .Select(c => c.DivisionId!.Value)
                .Distinct()
                .ToListAsync();
            var usedByRoleIds = await _roleRepo.GetQuery()
                .Where(r => r.DivisionId != null)
                .Select(r => r.DivisionId!.Value)
                .Distinct()
                .ToListAsync();
            var usedIds = usedByCategoryIds.Union(usedByRoleIds).ToHashSet();
            var unusedDivisions = await _repository.CountAsync(d => !usedIds.Contains(d.Id));

            return Ok(new
            {
                totalDivisions,
                totalCategories,
                totalRoles,
                unusedDivisions
            });
        }
    }
}
