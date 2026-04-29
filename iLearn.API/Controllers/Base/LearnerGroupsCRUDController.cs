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
    public class LearnerGroupsCRUDController : GenericController<LearnerGroup>
    {
        private readonly ILearnerGroupService _service;

        public LearnerGroupsCRUDController(
            IGenericRepository<LearnerGroup> repository,
            ICurrentUserService currentUser,
            ILearnerGroupService service) : base(repository, currentUser)
        {
            _service = service;
        }

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
                    g.CategoryId,
                    CategoryName = g.Category != null ? g.Category.Name : null,
                    MemberCount = g.Members.Count(),
                    g.CreatedBy,
                    g.CreatedAt
                });

            if (_currentUser.DivisionId.HasValue)
                query = query.Where(g => g.DivisionId == _currentUser.DivisionId.Value);

            return Ok(await DataSourceLoader.LoadAsync(query, loadOptions));
        }

        [HttpPost("Post")]
        public override async Task<IActionResult> Post([FromForm] string values)
        {
            var newEntity = new LearnerGroup();
            JsonConvert.PopulateObject(values, newEntity);

            if (string.IsNullOrWhiteSpace(newEntity.Description))
            {
                return BadRequest(new { message = "Description is required." });
            }

            try
            {
                var created = await _service.CreateAsync(new CreateLearnerGroupDto
                {
                    Name = newEntity.Name ?? string.Empty,
                    Description = newEntity.Description,
                    CategoryId = newEntity.CategoryId
                });
                return Ok(created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var entity = await _repository.GetByIdAsync(key);
            if (entity == null) return NotFound();

            JsonConvert.PopulateObject(values, entity);

            if (string.IsNullOrWhiteSpace(entity.Description))
            {
                return BadRequest(new { message = "Description is required." });
            }

            try
            {
                await _service.UpdateAsync(key, new UpdateLearnerGroupDto
                {
                    Name = entity.Name ?? string.Empty,
                    Description = entity.Description,
                    CategoryId = entity.CategoryId
                });
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("Delete")]
        public override async Task<IActionResult> Delete([FromForm] int key)
        {
            try
            {
                await _service.DeleteAsync(key);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
