using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class CourseWizardController : ControllerBase
    {
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly IGenericRepository<CourseResource> _courseResourceRepo;
        private readonly IGenericRepository<AssignmentRule> _ruleRepo;

        public CourseWizardController(
            IGenericRepository<Course> courseRepo,
            IGenericRepository<CourseVersion> versionRepo,
            IGenericRepository<CourseResource> courseResourceRepo,
            IGenericRepository<AssignmentRule> ruleRepo)
        {
            _courseRepo = courseRepo;
            _versionRepo = versionRepo;
            _courseResourceRepo = courseResourceRepo;
            _ruleRepo = ruleRepo;
        }

        [HttpPost("Submit")]
        public async Task<IActionResult> Submit([FromBody] CourseWizardDto dto)
        {
            Course course;
            int newVersionNumber = 1;

            // 1. จัดการ Course (Create or Get)
            if (!dto.CourseId.HasValue || dto.CourseId == 0)
            {
                // --- New Course ---
                course = new Course
                {
                    Code = dto.Code,
                    Title = dto.Title,
                    Description = dto.Description,
                    Type = dto.Type,
                    IsActive = true,
                    // CategoryId = dto.CategoryId // อย่าลืมเพิ่ม Property นี้ใน Course Entity ถ้ายังไม่มี
                };
                await _courseRepo.AddAsync(course); // ได้ course.Id กลับมา
            }
            else
            {
                // --- Update Course (New Version) ---
                course = await _courseRepo.GetQuery()
                    .Include(c => c.Versions)
                    .FirstOrDefaultAsync(c => c.Id == dto.CourseId);

                if (course == null) return NotFound("Course not found");

                // อัปเดตข้อมูล Course หลักด้วย (เผื่อมีการแก้ไขชื่อ)
                course.Title = dto.Title;
                course.Description = dto.Description;
                course.Type = dto.Type; // Type ไม่ควรเปลี่ยนบ่อย แต่เปิดไว้ได้

                // หา Version ล่าสุดเพื่อ +1
                var lastVersion = course.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
                newVersionNumber = (lastVersion?.VersionNumber ?? 0) + 1;

                // Set Active เก่าเป็น false (Optional: แล้วแต่ Business Logic ว่าให้มี Active ได้กี่ตัว)
                if (lastVersion != null)
                {
                    lastVersion.IsActive = false;
                    await _versionRepo.UpdateAsync(lastVersion);
                }

                await _courseRepo.UpdateAsync(course);
            }

            // 2. สร้าง Version ใหม่
            var newVersion = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = newVersionNumber,
                Note = dto.VersionNote ?? (newVersionNumber == 1 ? "Initial Release" : "Update"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _versionRepo.AddAsync(newVersion);

            // 3. ผูก Resources
            if (dto.ResourceIds != null)
            {
                foreach (var resId in dto.ResourceIds)
                {
                    await _courseResourceRepo.AddAsync(new CourseResource
                    {
                        CourseVersionId = newVersion.Id,
                        ResourceId = resId
                    });
                }
            }

            // 4. จัดการ Rules (เฉพาะ Type Special)
            // หมายเหตุ: Rules มักจะผูกกับ Course ไม่ใช่ Version แต่ถ้าต้องการแยก Version ก็ต้องย้ายไปผูก Version
            // ในที่นี้สมมติว่า Rules ผูกกับ Course (ใช้ชุดเดียวกันตลอด หรือลบของเก่าสร้างใหม่)
            if (dto.Type == Domain.Enums.CourseType.Special && dto.Rules != null)
            {
                // ลบ Rules เก่าทิ้งก่อน (Simple Reset Strategy)
                var oldRules = await _ruleRepo.GetAsync(r => r.CourseId == course.Id);
                foreach (var r in oldRules) await _ruleRepo.DeleteAsync(r);

                // เพิ่ม Rules ใหม่
                foreach (var ruleDto in dto.Rules)
                {
                    await _ruleRepo.AddAsync(new AssignmentRule
                    {
                        CourseId = course.Id,
                        RoleId = ruleDto.RoleId,
                        DivisionId = ruleDto.DivisionId
                    });
                }
            }

            return Ok(new { success = true, courseId = course.Id, version = newVersionNumber });
        }
    }
}