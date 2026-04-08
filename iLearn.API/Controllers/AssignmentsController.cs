using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly ICourseAssignmentService _courseAssignmentService;
        private readonly IStudentApiService _studentApiService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly AppDbContext _dbContext;

        public AssignmentsController(
            IGenericRepository<Assignment> repo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Course> courseRepo,
            IAssignmentBatchService assignmentBatchService,
            IAssignmentDashboardService dashboardService,
            ICourseAssignmentService courseAssignmentService,
            IStudentApiService studentApiService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            AppDbContext dbContext)
        {
            _repo = repo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _courseRepo = courseRepo;
            _assignmentBatchService = assignmentBatchService;
            _dashboardService = dashboardService;
            _courseAssignmentService = courseAssignmentService;
            _studentApiService = studentApiService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _dbContext = dbContext;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] PaginationParams p)
        {
            var history = await BuildAssignmentHistoryAsync();
            var summary = BuildHistorySummary(history);

            var filtered = ApplyHistoryFilters(history, p.Search, p.Status);
            var ordered = ApplyHistorySorting(filtered, p.SortBy, p.SortDescending).ToList();

            var page = p.Page < 1 ? 1 : p.Page;
            var pageSize = p.PageSize < 1 ? 20 : p.PageSize;
            var totalCount = ordered.Count;
            var paged = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                data = paged,
                totalCount,
                page,
                pageSize,
                summary
            });
        }

        [HttpGet("gantt")]
        public async Task<IActionResult> GetGanttTasks()
        {
            var tasks = await BuildGanttTasksAsync();
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
            var result = await BuildAssignmentDashboardAsync(id);
            if (result == null) return NotFound(new { message = "Assignment not found" });
            return Ok(new { success = true, data = result });
        }

        [HttpGet("resolve/{assignmentNo}")]
        public async Task<IActionResult> ResolveByNo(string assignmentNo)
        {
            var assignment = await _dbContext.Assignments
                .Where(a => !a.IsDeleted && a.AssignmentNo == assignmentNo)
                .Select(a => new { a.Id })
                .FirstOrDefaultAsync();

            if (assignment == null)
                return NotFound(new { message = "Assignment not found" });

            return Ok(new { success = true, data = assignment.Id });
        }

        [HttpGet("reassign-data/{id}")]
        public async Task<IActionResult> GetReassignData(int id)
        {
            var divisionId = _currentUser.DivisionId;

            var mainAssignment = await _dbContext.Assignments
                .AsNoTracking()
                .Where(a => a.Id == id && (!divisionId.HasValue || a.DivisionId == divisionId.Value))
                .Select(a => new { a.AssignmentNo, a.StudentGroupId })
                .FirstOrDefaultAsync();

            if (mainAssignment == null)
                return NotFound(new { message = "Assignment not found" });

            var courseIds = await _dbContext.Assignments
                .AsNoTracking()
                .Where(a => (!divisionId.HasValue || a.DivisionId == divisionId.Value)
                    && (string.IsNullOrWhiteSpace(mainAssignment.AssignmentNo)
                        ? a.Id == id
                        : a.AssignmentNo == mainAssignment.AssignmentNo)
                    && a.CourseId.HasValue)
                .Join(_dbContext.Courses.Where(c => !c.IsDeleted),
                    a => a.CourseId, c => c.Id,
                    (a, c) => c.Id)
                .Distinct()
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    courseIds,
                    studentGroupId = mainAssignment.StudentGroupId
                }
            });
        }

        [HttpPost("{id}/reset-enrollments")]
        public async Task<IActionResult> ResetEnrollments(int id, [FromBody] ResetEnrollmentsDto dto)
        {
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null)
                return NotFound(new { message = "Assignment not found" });

            if (!IsAccessibleToCurrentDivision(mainRule.DivisionId))
                return Forbid();

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var targetRuleIds = batchRules.Select(r => r.Id).ToList();

            // Filter by specific rules if provided
            if (dto.RuleIds is { Count: > 0 })
                targetRuleIds = targetRuleIds.Intersect(dto.RuleIds).ToList();

            if (targetRuleIds.Count == 0)
                return BadRequest(new { message = "No matching courses found in this assignment." });

            var normalizedStudentCodes = dto.StudentCodes is { Count: > 0 }
                ? NormalizeStudentCodes(dto.StudentCodes)
                : null;

            // Find enrollment IDs via EnrollmentAssignment links
            var linksQuery = _dbContext.EnrollmentAssignments
                .Where(ea => targetRuleIds.Contains(ea.AssignmentId)
                    && !ea.IsDeleted
                    && ea.Enrollment != null);

            if (normalizedStudentCodes != null)
                linksQuery = linksQuery.Where(ea => normalizedStudentCodes.Contains(ea.Enrollment!.StudentCode));

            var enrollmentIds = await linksQuery
                .Select(ea => ea.EnrollmentId)
                .Distinct()
                .ToListAsync();

            if (enrollmentIds.Count == 0)
                return BadRequest(new { message = "No enrollments found matching the selected criteria." });

            // If filtering by specific courses, also filter enrollments by those courses
            var courseIds = batchRules
                .Where(r => targetRuleIds.Contains(r.Id) && r.CourseId.HasValue)
                .Select(r => r.CourseId!.Value)
                .ToList();

            var enrollments = await _dbContext.Enrollments
                .Where(e => enrollmentIds.Contains(e.Id)
                    && !e.IsDeleted
                    && (courseIds.Count == 0 || courseIds.Contains(e.CourseId ?? 0)))
                .ToListAsync();

            if (enrollments.Count == 0)
                return BadRequest(new { message = "No enrollments found matching the selected criteria." });

            var now = _dateTime.Now;
            var resetCount = 0;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Snapshot current completion state in EnrollmentAssignment links
                var enrollmentIdsToReset = enrollments.Select(e => e.Id).ToList();
                var links = await _dbContext.EnrollmentAssignments
                    .Where(ea => targetRuleIds.Contains(ea.AssignmentId)
                        && enrollmentIdsToReset.Contains(ea.EnrollmentId)
                        && !ea.IsDeleted)
                    .ToListAsync();

                // Clear snapshot so dashboard reads the actual (reset) enrollment values
                foreach (var link in links)
                {
                    link.SnapshotCompleted = false;
                    link.SnapshotCompletedDate = null;
                    link.SnapshotProgress = 0;
                }

                foreach (var enrollment in enrollments)
                {
                    enrollment.IsCompleted = false;
                    enrollment.CompletedDate = null;
                    enrollment.Progress = 0;
                    enrollment.TotalScore = 0;
                    enrollment.ResetAt = now;
                    resetCount++;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return Ok(new
            {
                success = true,
                message = $"Successfully reset {resetCount} enrollment(s).",
                resetCount
            });
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

        [HttpPost("{id}/courses")]
        public async Task<IActionResult> AddCourses(int id, [FromBody] ManageAssignmentCoursesDto dto)
        {
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null) return NotFound(new { message = "Assignment not found" });

            if (!IsAccessibleToCurrentDivision(mainRule.DivisionId))
            {
                return Forbid();
            }

            var requestedCourseIds = dto.CourseIds?
                .Distinct()
                .ToList() ?? [];

            if (requestedCourseIds.Count == 0)
            {
                return BadRequest(new { message = "At least one course is required." });
            }

            var accessibleCourses = await GetAccessibleCoursesAsync(requestedCourseIds);
            if (HasUnauthorizedCourses(requestedCourseIds, accessibleCourses))
            {
                return Forbid();
            }

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var existingCourseIds = batchRules
                .Where(rule => rule.CourseId.HasValue)
                .Select(rule => rule.CourseId!.Value)
                .Distinct()
                .ToHashSet();

            var newCourseIds = requestedCourseIds
                .Where(courseId => !existingCourseIds.Contains(courseId))
                .ToList();

            if (newCourseIds.Count == 0)
            {
                return Ok(new { success = true, message = "No new courses were added.", addedCount = 0 });
            }

            var studentCodes = await GetBatchStudentCodesAsync(batchRules.Select(rule => rule.Id).ToList(), batchRules);
            var employeeCodesText = string.Join(",", studentCodes);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var newRules = newCourseIds.Select(courseId => new Assignment
                {
                    AssignmentNo = mainRule.AssignmentNo,
                    Description = mainRule.Description,
                    CourseId = courseId,
                    EmployeeCodes = employeeCodesText,
                    StartDate = mainRule.StartDate,
                    DueDate = mainRule.DueDate,
                    Division = mainRule.Division,
                    StudentGroupId = mainRule.StudentGroupId,
                    DivisionId = mainRule.DivisionId
                }).ToList();

                await _dbContext.Assignments.AddRangeAsync(newRules);
                await _dbContext.SaveChangesAsync();

                if (studentCodes.Count > 0)
                {
                    var assignmentRuleIdsByCourseId = newRules
                        .Where(rule => rule.CourseId.HasValue)
                        .ToDictionary(rule => rule.CourseId!.Value, rule => rule.Id);

                    await _courseAssignmentService.AssignCoursesToEmployees(
                        assignmentRuleIdsByCourseId,
                        studentCodes,
                        mainRule.StartDate,
                        mainRule.DueDate,
                        forceReset: false);
                }

                await transaction.CommitAsync();
                return Ok(new { success = true, message = "Courses added successfully.", addedCount = newRules.Count });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpDelete("{id}/courses/{ruleId}")]
        public async Task<IActionResult> RemoveCourse(int id, int ruleId)
        {
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null) return NotFound(new { message = "Assignment not found" });

            if (!IsAccessibleToCurrentDivision(mainRule.DivisionId))
            {
                return Forbid();
            }

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var targetRule = batchRules.FirstOrDefault(rule => rule.Id == ruleId);
            if (targetRule == null)
            {
                return NotFound(new { message = "Assigned course not found." });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var links = await _enrollmentAssignmentRepo.GetAsync(link => link.AssignmentId == targetRule.Id, includeProperties: "Enrollment");
                foreach (var link in links)
                {
                    link.IsDeleted = true;
                    link.DeletedAt = _dateTime.Now;
                }

                targetRule.IsDeleted = true;
                targetRule.DeletedAt = _dateTime.Now;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var remainingRules = batchRules.Count(rule => rule.Id != targetRule.Id);
                return Ok(new
                {
                    success = true,
                    message = "Assigned course removed successfully.",
                    assignmentDeleted = remainingRules == 0
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPost("{id}/students")]
        public async Task<IActionResult> AddStudents(int id, [FromBody] ManageAssignmentStudentsDto dto)
        {
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null) return NotFound(new { message = "Assignment not found" });

            if (!IsAccessibleToCurrentDivision(mainRule.DivisionId))
            {
                return Forbid();
            }

            var requestedStudentCodes = NormalizeStudentCodes(dto.EmployeeCodes);
            if (requestedStudentCodes.Count == 0)
            {
                return BadRequest(new { message = "At least one student is required." });
            }

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var ruleIds = batchRules.Select(rule => rule.Id).ToList();
            var currentStudentCodes = await GetBatchStudentCodesAsync(ruleIds, batchRules);
            var newStudentCodes = requestedStudentCodes
                .Except(currentStudentCodes, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (newStudentCodes.Count == 0)
            {
                return Ok(new { success = true, message = "No new students were added.", addedCount = 0 });
            }

            var updatedStudentCodes = currentStudentCodes
                .Union(newStudentCodes, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var rule in batchRules)
                {
                    rule.EmployeeCodes = string.Join(",", updatedStudentCodes);
                }

                var assignmentRuleIdsByCourseId = batchRules
                    .Where(rule => rule.CourseId.HasValue)
                    .ToDictionary(rule => rule.CourseId!.Value, rule => rule.Id);

                if (assignmentRuleIdsByCourseId.Count > 0)
                {
                    await _courseAssignmentService.AssignCoursesToEmployees(
                        assignmentRuleIdsByCourseId,
                        newStudentCodes,
                        mainRule.StartDate,
                        mainRule.DueDate,
                        forceReset: false);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { success = true, message = "Students added successfully.", addedCount = newStudentCodes.Count });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpDelete("{id}/students/{studentCode}")]
        public async Task<IActionResult> RemoveStudent(int id, string studentCode)
        {
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null) return NotFound(new { message = "Assignment not found" });

            if (!IsAccessibleToCurrentDivision(mainRule.DivisionId))
            {
                return Forbid();
            }

            var normalizedStudentCode = studentCode?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedStudentCode))
            {
                return BadRequest(new { message = "Student code is required." });
            }

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var ruleIds = batchRules.Select(rule => rule.Id).ToList();
            var currentStudentCodes = await GetBatchStudentCodesAsync(ruleIds, batchRules);
            if (!currentStudentCodes.Contains(normalizedStudentCode, StringComparer.OrdinalIgnoreCase))
            {
                return NotFound(new { message = "Student is not assigned to this assignment." });
            }

            var remainingStudentCodes = currentStudentCodes
                .Where(code => !string.Equals(code, normalizedStudentCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var links = await _enrollmentAssignmentRepo.GetAsync(
                    link => ruleIds.Contains(link.AssignmentId)
                        && link.Enrollment != null
                        && link.Enrollment.StudentCode == normalizedStudentCode,
                    includeProperties: "Enrollment");

                foreach (var link in links)
                {
                    link.IsDeleted = true;
                    link.DeletedAt = _dateTime.Now;
                }

                var employeeCodesText = string.Join(",", remainingStudentCodes);
                foreach (var rule in batchRules)
                {
                    rule.EmployeeCodes = employeeCodesText;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { success = true, message = "Student removed successfully." });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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

        private async Task<List<string>> GetBatchStudentCodesAsync(List<int> ruleIds, IEnumerable<Assignment> batchRules)
        {
            var studentCodesFromLinks = await _dbContext.EnrollmentAssignments
                .AsNoTracking()
                .Where(link => ruleIds.Contains(link.AssignmentId) && !link.IsDeleted && link.Enrollment != null)
                .Select(link => link.Enrollment!.StudentCode)
                .Distinct()
                .ToListAsync();

            var studentCodesFromRules = batchRules
                .SelectMany(rule => (rule.EmployeeCodes ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToList();

            return studentCodesFromLinks
                .Concat(studentCodesFromRules)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> NormalizeStudentCodes(IEnumerable<string>? studentCodes)
        {
            return studentCodes?
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }

        private async Task<List<AssignmentHistoryDto>> BuildAssignmentHistoryAsync()
        {
            var divisionId = _currentUser.DivisionId;
            var currentDate = _dateTime.Now;

            var assignmentQuery = _dbContext.Assignments
                .AsNoTracking()
                .Where(a => !divisionId.HasValue || a.DivisionId == divisionId.Value);

            var assignmentRows = await assignmentQuery
                .Select(a => new AssignmentHistoryAssignmentRow
                {
                    Id = a.Id,
                    AssignmentNo = a.AssignmentNo,
                    Description = a.Description,
                    EmployeeCodes = a.EmployeeCodes,
                    CourseId = a.CourseId,
                    StartDate = a.StartDate,
                    DueDate = a.DueDate,
                    CreatedBy = a.CreatedBy,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            if (assignmentRows.Count == 0)
            {
                return [];
            }

            var courseIds = assignmentRows
                .Where(a => a.CourseId.HasValue)
                .Select(a => a.CourseId!.Value)
                .Distinct()
                .ToList();

            var courseMap = courseIds.Count == 0
                ? new Dictionary<int, AssignmentHistoryCourseRow>()
                : await _dbContext.Courses
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(c => courseIds.Contains(c.Id))
                    .Select(c => new AssignmentHistoryCourseRow
                    {
                        Id = c.Id,
                        Title = c.Title,
                        IsDeleted = c.IsDeleted
                    })
                    .ToDictionaryAsync(c => c.Id);

            var assignmentIdsQuery = assignmentQuery.Select(a => a.Id);

            var linkRows = await _dbContext.EnrollmentAssignments
                .AsNoTracking()
                .Where(ea => assignmentIdsQuery.Contains(ea.AssignmentId))
                .Select(ea => new AssignmentHistoryLinkRow
                {
                    AssignmentId = ea.AssignmentId,
                    StudentCode = ea.Enrollment != null ? ea.Enrollment.StudentCode : null,
                    IsCompleted = ea.SnapshotCompleted || (ea.Enrollment != null && ea.Enrollment.IsCompleted)
                })
                .Where(ea => ea.StudentCode != null)
                .ToListAsync();

            var linksByAssignmentId = linkRows.ToLookup(link => link.AssignmentId);

            return assignmentRows
                .GroupBy(row => string.IsNullOrWhiteSpace(row.AssignmentNo) ? $"assignment:{row.Id}" : row.AssignmentNo!)
                .Select(group => MapHistoryDto(group, linksByAssignmentId, currentDate, courseMap))
                .ToList();
        }

        private async Task<AssignmentDashboardDto?> BuildAssignmentDashboardAsync(int assignmentId)
        {
            var divisionId = _currentUser.DivisionId;

            var mainAssignment = await _dbContext.Assignments
                .AsNoTracking()
                .Where(a => a.Id == assignmentId && (!divisionId.HasValue || a.DivisionId == divisionId.Value))
                .Select(a => new DashboardAssignmentRow
                {
                    Id = a.Id,
                    AssignmentNo = a.AssignmentNo,
                    Description = a.Description,
                    CourseId = a.CourseId,
                    DivisionId = a.DivisionId,
                    StartDate = a.StartDate,
                    DueDate = a.DueDate,
                    CreatedBy = a.CreatedBy,
                    CreatedAt = a.CreatedAt,
                    StudentGroupId = a.StudentGroupId,
                    StudentGroupName = a.StudentGroup != null ? a.StudentGroup.Name : null
                })
                .FirstOrDefaultAsync();

            if (mainAssignment == null)
            {
                return null;
            }

            var assignmentRows = await _dbContext.Assignments
                .AsNoTracking()
                .Where(a => (!divisionId.HasValue || a.DivisionId == divisionId.Value)
                    && (string.IsNullOrWhiteSpace(mainAssignment.AssignmentNo)
                        ? a.Id == mainAssignment.Id
                        : a.AssignmentNo == mainAssignment.AssignmentNo))
                .Select(a => new DashboardAssignmentRow
                {
                    Id = a.Id,
                    AssignmentNo = a.AssignmentNo,
                    Description = a.Description,
                    CourseId = a.CourseId,
                    DivisionId = a.DivisionId,
                    StartDate = a.StartDate,
                    DueDate = a.DueDate,
                    CreatedBy = a.CreatedBy,
                    CreatedAt = a.CreatedAt,
                    StudentGroupId = a.StudentGroupId,
                    StudentGroupName = a.StudentGroup != null ? a.StudentGroup.Name : null
                })
                .ToListAsync();

            if (assignmentRows.Count == 0)
            {
                return null;
            }

            var courseIds = assignmentRows
                .Where(row => row.CourseId.HasValue)
                .Select(row => row.CourseId!.Value)
                .Distinct()
                .ToList();

            var courseMap = courseIds.Count == 0
                ? new Dictionary<int, AssignmentHistoryCourseRow>()
                : await _dbContext.Courses
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(course => courseIds.Contains(course.Id))
                    .Select(course => new AssignmentHistoryCourseRow
                    {
                        Id = course.Id,
                        Title = course.Title,
                        IsDeleted = course.IsDeleted,
                        Code = course.Code
                    })
                    .ToDictionaryAsync(course => course.Id);

            var ruleIds = assignmentRows.Select(row => row.Id).ToList();

            var studentRows = await _dbContext.EnrollmentAssignments
                .AsNoTracking()
                .Where(link => ruleIds.Contains(link.AssignmentId) && link.Enrollment != null)
                .Select(link => new DashboardStudentRow
                {
                    AssignmentId = link.AssignmentId,
                    StudentCode = link.Enrollment!.StudentCode,
                    Progress = link.SnapshotCompleted ? link.SnapshotProgress : link.Enrollment.Progress,
                    IsCompleted = link.SnapshotCompleted || link.Enrollment.IsCompleted,
                    CompletedDate = link.SnapshotCompleted ? link.SnapshotCompletedDate : link.Enrollment.CompletedDate,
                    StartDate = link.StartDate,
                    DueDate = link.DueDate
                })
                .ToListAsync();

            var uniqueStudentCodes = studentRows
                .Select(row => row.StudentCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var studentNames = uniqueStudentCodes.Count == 0
                ? new Dictionary<string, ExternalStudentDto>(StringComparer.OrdinalIgnoreCase)
                : await _studentApiService.GetStudentsByCodesAsync(uniqueStudentCodes);

            var studentsByCode = studentRows
                .GroupBy(row => row.StudentCode)
                .Select(group => new
                {
                    StudentCode = group.Key,
                    AllCompleted = group.All(row => row.IsCompleted),
                    AnyStarted = group.Any(row => row.IsCompleted || row.Progress > 0)
                })
                .ToList();

            var completedCount = studentsByCode.Count(item => item.AllCompleted);
            var inProgressCount = studentsByCode.Count(item => !item.AllCompleted && item.AnyStarted);
            var notStartedCount = studentsByCode.Count(item => !item.AllCompleted && !item.AnyStarted);
            var totalEnrollments = studentRows.Count;
            var completedEnrollments = studentRows.Count(row => row.IsCompleted);
            var completionRate = totalEnrollments == 0
                ? 0
                : Math.Round((double)completedEnrollments / totalEnrollments * 100);

            var studentCountByRule = studentRows
                .GroupBy(row => row.AssignmentId)
                .ToDictionary(group => group.Key, group => group.Count());

            var completedCountByRule = studentRows
                .Where(row => row.IsCompleted)
                .GroupBy(row => row.AssignmentId)
                .ToDictionary(group => group.Key, group => group.Count());

            var courseSummaries = assignmentRows
                .Select(row =>
                {
                    AssignmentHistoryCourseRow? course = null;
                    if (row.CourseId.HasValue)
                    {
                        courseMap.TryGetValue(row.CourseId.Value, out course);
                    }

                    return new CourseSummaryDto
                    {
                        AssignmentRuleId = row.Id,
                        CourseCode = course?.Code ?? "-",
                        CourseTitle = course?.Title ?? "Unknown Course",
                        CompletedStudents = completedCountByRule.GetValueOrDefault(row.Id),
                        TotalStudents = studentCountByRule.GetValueOrDefault(row.Id),
                        IsCourseDeleted = course?.IsDeleted ?? false
                    };
                })
                .ToList();

            var students = studentRows
                .Select(row =>
                {
                    var assignment = assignmentRows.FirstOrDefault(item => item.Id == row.AssignmentId);
                    AssignmentHistoryCourseRow? course = null;
                    if (assignment?.CourseId.HasValue == true)
                    {
                        courseMap.TryGetValue(assignment.CourseId.Value, out course);
                    }

                    var status = row.IsCompleted ? "Completed" : row.Progress > 0 ? "In Progress" : "Pending";
                    return new StudentProgressDto
                    {
                        StudentCode = row.StudentCode,
                        StudentName = studentNames.GetValueOrDefault(row.StudentCode)?.Name ?? row.StudentCode,
                        AssignmentRuleId = row.AssignmentId,
                        CourseCode = course?.Code ?? "-",
                        CourseTitle = course?.Title ?? "Unknown Course",
                        Progress = row.Progress,
                        IsCompleted = row.IsCompleted,
                        Status = status,
                        CompletedDate = row.CompletedDate,
                        StartDate = row.StartDate,
                        DueDate = row.DueDate
                    };
                })
                .ToList();

            return new AssignmentDashboardDto
            {
                AssignmentNo = mainAssignment.AssignmentNo ?? string.Empty,
                Description = mainAssignment.Description ?? string.Empty,
                CreatedBy = mainAssignment.CreatedBy,
                StartDate = mainAssignment.StartDate,
                DueDate = mainAssignment.DueDate,
                TotalEmployees = studentsByCode.Count,
                TotalCourses = courseSummaries.Count,
                CompletionRate = completionRate,
                StudentGroupId = mainAssignment.StudentGroupId,
                StudentGroupName = mainAssignment.StudentGroupName,
                HasDeletedCourse = courseSummaries.Any(course => course.IsCourseDeleted),
                ChartData = new DashboardChartDto
                {
                    Completed = completedCount,
                    InProgress = inProgressCount,
                    NotStarted = notStartedCount
                },
                Courses = courseSummaries,
                Students = students
            };
        }

        private async Task<List<GanttTaskRow>> BuildGanttTasksAsync()
        {
            var divisionId = _currentUser.DivisionId;
            var currentDate = _dateTime.Now;

            var assignmentRows = await _dbContext.Assignments
                .AsNoTracking()
                .Where(a => !divisionId.HasValue || a.DivisionId == divisionId.Value)
                .Select(a => new AssignmentHistoryAssignmentRow
                {
                    Id = a.Id,
                    AssignmentNo = a.AssignmentNo,
                    Description = a.Description,
                    StartDate = a.StartDate,
                    DueDate = a.DueDate,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            if (assignmentRows.Count == 0)
            {
                return [];
            }

            var assignmentIds = assignmentRows.Select(item => item.Id).ToList();

            var linkRows = await _dbContext.EnrollmentAssignments
                .AsNoTracking()
                .Where(ea => assignmentIds.Contains(ea.AssignmentId) && ea.Enrollment != null)
                .Select(ea => new GanttLinkRow
                {
                    AssignmentId = ea.AssignmentId,
                    IsCompleted = ea.SnapshotCompleted || ea.Enrollment!.IsCompleted
                })
                .ToListAsync();

            var linksByAssignmentId = linkRows.ToLookup(link => link.AssignmentId);

            return assignmentRows
                .GroupBy(row => string.IsNullOrWhiteSpace(row.AssignmentNo) ? $"assignment:{row.Id}" : row.AssignmentNo!)
                .Select(group => MapGanttTask(group, linksByAssignmentId, currentDate))
                .OrderByDescending(task => task.AssignmentNo)
                .ThenByDescending(task => task.StartDate)
                .ToList();
        }

        private static IEnumerable<AssignmentHistoryDto> ApplyHistoryFilters(
            IEnumerable<AssignmentHistoryDto> history,
            string? search,
            string? status)
        {
            var filtered = history;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                filtered = filtered.Where(item =>
                    (!string.IsNullOrWhiteSpace(item.AssignmentNo) && item.AssignmentNo.Contains(term, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(item.Description) && item.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(item.CourseNames) && item.CourseNames.Contains(term, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(item.CreatedBy) && item.CreatedBy.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                filtered = filtered.Where(item => string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            return filtered;
        }

        private static IOrderedEnumerable<AssignmentHistoryDto> ApplyHistorySorting(
            IEnumerable<AssignmentHistoryDto> history,
            string? sortBy,
            bool sortDescending)
        {
            var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy) ? "assignmentNo" : sortBy;

            Func<AssignmentHistoryDto, object?> keySelector = normalizedSortBy switch
            {
                "createdBy" => item => item.CreatedBy,
                "courseNames" => item => item.CourseNames,
                "description" => item => item.Description,
                "studentCount" => item => item.StudentCount,
                "progress" or "completedEnrollmentCount" => item => item.TotalEnrollmentCount > 0
                    ? Math.Round((double)item.CompletedEnrollmentCount / item.TotalEnrollmentCount * 100)
                    : 0,
                "startDate" => item => item.StartDate,
                "dueDate" => item => item.DueDate,
                "status" => item => item.Status,
                _ => item => item.AssignmentNo
            };

            return sortDescending
                ? history.OrderByDescending(keySelector).ThenByDescending(item => item.CreatedAt)
                : history.OrderBy(keySelector).ThenByDescending(item => item.CreatedAt);
        }

        private static object BuildHistorySummary(IEnumerable<AssignmentHistoryDto> history)
        {
            var rows = history.ToList();
            return new
            {
                all = rows.Count,
                inProgress = rows.Count(item => item.Status == "InProgress"),
                upcoming = rows.Count(item => item.Status == "Upcoming"),
                expired = rows.Count(item => item.Status == "Expired"),
                completed = rows.Count(item => item.Status == "Completed")
            };
        }

        private static AssignmentHistoryDto MapHistoryDto(
            IGrouping<string, AssignmentHistoryAssignmentRow> group,
            ILookup<int, AssignmentHistoryLinkRow> linksByAssignmentId,
            DateTime currentDate,
            IReadOnlyDictionary<int, AssignmentHistoryCourseRow> courseMap)
        {
            var first = group.First();
            var relatedLinks = group
                .SelectMany(item => linksByAssignmentId[item.Id])
                .Where(link => !string.IsNullOrWhiteSpace(link.StudentCode))
                .ToList();

            var allCompleted = relatedLinks.Count > 0 && relatedLinks.All(link => link.IsCompleted);
            var status = AssignmentDashboardService.CalculateStatus(
                relatedLinks.Count > 0,
                allCompleted,
                first.StartDate,
                first.DueDate,
                currentDate);

            var courseEntries = group
                .Where(item => item.CourseId.HasValue && courseMap.ContainsKey(item.CourseId.Value))
                .Select(item => courseMap[item.CourseId!.Value])
                .DistinctBy(course => course.Id)
                .ToList();

            var deletedCourses = courseEntries.Where(course => course.IsDeleted).ToList();
            var activeCourses = courseEntries.Where(course => !course.IsDeleted).ToList();
            var allCourseNameParts = activeCourses
                .Select(course => course.Title ?? "Unknown Course")
                .Concat(deletedCourses.Select(course => $"{course.Title ?? "Unknown Course"} [Deleted]"));

            return new AssignmentHistoryDto
            {
                Id = first.Id,
                AssignmentNo = group.Key,
                Description = first.Description ?? string.Empty,
                EmployeeCodes = first.EmployeeCodes ?? string.Empty,
                StartDate = first.StartDate,
                DueDate = first.DueDate,
                CourseNames = string.Join(", ", allCourseNameParts),
                Status = status,
                CreatedBy = first.CreatedBy,
                CreatedAt = first.CreatedAt,
                CourseCount = courseEntries.Count,
                StudentCount = string.IsNullOrWhiteSpace(first.EmployeeCodes)
                    ? 0
                    : first.EmployeeCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
                CompletedEnrollmentCount = relatedLinks.Count(link => link.IsCompleted),
                TotalEnrollmentCount = relatedLinks.Count,
                HasDeletedCourse = deletedCourses.Count > 0,
                DeletedCourseNames = deletedCourses.Count > 0
                    ? string.Join(", ", deletedCourses.Select(course => course.Title ?? "Unknown"))
                    : null
            };
        }

        private static GanttTaskRow MapGanttTask(
            IGrouping<string, AssignmentHistoryAssignmentRow> group,
            ILookup<int, GanttLinkRow> linksByAssignmentId,
            DateTime currentDate)
        {
            var first = group.First();
            var relatedLinks = group
                .SelectMany(item => linksByAssignmentId[item.Id])
                .ToList();

            var totalEnrollments = relatedLinks.Count;
            var completedEnrollments = relatedLinks.Count(link => link.IsCompleted);
            var allCompleted = totalEnrollments > 0 && completedEnrollments == totalEnrollments;
            var status = AssignmentDashboardService.CalculateStatus(
                totalEnrollments > 0,
                allCompleted,
                first.StartDate,
                first.DueDate,
                currentDate);

            var progress = totalEnrollments > 0
                ? (int)Math.Round((double)completedEnrollments / totalEnrollments * 100)
                : 0;

            var startDate = first.StartDate ?? first.CreatedAt;
            var dueDate = first.DueDate ?? startDate.AddDays(7);
            if (dueDate <= startDate)
            {
                dueDate = startDate.AddDays(1);
            }

            var assignmentNo = string.IsNullOrWhiteSpace(first.AssignmentNo)
                ? $"Assignment {first.Id}"
                : first.AssignmentNo!;

            return new GanttTaskRow
            {
                Id = first.Id,
                ParentId = 0,
                AssignmentNo = assignmentNo,
                Title = $"{assignmentNo} - {first.Description ?? "No Description"}",
                StartDate = startDate,
                DueDate = dueDate,
                Progress = progress,
                Color = GetStatusColor(status),
                Status = status
            };
        }

        private static string GetStatusColor(string status)
        {
            return status switch
            {
                "Completed" => "#52c41a",
                "InProgress" => "#1890ff",
                "Upcoming" => "#faad14",
                "Expired" => "#ff4d4f",
                _ => "#aaaaaa"
            };
        }

        private sealed class AssignmentHistoryAssignmentRow
        {
            public int Id { get; set; }
            public string? AssignmentNo { get; set; }
            public string? Description { get; set; }
            public string? EmployeeCodes { get; set; }
            public int? CourseId { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
            public string? CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private sealed class AssignmentHistoryCourseRow
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public bool IsDeleted { get; set; }
            public string? Code { get; set; }
        }

        private sealed class AssignmentHistoryLinkRow
        {
            public int AssignmentId { get; set; }
            public string? StudentCode { get; set; }
            public bool IsCompleted { get; set; }
        }

        private sealed class GanttLinkRow
        {
            public int AssignmentId { get; set; }
            public bool IsCompleted { get; set; }
        }

        private sealed class GanttTaskRow
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
            public string AssignmentNo { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime DueDate { get; set; }
            public int Progress { get; set; }
            public string Color { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        private sealed class DashboardAssignmentRow
        {
            public int Id { get; set; }
            public string? AssignmentNo { get; set; }
            public string? Description { get; set; }
            public int? CourseId { get; set; }
            public int? DivisionId { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
            public string? CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public int? StudentGroupId { get; set; }
            public string? StudentGroupName { get; set; }
        }

        private sealed class DashboardStudentRow
        {
            public int AssignmentId { get; set; }
            public string StudentCode { get; set; } = string.Empty;
            public double Progress { get; set; }
            public bool IsCompleted { get; set; }
            public DateTime? CompletedDate { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
        }
    }
}