using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IGenericRepository<User> _userRepo;
        private readonly ICourseAssignmentService _assignmentService;

        public UsersController(
            IGenericRepository<User> userRepo,
            ICourseAssignmentService assignmentService)
        {
            _userRepo = userRepo;
            _assignmentService = assignmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userRepo.GetAllAsync();
            var dtos = users.Select(u => u.ToDto());
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user.ToDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            // 1. สร้าง User ตามปกติ
            var user = dto.ToEntity();

            // (ควรเช็ค User ซ้ำตรงนี้ด้วย Nid)

            var createdUser = await _userRepo.AddAsync(user);

            // 2. [สำคัญ] Trigger ระบบ Auto-Assignment! 🚀
            // ระบบจะไปค้นหาคอร์ส General ทั้งหมดแล้วยัดให้ User คนนี้ทันที
            await _assignmentService.AssignGeneralCoursesToNewUserAsync(createdUser.Id);

            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            await _userRepo.DeleteAsync(user);
            return NoContent();
        }
    }
}