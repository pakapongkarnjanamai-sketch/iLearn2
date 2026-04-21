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
    public class StudentGroupsCRUDController : GenericController<StudentGroup>
    {
        public StudentGroupsCRUDController(
            IGenericRepository<StudentGroup> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            var query = _repository.GetQuery()
                .AsNoTracking()
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    g.DivisionId,
                    MemberCount = g.Members.Count(),
                    g.CreatedBy,
                    g.CreatedAt
                });

            if (_currentUser.DivisionId.HasValue)
                query = query.Where(g => g.DivisionId == _currentUser.DivisionId.Value);

            return Ok(await DataSourceLoader.LoadAsync(query, loadOptions));
        }

        [HttpDelete("Delete")]
        public override async Task<IActionResult> Delete([FromForm] int key)
        {
            var entity = await _repository.GetByIdAsync(key);
            if (entity == null) return NotFound();

            if (_currentUser.DivisionId.HasValue && entity.DivisionId != _currentUser.DivisionId.Value)
                return NotFound();

            await _repository.DeleteAsync(entity);
            return Ok();
        }
    }
}
