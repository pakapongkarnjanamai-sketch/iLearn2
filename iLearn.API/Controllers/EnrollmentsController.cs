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

        // New: update completion flag
        [HttpPut("{id}/completion")]
        public async Task<IActionResult> UpdateCompletion(int id, [FromBody] bool isComplete)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(id);
            if (enrollment == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Not Found" });

            enrollment.IsCompleted = isComplete;
            if (isComplete)
            {
                enrollment.CompletedDate = DateTime.UtcNow;
                enrollment.Progress = 100;
            }
            else
            {
                enrollment.CompletedDate = null;
            }

            await _enrollmentRepo.UpdateAsync(enrollment);

            return Ok(new ApiResponse<EnrollmentDto> { Success = true, Data = enrollment.ToDto() });
        }

        // GET: api/enrollments/player-info/{enrollmentId}?studentCode=EMPxxx
        [HttpGet("player-info/{enrollmentId}")]
        public async Task<IActionResult> GetPlayerInfo(int enrollmentId, [FromQuery] string studentCode)
        {
            // 1. ดึง Enrollment
            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.Id == enrollmentId,
                includeProperties: "Course"
            );
            var enrollment = enrollments.FirstOrDefault();

            if (enrollment == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Enrollment not found" });

            // 2. Security Check
            if (string.IsNullOrEmpty(studentCode) || !string.Equals(enrollment.StudentCode, studentCode, StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Unauthorized access" });

            // 3. ดึง CourseVersion
            var targetVersionNumber = enrollment.EnrolledVersion;
            var versions = await _versionRepo.GetAsync(
                filter: v => v.CourseId == enrollment.CourseId && v.VersionNumber == targetVersionNumber,
                includeProperties: "CourseResources.Resource,Course"
            );
            var targetVersion = versions.FirstOrDefault();

            // (Fallback logic เดิมของคุณ...)
            if (targetVersion == null)
            {
                var activeVersions = await _versionRepo.GetAsync(
                  filter: v => v.CourseId == enrollment.CourseId && v.IsActive,
                  includeProperties: "CourseResources.Resource,Course"
              );
                targetVersion = activeVersions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            }

            if (targetVersion == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Content not found" });

            // -------------------------------------------------------------------------
            // 4. [สำคัญ] ดึงประวัติการเรียน (LearningLogs) ของ User คนนี้ ใน Version นี้
            // -------------------------------------------------------------------------
            var userLogs = await _logRepo.GetAsync(l =>
                l.StudentCode == studentCode &&
                l.CourseVersionId == targetVersion.Id
            );

            // 5. Map ข้อมูลลง DTO ผสมกันระหว่าง Resource + Log
            var resources = targetVersion.CourseResources
                .Select(cr => {
                    // หา Log ที่ตรงกับ Resource นี้
                    var log = userLogs.FirstOrDefault(l => l.ResourceId == cr.Resource.Id);

                    // ตรวจสอบว่าผ่านหรือยัง
                    bool isDone = log != null && (
                        log.Status.ToLower() == "completed" ||
                        log.Status.ToLower() == "passed"
                    );

                    return new PlayerResourceDto
                    {
                        Id = cr.Resource.Id,
                        Name = cr.Resource.Name,
                        Type = cr.Resource.TypeId == 2 ? "Exam" : "Lesson",
                        LaunchUrl = cr.Resource.URL,

                        // ใส่ข้อมูลจาก Log (ถ้ามี)
                        IsCompleted = isDone,
                        Score = log?.Score,
                        Time = log?.SessionTime // ส่งเวลาเดิมกลับไปแสดง
                    };
                })
                .ToList();

            var dto = new PlayerInfoDto
            {
                CourseVersionId = targetVersion.Id,
                StudentCode = enrollment.StudentCode,
                CourseTitle = targetVersion.Course?.Title ?? "Unknown Course",
                // send boolean flag
                IsCompleted = enrollment.IsCompleted,
                Resources = resources
            };

            return Ok(new ApiResponse<PlayerInfoDto> { Success = true, Data = dto });
        }


    }
}