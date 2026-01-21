using iLearn.Application.Common; // สำหรับ ApiResponse
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services; // เพิ่ม namespace นี้
using iLearn.Application.Mappings;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Authorization; // จำเป็นสำหรับการระบุ User
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Authorize] // บังคับว่าต้อง Login ถึงจะเรียกหน้านี้ได้
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly ICurrentUserService _currentUserService; // 1. เพิ่ม Service ระบุตัวตน

        public EnrollmentsController(
            IGenericRepository<Enrollment> enrollmentRepo,
            ICurrentUserService currentUserService)
        {
            _enrollmentRepo = enrollmentRepo;
            _currentUserService = currentUserService;
        }

        // GET: api/enrollments/my-courses
        // Endpoint นี้จะดึงเฉพาะคอร์สของคนที่ Login อยู่เท่านั้น
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            // 1. ดึง StudentCode จาก Token ของผู้ใช้งานปัจจุบัน
            var studentCode = _currentUserService.UserId;

            if (string.IsNullOrEmpty(studentCode))
            {
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "User identity not found." });
            }

            // 2. Query ข้อมูล โดย Include "Course" มาด้วยเพื่อให้ได้ชื่อคอร์สและรูปภาพ
            // หมายเหตุ: includeProperties ต้องตรงกับชื่อ Property ใน Entity Enrollment
            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.StudentCode == studentCode,
                includeProperties: "Course"
            );

            // 3. แปลงเป็น DTO
            var dtos = enrollments.Select(e => e.ToDto()).ToList();

            return Ok(new ApiResponse<IEnumerable<EnrollmentDto>>
            {
                Success = true,
                Data = dtos
            });
        }

        // --- Existing Methods (คงเดิมไว้ หรือปรับให้ใช้ ApiResponse) ---

        // GET: api/enrollments?studentCode=EMP001
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? studentCode, [FromQuery] int? courseId)
        {
            IReadOnlyList<Enrollment> enrollments;

            if (!string.IsNullOrEmpty(studentCode) && courseId.HasValue)
            {
                enrollments = await _enrollmentRepo.GetAsync(e => e.StudentCode == studentCode && e.CourseId == courseId.Value, includeProperties: "Course");
            }
            else if (!string.IsNullOrEmpty(studentCode))
            {
                enrollments = await _enrollmentRepo.GetAsync(e => e.StudentCode == studentCode, includeProperties: "Course");
            }
            else if (courseId.HasValue)
            {
                enrollments = await _enrollmentRepo.GetAsync(e => e.CourseId == courseId.Value, includeProperties: "Course");
            }
            else
            {
                enrollments = await _enrollmentRepo.GetAllAsync();
            }

            var dtos = enrollments.Select(e => e.ToDto());
            return Ok(new ApiResponse<IEnumerable<EnrollmentDto>> { Success = true, Data = dtos });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(id);
            if (enrollment == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Not Found" });

            return Ok(new ApiResponse<EnrollmentDto> { Success = true, Data = enrollment.ToDto() });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(id);
            if (enrollment == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Not Found" });

            enrollment.Status = status;

            if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || status.Equals("Passed", StringComparison.OrdinalIgnoreCase))
            {
                enrollment.CompletedDate = DateTime.UtcNow; // ควรใช้ DateTimeService ถ้ามี
            }

            await _enrollmentRepo.UpdateAsync(enrollment);

            return Ok(new ApiResponse<EnrollmentDto> { Success = true, Data = enrollment.ToDto() });
        }
    }
}