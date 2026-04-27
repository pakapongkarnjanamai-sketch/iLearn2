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
    internal sealed record DivisionsSummaryStats(
        int TotalDivisions,
        int TotalCategories,
        int TotalRoles,
        int UnusedDivisions);

    [Authorize(Policy = "SuperAdminOnly")]
    public class DivisionsCRUDController : GenericController<Division>
    {
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly IGenericRepository<Role> _roleRepo;
        private readonly IMemoryCache _cache;

        public DivisionsCRUDController(
            IGenericRepository<Division> repository,
            ICurrentUserService currentUser,
            IGenericRepository<Category> categoryRepo,
            IGenericRepository<Role> roleRepo,
            IMemoryCache cache) : base(repository, currentUser)
        {
            _categoryRepo = categoryRepo;
            _roleRepo = roleRepo;
            _cache = cache;
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
        public async Task<IActionResult> GetSummaryStats(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(AdminSummaryStatsCache.DivisionsSummaryKey, out DivisionsSummaryStats? cachedStats) && cachedStats != null)
            {
                return Ok(cachedStats);
            }

            var totalDivisions = await _repository.GetQuery().CountAsync(cancellationToken);
            var totalCategories = await _categoryRepo.GetQuery().CountAsync(cancellationToken);
            var totalRoles = await _roleRepo.GetQuery().CountAsync(cancellationToken);

            var usedDivisionIds = _categoryRepo.GetQuery()
                .Where(c => c.DivisionId != null)
                .Select(c => c.DivisionId!.Value)
                .Union(_roleRepo.GetQuery()
                    .Where(r => r.DivisionId != null)
                    .Select(r => r.DivisionId!.Value));

            var unusedDivisions = await _repository.GetQuery()
                .CountAsync(d => !usedDivisionIds.Contains(d.Id), cancellationToken);

            var stats = new DivisionsSummaryStats(
                totalDivisions,
                totalCategories,
                totalRoles,
                unusedDivisions);

            _cache.Set(AdminSummaryStatsCache.DivisionsSummaryKey, stats, AdminSummaryStatsCache.SummaryOptions);

            return Ok(stats);
        }

        [HttpPost("Post")]
        public override async Task<IActionResult> Post([FromForm] string values)
        {
            var result = await base.Post(values);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateDivisions(_cache);
            }

            return result;
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var result = await base.Put(key, values);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateDivisions(_cache);
            }

            return result;
        }

        [HttpDelete("Delete")]
        public override async Task<IActionResult> Delete([FromForm] int key)
        {
            var result = await base.Delete(key);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateDivisions(_cache);
            }

            return result;
        }
    }
}
