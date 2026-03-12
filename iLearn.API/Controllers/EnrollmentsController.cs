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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly ICurrentUserService _currentUser;
        private readonly IGenericRepository<LearningLog> _logRepo;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly IScormService _scormService;
        private readonly IStudentGroupService _studentGroupService;
        private readonly IAssignmentNoGenerator _assignmentNoGen;

        public EnrollmentsController(
            IGenericRepository<Enrollment> enrollmentRepo,
            ICourseAssignmentService enrollmentService,
            IGenericRepository<Assignment> assignmentRepo,
            ICurrentUserService currentUser,
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<CourseVersion> versionRepo,
            IScormService scormService,
            IStudentGroupService studentGroupService,
            IAssignmentNoGenerator assignmentNoGen)
        {
            _enrollmentRepo      = enrollmentRepo;
            _enrollmentService   = enrollmentService;
            _assignmentRepo      = assignmentRepo;
            _currentUser         = currentUser;
            _logRepo             = logRepo;
            _versionRepo         = versionRepo;
            _scormService        = scormService;
            _studentGroupService = studentGroupService;
            _assignmentNoGen     = assignmentNoGen;
        }

        [HttpPost("ResetStatus")]
        public async Task<IActionResult> ResetStatus([FromQuery] int key)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(key);
            if (enrollment == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            // Reset ข้อมูลสรุปใน Enrollment และตั้ง ResetAt (Log เก่ายังอยู่ใน DB เพื่อ history)
            enrollment.IsCompleted   = false;
            enrollment.CompletedDate = null;
            enrollment.Progress      = 0;
            enrollment.ResetAt       = DateTime.Now;
            await _enrollmentRepo.UpdateAsync(enrollment);

            return Ok(new { success = true });
        }

        // --- ปรับปรุงฟังก์ชัน GetMyCourses ---
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses([FromQuery] string studentCode)
        {
            if (string.IsNullOrEmpty(studentCode))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Student code is required." });
            }

            var currentDate = DateTime.UtcNow;
            var oneMonthAgo  = currentDate.AddMonths(-1);

            // ดึง Enrollment พร้อม AssignmentLinks เพื่อใช้ StartDate/DueDate ที่ถูกต้องจากตารางกลาง
            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.StudentCode == studentCode && e.Course != null,
                includeProperties: "Course,AssignmentLinks"
            );

            // กรอง in-memory: แสดงเฉพาะที่อยู่ในช่วงเวลา หรือจบไปไม่เกิน 1 เดือน
            var filtered = enrollments.Where(e =>
            {
                if (e.IsCompleted)
                    return e.CompletedDate.HasValue && e.CompletedDate >= oneMonthAgo;

                // ใช้ dates จาก AssignmentLinks ถ้ามี ไม่งั้น fallback ไป Enrollment.StartDate/DueDate
                DateTime? effectiveStart = e.AssignmentLinks.Any()
                    ? e.AssignmentLinks.Min(a => a.StartDate)
                    : e.StartDate;
                DateTime? effectiveDue = e.AssignmentLinks.Any()
                    ? e.AssignmentLinks.Max(a => a.DueDate)
                    : e.DueDate;

                bool startOk = !effectiveStart.HasValue || effectiveStart <= currentDate;
                bool dueOk   = !effectiveDue.HasValue   || effectiveDue   >= currentDate;
                return startOk && dueOk;
            }).ToList();

            // แปลง + จัดเรียงตาม DueDate ที่ใกล้ที่สุด
            var dtos = filtered
                .OrderBy(e => e.IsCompleted)
                .ThenBy(e => e.AssignmentLinks.Any()
                    ? e.AssignmentLinks.Min(a => a.DueDate)
                    : e.DueDate)
                .Select(e =>
                {
                    var dto = e.ToDto();
                    // override ด้วย dates จาก AssignmentLinks (ใกล้ที่สุดก่อน)
                    if (e.AssignmentLinks.Any())
                    {
                        dto.StartDate = e.AssignmentLinks.Min(a => a.StartDate);
                        dto.DueDate   = e.AssignmentLinks.Min(a => a.DueDate);
                    }
                    return dto;
                })
                .ToList();

            return Ok(new ApiResponse<IEnumerable<EnrollmentDto>>
            {
                Success = true,
                Data    = dtos
            });
        }

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
                var targetVersionId = enrollment.EnrolledCourseVersion;
                isCompleted = enrollment.IsCompleted;

                // ดึง Version ที่ลงทะเบียนไว้ (ค้นหาจาก Id)
                var versions = await _versionRepo.GetAsync(
                    filter: v => v.CourseId == courseId && v.Id == targetVersionId,
                    includeProperties: "CourseResources.Resource,Course"
                );
                targetVersion = versions.FirstOrDefault();

                // ดึง Log การเรียน — กรอง Log ที่สร้างหลัง ResetAt เท่านั้น (Log เก่าถือเป็น history)
                if (targetVersion != null)
                {
                    userLogs = (await _logRepo.GetAsync(l =>
                        l.StudentCode     == studentCode       &&
                        l.CourseVersionId == targetVersion.Id  &&
                        l.EnrollmentId    == enrollment.Id     &&
                        (enrollment.ResetAt == null || l.CreatedAt >= enrollment.ResetAt)
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
                // เรียงตาม VersionNumber แล้วเอาตัวล่าสุด
                targetVersion = activeVersions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            }

            if (targetVersion == null)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = "Content not found or Course is not active" });
            }

            // 2. Map ข้อมูลลง DTO
            var resources = targetVersion.CourseResources
                .OrderBy(cr => cr.Resource.TypeId == 1 ? 0 : 1) // Learn first
                .ThenBy(cr => cr.Resource.Name)
                .Select(cr => {
                    var log = userLogs.FirstOrDefault(l => l.ResourceId == cr.Resource.Id);
                    bool isDone = log != null && (
                        log.Status.ToLower() == "completed" ||
                        log.Status.ToLower() == "passed"
                    );

                    return new PlayerResourceDto
                    {
                        Id = cr.Resource.Id,
                        Name = cr.Resource.Name,
                        Type = cr.Resource.TypeId == 2 ? "Exam" : "Learn",
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
                IsReadOnly = isReadOnly,
                EnrollmentId = enrollment?.Id,
                Resources = resources
            };

            return Ok(new ApiResponse<PlayerInfoDto> { Success = true, Data = dto });
        }

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
            if (!ModelState.IsValid)
                return BadRequest(new { message = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            // resolve GroupId → EmployeeCodes ถ้า Assign จาก Student Group
            if (dto.GroupId.HasValue && dto.EmployeeCodes.Count == 0)
            {
                dto.EmployeeCodes = await _studentGroupService.GetStudentCodesAsync(dto.GroupId.Value);
            }

            if (dto.CourseIds == null || !dto.CourseIds.Any() || dto.EmployeeCodes == null || !dto.EmployeeCodes.Any())
            {
                return BadRequest(new { message = "Courses and Employees are required." });
            }

            // 1. Generate AssignmentNo via DB sequence (race-condition free)
            string assignmentNo = await _assignmentNoGen.NextAsync();

            // แปลง Array พนักงานให้อยู่ในรูป Comma-separated
            string employeesStr = string.Join(",", dto.EmployeeCodes);

            // 2. วนลูปสร้าง Assignments Rule ตามจำนวนวิชาที่เลือก
            int firstAssignmentId = 0;
            foreach (var courseId in dto.CourseIds)
            {
                var rule = new Assignment
                {
                    AssignmentNo   = assignmentNo,
                    Description    = dto.Description,
                    CourseId       = courseId,
                    EmployeeCodes  = employeesStr,
                    StartDate      = dto.StartDate,
                    DueDate        = dto.DueDate,
                    Division       = dto.Division,
                    StudentGroupId = dto.GroupId,
                    DivisionId     = _currentUser.DivisionId  // 🆕 Data Isolation: ยัด DivisionId อัตโนมัติ
                };

                // บันทึก Rule ลง Database
                await _assignmentRepo.AddAsync(rule);

                if (firstAssignmentId == 0) firstAssignmentId = rule.Id;

                // 3. นำ EmployeeCodes ไป Insert ลงตาราง Enrollment และผูกกับ rule.Id ด้วย
                await _enrollmentService.AssignCourseToEmployees(courseId, dto.EmployeeCodes, dto.StartDate, dto.DueDate, rule.Id, forceReset: true);
            }

            return Ok(new { message = "Courses assigned successfully!", assignmentNo = assignmentNo, assignmentId = firstAssignmentId });
        }
    }
}