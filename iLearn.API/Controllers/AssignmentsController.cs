using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
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
        public readonly IGenericRepository<Enrollment> _enrollmentRepo;
        public readonly IGenericRepository<Course> _courseRepo;
        private readonly IStudentApiService _studentApiService;
        private readonly IStudentGroupService _studentGroupService;

        public AssignmentsController(
            IGenericRepository<Assignment> repo,
            ICourseAssignmentService assignmentService,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<Course> courseRepo,
            IStudentApiService studentApiService,
            IStudentGroupService studentGroupService)
        {
            _repo = repo;
            _assignmentService = assignmentService;
            _enrollmentRepo = enrollmentRepo;
            _courseRepo = courseRepo;
            _studentApiService = studentApiService;
            _studentGroupService = studentGroupService;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var history = await _assignmentService.GetAssignmentHistoryAsync();
            return Ok(new { data = history, totalCount = history.Count });
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var assignments = await _repo.GetAsync(r => r.CourseId == courseId);
            return Ok(assignments.Select(r => new { r.Id, r.CourseId }));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _repo.GetByIdAsync(id);
            if (rule == null) return NotFound();

            var relatedRules = await _repo.GetAsync(r => r.AssignmentNo == rule.AssignmentNo);
            var relatedRuleIds = relatedRules.Select(r => r.Id).ToList();

            // Unlink Enrollment จาก Assignment — ไม่ลบ Enrollment เพราะเป็น audit trail ของการเรียน
            var relatedEnrollments = await _enrollmentRepo.GetAsync(e =>
                e.AssignmentRuleId.HasValue && relatedRuleIds.Contains(e.AssignmentRuleId.Value));

            foreach (var enrollment in relatedEnrollments)
            {
                enrollment.AssignmentRuleId = null;
                await _enrollmentRepo.UpdateAsync(enrollment);
            }

            // Soft Delete Assignments
            foreach (var r in relatedRules)
                await _repo.DeleteAsync(r);

            return NoContent();
        }

        [HttpGet("dashboard/{id}")]
        public async Task<IActionResult> GetDashboardData(int id)
        {
            // 1. หา Assignment หลักเพื่อเอา AssignmentNo
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null) return NotFound(new { message = "Assignment not found" });

            // 2. ดึง Assignments ทั้งหมดในกลุ่มเดียวกัน พร้อมข้อมูล Course
            var allRules = await _repo.GetAsync(
                r => r.AssignmentNo == mainRule.AssignmentNo,
                includeProperties: "Course"
            );

            var ruleIds = allRules.Select(r => r.Id).ToList();

            // 3. ดึง Enrollments เฉพาะที่ผูกกับ Rule ในกลุ่มนี้ พร้อม Course
            var enrollments = await _enrollmentRepo.GetAsync(
                e => e.AssignmentRuleId.HasValue && ruleIds.Contains(e.AssignmentRuleId.Value),
                includeProperties: "Course"
            );

            // 4. คำนวณสถิติ — นับ per student ไม่ใช่ per enrollment
            var studentEnrollments = enrollments
                .GroupBy(e => e.StudentCode)
                .Select(g => new
                {
                    StudentCode  = g.Key,
                    AllCompleted = g.All(e => e.IsCompleted),
                    AnyStarted   = g.Any(e => e.IsCompleted || e.Progress > 0)
                })
                .ToList();

            var uniqueStudentsCount  = studentEnrollments.Count;
            var completedCount       = studentEnrollments.Count(s => s.AllCompleted);
            var inProgressCount      = studentEnrollments.Count(s => !s.AllCompleted && s.AnyStarted);
            var notStartedCount      = studentEnrollments.Count(s => !s.AllCompleted && !s.AnyStarted);

            var totalEnrollments     = enrollments.Count();
            var completedEnrollments = enrollments.Count(e => e.IsCompleted);
            var completionRate       = totalEnrollments == 0
                ? 0
                : Math.Round(((double)completedEnrollments / totalEnrollments) * 100);

            // 5. เตรียมข้อมูลสรุปรายวิชา
            var courseSummaries = allRules.Select(r => new CourseSummaryDto
            {
                AssignmentRuleId  = r.Id,
                CourseCode        = r.Course?.Code ?? "-",
                CourseTitle       = r.Course?.Title ?? "Unknown Course",
                CompletedStudents = enrollments.Count(e => e.AssignmentRuleId == r.Id && e.IsCompleted),
                TotalStudents     = enrollments.Count(e => e.AssignmentRuleId == r.Id)
            }).ToList();

            // 6. ดึงชื่อนักเรียนจาก External API (parallel เพื่อความเร็ว)
            var uniqueCodes  = enrollments.Select(e => e.StudentCode).Distinct().ToList();
            var nameTasks    = uniqueCodes.Select(async code =>
            {
                try
                {
                    var student = await _studentApiService.GetStudentByCodeAsync(code);
                    return (code, name: student?.Name ?? code);
                }
                catch
                {
                    return (code, name: code); // fallback เป็น code ถ้า API ล้มเหลว
                }
            });
            var nameResults  = await Task.WhenAll(nameTasks);
            var studentNames = nameResults.ToDictionary(x => x.code, x => x.name);

            // สร้าง lookup: assignmentRuleId → Course จาก allRules
            var ruleCourseMap = allRules.ToDictionary(r => r.Id, r => r.Course);

            var studentsProgress = enrollments.Select(e =>
            {
                var course = e.Course ?? (e.AssignmentRuleId.HasValue && ruleCourseMap.TryGetValue(e.AssignmentRuleId.Value, out var c) ? c : null);
                return new StudentProgressDto
                {
                    StudentCode      = e.StudentCode,
                    StudentName      = studentNames.GetValueOrDefault(e.StudentCode, e.StudentCode),
                    AssignmentRuleId = e.AssignmentRuleId,
                    CourseCode       = course?.Code ?? "-",
                    CourseTitle      = course?.Title ?? "Unknown Course",
                    Progress         = e.Progress,
                    IsCompleted      = e.IsCompleted,
                    CompletedDate    = e.CompletedDate,
                    StartDate        = e.StartDate,
                    DueDate          = e.DueDate
                };
            }).ToList();

            // 7. ประกอบร่าง DTO
            var result = new AssignmentDashboardDto
            {
                AssignmentNo   = mainRule.AssignmentNo,
                Description    = mainRule.Description,
                CreatedBy      = mainRule.CreatedBy,
                StartDate      = mainRule.StartDate,
                DueDate        = mainRule.DueDate,
                TotalEmployees = uniqueStudentsCount,
                TotalCourses   = allRules.Count(),
                CompletionRate = completionRate,
                ChartData      = new DashboardChartDto
                {
                    Completed   = completedCount,
                    InProgress  = inProgressCount,
                    NotStarted  = notStartedCount
                },
                Courses  = courseSummaries,
                Students = studentsProgress
            };

            return Ok(new { success = true, data = result });
        }

        [HttpPost("validate-before-assign")]
        public async Task<IActionResult> ValidateBeforeAssign([FromBody] BulkAssignDto dto)
        {
            // resolve GroupId → EmployeeCodes ถ้า Assign จาก Student Group
            if (dto.GroupId.HasValue && dto.EmployeeCodes.Count == 0)
            {
                dto.EmployeeCodes = await _studentGroupService.GetStudentCodesAsync(dto.GroupId.Value);
                if (dto.EmployeeCodes.Count == 0)
                    return BadRequest(new { message = "The selected group has no members." });
            }

            var conflicts = await _enrollmentRepo.GetAsync(
                filter: e => dto.EmployeeCodes.Contains(e.StudentCode) &&
                             dto.CourseIds.Contains(e.CourseId ?? 0) &&
                             !e.IsCompleted,
                includeProperties: "Course"
            );

            var result = conflicts.Select(c => new {
                StudentCode = c.StudentCode,
                CourseTitle = c.Course?.Title ?? "Unknown",
                DueDate     = c.DueDate
            }).ToList();

            return Ok(new { success = true, conflicts = result, resolvedCount = dto.EmployeeCodes.Count });
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
            var activeEnrollments = await _enrollmentRepo.GetAsync(
                e => e.AssignmentRuleId.HasValue &&
                     ruleIds.Contains(e.AssignmentRuleId.Value) &&
                     !e.IsCompleted
            );
            foreach (var enrollment in activeEnrollments)
            {
                enrollment.DueDate = dto.NewDueDate;
                await _enrollmentRepo.UpdateAsync(enrollment);
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
                r => r.StudentGroupId == groupId,
                includeProperties: "Course"
            );

            if (!assignments.Any())
                return Ok(new { success = true, data = new List<object>() });

            var allIds      = assignments.Select(a => a.Id).ToList();
            var enrollments = await _enrollmentRepo.GetAsync(
                e => e.AssignmentRuleId.HasValue && allIds.Contains(e.AssignmentRuleId.Value)
            );

            var now = DateTime.UtcNow.AddHours(7);

            var history = assignments
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo))
                .GroupBy(r => r.AssignmentNo)
                .Select(g =>
                {
                    var first   = g.First();
                    var ruleIds = g.Select(a => a.Id).ToList();
                    var related = enrollments
                        .Where(e => e.AssignmentRuleId.HasValue && ruleIds.Contains(e.AssignmentRuleId.Value))
                        .ToList();

                    bool allDone = related.Any() && related.All(e => e.IsCompleted);
                    string status = "InProgress";
                    if (allDone)
                        status = "Completed";
                    else if (first.StartDate.HasValue && first.StartDate.Value > now)
                        status = "Upcoming";
                    else if (first.DueDate.HasValue && first.DueDate.Value < now)
                        status = "Expired";

                    var done  = related.Count(e => e.IsCompleted);
                    var total = related.Count;
                    var pct   = total > 0 ? Math.Round((double)done / total * 100) : 0;

                    return new
                    {
                        id                       = first.Id,
                        assignmentNo             = g.Key,
                        description              = first.Description,
                        courseNames              = string.Join(", ", g
                            .Select(c => c.Course != null ? c.Course.Title : "Unknown")
                            .Distinct()),
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