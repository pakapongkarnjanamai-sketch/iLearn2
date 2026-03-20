// File: iLearn.API/Controllers/DashboardController.cs
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.API.Services;
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
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly IMaintenanceStatusService _maintenanceStatusService;

        public DashboardController(
            IGenericRepository<Course> courseRepo,
            IGenericRepository<User> userRepo,
            IGenericRepository<Resource> resourceRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<LearningLog> learningLogRepo,
            IGenericRepository<Assignment> assignmentRepo,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            IMaintenanceStatusService maintenanceStatusService)
        {
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _resourceRepo = resourceRepo;
            _enrollmentRepo = enrollmentRepo;
            _learningLogRepo = learningLogRepo;
            _assignmentRepo = assignmentRepo;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _maintenanceStatusService = maintenanceStatusService;
        }

        [HttpGet("Stats")]
        public async Task<IActionResult> GetStats()
        {
            var activeCourses = await _courseRepo.CountAsync(c => c.IsActive);
            var draftCourses = await _courseRepo.CountAsync(c => !c.IsActive);
            var totalResources = await _resourceRepo.CountAsync();

            var now = _dateTime.Now;
            var inProgressAssignments = await _assignmentRepo.CountAsync(
                a => (!a.StartDate.HasValue || a.StartDate.Value <= now)
                  && (!a.DueDate.HasValue  || a.DueDate.Value  >= now)
                  && (!_currentUser.DivisionId.HasValue || a.DivisionId == _currentUser.DivisionId.Value));

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
            var today = _dateTime.Now.Date;

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
            var today = _dateTime.Now.Date;

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

        [HttpGet("MaintenanceStatus")]
        public IActionResult GetMaintenanceStatus()
        {
            var operations = _maintenanceStatusService.GetActiveOperations()
                .Select(x => new
                {
                    x.OperationId,
                    x.OperationName,
                    x.CurrentStep,
                    x.CurrentItemName,
                    x.CurrentItem,
                    x.TotalItems,
                    x.SuccessCount,
                    x.FailureCount,
                    x.InitiatedBy,
                    startedAt = x.StartedAt,
                    lastUpdatedAt = x.LastUpdatedAt
                })
                .ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    hasActiveMaintenance = operations.Any(),
                    operations
                }
            });
        }
    }
}   