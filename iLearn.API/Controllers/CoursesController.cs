using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ICourseAssignmentService _assignmentService;

        public CoursesController(
            ICourseRepository courseRepo,
            ICourseAssignmentService assignmentService)
        {
            _courseRepo = courseRepo;
            _assignmentService = assignmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // ใช้ GetAllAsync หรือ GetActiveCoursesAsync ตามโจทย์
            var courses = await _courseRepo.GetAllAsync();
            var dtos = courses.Select(c => c.ToDto());
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();
            return Ok(course.ToDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCourseDto dto)
        {
            // 1. ตรวจสอบรหัสซ้ำ
            if (!await _courseRepo.IsCourseCodeUniqueAsync(dto.Code))
            {
                return BadRequest($"Course code '{dto.Code}' already exists.");
            }

            // 2. แปลง DTO -> Entity
            var course = dto.ToEntity();

            // 3. บันทึกลง DB
            var createdCourse = await _courseRepo.AddAsync(course);

            // 4. [สำคัญ] Trigger ระบบ Auto-Assignment! 🚀
            // ถ้าเป็น General -> แจกทุกคน
            // ถ้าเป็น Special -> เช็คกฎ (แต่ตอนสร้างใหม่อาจจะยังไม่มีกฎ ต้องไปเพิ่มกฎทีหลังแล้วค่อยกด Assign ก็ได้)
            if (createdCourse.Type == CourseType.General)
            {
                await _assignmentService.ProcessAssignmentForCourseAsync(createdCourse.Id);
            }

            return CreatedAtAction(nameof(GetById), new { id = createdCourse.Id }, createdCourse.ToDto());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateCourseDto dto)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            // Update fields
            course.Title = dto.Title;
            course.Description = dto.Description;
            // course.Code = dto.Code; // ปกติไม่ค่อยให้แก้ Code
            // course.Type = dto.Type; // เปลี่ยน Type อาจต้องคิดเรื่อง Re-assign

            await _courseRepo.UpdateAsync(course);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            await _courseRepo.DeleteAsync(course);
            return NoContent();
        }

        // Endpoint พิเศษสำหรับกด Assign Manual (เผื่อกรณีสร้างกฎทีหลัง)
        [HttpPost("{id}/assign-now")]
        public async Task<IActionResult> TriggerAssignment(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            await _assignmentService.ProcessAssignmentForCourseAsync(id);

            return Ok(new { message = "Assignment process started." });
        }
    }
}