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
    internal sealed record LearningLogsSummaryStats(
        int TotalLogs,
        int CompletedCount,
        int FailedCount,
        int InProgressCount,
        double AvgScore,
        long TotalTimeSpent);

    [Authorize(Policy = "SuperAdminOnly")]
    public class LearningLogsCRUDController : GenericController<LearningLog>
    {
        private readonly IGenericRepository<Resource> _resourceRepo;
        private readonly IMemoryCache _cache;

        public LearningLogsCRUDController(
            IGenericRepository<LearningLog> repository,
            ICurrentUserService currentUser,
            IGenericRepository<Resource> resourceRepo,
            IMemoryCache cache) : base(repository, currentUser)
        {
            _resourceRepo = resourceRepo;
            _cache = cache;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery()
                .Include(l => l.Enrollment)
                    .ThenInclude(e => e!.Course)
                .Select(l => new
                {
                    l.Id,
                    l.StudentCode,
                    l.EnrollmentId,
                    l.ResourceId,
                    l.CourseVersionId,
                    courseCode = l.Enrollment != null && l.Enrollment.Course != null ? l.Enrollment.Course.Code : "",
                    courseTitle = l.Enrollment != null && l.Enrollment.Course != null ? l.Enrollment.Course.Title : "",
                    l.Status,
                    l.Progress,
                    l.Score,
                    l.TotalSecondsPlayed,
                    l.AttemptCount,
                    l.SessionTime,
                    l.CreatedAt,
                    l.UpdatedAt
                });

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(AdminSummaryStatsCache.LearningLogsSummaryKey, out LearningLogsSummaryStats? cachedStats) && cachedStats != null)
            {
                return Ok(cachedStats);
            }

            var aggregate = await _repository.GetQuery()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    totalLogs = g.Count(),
                    completedCount = g.Count(l => l.Status == "completed" || l.Status == "passed"),
                    failedCount = g.Count(l => l.Status == "failed"),
                    inProgressCount = g.Count(l => l.Status == "incomplete" || l.Status == "in_progress"),
                    avgScore = g.Where(l => l.Score.HasValue).Average(l => (double?)l.Score),
                    totalTimeSpent = g.Sum(l => (long?)l.TotalSecondsPlayed)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var stats = new LearningLogsSummaryStats(
                aggregate?.totalLogs ?? 0,
                aggregate?.completedCount ?? 0,
                aggregate?.failedCount ?? 0,
                aggregate?.inProgressCount ?? 0,
                Math.Round(aggregate?.avgScore ?? 0, 1),
                aggregate?.totalTimeSpent ?? 0);

            _cache.Set(AdminSummaryStatsCache.LearningLogsSummaryKey, stats, AdminSummaryStatsCache.SummaryOptions);

            return Ok(stats);
        }

        [HttpPost("Post")]
        public override async Task<IActionResult> Post([FromForm] string values)
        {
            var result = await base.Post(values);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateLearningLogs(_cache);
            }

            return result;
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var result = await base.Put(key, values);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateLearningLogs(_cache);
            }

            return result;
        }

        [HttpDelete("Delete")]
        public override async Task<IActionResult> Delete([FromForm] int key)
        {
            var result = await base.Delete(key);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateLearningLogs(_cache);
            }

            return result;
        }
    }
}
