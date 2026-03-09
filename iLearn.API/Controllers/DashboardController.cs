// File: iLearn.API/Controllers/DashboardController.cs
using iLearn.Application.Interfaces.Repositories;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<Resource> _resourceRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<LearningLog> _learningLogRepo;
        private readonly IGenericRepository<Assignment> _assignmentRepo;

        public DashboardController(
            IGenericRepository<Course> courseRepo,
            IGenericRepository<User> userRepo,
            IGenericRepository<Resource> resourceRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<LearningLog> learningLogRepo,
            IGenericRepository<Assignment> assignmentRepo)
        {
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _resourceRepo = resourceRepo;
            _enrollmentRepo = enrollmentRepo;
            _learningLogRepo = learningLogRepo;
            _assignmentRepo = assignmentRepo;
        }

        [HttpGet("Stats")]
        public async Task<IActionResult> GetStats()
        {
            // ใช้ CountAsync ซึ่งจะไป Gen SQL "SELECT COUNT(*)..." ที่เร็วมาก และไม่ดึง Data ออกมา
            var activeCourses = await _courseRepo.CountAsync(c => c.IsActive);
            var draftCourses = await _courseRepo.CountAsync(c => !c.IsActive);
            var totalResources = await _resourceRepo.CountAsync();

            var now = DateTime.UtcNow;
            var inProgressAssignments = await _assignmentRepo.CountAsync(
                a => (!a.StartDate.HasValue || a.StartDate.Value <= now)
                  && (!a.DueDate.HasValue  || a.DueDate.Value  >= now));

            return Ok(new
            {
                success = true,
                data = new
                {
                    activeCourses,
                    draftCourses,
                    inProgressAssignments,
                    totalResources
                }
            });
        }

        [HttpGet("EnrollmentTrends")]
        public IActionResult GetEnrollmentTrends()
        {
            var today = DateTime.Today;

            var months = Enumerable.Range(0, 6)
                .Select(i => today.AddMonths(-5 + i))
                .Select(d => new { d.Year, d.Month })
                .ToList();

            var cutoff = new DateTime(months[0].Year, months[0].Month, 1);

            var enrollments = _enrollmentRepo.GetQuery()
                .Where(e => e.StartDate.HasValue && e.StartDate.Value >= cutoff)
                .Select(e => new { e.StartDate!.Value.Year, e.StartDate!.Value.Month })
                .ToList();

            var trends = months.Select(m => new
            {
                month = new DateTime(m.Year, m.Month, 1).ToString("MMM"),
                enrollments = enrollments.Count(e => e.Year == m.Year && e.Month == m.Month)
            });

            return Ok(new { success = true, data = trends });
        }

        [HttpGet("LearningActivityTrends")]
        public IActionResult GetLearningActivityTrends()
        {
            var today = DateTime.Today;

            // 6 เดือนย้อนหลัง
            var months = Enumerable.Range(0, 6)
                .Select(i => today.AddMonths(-5 + i))
                .Select(d => new { d.Year, d.Month })
                .ToList();

            var cutoff = new DateTime(months[0].Year, months[0].Month, 1);

            // นับ LearningLog (session การเรียน) ตาม CreatedAt
            var logs = _learningLogRepo.GetQuery()
                .Where(l => l.CreatedAt >= cutoff)
                .Select(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
                .ToList();

            var trends = months.Select(m => new
            {
                month = new DateTime(m.Year, m.Month, 1).ToString("MMM yy"),
                sessions = logs.Count(l => l.Year == m.Year && l.Month == m.Month)
            });

            return Ok(new { success = true, data = trends });
        }
    }
}   