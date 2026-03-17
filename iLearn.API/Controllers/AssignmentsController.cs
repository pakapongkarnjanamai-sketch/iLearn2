using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentsController : ControllerBase
    {
        private readonly IGenericRepository<Assignment> _repo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IAssignmentBatchService _assignmentBatchService;
        private readonly IAssignmentDashboardService _dashboardService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly AppDbContext _dbContext;

        public AssignmentsController(
            IGenericRepository<Assignment> repo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Course> courseRepo,
            IAssignmentBatchService assignmentBatchService,
            IAssignmentDashboardService dashboardService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            AppDbContext dbContext)
        {
            _repo = repo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _courseRepo = courseRepo;
            _assignmentBatchService = assignmentBatchService;
            _dashboardService = dashboardService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _dbContext = dbContext;
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
                    id = item.Id,
                    parentId = 0,
                    title = $"{item.AssignmentNo} - {item.Description ?? "No Description"}",
                    startDate = start,
                    dueDate = end,
                    progress,
                    color,
                    status = item.Status,
                    assignmentNo = item.AssignmentNo
                });
            }

            return Ok(tasks);
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var assignments = await _repo.GetAsync(r =>
                r.CourseId == courseId &&
                (!_currentUser.DivisionId.HasValue || r.DivisionId == _currentUser.DivisionId.Value)
            );
            return Ok(assignments.Select(r => new { r.Id, r.CourseId }));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _repo.GetByIdAsync(id);
            if (rule == null) return NotFound();

            if (!IsAccessibleToCurrentDivision(rule.DivisionId))
            {
                return Forbid();
            }

            var relatedRules = await _assignmentBatchService.LoadBatchAsync(rule);
            var relatedIds = relatedRules.Select(r => r.Id).ToList();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var links = await _enrollmentAssignmentRepo.GetAsync(
                    ea => relatedIds.Contains(ea.AssignmentId));

                foreach (var link in links)
                {
                    link.IsDeleted = true;
                    link.DeletedAt = _dateTime.Now;
                }

                foreach (var relatedRule in relatedRules)
                {
                    relatedRule.IsDeleted = true;
                    relatedRule.DeletedAt = _dateTime.Now;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

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
            var accessibleCourses = await GetAccessibleCoursesAsync(dto.CourseIds);
            if (HasUnauthorizedCourses(dto.CourseIds, accessibleCourses))
            {
                return Forbid();
            }

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

            var allRules = await _assignmentBatchService.LoadBatchAsync(mainRule);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var rule in allRules)
                {
                    rule.DueDate = dto.NewDueDate;
                }

                var ruleIds = allRules.Select(r => r.Id).ToList();
                var activeLinks = await _enrollmentAssignmentRepo.GetAsync(
                    ea => ruleIds.Contains(ea.AssignmentId),
                    includeProperties: "Enrollment"
                );

                foreach (var link in activeLinks.Where(ea => ea.Enrollment != null && !(ea.SnapshotCompleted || ea.Enrollment.IsCompleted)))
                {
                    link.DueDate = dto.NewDueDate;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return Ok(new { success = true, message = "Due date extended successfully.", newDueDate = dto.NewDueDate });
        }

        [HttpGet("lookup-courses")]
        public async Task<IActionResult> GetLookupCourses()
        {
            var courses = await GetAccessibleCoursesAsync([], includeCourseType: true);

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

        [HttpGet("group/{groupId}/history")]
        public async Task<IActionResult> GetGroupHistory(int groupId)
        {
            var history = await _dashboardService.GetGroupHistoryAsync(groupId);
            return Ok(new { success = true, data = history });
        }

        private bool IsAccessibleToCurrentDivision(int? divisionId)
        {
            return !_currentUser.DivisionId.HasValue || divisionId == _currentUser.DivisionId.Value;
        }

        private async Task<IReadOnlyList<Course>> GetAccessibleCoursesAsync(IEnumerable<int> courseIds, bool includeCourseType = false)
        {
            var targetCourseIds = courseIds.Distinct().ToList();
            var includeProperties = includeCourseType ? "Category,CourseType" : "Category";

            return await _courseRepo.GetAsync(
                c => c.IsActive
                    && (!targetCourseIds.Any() || targetCourseIds.Contains(c.Id))
                    && (!_currentUser.DivisionId.HasValue || c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value),
                includeProperties: includeProperties
            );
        }

        private static bool HasUnauthorizedCourses(IEnumerable<int> requestedCourseIds, IEnumerable<Course> accessibleCourses)
        {
            var accessibleCourseIds = accessibleCourses
                .Select(c => c.Id)
                .Distinct()
                .ToHashSet();

            return requestedCourseIds.Any(courseId => !accessibleCourseIds.Contains(courseId));
        }
    }
}