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
    public class EnrollmentsCRUDController : GenericController<Enrollment>
    {
        public EnrollmentsCRUDController(
            IGenericRepository<Enrollment> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

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
        public async Task<IActionResult> GetSummaryStats()
        {
            var all = await _repository.GetQuery()
                .Select(e => new { e.IsCompleted, e.Progress })
                .ToListAsync();

            return Ok(new
            {
                totalCount = all.Count,
                completedCount = all.Count(e => e.IsCompleted),
                inProgressCount = all.Count(e => !e.IsCompleted && e.Progress > 0),
                enrolledCount = all.Count(e => !e.IsCompleted && e.Progress == 0),
                avgProgress = all.Count > 0 ? Math.Round(all.Average(e => e.Progress), 1) : 0
            });
        }
    }
}
