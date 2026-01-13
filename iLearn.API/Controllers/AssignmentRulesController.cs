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
    public class AssignmentRulesController : ControllerBase
    {
        private readonly IGenericRepository<AssignmentRule> _repo;
        private readonly ICourseAssignmentService _assignmentService;

        public AssignmentRulesController(
            IGenericRepository<AssignmentRule> repo,
            ICourseAssignmentService assignmentService)
        {
            _repo = repo;
            _assignmentService = assignmentService;
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var rules = await _repo.GetAsync(r => r.CourseId == courseId);
            return Ok(rules.Select(r => r.ToDto()));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAssignmentRuleDto dto)
        {
            var rule = dto.ToEntity();
            var result = await _repo.AddAsync(rule);

            // 💡 Option: พอเพิ่มกฎใหม่ปุ๊บ อยากให้ Assign ทันทีเลยไหม?
            // ถ้าใช่ ให้เรียก Service นี้ (แต่ระวัง Performance ถ้าคนเยอะ อาจแยกปุ่มกดต่างหาก)
            // await _assignmentService.ProcessAssignmentForCourseAsync(result.CourseId);

            return Ok(result.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _repo.GetByIdAsync(id);
            if (rule == null) return NotFound();

            await _repo.DeleteAsync(rule);
            return NoContent();
        }
    }
}