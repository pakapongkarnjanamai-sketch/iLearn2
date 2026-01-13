using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LearningLogsController : ControllerBase
    {
        private readonly IGenericRepository<LearningLog> _logRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;

        public LearningLogsController(
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<Enrollment> enrollmentRepo)
        {
            _logRepo = logRepo;
            _enrollmentRepo = enrollmentRepo;
        }

        // GET: api/learninglogs?studentCode=EMP001&courseId=5
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? studentCode, [FromQuery] int? courseId)
        {
            IReadOnlyList<LearningLog> logs;

            if (!string.IsNullOrEmpty(studentCode) && courseId.HasValue)
            {
                logs = await _logRepo.GetAsync(l => l.StudentCode == studentCode && l.CourseId == courseId.Value);
            }
            else if (!string.IsNullOrEmpty(studentCode))
            {
                logs = await _logRepo.GetAsync(l => l.StudentCode == studentCode);
            }
            else
            {
                // ไม่ควรอนุญาตให้ดึงทั้งหมดโดยไม่มีเงื่อนไขถ้าข้อมูลเยอะ (อาจต้องทำ Pagination)
                logs = await _logRepo.GetAllAsync();
            }

            return Ok(logs.Select(l => l.ToDto()));
        }

        // POST: api/learninglogs
        // API นี้จะถูกเรียกโดย SCORM Player หรือ Video Player ทุกๆ x วินาที หรือเมื่อจบ
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLearningLogDto dto)
        {
            var log = dto.ToEntity();
            var createdLog = await _logRepo.AddAsync(log);

            // [Optional] Update Enrollment Status?
            // ถ้าเป็นการส่ง log ครั้งสุดท้ายว่าเรียนจบแล้ว อาจจะไปอัปเดต Enrollment เลยก็ได้
            // await UpdateEnrollmentProgress(dto.StudentCode, dto.CourseId);

            return Ok(createdLog.ToDto());
        }

        // ตัวอย่าง Helper Function สำหรับอัปเดต Enrollment (ถ้าต้องการ)
        private async Task UpdateEnrollmentProgress(string studentCode, int courseId)
        {
            var enrollments = await _enrollmentRepo.GetAsync(e => e.StudentCode == studentCode && e.CourseId == courseId);
            var enrollment = enrollments.FirstOrDefault();

            if (enrollment != null && enrollment.Status != "Completed")
            {
                // Logic ง่ายๆ: มี Log เข้ามาถือว่า In Progress
                enrollment.Status = "In Progress";
                await _enrollmentRepo.UpdateAsync(enrollment);
            }
        }
    }
}