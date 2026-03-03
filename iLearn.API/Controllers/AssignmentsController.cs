using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
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
        public AssignmentsController(
            IGenericRepository<Assignment> repo,
            ICourseAssignmentService assignmentService,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<Course> courseRepo)
        {
            _repo = repo;
            _assignmentService = assignmentService;
            _enrollmentRepo = enrollmentRepo;
            _courseRepo = courseRepo;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            // เรียกใช้ Logic จาก Service ที่เราสร้างไว้ 
            var history = await _assignmentService.GetAssignmentHistoryAsync();

            // ส่งกลับในรูปแบบที่ DevExtreme ต้องการ
            return Ok(new { data = history, totalCount = history.Count });
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var assignments = await _repo.GetAsync(r => r.CourseId == courseId);
            return Ok(assignments.Select(r => new { r.Id, r.CourseId })); // ปรับให้คืนค่าตามความเหมาะสม
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _repo.GetByIdAsync(id);
            if (rule == null) return NotFound();

            var relatedRules = await _repo.GetAsync(r => r.AssignmentNo == rule.AssignmentNo);
            var relatedRuleIds = relatedRules.Select(r => r.Id).ToList();

            // 🌟 3. ค้นหา Enrollment ทั้งหมดที่ผูกกับ Assignment กลุ่มนี้
            var relatedEnrollments = await _enrollmentRepo.GetAsync(e =>
                e.AssignmentRuleId.HasValue && relatedRuleIds.Contains(e.AssignmentRuleId.Value));

            // 🌟 4. สั่งลบ Enrollment (ลูก) ทิ้งก่อน
            foreach (var enrollment in relatedEnrollments)
            {
                await _enrollmentRepo.DeleteAsync(enrollment);
            }

            // 🌟 5. เมื่อลบลูกหมดแล้ว จึงจะสามารถลบ Assignment (แม่) ได้อย่างปลอดภัย
            foreach (var r in relatedRules)
            {
                await _repo.DeleteAsync(r);
            }

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

            // 3. ดึง Enrollments เฉพาะที่ผูกกับ Rule ในกลุ่มนี้
            var enrollments = await _enrollmentRepo.GetAsync(
                e => e.AssignmentRuleId.HasValue && ruleIds.Contains(e.AssignmentRuleId.Value)
            );

            // 4. คำนวณสถิติ
            var totalEnrollments = enrollments.Count();
            var completedCount = enrollments.Count(e => e.IsCompleted);
            var inProgressCount = enrollments.Count(e => !e.IsCompleted && e.Progress > 0);
            var notStartedCount = totalEnrollments - completedCount - inProgressCount;
            var uniqueStudentsCount = enrollments.Select(e => e.StudentCode).Distinct().Count();
            var completionRate = totalEnrollments == 0 ? 0 : Math.Round(((double)completedCount / totalEnrollments) * 100);

            // 5. เตรียมข้อมูลสรุปรายวิชา
            var courseSummaries = allRules.Select(r => new CourseSummaryDto
            {
                AssignmentRuleId = r.Id,
                CourseCode = r.Course?.Code ?? "-",
                CourseTitle = r.Course?.Title ?? "Unknown Course",
                CompletedStudents = enrollments.Count(e => e.AssignmentRuleId == r.Id && e.IsCompleted),
                TotalStudents = enrollments.Count(e => e.AssignmentRuleId == r.Id)
            }).ToList();

            // 6. เตรียมข้อมูลนักเรียน
            var studentsProgress = enrollments.Select(e => new StudentProgressDto
            {
                StudentCode = e.StudentCode,
                AssignmentRuleId = e.AssignmentRuleId,
                Progress = e.Progress,
                IsCompleted = e.IsCompleted,
                CompletedDate = e.CompletedDate
            }).ToList();

            // 7. ประกอบร่าง DTO
            var result = new AssignmentDashboardDto
            {
                AssignmentNo = mainRule.AssignmentNo,
                Description = mainRule.Description,
                StartDate = mainRule.StartDate,
                DueDate = mainRule.DueDate,
                TotalEmployees = uniqueStudentsCount,
                TotalCourses = allRules.Count(),
                CompletionRate = completionRate,
                ChartData = new DashboardChartDto
                {
                    Completed = completedCount,
                    InProgress = inProgressCount,
                    NotStarted = notStartedCount
                },
                Courses = courseSummaries,
                Students = studentsProgress
            };

            return Ok(new { success = true, data = result });
        }

        [HttpPost("validate-before-assign")]
        public async Task<IActionResult> ValidateBeforeAssign([FromBody] BulkAssignDto dto)
        {
            // หาว่ามี Enrollment ไหนที่พนักงานกลุ่มนี้ กำลังเรียน (IsCompleted = false) ในคอร์สกลุ่มนี้อยู่บ้าง
            var conflicts = await _enrollmentRepo.GetAsync(
                filter: e => dto.EmployeeCodes.Contains(e.StudentCode) &&
                             dto.CourseIds.Contains(e.CourseId ?? 0) &&
                             !e.IsCompleted,
                includeProperties: "Course"
            );

            var result = conflicts.Select(c => new {
                StudentCode = c.StudentCode,
                CourseTitle = c.Course?.Title ?? "Unknown",
                DueDate = c.DueDate
            }).ToList();

            return Ok(new { success = true, conflicts = result });
        }

        [HttpGet("lookup-courses")]
        public async Task<IActionResult> GetLookupCourses()
        {
            // ดึงเฉพาะคอร์สที่ใช้งานอยู่ (IsActive) พร้อมผูก DivisionId มาจาก Category
            var courses = await _courseRepo.GetAsync(c => c.IsActive, includeProperties: "Category");

            var result = courses.Select(c => new LookupCourseDto
            {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                CategoryId = c.CategoryId,
                DivisionId = c.Category?.DivisionId // ดึง DivisionId จาก Category
            }).ToList();

            return Ok(new { data = result });
        }

     
    }
}