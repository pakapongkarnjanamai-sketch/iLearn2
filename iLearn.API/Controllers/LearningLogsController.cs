using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Mappings;
using iLearn.Application.Services;
using iLearn.Domain.Common;
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
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly ICurrentUserService _currentUser;
        public LearningLogsController(
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<CourseVersion> versionRepo,
            ICurrentUserService currentUserService)
        {
            _logRepo = logRepo;
            _enrollmentRepo = enrollmentRepo;
            _versionRepo = versionRepo;
            _currentUser = currentUserService;
        }

        //// GET: api/learninglogs?studentCode=EMP001&courseId=5
        //[HttpGet]
        //public async Task<IActionResult> Get([FromQuery] string? studentCode, [FromQuery] int? courseId)
        //{
        //    IReadOnlyList<LearningLog> logs;

        //    if (!string.IsNullOrEmpty(studentCode) && courseId.HasValue)
        //    {
        //        logs = await _logRepo.GetAsync(l => l.StudentCode == studentCode && l.CourseVersionId == courseId.Value);
        //    }
        //    else if (!string.IsNullOrEmpty(studentCode))
        //    {
        //        logs = await _logRepo.GetAsync(l => l.StudentCode == studentCode);
        //    }
        //    else
        //    {
        //        // ไม่ควรอนุญาตให้ดึงทั้งหมดโดยไม่มีเงื่อนไขถ้าข้อมูลเยอะ (อาจต้องทำ Pagination)
        //        logs = await _logRepo.GetAllAsync();
        //    }

        //    return Ok(logs.Select(l => l.ToDto()));
        //}

        //// POST: api/learninglogs
        //// API นี้จะถูกเรียกโดย SCORM Player หรือ Video Player ทุกๆ x วินาที หรือเมื่อจบ
        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] CreateLearningLogDto dto)
        //{
        //    var log = dto.ToEntity();
        //    var createdLog = await _logRepo.AddAsync(log);

        //    // [Optional] Update Enrollment Status?
        //    // ถ้าเป็นการส่ง log ครั้งสุดท้ายว่าเรียนจบแล้ว อาจจะไปอัปเดต Enrollment เลยก็ได้
        //    // await UpdateEnrollmentProgress(dto.StudentCode, dto.CourseId);

        //    return Ok(createdLog.ToDto());
        //}

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

        //[HttpGet("player-info/{enrollmentId}")]
        //public async Task<IActionResult> GetPlayerInfo(int enrollmentId)
        //{
        //    // 1. ดึง Enrollment
        //    var enrollment = await _enrollmentRepo.GetByIdAsync(enrollmentId);
        //    if (enrollment == null)
        //        return NotFound(new ApiResponse<string> { Success = false, Message = "Enrollment not found" });

        //    // 2. Security Check (ตรวจสอบความเป็นเจ้าของ)
        //    if (!string.Equals(enrollment.StudentCode, _currentUser.UserId, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return Unauthorized(new ApiResponse<string> { Success = false, Message = "Unauthorized access" });
        //    }

        //    // 3. ดึง CourseVersion พร้อม Resources
        //    var versions = await _versionRepo.GetAsync(
        //        filter: v => v.CourseId == enrollment.CourseId && v.VersionNumber == enrollment.EnrolledVersion,
        //        includeProperties: "CourseResources.Resource,Course"
        //    );

        //    var version = versions.FirstOrDefault();
        //    if (version == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Content not found" });

        //    // [ใหม่] 3.5 ดึง LearningLog ของ User ใน Version นี้ทั้งหมดมาตรวจสอบสถานะ
        //    var resourceIds = version.CourseResources.Select(cr => cr.ResourceId).ToList();
        //    var userLogs = await _logRepo.GetAsync(l =>
        //        l.StudentCode == enrollment.StudentCode &&
        //        l.CourseVersionId == version.Id &&
        //        resourceIds.Contains(l.ResourceId)
        //    );

        //    // 4. Map ข้อมูลลง DTO พร้อมระบุสถานะ IsCompleted
        //    var resources = version.CourseResources
        //        .Select(cr => {
        //            var log = userLogs.FirstOrDefault(l => l.ResourceId == cr.Resource.Id);
        //            // ถือว่าจบถ้า status เป็น completed หรือ passed
        //            var isDone = log != null && (
        //                log.LessonStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
        //                log.LessonStatus.Equals("passed", StringComparison.OrdinalIgnoreCase)
        //            );

        //            return new PlayerResourceDto
        //            {
        //                Id = cr.Resource.Id,
        //                Name = cr.Resource.Name,
        //                Type = cr.Resource.TypeId == 2 ? "Exam" : "Lesson",
        //                LaunchUrl = cr.Resource.URL,
        //                IsCompleted = isDone // ส่งค่ากลับไป
        //            };
        //        }).ToList();

        //    var dto = new PlayerInfoDto
        //    {
        //        CourseVersionId = version.Id,
        //        StudentCode = enrollment.StudentCode,
        //        CourseTitle = version.Course?.Title ?? "Unknown Course",
        //        Resources = resources
        //    };

        //    return Ok(new ApiResponse<PlayerInfoDto> { Success = true, Data = dto });
        //}

        // POST: api/learninglogs/update-progress
        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressDto input)
        {
            // ------------------------------------------------------------------
            // [จุดสำคัญ] กรองข้อมูล: บันทึกเฉพาะเมื่อเรียนจบ หรือครบ 100% เท่านั้น
            // ------------------------------------------------------------------
            bool isCompleted =
                (input.Progress >= 100) ||
                (input.Status?.ToLower() == "passed") ||
                (input.Status?.ToLower() == "completed");

            if (!isCompleted)
            {
                // ถ้ายังเรียนไม่จบ ให้ตอบกลับว่าสำเร็จ (หลอก Frontend) แต่ไม่บันทึกลง DB
                return Ok(new ApiResponse<string> { Success = true, Message = "Progress ignored (not completed)." });
            }

            // 1. ตรวจสอบ Enrollment และสิทธิ์
            var enrollment = await _enrollmentRepo.GetByIdAsync(input.EnrollmentId);
            if (enrollment == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Enrollment not found" });

            if (!string.Equals(enrollment.StudentCode, input.StudentCode, StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Student code mismatch" });

            // 2. ค้นหา Log เดิม (ถ้ามี)
            // ใช้ CourseVersionId (หรือ EnrolledVersion) เพื่อแยกเวอร์ชันของบทเรียน
            int versionId = enrollment.EnrolledVersion;

            var logs = await _logRepo.GetAsync(l =>
                l.StudentCode == input.StudentCode &&
                l.ResourceId == input.ResourceId &&
                l.CourseVersionId == versionId
            );

            var existingLog = logs.FirstOrDefault();

            if (existingLog != null)
            {
                // -- อัปเดตข้อมูลเดิม --
                existingLog.Status = "completed"; // บังคับเป็น completed เพราะผ่านเงื่อนไขด้านบนมาแล้ว
                existingLog.Progress = 100;       // บังคับเป็น 100%
                existingLog.Score = input.Score ?? existingLog.Score;
                existingLog.SessionTime = input.SessionTime;
                //existingLog.LastAccessed = DateTime.Now;

                // (Optional) ถ้าเรียนซ้ำ ให้ปรับวันที่ล่าสุด
                // existingLog.CompletedDate = DateTime.Now; 

                await _logRepo.UpdateAsync(existingLog);
            }
            else
            {
                // -- สร้างใหม่ (Create) --
                var newLog = new LearningLog
                {
                    StudentCode = input.StudentCode,
                    ResourceId = input.ResourceId,
                    CourseVersionId = versionId,

                    // กำหนดค่าเริ่มต้นเป็น Completed/100% ทันที
                    Status = "completed",
                    Progress = 100,

                    Score = input.Score,
                    SessionTime = input.SessionTime,
                    //StartDate = DateTime.Now,
                    //LastAccessed = DateTime.Now
                };

                await _logRepo.AddAsync(newLog);
            }

            // (เพิ่มเติม) คุณอาจจะไปอัปเดตสถานะของ Enrollment หลักให้เป็น Completed ด้วยก็ได้
            // ถ้าเช็คแล้วว่าครบทุก Resource ในคอร์สนั้นแล้ว

            return Ok(new ApiResponse<string> { Success = true, Message = "Progress saved successfully." });
        }
    }
}