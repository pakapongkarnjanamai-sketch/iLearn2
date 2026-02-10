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
        private readonly ICurrentUserService _currentUser;
        private readonly IGenericRepository<LearningLog> _logRepo;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        public EnrollmentsController(
            IGenericRepository<Enrollment> enrollmentRepo,
            ICurrentUserService currentUserService,
            ICurrentUserService currentUser,
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<CourseVersion> versionRepo)
        {
            _enrollmentRepo = enrollmentRepo;
            _currentUserService = currentUserService;
            _currentUser = currentUser;
            _logRepo = logRepo;
            _versionRepo = versionRepo;
        }

 
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses([FromQuery] string studentCode)
        {
         
            // ตรวจสอบว่ามีการส่งค่ามาหรือไม่
            if (string.IsNullOrEmpty(studentCode))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Student code is required." });
            }

            // 2. Query ข้อมูล โดยใช้ studentCode ที่รับเข้ามา
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

        // GET: api/enrollments/player-info/{enrollmentId}?studentCode=EMPxxx
        [HttpGet("player-info/{enrollmentId}")]
        public async Task<IActionResult> GetPlayerInfo(int enrollmentId, [FromQuery] string studentCode)
        {
            // 1. ดึงข้อมูล Enrollment พร้อม Course (ใช้ GetAsync เพื่อ Include Course ได้ง่าย)
            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.Id == enrollmentId,
                includeProperties: "Course"
            );
            var enrollment = enrollments.FirstOrDefault();

            if (enrollment == null)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = "Enrollment not found" });
            }

            // 2. ตรวจสอบว่า StudentCode ตรงกันหรือไม่ (Security Check)
            if (string.IsNullOrEmpty(studentCode) || !string.Equals(enrollment.StudentCode, studentCode, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Unauthorized access: Student code mismatch." });
            }

            // 3. ดึง CourseVersion
            // [แก้ไข] ใช้ CourseId และ EnrolledVersion (เลขเวอร์ชัน) ในการค้นหา
            var targetVersionNumber = enrollment.EnrolledVersion;

            var versions = await _versionRepo.GetAsync(
                filter: v => v.CourseId == enrollment.CourseId && v.VersionNumber == targetVersionNumber,
                includeProperties: "CourseResources.Resource,Course"
            );

            var targetVersion = versions.FirstOrDefault();

            // Fallback: ถ้าหาเวอร์ชันที่ระบุไม่เจอ ให้ลองดึงเวอร์ชันล่าสุดที่ Active (เผื่อกรณีข้อมูลเก่า)
            if (targetVersion == null)
            {
                var activeVersions = await _versionRepo.GetAsync(
                   filter: v => v.CourseId == enrollment.CourseId && v.IsActive,
                 
                   includeProperties: "CourseResources.Resource,Course"
               );
                targetVersion = activeVersions
                                .OrderByDescending(v => v.VersionNumber)
                                .FirstOrDefault();
            }


            if (targetVersion == null)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = "Content not found for this course version." });
            }

            // 4. Map ข้อมูลลง DTO
            var resources = targetVersion.CourseResources
                .Select(cr => new PlayerResourceDto
                {
                    Id = cr.Resource.Id,
                    Name = cr.Resource.Name,
                    Type = cr.Resource.TypeId == 2 ? "Exam" : "Lesson", // ปรับตาม TypeId ของคุณ
                    LaunchUrl = cr.Resource.URL
                }).ToList();

            var dto = new PlayerInfoDto
            {
                CourseVersionId = targetVersion.Id, // ส่ง ID จริงของ Version กลับไป
                StudentCode = enrollment.StudentCode,
                CourseTitle = targetVersion.Course?.Title ?? "Unknown Course",
                Resources = resources
            };

            return Ok(new ApiResponse<PlayerInfoDto> { Success = true, Data = dto });
        }

   

    }
}