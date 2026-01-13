using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IGenericRepository<Role> _repo;

        public RolesController(IGenericRepository<Role> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // ถ้าอยากได้ DivisionName ด้วย ต้องแก้ GenericRepo ให้รองรับ Include
            // หรือในระยะสั้นยอมให้ DivisionName ว่างไปก่อนได้
            var items = await _repo.GetAllAsync();
            return Ok(items.Select(x => x.ToDto()));
        }

        // API สำหรับ Dropdown เลือก Role ตาม Division
        [HttpGet("by-division/{divisionId}")]
        public async Task<IActionResult> GetByDivision(int divisionId)
        {
            var items = await _repo.GetAsync(r => r.DivisionId == divisionId);
            return Ok(items.Select(x => x.ToDto()));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleDto dto)
        {
            var entity = new Role { Name = dto.Name, DivisionId = dto.DivisionId };
            var result = await _repo.AddAsync(entity);
            return Ok(result.ToDto());
        }
    }
}