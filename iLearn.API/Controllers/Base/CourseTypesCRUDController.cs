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
    internal sealed record CourseTypesSummaryStats(
        int TotalTypes,
        int TotalCourses,
        double AvgCoursesPerType,
        int UnusedTypes);

    [Authorize(Policy = "SuperAdminOnly")]
    public class CourseTypesCRUDController : GenericController<CourseType>
    {
        private readonly IMemoryCache _cache;

        public CourseTypesCRUDController(
            IGenericRepository<CourseType> repository,
            ICurrentUserService currentUser,
            IMemoryCache cache) : base(repository, currentUser)
        {
            _cache = cache;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery()
                .Select(ct => new
                {
                    ct.Id,
                    ct.Name,
                    ct.Description,
                    ct.IsActive,
                    courseCount = ct.Courses.Count(),
                    ct.CreatedAt
                });

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(AdminSummaryStatsCache.CourseTypesSummaryKey, out CourseTypesSummaryStats? cachedStats) && cachedStats != null)
            {
                return Ok(cachedStats);
            }

            var aggregate = await _repository.GetQuery()
                .Select(ct => new
                {
                    courseCount = ct.Courses.Count()
                })
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    totalTypes = g.Count(),
                    totalCourses = g.Sum(x => x.courseCount),
                    unusedTypes = g.Count(x => x.courseCount == 0)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalTypes = aggregate?.totalTypes ?? 0;
            var totalCourses = aggregate?.totalCourses ?? 0;
            var unusedTypes = aggregate?.unusedTypes ?? 0;

            var stats = new CourseTypesSummaryStats(
                totalTypes,
                totalCourses,
                totalTypes > 0 ? Math.Round((double)totalCourses / totalTypes, 1) : 0,
                unusedTypes);

            _cache.Set(AdminSummaryStatsCache.CourseTypesSummaryKey, stats, AdminSummaryStatsCache.SummaryOptions);

            return Ok(stats);
        }

        [HttpPost("Post")]
        public override async Task<IActionResult> Post([FromForm] string values)
        {
            var result = await base.Post(values);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateCourseTypes(_cache);
            }

            return result;
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var result = await base.Put(key, values);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateCourseTypes(_cache);
            }

            return result;
        }

        [HttpDelete("Delete")]
        public override async Task<IActionResult> Delete([FromForm] int key)
        {
            var result = await base.Delete(key);
            if (result is OkObjectResult || result is OkResult)
            {
                AdminSummaryStatsCache.InvalidateCourseTypes(_cache);
            }

            return result;
        }
    }
}
