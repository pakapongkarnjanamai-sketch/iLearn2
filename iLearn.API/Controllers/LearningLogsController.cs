using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
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

        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressDto input)
        {
            // 1. ตรวจสอบ Enrollment และสิทธิ์
            var enrollment = await _enrollmentRepo.GetByIdAsync(input.EnrollmentId);
            if (enrollment == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Enrollment not found" });

            if (!string.Equals(enrollment.StudentCode, input.StudentCode, StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Student code mismatch" });

            // --- NEW: Read Only Mode ---
            // ถ้าเรียนจบไปแล้ว ไม่ให้บันทึกเพิ่ม (ตามข้อ 1.1)
            if (enrollment.Status == "Completed" || enrollment.Status == "Passed")
            {
                return Ok(new ApiResponse<string> { Success = true, Message = "Course is already completed. No updates allowed." });
            }

            // 2. เตรียมข้อมูล Version
            int versionId = enrollment.EnrolledVersion;

            // ดึง Log เดิมทั้งหมดของ User ใน Version นี้มาเพื่อเปรียบเทียบ
            var existingLogs = await _logRepo.GetAsync(l =>
                l.StudentCode == input.StudentCode &&
                l.CourseVersionId == versionId
            );

            // 3. วนลูปบันทึกราย Resource (ตามข้อ 2)
            foreach (var resInput in input.Resources)
            {
                var log = existingLogs.FirstOrDefault(l => l.ResourceId == resInput.ResourceId);

                // ตรวจสอบสถานะผ่าน (Passed/Completed)
                bool isInputPassed = resInput.Status?.ToLower() == "passed" ||
                                     resInput.Status?.ToLower() == "completed" ||
                                     resInput.Progress >= 100;

                if (log != null)
                {
                    // --- กรณีมี Log เดิม ---

                    // Logic เวลา: ถ้าของเดิมผ่านแล้ว จะไม่บันทึกเวลาใหม่ (ตามข้อ 2)
                    bool isAlreadyPassed = log.Status?.ToLower() == "passed" || log.Status?.ToLower() == "completed";

                    if (!isAlreadyPassed)
                    {
                        // ถ้ายังไม่เคยผ่าน ให้บันทึกสถานะล่าสุด
                        log.Status = isInputPassed ? "passed" : (resInput.Status ?? "incomplete");
                        log.Progress = isInputPassed ? 100 : (resInput.Progress ?? 0);
                        log.Score = resInput.Score ?? log.Score;

                        // บันทึกเวลาเฉพาะตอนที่ยังไม่ผ่าน
                        if (!string.IsNullOrEmpty(resInput.SessionTime))
                        {
                            log.SessionTime = resInput.SessionTime;
                        }
                    }
                    // ถ้า isAlreadyPassed = true -> เราจะไม่ update อะไรเลย (Read only for this resource)

                    await _logRepo.UpdateAsync(log);
                }
                else
                {
                    // --- กรณีไม่มี Log (สร้างใหม่) ---
                    var newLog = new LearningLog
                    {
                        StudentCode = input.StudentCode,
                        ResourceId = resInput.ResourceId,
                        CourseVersionId = versionId,
                        Status = isInputPassed ? "passed" : (resInput.Status ?? "incomplete"),
                        Progress = isInputPassed ? 100 : (resInput.Progress ?? 0),
                        Score = resInput.Score,
                        SessionTime = resInput.SessionTime,
                        // CreateDate = DateTime.Now 
                    };
                    await _logRepo.AddAsync(newLog);
                }
            }

            // 4. ตรวจสอบว่าเรียนจบทั้ง Course หรือยัง?
            // ดึง Log ล่าสุดอีกรอบ (รวมที่เพิ่งบันทึกไป)
            var allLogs = await _logRepo.GetAsync(l => l.StudentCode == input.StudentCode && l.CourseVersionId == versionId);

            // ดึงจำนวน Resource ทั้งหมดใน Version นี้
            var version = (await _versionRepo.GetAsync(v => v.Id == versionId, includeProperties: "CourseResources")).FirstOrDefault();

            if (version != null && version.CourseResources != null)
            {
                var allResourceIds = version.CourseResources.Select(cr => cr.ResourceId).ToList();

                // นับจำนวน Resource ที่ผ่านแล้ว (Status = passed/completed)
                int passedCount = allLogs.Count(l =>
                    allResourceIds.Contains(l.ResourceId) &&
                    (l.Status == "passed" || l.Status == "completed")
                );

                // ถ้าผ่านครบทุกตัว
                if (passedCount >= allResourceIds.Count)
                {
                    enrollment.Status = "Completed";
                    enrollment.Progress = 100;
                    enrollment.CompletedDate = DateTime.Now;
                    await _enrollmentRepo.UpdateAsync(enrollment);
                }
                else
                {
                    // ถ้ายังไม่ครบ อาจจะอัปเดต % รวม
                    double courseProgress = ((double)passedCount / allResourceIds.Count) * 100;
                    enrollment.Progress = courseProgress;
                    await _enrollmentRepo.UpdateAsync(enrollment);
                }
            }

            return Ok(new ApiResponse<string> { Success = true, Message = "Progress saved successfully." });
        }


    }
}