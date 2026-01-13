using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DivisionsController : ControllerBase
    {
        private readonly IGenericRepository<Division> _repo;

        public DivisionsController(IGenericRepository<Division> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repo.GetAllAsync();
            return Ok(items.Select(x => x.ToDto()));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DivisionDto dto) // ใช้ Dto รับค่า Name
        {
            var entity = new Division { Name = dto.Name };
            var result = await _repo.AddAsync(entity);
            return Ok(result.ToDto());
        }
    }
}