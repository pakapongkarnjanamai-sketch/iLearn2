using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;

        public EnrollmentsController(IGenericRepository<Enrollment> enrollmentRepo)
        {
            _enrollmentRepo = enrollmentRepo;
        }

        // GET: api/enrollments?studentCode=EMP001
        // ใช้ดูว่านักเรียนรหัสนี้ มีคอร์สอะไรบ้าง
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? studentCode, [FromQuery] int? courseId)
        {
            IReadOnlyList<Enrollment> enrollments;

            // ใช้ GetAsync ของ Repository เพื่อ Filter ข้อมูลจาก Database โดยตรง (Performance ดีกว่า GetAll)
            if (!string.IsNullOrEmpty(studentCode) && courseId.HasValue)
            {
                enrollments = await _enrollmentRepo.GetAsync(e => e.StudentCode == studentCode && e.CourseId == courseId.Value);
            }
            else if (!string.IsNullOrEmpty(studentCode))
            {
                enrollments = await _enrollmentRepo.GetAsync(e => e.StudentCode == studentCode);
            }
            else if (courseId.HasValue)
            {
                enrollments = await _enrollmentRepo.GetAsync(e => e.CourseId == courseId.Value);
            }
            else
            {
                enrollments = await _enrollmentRepo.GetAllAsync();
            }

            // แปลง Entity เป็น DTO
            var dtos = enrollments.Select(e => e.ToDto());
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(id);
            if (enrollment == null) return NotFound();

            return Ok(enrollment.ToDto());
        }

        // PUT: api/enrollments/5/status
        // ใช้จำลองการเรียนจบ (Update Progress)
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(id);
            if (enrollment == null) return NotFound();

            enrollment.Status = status;

            // ถ้าเรียนจบ ให้ใส่วันที่จบ
            if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                enrollment.CompletedDate = DateTime.UtcNow;
            }

            await _enrollmentRepo.UpdateAsync(enrollment);

            return Ok(enrollment.ToDto());
        }
    }
}