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
    public class CourseTypesCRUDController : GenericController<CourseType>
    {
        public CourseTypesCRUDController(
            IGenericRepository<CourseType> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

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
        public async Task<IActionResult> GetSummaryStats()
        {
            var all = await _repository.GetQuery()
                .Select(ct => new
                {
                    ct.Id,
                    courseCount = ct.Courses.Count()
                })
                .ToListAsync();

            var totalTypes = all.Count;
            var totalCourses = all.Sum(ct => ct.courseCount);

            return Ok(new
            {
                totalTypes,
                totalCourses,
                avgCoursesPerType = totalTypes > 0
                    ? Math.Round((double)totalCourses / totalTypes, 1)
                    : 0,
                unusedTypes = all.Count(ct => ct.courseCount == 0)
            });
        }
    }
}
