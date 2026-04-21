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
    public class LearningLogsCRUDController : GenericController<LearningLog>
    {
        private readonly IGenericRepository<Resource> _resourceRepo;

        public LearningLogsCRUDController(
            IGenericRepository<LearningLog> repository,
            ICurrentUserService currentUser,
            IGenericRepository<Resource> resourceRepo) : base(repository, currentUser)
        {
            _resourceRepo = resourceRepo;
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
        public async Task<IActionResult> GetSummaryStats()
        {
            var all = await _repository.GetQuery()
                .Select(l => new { l.Status, l.Score, l.TotalSecondsPlayed })
                .ToListAsync();

            var statusLower = all.Select(l => l.Status?.ToLower() ?? "").ToList();

            return Ok(new
            {
                totalLogs = all.Count,
                completedCount = statusLower.Count(s => s == "completed" || s == "passed"),
                failedCount = statusLower.Count(s => s == "failed"),
                inProgressCount = statusLower.Count(s => s == "incomplete" || s == "in_progress"),
                avgScore = all.Where(l => l.Score.HasValue).Any()
                    ? Math.Round(all.Where(l => l.Score.HasValue).Average(l => l.Score!.Value), 1)
                    : 0,
                totalTimeSpent = all.Sum(l => l.TotalSecondsPlayed)
            });
        }
    }
}
