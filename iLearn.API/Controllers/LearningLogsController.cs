using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LearningLogsController : ControllerBase
    {
        private readonly IGenericRepository<LearningLog> _logRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly IMemoryCache _cache;
        public LearningLogsController(
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<CourseVersion> versionRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            ICurrentUserService currentUserService,
            IMemoryCache cache)
        {
            _logRepo = logRepo;
            _enrollmentRepo = enrollmentRepo;
            _versionRepo = versionRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _currentUser = currentUserService;
            _cache = cache;
        }

        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressDto input)
        {
            // 1. ตรวจสอบ Enrollment
            var enrollment = await _enrollmentRepo.GetByIdAsync(input.EnrollmentId);
            if (enrollment == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Enrollment not found" });

            // ตรวจสอบว่าเป็นเจ้าของ Enrollment หรือไม่
            if (!string.Equals(enrollment.StudentCode, input.StudentCode, StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "StudentDto code mismatch" });

            // ✅ ถ้าจบแล้ว (Completed) ไม่ให้แก้ (เว้นแต่ Admin จะ Reset IsCompleted = false มาแล้ว)
            if (enrollment.IsCompleted)
            {
                return Ok(new ApiResponse<string> { Success = true, Message = "Course is completed." });
            }

            // 🌟 ตรวจสอบก่อนว่าประวัติการเรียนนี้ยังมี Version ให้เรียนอยู่หรือไม่ (กรณีคอร์สถูกลบ)
            if (!enrollment.EnrolledCourseVersion.HasValue)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "ไม่พบเวอร์ชันของหลักสูตรในระบบ (หลักสูตรอาจถูกลบไปแล้ว)" });
            }

            // 🌟 ดึงค่าตัวเลขออกมาอย่างปลอดภัยด้วย .Value
            int versionId = enrollment.EnrolledCourseVersion.Value;

            // 2. ✅ ดึง Log โดยอ้างอิง EnrollmentId โดยตรง (แม่นยำกว่า StudentCode + Version อย่างเดียว)
            var existingLogs = await _logRepo.GetAsync(l => l.EnrollmentId == input.EnrollmentId);

            foreach (var resInput in input.Resources)
            {
                var log = existingLogs.FirstOrDefault(l => l.ResourceId == resInput.ResourceId);

                // แปลงสถานะจาก Input
                bool isInputPassed = resInput.Status?.ToLower() == "passed" ||
                                     resInput.Status?.ToLower() == "completed";
                string newStatus = isInputPassed ? "passed" : (resInput.Status ?? "incomplete");

                // คำนวณเวลาในรอบนี้ (Session Time) เป็นวินาที
                int sessionSeconds = ParseSessionTime(resInput.SessionTime);

                if (log != null)
                {
                    // --- กรณีมี Log เดิม (Update) ---

                    // ✅ 1. บวกเวลาสะสมเพิ่มเข้าไปเสมอ (เพื่อบันทึก Actual Usage)
                    log.TotalSecondsPlayed += sessionSeconds;

                    // ✅ 2. อัปเดต SessionTime ล่าสุดเก็บไว้ดู
                    if (!string.IsNullOrEmpty(resInput.SessionTime))
                    {
                        log.SessionTime = resInput.SessionTime;
                    }

                    // ✅ 3. อัปเดตสถานะและคะแนน (เขียนทับได้เลย เพราะถือว่ากำลังเรียนรอบใหม่)
                    // Logic เดิมจะกันไม่ให้แก้ถ้าผ่านแล้ว แต่กรณีนี้เรายอมให้แก้เพื่อให้สถานะเป็นปัจจุบัน
                    log.Status = newStatus;
                    log.Progress = isInputPassed ? 100 : (resInput.Progress ?? 0);

                    // อัปเดตคะแนนเฉพาะถ้ามีส่งมา (หรือจะใช้ Logic คะแนนสูงสุดก็ได้ตามต้องการ)
                    if (resInput.Score.HasValue)
                    {
                        log.Score = resInput.Score;
                    }

                    // ✅ 4. เพิ่มจำนวนครั้งที่พยายาม (Logic คร่าวๆ: ถ้านักเรียนส่ง Status มาใหม่ ให้ถือเป็นความเคลื่อนไหว)
                    // หรือจะนับเฉพาะตอน Client ส่งสัญญาณเริ่มเรียนก็ได้ แต่นับตอน Save ก็พอใช้แทนได้
                    log.AttemptCount++;

                    await _logRepo.UpdateAsync(log);
                }
                else
                {
                    // --- กรณีไม่มี Log (Create New) ---
                    var newLog = new LearningLog
                    {
                        EnrollmentId = input.EnrollmentId, // ✅ อย่าลืมใส่ EnrollmentId
                        StudentCode = input.StudentCode,
                        ResourceId = resInput.ResourceId,
                        CourseVersionId = versionId,
                        Status = newStatus,
                        Progress = isInputPassed ? 100 : (resInput.Progress ?? 0),
                        Score = resInput.Score,
                        SessionTime = resInput.SessionTime,
                        TotalSecondsPlayed = sessionSeconds, // เริ่มต้นด้วยเวลาของรอบแรก
                        AttemptCount = 1,
                        CreatedAt = DateTime.Now // สมมติว่ามี
                    };
                    await _logRepo.AddAsync(newLog);
                }
            }

            // 3. ตรวจสอบการจบหลักสูตร (Completion Check)
            var version = (await _versionRepo.GetAsync(v => v.Id == versionId, includeProperties: "CourseResources")).FirstOrDefault();
            if (version != null && version.CourseResources != null)
            {
                // รีโหลด Log ใหม่เพื่อให้ได้ค่าล่าสุด
                var updatedLogs = await _logRepo.GetAsync(l => l.EnrollmentId == input.EnrollmentId);

                var allResourceIds = version.CourseResources.Select(cr => cr.ResourceId).ToList();
                int passedCount = updatedLogs.Count(l =>
                    allResourceIds.Contains(l.ResourceId ?? 0) && // ป้องกัน error กรณี ResourceId เป็น null
                    (l.Status == "passed" || l.Status == "completed")
                );

                if (passedCount >= allResourceIds.Count && allResourceIds.Count > 0)
                {
                    enrollment.IsCompleted = true;
                    enrollment.CompletedDate = DateTime.Now;
                    enrollment.Progress = 100;
                }
                else
                {
                    enrollment.Progress = allResourceIds.Count > 0 ? ((double)passedCount / allResourceIds.Count) * 100 : 0;
                }

                // Sync TotalTimeSpent และ TotalScore จาก LearningLog กลับไปที่ Enrollment
                enrollment.TotalTimeSpent = updatedLogs.Sum(l => l.TotalSecondsPlayed);
                enrollment.TotalScore = updatedLogs
                    .Where(l => allResourceIds.Contains(l.ResourceId ?? 0))
                    .Max(l => (int?)l.Score ?? 0);

                await _enrollmentRepo.UpdateAsync(enrollment);

                // ── Snapshot สถานะปัจจุบันไปที่ EnrollmentAssignment links ──
                var eaLinks = await _enrollmentAssignmentRepo.GetAsync(
                    ea => ea.EnrollmentId == enrollment.Id);
                foreach (var link in eaLinks)
                {
                    link.SnapshotCompleted     = enrollment.IsCompleted;
                    link.SnapshotCompletedDate = enrollment.CompletedDate;
                    link.SnapshotProgress      = enrollment.Progress;
                    await _enrollmentAssignmentRepo.UpdateAsync(link);
                }
            }

            AdminSummaryStatsCache.InvalidateLearningLogs(_cache);
            AdminSummaryStatsCache.InvalidateEnrollments(_cache);

            return Ok(new ApiResponse<string> { Success = true, Message = "Progress saved." });
        }
        private int ParseSessionTime(string? timeStr)
        {
            if (string.IsNullOrEmpty(timeStr)) return 0;
            if (TimeSpan.TryParse(timeStr, out var ts))
            {
                return (int)ts.TotalSeconds;
            }
            return 0;
        }
    }
}