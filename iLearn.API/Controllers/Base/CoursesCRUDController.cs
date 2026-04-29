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
    public class CoursesCRUDController : GenericController<Course>
    {
        public CoursesCRUDController(
            IGenericRepository<Course> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

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

        [HttpGet("GetForLookup")]
        public async Task<IActionResult> GetForLookup(DataSourceLoadOptions loadOptions)
        {
            var query = _repository.GetQuery().AsQueryable();

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value);

            return Ok(DataSourceLoader.Load(query.Select(c => new { c.Id, c.Code }), loadOptions));
        }

        [HttpGet("GetActive")]
        public async Task<IActionResult> GetActive(DataSourceLoadOptions loadOptions)
        {
            IQueryable<Course> query = _repository.GetQuery()
                .Include(c => c.Category).ThenInclude(cat => cat.Division)
                .Include(c => c.CourseType)
                .Include(c => c.Versions)
                .Where(c => c.IsActive && c.Versions.Any(v => v.IsActive
                    && v.CourseResources.Any()
                    && v.CourseResources.All(cr => cr.Resource != null
                        && cr.Resource.IsActive
                        && cr.Resource.URL != null
                        && cr.Resource.URL != ""
                        && (cr.Resource.ResourceHref != null && cr.Resource.ResourceHref != ""
                            || cr.Resource.URL.StartsWith("http://")
                            || cr.Resource.URL.StartsWith("https://")
                            || cr.Resource.URL.StartsWith("/")
                            || cr.Resource.URL.Contains("/")
                            || cr.Resource.URL.Contains(".")))));

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value);

            return Ok(await DataSourceLoader.LoadAsync(query, loadOptions));
        }
    }
}
