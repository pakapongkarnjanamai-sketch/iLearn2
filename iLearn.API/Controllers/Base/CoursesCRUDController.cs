using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    public class CoursesCRUDController : GenericController<Course>
    {
        private readonly ICourseService _courseService;

        public CoursesCRUDController(
            IGenericRepository<Course> repository,
            ICurrentUserService currentUser,
            ICourseService courseService) : base(repository, currentUser)
        {
            _courseService = courseService;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery()
                .AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.Title,
                    c.IsActive,
                    c.Status,
                    StatusName = c.Status == CourseStatus.Open ? "Open"
                        : c.Status == CourseStatus.Draft ? "Draft"
                        : "Closed",
                    CanAssign = c.Status == CourseStatus.Open,
                    CanLearnerAccess = c.Status == CourseStatus.Open || c.Status == CourseStatus.Closed,
                    c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    DivisionId = c.Category != null ? c.Category.DivisionId : null,
                    c.CourseTypeId,
                    CourseTypeName = c.CourseType != null ? c.CourseType.Name : null
                })
                .AsQueryable();

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.DivisionId == _currentUser.DivisionId.Value);

            return Ok(await DataSourceLoader.LoadAsync(query, loadOptions));
        }

        [HttpGet("Get/{id}")]
        public override async Task<IActionResult> Get(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            return Ok(course);
        }

        [HttpGet("GetForLookup")]
        public Task<IActionResult> GetForLookup(DataSourceLoadOptions loadOptions)
        {
            var query = _repository.GetQuery().AsQueryable();

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value);

            return Task.FromResult<IActionResult>(Ok(DataSourceLoader.Load(query.Select(c => new { c.Id, c.Code }), loadOptions)));
        }

        [HttpGet("GetActive")]
        public Task<IActionResult> GetActive(DataSourceLoadOptions loadOptions)
        {
            IQueryable<Course> query = _repository.GetQuery()
                .Include(c => c.Category!).ThenInclude(category => category.Division)
                .Include(c => c.CourseType)
                .Include(c => c.Versions)
                .Where(c => c.Status == CourseStatus.Open && c.Versions.Any(v => v.IsActive
                    && v.CourseContentItems.Any()
                    && v.CourseContentItems.All(cr => cr.ContentItem != null
                        && cr.ContentItem.IsActive
                        && cr.ContentItem.URL != null
                        && cr.ContentItem.URL != ""
                        && (cr.ContentItem.LaunchHref != null && cr.ContentItem.LaunchHref != ""
                            || cr.ContentItem.URL.StartsWith("http://")
                            || cr.ContentItem.URL.StartsWith("https://")
                            || cr.ContentItem.URL.StartsWith("/")
                            || cr.ContentItem.URL.Contains("/")
                            || cr.ContentItem.URL.Contains(".")))));

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value);

            var projected = query.Select(c => new
            {
                c.Id,
                c.Code,
                c.Title,
                c.IsActive,
                c.Status,
                StatusName = c.Status == CourseStatus.Open ? "Open"
                    : c.Status == CourseStatus.Draft ? "Draft"
                    : "Closed",
                CanAssign = c.Status == CourseStatus.Open,
                CanLearnerAccess = c.Status == CourseStatus.Open || c.Status == CourseStatus.Closed,
                c.CategoryId,
                CategoryName = c.Category != null ? c.Category.Name : null,
                DivisionId = c.Category != null ? c.Category.DivisionId : null,
                c.CourseTypeId,
                CourseTypeName = c.CourseType != null ? c.CourseType.Name : null
            });

            return Task.FromResult<IActionResult>(Ok(DataSourceLoader.Load(projected, loadOptions)));
        }
    }
}
