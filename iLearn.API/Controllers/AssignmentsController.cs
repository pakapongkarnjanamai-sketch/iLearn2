using iLearn.Application.DTOs;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentsController : ControllerBase
    {
        private readonly IGenericRepository<Assignment> _repo;
        private readonly ICourseAssignmentService _assignmentService;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IAssignmentDashboardService _dashboardService;
        private readonly ICurrentUserService _currentUser; // 💡 1. ประกาศตัวแปร

        public AssignmentsController(
            IGenericRepository<Assignment> repo,
            ICourseAssignmentService assignmentService,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Course> courseRepo,
            IAssignmentDashboardService dashboardService,
            ICurrentUserService currentUser) // 💡 2. รับค่าเข้ามา
        {
            _repo = repo;
            _assignmentService = assignmentService;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _courseRepo = courseRepo;
            _dashboardService = dashboardService;
            _currentUser = currentUser; // 💡 3. กำหนดค่า
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] PaginationParams p)
        {
            var result = await _dashboardService.GetAssignmentHistoryPagedAsync(p);
            return Ok(result);
        }

        [HttpGet("gantt")]
        public async Task<IActionResult> GetGanttTasks()
        {
            var all = await _dashboardService.GetAssignmentHistoryPagedAsync(
                new PaginationParams { Page = 1, PageSize = 500 });

            var tasks = new List<object>();

            foreach (var item in all.Data)
            {
                var progress = item.TotalEnrollmentCount > 0
                    ? (int)Math.Round((double)item.CompletedEnrollmentCount / item.TotalEnrollmentCount * 100)
                    : 0;

                var color = item.Status switch
                {
                    "Completed" => "#52c41a",
                    "InProgress" => "#1890ff",
                    "Upcoming" => "#faad14",
                    "Expired" => "#ff4d4f",
                    _ => "#aaaaaa"
                };

                var start = item.StartDate ?? item.CreatedAt;
                var end = item.DueDate ?? start.AddDays(7);
                if (end <= start) end = start.AddDays(1);

                tasks.Add(new
                {
                    id = item.Id, // ใช้ ID จริงของงานได้เลย
                    parentId = 0,
                    title = $"{item.AssignmentNo} - {item.Description ?? "No Description"}", // จัดฟอร์แมตชื่อเรื่องใหม่
                    startDate = start, // 💡 เปลี่ยนชื่อคีย์ให้เป็น startDate
                    dueDate = end,   // 💡 เปลี่ยนชื่อคีย์ให้เป็น dueDate
                    progress,
                    color,
                    status = item.Status,
                    assignmentNo = item.AssignmentNo
                });
            }

            return Ok(tasks); // ส่งแค่ข้อมูลตัวแม่กลับไป
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var assignments = await _repo.GetAsync(r =>
                r.CourseId == courseId &&
                // 💡 เพิ่มการกรอง Division ตัวเอง
                (!_currentUser.DivisionId.HasValue || r.DivisionId == _currentUser.DivisionId.Value)
            );
            return Ok(assignments.Select(r => new { r.Id, r.CourseId }));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _repo.GetByIdAsync(id);
            if (rule == null) return NotFound();

            var relatedRules   = await _repo.GetAsync(r => r.AssignmentNo == rule.AssignmentNo);
            var relatedIds = relatedRules.Select(r => r.Id).ToList();

            // ลบ EnrollmentAssignment links
            var links = await _enrollmentAssignmentRepo.GetAsync(
                ea => relatedIds.Contains(ea.AssignmentId));
            foreach (var link in links)
                await _enrollmentAssignmentRepo.DeleteAsync(link);

            // Soft Delete Assignments
            foreach (var r in relatedRules)
                await _repo.DeleteAsync(r);

            return NoContent();
        }

        [HttpGet("dashboard/{id}")]
        public async Task<IActionResult> GetDashboardData(int id)
        {
            var result = await _dashboardService.GetDashboardAsync(id);
            if (result == null) return NotFound(new { message = "Assignment not found" });
            return Ok(new { success = true, data = result });
        }

        [HttpPost("validate-before-assign")]
        public async Task<IActionResult> ValidateBeforeAssign([FromBody] BulkAssignDto dto)
        {
            var result = await _dashboardService.ValidateBeforeAssignAsync(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new
            {
                success             = result.Success,
                inProgressConflicts = result.InProgressConflicts,
                completedConflicts  = result.CompletedConflicts,
                resolvedCount       = result.ResolvedCount
            });
        }

        [HttpPatch("{id}/extend-due-date")]
        public async Task<IActionResult> ExtendDueDate(int id, [FromBody] ExtendDueDateDto dto)
        {
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null) return NotFound(new { message = "Assignment not found" });

            if (mainRule.StartDate.HasValue && dto.NewDueDate <= mainRule.StartDate.Value)
                return BadRequest(new { message = "Due date must be after the start date." });

            var allRules = await _repo.GetAsync(r => r.AssignmentNo == mainRule.AssignmentNo);
            foreach (var rule in allRules)
            {
                rule.DueDate = dto.NewDueDate;
                await _repo.UpdateAsync(rule);
            }

            var ruleIds = allRules.Select(r => r.Id).ToList();
            var activeLinks = await _enrollmentAssignmentRepo.GetAsync(
                ea => ruleIds.Contains(ea.AssignmentId),
                includeProperties: "Enrollment"
            );
            foreach (var link in activeLinks.Where(ea => ea.Enrollment != null && !(ea.SnapshotCompleted || ea.Enrollment.IsCompleted)))
            {
                link.DueDate = dto.NewDueDate;
                await _enrollmentAssignmentRepo.UpdateAsync(link);
            }

            return Ok(new { success = true, message = "Due date extended successfully.", newDueDate = dto.NewDueDate });
        }

        [HttpGet("lookup-courses")]
        public async Task<IActionResult> GetLookupCourses()
        {
            var courses = await _courseRepo.GetAsync(c => c.IsActive, includeProperties: "Category,CourseType");

            var result = courses.Select(c => new LookupCourseDto
            {
                Id           = c.Id,
                Code         = c.Code,
                Title        = c.Title,
                CategoryId   = c.CategoryId,
                DivisionId   = c.Category?.DivisionId,
                CourseTypeId = c.CourseTypeId,
                CourseTypeName = c.CourseType?.Name
            }).ToList();

            return Ok(new { data = result });
        }

        // ── Assignment History for a specific Student Group ──────────────────────
        [HttpGet("group/{groupId}/history")]
        public async Task<IActionResult> GetGroupHistory(int groupId)
        {
            var assignments = await _repo.GetAsync(
                r => r.StudentGroupId == groupId &&
                // 💡 เพิ่มการกรอง Division ตัวเอง
                (!_currentUser.DivisionId.HasValue || r.DivisionId == _currentUser.DivisionId.Value),
                includeProperties: "Course"
            );

            if (!assignments.Any())
                return Ok(new { success = true, data = new List<object>() });

            var allIds = assignments.Select(a => a.Id).ToList();

            var links = await _enrollmentAssignmentRepo.GetAsync(
                ea => allIds.Contains(ea.AssignmentId),
                includeProperties: "Enrollment"
            );

            var now = DateTime.UtcNow.AddHours(7);

            var history = assignments
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo))
                .GroupBy(r => r.AssignmentNo)
                .Select(g =>
                {
                    var first   = g.First();
                    var ruleIds = g.Select(a => a.Id).ToList();

                    var relatedLinks = links
                        .Where(ea => ruleIds.Contains(ea.AssignmentId) && ea.Enrollment != null)
                        .ToList();

                    bool allDone = relatedLinks.Any()
                        && relatedLinks.All(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);

                    string status = AssignmentDashboardService.CalculateStatus(
                        relatedLinks.Any(), allDone, first.StartDate, first.DueDate, now);

                    var done  = relatedLinks.Count(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);
                    var total = relatedLinks.Count;
                    var pct   = total > 0 ? Math.Round((double)done / total * 100) : 0;

                    return new
                    {
                        id                       = first.Id,
                        assignmentNo             = g.Key,
                        description              = first.Description,
                        courseNames              = string.Join(", ", g
                            .Select(c => c.Course != null ? c.Course.Title : "Unknown").Distinct()),
                        courseCount              = g.Select(a => a.CourseId).Distinct().Count(),
                        startDate                = first.StartDate,
                        dueDate                  = first.DueDate,
                        status,
                        completedEnrollmentCount = done,
                        totalEnrollmentCount     = total,
                        completionPct            = pct
                    };
                })
                .OrderByDescending(x => x.assignmentNo)
                .ToList();

            return Ok(new { success = true, data = history });
        }
    }
}