using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IGenericRepository<Role> _repo;
        private readonly ICurrentUserService _currentUser;

        public RolesController(IGenericRepository<Role> repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repo.GetAllAsync();

            // ── Data Isolation: กรองตาม DivisionId ของผู้ใช้ปัจจุบัน ──
            if (_currentUser.DivisionId.HasValue)
            {
                var myDivId = _currentUser.DivisionId.Value;
                items = items.Where(r => r.DivisionId == myDivId || r.DivisionId == null).ToList();
            }

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
            // ── Data Isolation: บังคับ DivisionId จากผู้ใช้ปัจจุบัน ──
            var divisionId = dto.DivisionId;
            if (_currentUser.DivisionId.HasValue)
            {
                divisionId = _currentUser.DivisionId.Value;
            }

            var entity = new Role { Name = dto.Name, DivisionId = divisionId };
            var result = await _repo.AddAsync(entity);
            return Ok(result.ToDto());
        }
    }
}