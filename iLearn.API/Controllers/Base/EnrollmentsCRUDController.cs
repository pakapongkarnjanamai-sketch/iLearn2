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
    internal sealed record EnrollmentsSummaryStats(
        int TotalCount,
        int CompletedCount,
        int InProgressCount,
        int EnrolledCount,
        double AvgProgress);

    [Authorize(Policy = "SuperAdminOnly")]
    public class EnrollmentsCRUDController : GenericController<Enrollment>
    {
        private readonly IMemoryCache _cache;

        public EnrollmentsCRUDController(
            IGenericRepository<Enrollment> repository,
            ICurrentUserService currentUser,
            IMemoryCache cache) : base(repository, currentUser)
        {
            _cache = cache;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery()
                .Include(e => e.Course)
                .Select(e => new
                {
                    e.Id,
                    e.StudentCode,
                    e.CourseId,
                    courseCode = e.Course != null ? e.Course.Code : "",
                    courseTitle = e.Course != null ? e.Course.Title : "",
                    e.IsCompleted,
                    e.Progress,
                    e.TotalScore,
                    e.TotalTimeSpent,
                    e.StartDate,
                    e.DueDate,
                    e.CompletedDate,
                    e.ResetAt,
                    e.CreatedAt
                });

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(AdminSummaryStatsCache.EnrollmentsSummaryKey, out EnrollmentsSummaryStats? cachedStats) && cachedStats != null)
            {
                return Ok(cachedStats);
            }

            var aggregate = await _repository.GetQuery()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    totalCount = g.Count(),
                    completedCount = g.Count(e => e.IsCompleted),
                    inProgressCount = g.Count(e => !e.IsCompleted && e.Progress > 0),
                    enrolledCount = g.Count(e => !e.IsCompleted && e.Progress == 0),
                    avgProgress = g.Average(e => (double?)e.Progress)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var stats = new EnrollmentsSummaryStats(
                aggregate?.totalCount ?? 0,
                aggregate?.completedCount ?? 0,
                aggregate?.inProgressCount ?? 0,
                aggregate?.enrolledCount ?? 0,
                Math.Round(aggregate?.avgProgress ?? 0, 1));

            _cache.Set(AdminSummaryStatsCache.EnrollmentsSummaryKey, stats, AdminSummaryStatsCache.SummaryOptions);

            return Ok(stats);
        }

        [HttpPost("Post")]
        public override async Task<IActionResult> Post([FromForm] string values)
        {
            var result = await base.Post(values);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateEnrollments(_cache);
            }

            return result;
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var result = await base.Put(key, values);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateEnrollments(_cache);
            }

            return result;
        }

        [HttpDelete("Delete")]
        public override async Task<IActionResult> Delete([FromForm] int key)
        {
            var result = await base.Delete(key);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateEnrollments(_cache);
            }

            return result;
        }
    }
}
