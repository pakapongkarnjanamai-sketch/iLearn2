using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iLearn.API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly ICurrentUserService _currentUser;

        public CategoriesController(
            IGenericRepository<Category> categoryRepo,
            ICurrentUserService currentUser)
        {
            _categoryRepo = categoryRepo;
            _currentUser = currentUser;
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup(DataSourceLoadOptions loadOptions)
        {
            var query = _categoryRepo.GetQuery().AsNoTracking();

            if (_currentUser.DivisionId.HasValue)
            {
                query = query.Where(c => c.DivisionId == _currentUser.DivisionId.Value);
            }

            var lookupQuery = query
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    DivisionId = c.DivisionId
                });

            return Ok(await DataSourceLoader.LoadAsync(lookupQuery, loadOptions));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var query = _categoryRepo.GetQuery().AsNoTracking().Where(c => c.Id == id);

            if (_currentUser.DivisionId.HasValue)
            {
                query = query.Where(c => c.DivisionId == _currentUser.DivisionId.Value);
            }

            var category = await query
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    DivisionId = c.DivisionId
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }
    }
}