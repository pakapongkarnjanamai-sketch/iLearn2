using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly ICourseAssignmentService _enrollmentService;
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICurrentUserService _currentUser;
        private readonly IGenericRepository<LearningLog> _logRepo;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly IScormService _scormService;

        public EnrollmentsController(
            IGenericRepository<Enrollment> enrollmentRepo,
            ICourseAssignmentService enrollmentService,
            IGenericRepository<Assignment> assignmentRepo,
            ICurrentUserService currentUserService,
            ICurrentUserService currentUser,
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<CourseVersion> versionRepo,
            IScormService scormService)
        {
            _enrollmentRepo = enrollmentRepo;
            _enrollmentService = enrollmentService;
            _assignmentRepo = assignmentRepo;
            _currentUserService = currentUserService;
            _currentUser = currentUser;
            _logRepo = logRepo;
            _versionRepo = versionRepo;
            _scormService = scormService;
        }
        [HttpPost("ResetStatus")]
        public async Task<IActionResult> ResetStatus([FromQuery] int key)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(key);
            if (enrollment == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            // 1. Reset ข้อมูลสรุปใน Enrollment
            enrollment.IsCompleted = false;
            enrollment.CompletedDate = null;
            enrollment.Progress = 0;
            await _enrollmentRepo.UpdateAsync(enrollment);

            // 2. Reset สถานะใน LearningLogs ชุดเดิม (ทางเลือกที่ 2: เก็บเวลาสะสมไว้)
            var logs = await _logRepo.GetAsync(l => l.EnrollmentId == key);
            foreach (var log in logs)
            {
                log.Status = "incomplete"; // เปลี่ยนเพื่อให้ Player ยอมบันทึกใหม่
                log.Progress = 0;
                await _logRepo.UpdateAsync(log);
            }

            return Ok(new { success = true });
        }
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses([FromQuery] string studentCode)
        {
            if (string.IsNullOrEmpty(studentCode))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "StudentDto code is required." });
            }

            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.StudentCode == studentCode,
                includeProperties: "Course"
            );

            var dtos = enrollments.OrderBy(a => a.IsCompleted).OrderBy(b =>b.DueDate).Select(e => e.ToDto()).ToList();

            return Ok(new ApiResponse<IEnumerable<EnrollmentDto>>
            {
                Success = true,
                Data = dtos
            });
        }

        // [New] API สำหรับดึง Player Info โดยใช้ Course ID
        // รองรับทั้งแบบมี Enrollment (Scoring) และไม่มี (View Only)
        [HttpGet("player-info/{courseId}")]
        public async Task<IActionResult> GetPlayerInfoByCourse(int courseId, [FromQuery] string studentCode)
        {
            // 1. ค้นหา Enrollment
            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.CourseId == courseId && e.StudentCode == studentCode,
                includeProperties: "Course"
            );
            var enrollment = enrollments.FirstOrDefault();

            CourseVersion? targetVersion = null;
            bool isReadOnly = false;
            bool isCompleted = false;
            List<LearningLog> userLogs = new();

            if (enrollment != null)
            {
                // --- กรณีมี Enrollment (Scoring Mode) ---
                var targetVersionNumber = enrollment.EnrolledCourseVersion;
                isCompleted = enrollment.IsCompleted;

                // ดึง Version ที่ลงทะเบียนไว้
                var versions = await _versionRepo.GetAsync(
                    filter: v => v.CourseId == courseId && v.Id == targetVersionNumber,
                    includeProperties: "CourseResources.Resource,Course"
                );
                targetVersion = versions.FirstOrDefault();

                // ดึง Log การเรียน
                if (targetVersion != null)
                {
                    userLogs = (await _logRepo.GetAsync(l =>
                        l.StudentCode == studentCode &&
                        l.CourseVersionId == targetVersion.Id
                    )).ToList();
                }
            }
            else
            {
                // --- กรณีไม่มี Enrollment (View Only Mode) ---
                isReadOnly = true;

                // ดึง Version ล่าสุดที่ Active มาแสดง
                var activeVersions = await _versionRepo.GetAsync(
                  filter: v => v.CourseId == courseId && v.IsActive,
                  includeProperties: "CourseResources.Resource,Course"
                );
                targetVersion = activeVersions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            }

            if (targetVersion == null)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = "Content not found or Course is not active" });
            }

            // 2. Map ข้อมูลลง DTO
            var resources = targetVersion.CourseResources
                .OrderBy(cr => cr.Resource.TypeId == 1 ? 0 : 1) // Lesson first
                .ThenBy(cr => cr.Resource.Name)
                .Select(cr => {
                    // หา Log ของ Resource นี้ (ถ้ามี)
                    var log = userLogs.FirstOrDefault(l => l.ResourceId == cr.Resource.Id);

                    bool isDone = log != null && (
                        log.Status.ToLower() == "completed" ||
                        log.Status.ToLower() == "passed"
                    );

                    return new PlayerResourceDto
                    {
                        Id = cr.Resource.Id,
                        Name = cr.Resource.Name,
                        Type = cr.Resource.TypeId == 2 ? "Exam" : "Lesson",
                        LaunchUrl = !string.IsNullOrEmpty(cr.Resource.URL) && !string.IsNullOrEmpty(cr.Resource.ResourceHref)
                            ? _scormService.GetScormUrl(cr.Resource.URL, cr.Resource.ResourceHref)
                            : cr.Resource.URL ?? string.Empty,
                        IsCompleted = isDone,
                        Score = log?.Score,
                        Time = log?.SessionTime
                    };
                })
                .ToList();

            var dto = new PlayerInfoDto
            {
                CourseVersionId = targetVersion.Id,
                StudentCode = studentCode,
                CourseTitle = targetVersion.Course?.Title ?? "Unknown Course",
                IsCompleted = isCompleted,
                IsReadOnly = isReadOnly, // ส่ง Flag นี้กลับไป
                EnrollmentId = enrollment?.Id,
                Resources = resources

            };

            return Ok(new ApiResponse<PlayerInfoDto> { Success = true, Data = dto });
        }

        // --- Existing Methods ---
        // (Methods เดิมเช่น GetById, UpdateCompletion สามารถคงไว้ได้ตามปกติ)
        // ... (ตัด code เดิมออกเพื่อความกระชับ แต่ในการใช้งานจริงให้คงไว้)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(id);
            if (enrollment == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Not Found" });
            return Ok(new ApiResponse<EnrollmentDto> { Success = true, Data = enrollment.ToDto() });
        }

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

        [HttpPost("BulkAssign")]
        public async Task<IActionResult> BulkAssign([FromBody] BulkAssignDto dto)
        {
            if (dto.CourseIds == null || !dto.CourseIds.Any() || dto.EmployeeCodes == null || !dto.EmployeeCodes.Any())
            {
                return BadRequest(new { message = "Courses and Employees are required." });
            }

            // 1. สร้าง Assignments No (รันเลข)
            // ตัวอย่างการทำ Format: AS-YYYYMMDD-001 (ของจริงอาจจะต้องไป Query หาเลขล่าสุดใน DB มา +1 ครับ)
            string datePrefix = DateTime.Now.ToString("yyyyMMdd");
            // TODO: Query หาเลข Running จาก DB 
            // int nextRunningNo = (_dbContext.Assignments.Count(x => x.AssignmentNo.StartsWith($"AS-{datePrefix}")) + 1);
            int nextRunningNo = 1; // สมมติว่าเป็น 1
            string assignmentNo = $"AS-{datePrefix}-{nextRunningNo:D3}";

            // แปลง Array พนักงานให้อยู่ในรูป Comma-separated (ถ้า Database ของคุณเก็บเป็น String)
            string employeesStr = string.Join(",", dto.EmployeeCodes);

            // 2. วนลูปสร้าง Assignments Rule ตามจำนวนวิชาที่เลือก
            foreach (var courseId in dto.CourseIds)
            {
                var rule = new Assignment
                {
                    AssignmentNo = assignmentNo,
                    Description = dto.Description,
                    CourseId = courseId,
                    EmployeeCodes = employeesStr,
                    StartDate = dto.StartDate,
                    DueDate = dto.DueDate,
                    Division = dto.Division
                };

              
                // บันทึก Rule ลง Database
                await _assignmentRepo.AddAsync(rule);

                // 3. นำ EmployeeCodes ไป Insert ลงตาราง Enrollment และผูกกับ rule.Id ด้วย
                await _enrollmentService.AssignCourseToEmployees(courseId, dto.EmployeeCodes, dto.StartDate, dto.DueDate, rule.Id);
            }

            return Ok(new { message = "Courses assigned successfully!", assignmentNo = assignmentNo });
        }
    }
}