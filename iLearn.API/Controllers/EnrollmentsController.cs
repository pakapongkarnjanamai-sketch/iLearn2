using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.API.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly ICourseAssignmentService _enrollmentService;
        private readonly IAssignmentDashboardService _assignmentDashboardService;
        private readonly ICurrentUserService _currentUser;
        private readonly IGenericRepository<LearningLog> _logRepo;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IScormService _scormService;
        private readonly IStudentGroupService _studentGroupService;
        private readonly IAssignmentNoGenerator _assignmentNoGen;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILearnerProxyIdentityResolver _learnerProxyIdentityResolver;
        private readonly IScormRuntimeStateService _scormRuntimeStateService;

        public EnrollmentsController(
            IGenericRepository<Enrollment> enrollmentRepo,
            ICourseAssignmentService enrollmentService,
            IAssignmentDashboardService assignmentDashboardService,
            ICurrentUserService currentUser,
            IGenericRepository<LearningLog> logRepo,
            IGenericRepository<CourseVersion> versionRepo,
            IGenericRepository<Course> courseRepo,
            IScormService scormService,
            IStudentGroupService studentGroupService,
            IAssignmentNoGenerator assignmentNoGen,
            IDateTime dateTime,
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            ILearnerProxyIdentityResolver learnerProxyIdentityResolver,
            IScormRuntimeStateService scormRuntimeStateService)
        {
            _enrollmentRepo      = enrollmentRepo;
            _enrollmentService   = enrollmentService;
            _assignmentDashboardService = assignmentDashboardService;
            _currentUser         = currentUser;
            _logRepo             = logRepo;
            _versionRepo         = versionRepo;
            _courseRepo          = courseRepo;
            _scormService        = scormService;
            _studentGroupService = studentGroupService;
            _assignmentNoGen     = assignmentNoGen;
            _dateTime            = dateTime;
            _unitOfWork          = unitOfWork;
            _cache               = cache;
            _learnerProxyIdentityResolver = learnerProxyIdentityResolver;
            _scormRuntimeStateService = scormRuntimeStateService;
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("ResetStatus")]
        public async Task<IActionResult> ResetStatus([FromQuery] int key)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(key);
            if (enrollment == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            // Reset enrollment summary and set ResetAt while preserving history logs.
            enrollment.IsCompleted   = false;
            enrollment.CompletedDate = null;
            enrollment.Progress      = 0;
            enrollment.ResetAt       = _dateTime.Now;
            await _enrollmentRepo.UpdateAsync(enrollment);
            AdminSummaryStatsCache.InvalidateEnrollments(_cache);

            return Ok(new { success = true });
        }

        [Authorize(Policy = "DomainUser")]
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            if (!TryGetTrustedLearnerStudentCode(out var studentCode, out var errorResult))
            {
                return errorResult;
            }

            var currentDate = _dateTime.Now;
            var oneMonthAgo = currentDate.AddMonths(-1);

            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => !e.IsDeleted && e.StudentCode == studentCode && e.Course != null,
                includeProperties: "Course.CourseType,AssignmentLinks.Assignment",
                ignoreQueryFilters: true
            );

            var filtered = enrollments.Where(e =>
            {
                var schedule = GetEffectiveSchedule(e);
                if (!schedule.ShouldBeVisible)
                {
                    return false;
                }

                if (e.IsCompleted)
                {
                    return e.CompletedDate.HasValue && e.CompletedDate >= oneMonthAgo;
                }

                bool startOk = !schedule.StartDate.HasValue || schedule.StartDate <= currentDate;
                bool dueOk   = !schedule.DueDate.HasValue || schedule.DueDate >= currentDate;
                return startOk && dueOk;
            }).ToList();

            var dtos = filtered
                .OrderBy(e => e.IsCompleted)
                .ThenBy(e => GetEffectiveSchedule(e).DueDate)
                .Select(e =>
                {
                    var dto = e.ToDto();
                    var schedule = GetEffectiveSchedule(e);
                    return new LearnerEnrollmentDto
                    {
                        Id = dto.Id,
                        CourseId = dto.CourseId,
                        CourseCode = dto.CourseCode,
                        CourseTitle = dto.CourseTitle,
                        EnrolledCourseVersion = dto.EnrolledCourseVersion,
                        IsCompleted = dto.IsCompleted,
                        StartDate = schedule.StartDate,
                        DueDate = schedule.DisplayDueDate,
                        CompletedDate = dto.CompletedDate,
                        Progress = dto.Progress,
                        CourseTypeName = dto.CourseTypeName
                    };
                })
                .ToList();

            return Ok(new ApiResponse<IEnumerable<LearnerEnrollmentDto>>
            {
                Success = true,
                Data    = dtos
            });
        }

        [Authorize(Policy = "DomainUser")]
        [HttpGet("player-info/{courseId}")]
        public async Task<IActionResult> GetPlayerInfoByCourse(int courseId)
        {
            if (!TryGetTrustedLearnerStudentCode(out var studentCode, out var errorResult))
            {
                return errorResult;
            }

            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.CourseId == courseId && e.StudentCode == studentCode,
                includeProperties: "Course"
            );
            var enrollment = enrollments.FirstOrDefault();

            CourseVersion? targetVersion = null;
            bool isReadOnly = false;
            bool isCompleted = false;
            List<LearningLog> userLogs = new();
            Dictionary<int, ScormRuntimeStateDto> runtimeStateMap = new();

            if (enrollment != null)
            {
                var targetVersionId = enrollment.EnrolledCourseVersion;
                isCompleted = enrollment.IsCompleted;

                var versions = await _versionRepo.GetAsync(
                    filter: v => v.CourseId == courseId && v.Id == targetVersionId,
                    includeProperties: "CourseResources.Resource,Course"
                );
                targetVersion = versions.FirstOrDefault();

                if (targetVersion != null)
                {
                    userLogs = (await _logRepo.GetAsync(l =>
                        l.StudentCode     == studentCode       &&
                        l.CourseVersionId == targetVersion.Id  &&
                        l.EnrollmentId    == enrollment.Id     &&
                        (enrollment.ResetAt == null || l.CreatedAt >= enrollment.ResetAt)
                    )).ToList();

                    runtimeStateMap = (await _scormRuntimeStateService.GetActiveStatesAsync(enrollment.Id, enrollment.ResetAt))
                        .ToDictionary(state => state.ResourceId);
                }
            }
            else
            {
                isReadOnly = true;

                var activeVersions = await _versionRepo.GetAsync(
                  filter: v => v.CourseId == courseId && v.IsActive,
                  includeProperties: "CourseResources.Resource,Course"
                );
                targetVersion = activeVersions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            }

            if (targetVersion == null)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = "Content not found or Course is not active" });
            }

            var resources = targetVersion.CourseResources
                .OrderBy(cr => cr.Resource.TypeId == 1 ? 0 : 1) // Learn first
                .ThenBy(cr => cr.Resource.Name)
                .Select(cr => {
                    var log = userLogs.FirstOrDefault(l => l.ResourceId == cr.Resource.Id);
                    runtimeStateMap.TryGetValue(cr.Resource.Id, out var runtimeState);
                    bool isDone = log != null && (
                        log.Status.ToLower() == "completed" ||
                        log.Status.ToLower() == "passed"
                    );

                    return new PlayerResourceDto
                    {
                        Id = cr.Resource.Id,
                        Name = cr.Resource.Name,
                        Type = cr.Resource.TypeId == 2 ? "Exam" : "Learn",
                        LaunchUrl = !string.IsNullOrEmpty(cr.Resource.URL) && !string.IsNullOrEmpty(cr.Resource.ResourceHref)
                            ? _scormService.GetScormUrl(cr.Resource.URL, cr.Resource.ResourceHref)
                            : cr.Resource.URL ?? string.Empty,
                        ScormVersion = runtimeState?.ScormVersion ?? ScormRuntimeFieldMap.NormalizeVersion(cr.Resource.SchemaVersion),
                        IsCompleted = isDone,
                        Score = log?.Score,
                        Time = log?.SessionTime,
                        RuntimeState = runtimeState
                    };
                })
                .ToList();

            var dto = new PlayerInfoDto
            {
                CourseVersionId = targetVersion.Id,
                CourseTitle = targetVersion.Course?.Title ?? "Unknown Course",
                IsCompleted = isCompleted,
                IsReadOnly = isReadOnly,
                EnrollmentId = enrollment?.Id,
                Resources = resources
            };

            return Ok(new ApiResponse<PlayerInfoDto> { Success = true, Data = dto });
        }

        private bool TryGetTrustedLearnerStudentCode(out string studentCode, out IActionResult errorResult)
        {
            if (_learnerProxyIdentityResolver.TryResolveStudentCode(HttpContext, out studentCode, out var statusCode, out var errorMessage))
            {
                errorResult = null!;
                return true;
            }

            errorResult = StatusCode(statusCode, new ApiResponse<string>
            {
                Success = false,
                Message = errorMessage
            });

            return false;
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(id);
            if (enrollment == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Not Found" });
            return Ok(new ApiResponse<EnrollmentDto> { Success = true, Data = enrollment.ToDto() });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id}/completion")]
        public async Task<IActionResult> UpdateCompletion(int id, [FromBody] bool isComplete)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(id);
            if (enrollment == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Not Found" });

            enrollment.IsCompleted = isComplete;
            if (isComplete)
            {
                enrollment.CompletedDate = _dateTime.Now;
                enrollment.Progress = 100;
            }
            else
            {
                enrollment.CompletedDate = null;
            }
            await _enrollmentRepo.UpdateAsync(enrollment);
            AdminSummaryStatsCache.InvalidateEnrollments(_cache);
            return Ok(new ApiResponse<EnrollmentDto> { Success = true, Data = enrollment.ToDto() });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("BulkAssign")]
        public async Task<IActionResult> BulkAssign([FromBody] BulkAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            dto.EmployeeCodes = await ResolveEmployeeCodesAsync(dto);

            if (dto.CourseIds == null || !dto.CourseIds.Any() || dto.EmployeeCodes == null || !dto.EmployeeCodes.Any())
            {
                return BadRequest(new { message = "Courses and Employees are required." });
            }

            var accessibleCourses = await GetAccessibleCoursesAsync(dto.CourseIds);
            if (HasUnauthorizedCourses(dto.CourseIds, accessibleCourses))
            {
                return Forbid();
            }

            var validation = await _assignmentDashboardService.ValidateBeforeAssignAsync(dto);
            if (!validation.Success)
            {
                return BadRequest(new { message = validation.Message });
            }

            if (validation.InProgressConflicts.Count > 0 && !dto.ConfirmReassignInProgress)
            {
                return Conflict(CreateConflictResponse(
                    "Confirmation is required before resetting learners with in-progress assignments.",
                    validation));
            }

            if (validation.CompletedConflicts.Count > 0 && !dto.ConfirmReassignCompleted)
            {
                return Conflict(CreateConflictResponse(
                    "Confirmation is required before reassigning learners who already completed the course.",
                    validation));
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                string assignmentNo = await _assignmentNoGen.NextAsync();
                string employeesStr = string.Join(",", dto.EmployeeCodes);

                var rules = dto.CourseIds.Select(courseId => new Assignment
                {
                    AssignmentNo   = assignmentNo,
                    Description    = dto.Description,
                    CourseId       = courseId,
                    EmployeeCodes  = employeesStr,
                    StartDate      = dto.StartDate,
                    DueDate        = dto.DueDate,
                    Division       = dto.Division,
                    StudentGroupId = dto.GroupId,
                    DivisionId     = _currentUser.DivisionId
                }).ToList();

                await _unitOfWork.AddRangeAsync(rules);
                await _unitOfWork.SaveChangesAsync();

                var assignmentRuleIdsByCourseId = rules
                    .Where(rule => rule.CourseId.HasValue)
                    .ToDictionary(rule => rule.CourseId!.Value, rule => rule.Id);

                await _enrollmentService.AssignCoursesToEmployees(
                    assignmentRuleIdsByCourseId,
                    dto.EmployeeCodes,
                    dto.StartDate,
                    dto.DueDate,
                    forceReset: true);

                await transaction.CommitAsync();
                AdminSummaryStatsCache.InvalidateEnrollments(_cache);
                return Ok(new
                {
                    message = "Courses assigned successfully!",
                    assignmentNo,
                    assignmentId = rules.FirstOrDefault()?.Id ?? 0
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static List<EnrollmentAssignment> GetActiveLinks(Enrollment enrollment)
        {
            return enrollment.AssignmentLinks
                .Where(ea => !ea.IsDeleted && ea.Assignment != null && !ea.Assignment.IsDeleted)
                .ToList();
        }

        private static EnrollmentSchedule GetEffectiveSchedule(Enrollment enrollment)
        {
            var activeLinks = GetActiveLinks(enrollment);
            var hadDeletedAssignmentOnly = enrollment.AssignmentLinks.Any() && activeLinks.Count == 0;

            if (hadDeletedAssignmentOnly)
            {
                return new EnrollmentSchedule
                {
                    ShouldBeVisible = false,
                    StartDate = enrollment.StartDate,
                    DueDate = enrollment.DueDate,
                    DisplayDueDate = enrollment.DueDate
                };
            }

            if (activeLinks.Count == 0)
            {
                return new EnrollmentSchedule
                {
                    ShouldBeVisible = true,
                    StartDate = enrollment.StartDate,
                    DueDate = enrollment.DueDate,
                    DisplayDueDate = enrollment.DueDate
                };
            }

            return new EnrollmentSchedule
            {
                ShouldBeVisible = true,
                StartDate = activeLinks.Min(a => a.StartDate),
                DueDate = activeLinks.Max(a => a.DueDate),
                DisplayDueDate = activeLinks.Min(a => a.DueDate)
            };
        }

        private async Task<List<string>> ResolveEmployeeCodesAsync(BulkAssignDto dto)
        {
            if (!dto.GroupId.HasValue || dto.EmployeeCodes.Count > 0)
                return dto.EmployeeCodes;

            return await _studentGroupService.GetStudentCodesAsync(dto.GroupId.Value);
        }

        private async Task<IReadOnlyList<Course>> GetAccessibleCoursesAsync(IEnumerable<int> courseIds)
        {
            var targetCourseIds = courseIds.Distinct().ToList();
            return await _courseRepo.GetAsync(
                c => targetCourseIds.Contains(c.Id)
                    && c.IsActive
                    && (!_currentUser.DivisionId.HasValue || c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value),
                includeProperties: "Category"
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

        private static object CreateConflictResponse(string message, ValidateBeforeAssignResult validation)
        {
            return new
            {
                message,
                inProgressConflicts = validation.InProgressConflicts,
                completedConflicts = validation.CompletedConflicts
            };
        }

        private sealed class EnrollmentSchedule
        {
            public bool ShouldBeVisible { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? DisplayDueDate { get; set; }
        }
    }
}