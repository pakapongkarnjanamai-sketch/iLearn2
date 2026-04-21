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
    public class CourseVersionsCRUDController : GenericController<CourseVersion>
    {
        public CourseVersionsCRUDController(
            IGenericRepository<CourseVersion> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get/{id}")]
        public override async Task<IActionResult> Get(int id)
        {
            var entity = await _repository.GetQuery()
                .Include(c => c.Course).ThenInclude(ca => ca.Category)
                .Include(cr => cr.CourseResources).ThenInclude(c => c.Resource)
                .Where(i => i.Id == id).ToListAsync();

            if (entity == null) return NotFound();
            return Ok(entity);
        }
    }
}
