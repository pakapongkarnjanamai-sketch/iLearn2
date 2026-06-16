using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IAssignmentService _assignmentService;
        private readonly IAssignmentBatchService _assignmentBatchService;
        private readonly IAssignmentDashboardService _dashboardService;
        private readonly ICourseAssignmentService _courseAssignmentService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;

        public AssignmentsController(
            IGenericRepository<Assignment> repo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IAssignmentService assignmentService,
            IAssignmentBatchService assignmentBatchService,
            IAssignmentDashboardService dashboardService,
            ICourseAssignmentService courseAssignmentService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _enrollmentRepo = enrollmentRepo;
            _assignmentService = assignmentService;
            _assignmentBatchService = assignmentBatchService;
            _dashboardService = dashboardService;
            _courseAssignmentService = courseAssignmentService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] PaginationParams p, CancellationToken cancellationToken)
        {
            var response = await _assignmentService.GetHistoryAsync(
                p,
                _currentUser.DivisionId,
                _dateTime.Now,
                cancellationToken);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("gantt")]
        public async Task<IActionResult> GetGanttTasks(CancellationToken cancellationToken)
        {
            var tasks = await _assignmentService.GetGanttTasksAsync(
                _currentUser.DivisionId,
                _dateTime.Now,
                cancellationToken);
            return Ok(tasks);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var assignments = await _assignmentService.GetByCourseAsync(courseId, _currentUser.DivisionId);
            return Ok(assignments);
        }

        [Authorize(Policy = "AdminOnly")]
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

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
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

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return NoContent();
        }

        [Authorize(Policy = "DomainUser")]
        [HttpGet("dashboard/{id}")]
        public async Task<IActionResult> GetDashboardData(int id, CancellationToken cancellationToken)
        {
            var result = await _assignmentService.GetDashboardAsync(id, _currentUser.DivisionId, cancellationToken);
            if (result == null) return NotFound(new { message = "Assignment not found" });
            return Ok(new AssignmentDashboardResponseDto { Success = true, Data = result });
        }

        [Authorize(Policy = "DomainUser")]
        [HttpGet("resolve/{assignmentNo}")]
        public async Task<IActionResult> ResolveByNo(string assignmentNo, CancellationToken cancellationToken)
        {
            var assignmentId = await _assignmentService.ResolveAssignmentIdByNoAsync(assignmentNo, cancellationToken);

            if (!assignmentId.HasValue)
                return NotFound(new { message = "Assignment not found" });

            return Ok(new AssignmentResolveResponseDto { Success = true, Data = assignmentId.Value });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("reassign-data/{id}")]
        public async Task<IActionResult> GetReassignData(int id, CancellationToken cancellationToken)
        {
            var result = await _assignmentService.GetReassignDataAsync(id, _currentUser.DivisionId, cancellationToken);
            if (result == null)
                return NotFound(new { message = "Assignment not found" });

            return Ok(new AssignmentReassignDataResponseDto
            {
                Success = true,
                Data = result,
            });
        }

        [Authorize(Policy = "AdminOnly")]
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

            var normalizedLearnerCodes = dto.LearnerCodes is { Count: > 0 }
                ? _assignmentService.NormalizeLearnerCodes(dto.LearnerCodes)
                : null;

            // Find enrollment IDs via EnrollmentAssignment links
            var linksQuery = _enrollmentAssignmentRepo.GetQuery()
                .Where(ea => targetRuleIds.Contains(ea.AssignmentId)
                    && !ea.IsDeleted
                    && ea.Enrollment != null);

            if (normalizedLearnerCodes != null)
                linksQuery = linksQuery.Where(ea => normalizedLearnerCodes.Contains(ea.Enrollment!.LearnerCode));

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

            var enrollments = await _enrollmentRepo.GetQuery()
                .Where(e => enrollmentIds.Contains(e.Id)
                    && !e.IsDeleted
                    && (courseIds.Count == 0 || courseIds.Contains(e.CourseId ?? 0)))
                .ToListAsync();

            if (enrollments.Count == 0)
                return BadRequest(new { message = "No enrollments found matching the selected criteria." });

            var now = _dateTime.Now;
            var resetCount = 0;

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Snapshot current completion state in EnrollmentAssignment links
                var enrollmentIdsToReset = enrollments.Select(e => e.Id).ToList();
                var links = await _enrollmentAssignmentRepo.GetQuery()
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

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return Ok(new AssignmentResetEnrollmentsResponseDto
            {
                Success = true,
                Message = $"Successfully reset {resetCount} enrollment(s).",
                ResetCount = resetCount,
            });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("validate-before-assign")]
        public async Task<IActionResult> ValidateBeforeAssign([FromBody] BulkAssignDto dto)
        {
            var accessibleCourses = await _assignmentService.GetAccessibleCoursesAsync(dto.CourseIds, _currentUser.DivisionId);
            if (_assignmentService.HasUnauthorizedCourses(dto.CourseIds, accessibleCourses))
            {
                return Forbid();
            }

            var result = await _dashboardService.ValidateBeforeAssignAsync(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new ValidateBeforeAssignResponseDto
            {
                Success = result.Success,
                InProgressConflicts = result.InProgressConflicts,
                CompletedConflicts = result.CompletedConflicts,
                ResolvedCount = result.ResolvedCount,
            });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{id}/extend-due-date")]
        public async Task<IActionResult> ExtendDueDate(int id, [FromBody] ExtendDueDateDto dto)
        {
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null) return NotFound(new { message = "Assignment not found" });

            if (mainRule.StartDate.HasValue && dto.NewDueDate <= mainRule.StartDate.Value)
                return BadRequest(new { message = "Due date must be after the start date." });

            var allRules = await _assignmentBatchService.LoadBatchAsync(mainRule);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
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

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return Ok(new AssignmentExtendDueDateResponseDto
            {
                Success = true,
                Message = "Due date extended successfully.",
                NewDueDate = dto.NewDueDate,
            });
        }

        [Authorize(Policy = "AdminOnly")]
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

            var accessibleCourses = await _assignmentService.GetAccessibleCoursesAsync(requestedCourseIds, _currentUser.DivisionId);
            if (_assignmentService.HasUnauthorizedCourses(requestedCourseIds, accessibleCourses))
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
                return Ok(new AssignmentMutationResponseDto
                {
                    Success = true,
                    Message = "No new courses were added.",
                    AddedCount = 0,
                });
            }

            var learnerCodes = await _assignmentService.GetBatchLearnerCodesAsync(batchRules.Select(rule => rule.Id).ToList(), batchRules);
            var employeeCodesText = string.Join(",", learnerCodes);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
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
                    LearnerGroupId = mainRule.LearnerGroupId,
                    DivisionId = mainRule.DivisionId
                }).ToList();

                await _unitOfWork.AddRangeAsync(newRules);
                await _unitOfWork.SaveChangesAsync();

                if (learnerCodes.Count > 0)
                {
                    var assignmentRuleIdsByCourseId = newRules
                        .Where(rule => rule.CourseId.HasValue)
                        .ToDictionary(rule => rule.CourseId!.Value, rule => rule.Id);

                    await _courseAssignmentService.AssignCoursesToEmployees(
                        assignmentRuleIdsByCourseId,
                        learnerCodes,
                        mainRule.StartDate,
                        mainRule.DueDate,
                        forceReset: false);
                }

                await transaction.CommitAsync();
                return Ok(new AssignmentMutationResponseDto
                {
                    Success = true,
                    Message = "Courses added successfully.",
                    AddedCount = newRules.Count,
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [Authorize(Policy = "AdminOnly")]
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

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
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

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                var remainingRules = batchRules.Count(rule => rule.Id != targetRule.Id);
                return Ok(new AssignmentRemoveCourseResponseDto
                {
                    Success = true,
                    Message = "Assigned course removed successfully.",
                    AssignmentDeleted = remainingRules == 0,
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{id}/learners")]
        public async Task<IActionResult> AddLearners(int id, [FromBody] ManageAssignmentLearnersDto dto)
        {
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null) return NotFound(new { message = "Assignment not found" });

            if (!IsAccessibleToCurrentDivision(mainRule.DivisionId))
            {
                return Forbid();
            }

            var requestedLearnerCodes = _assignmentService.NormalizeLearnerCodes(dto.EmployeeCodes);
            if (requestedLearnerCodes.Count == 0)
            {
                return BadRequest(new { message = "At least one learner is required." });
            }

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var ruleIds = batchRules.Select(rule => rule.Id).ToList();
            var currentLearnerCodes = await _assignmentService.GetBatchLearnerCodesAsync(ruleIds, batchRules);
            var newLearnerCodes = requestedLearnerCodes
                .Except(currentLearnerCodes, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (newLearnerCodes.Count == 0)
            {
                return Ok(new AssignmentMutationResponseDto
                {
                    Success = true,
                    Message = "No new learners were added.",
                    AddedCount = 0,
                });
            }

            var updatedLearnerCodes = currentLearnerCodes
                .Union(newLearnerCodes, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var rule in batchRules)
                {
                    rule.EmployeeCodes = string.Join(",", updatedLearnerCodes);
                }

                var assignmentRuleIdsByCourseId = batchRules
                    .Where(rule => rule.CourseId.HasValue)
                    .ToDictionary(rule => rule.CourseId!.Value, rule => rule.Id);

                if (assignmentRuleIdsByCourseId.Count > 0)
                {
                    await _courseAssignmentService.AssignCoursesToEmployees(
                        assignmentRuleIdsByCourseId,
                        newLearnerCodes,
                        mainRule.StartDate,
                        mainRule.DueDate,
                        forceReset: false);
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new AssignmentMutationResponseDto
                {
                    Success = true,
                    Message = "Learners added successfully.",
                    AddedCount = newLearnerCodes.Count,
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id}/learners/{learnerCode}")]
        public async Task<IActionResult> RemoveLearner(int id, string learnerCode)
        {
            var mainRule = await _repo.GetByIdAsync(id);
            if (mainRule == null) return NotFound(new { message = "Assignment not found" });

            if (!IsAccessibleToCurrentDivision(mainRule.DivisionId))
            {
                return Forbid();
            }

            var normalizedLearnerCode = learnerCode?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedLearnerCode))
            {
                return BadRequest(new { message = "Learner code is required." });
            }

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var ruleIds = batchRules.Select(rule => rule.Id).ToList();
            var currentLearnerCodes = await _assignmentService.GetBatchLearnerCodesAsync(ruleIds, batchRules);
            if (!currentLearnerCodes.Contains(normalizedLearnerCode, StringComparer.OrdinalIgnoreCase))
            {
                return NotFound(new { message = "Learner is not assigned to this assignment." });
            }

            var remainingLearnerCodes = currentLearnerCodes
                .Where(code => !string.Equals(code, normalizedLearnerCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var links = await _enrollmentAssignmentRepo.GetAsync(
                    link => ruleIds.Contains(link.AssignmentId)
                        && link.Enrollment != null
                        && link.Enrollment.LearnerCode == normalizedLearnerCode,
                    includeProperties: "Enrollment");

                foreach (var link in links)
                {
                    link.IsDeleted = true;
                    link.DeletedAt = _dateTime.Now;
                }

                var employeeCodesText = string.Join(",", remainingLearnerCodes);
                foreach (var rule in batchRules)
                {
                    rule.EmployeeCodes = employeeCodesText;
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new AssignmentActionResponseDto
                {
                    Success = true,
                    Message = "Learner removed successfully.",
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("lookup-courses")]
        public async Task<IActionResult> GetLookupCourses(DataSourceLoadOptions loadOptions)
        {
            var courses = await _assignmentService.GetAccessibleCoursesAsync([], _currentUser.DivisionId, includeCourseType: true);

            var result = courses.Select(c => new LookupCourseDto
            {
                Id           = c.Id,
                Code         = c.Code,
                Title        = c.Title,
                CategoryId   = c.CategoryId,
                DivisionId   = c.Category?.DivisionId,
                CourseTypeId = c.CourseTypeId,
                CourseTypeName = c.CourseType?.Name
            }).AsQueryable();

            return Ok(DataSourceLoader.Load(result, loadOptions));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("group/{groupId}/history")]
        public async Task<IActionResult> GetGroupHistory(int groupId)
        {
            var history = await _dashboardService.GetGroupHistoryAsync(groupId);
            return Ok(new AssignmentGroupHistoryResponseDto
            {
                Success = true,
                Data = history,
            });
        }

        private bool IsAccessibleToCurrentDivision(int? divisionId)
        {
            return !_currentUser.DivisionId.HasValue || divisionId == _currentUser.DivisionId.Value;
        }

    }
}