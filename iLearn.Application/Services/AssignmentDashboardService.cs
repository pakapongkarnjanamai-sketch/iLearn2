using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;

namespace iLearn.Application.Services
{
    public class AssignmentDashboardService : IAssignmentDashboardService
    {
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IAssignmentBatchService _assignmentBatchService;
        private readonly ILearnerApiService _learnerApiService;
        private readonly ILearnerGroupService _learnerGroupService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;

        public AssignmentDashboardService(
            IGenericRepository<Assignment> assignmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Course> courseRepo,
            IAssignmentBatchService assignmentBatchService,
            ILearnerApiService learnerApiService,
            ILearnerGroupService learnerGroupService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            IUnitOfWork unitOfWork)
        {
            _assignmentRepo = assignmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _courseRepo = courseRepo;
            _assignmentBatchService = assignmentBatchService;
            _learnerApiService = learnerApiService;
            _learnerGroupService = learnerGroupService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _unitOfWork = unitOfWork;
        }

        public async Task<AssignmentDashboardDto?> GetDashboardAsync(int assignmentId)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId);
            if (mainRule == null) return null;

            if (!IsAccessibleToCurrentDivision(mainRule.DivisionId))
            {
                return null;
            }

            var allRules = await _assignmentBatchService.LoadBatchAsync(mainRule, includeProperties: "Course", ignoreQueryFilters: true);
            var activeRules = allRules.Where(r => !r.IsDeleted).ToList();
            if (activeRules.Count == 0) return null;

            var ruleIds = activeRules.Select(r => r.Id).ToList();

            var links = await _enrollmentAssignmentRepo.GetAsync(
                ea => ruleIds.Contains(ea.AssignmentId),
                includeProperties: "Enrollment,Enrollment.Course"
            );

            var enrollments = links
                .Where(ea => ea.Enrollment != null)
                .Select(ea => new EnrollmentProjection
                {
                    LearnerCode   = ea.Enrollment!.LearnerCode,
                    AssignmentId  = ea.AssignmentId,
                    Progress      = ea.SnapshotCompleted ? ea.SnapshotProgress : ea.Enrollment.Progress,
                    IsCompleted   = ea.SnapshotCompleted || ea.Enrollment.IsCompleted,
                    CompletedDate = ea.SnapshotCompleted ? ea.SnapshotCompletedDate : ea.Enrollment.CompletedDate,
                    StartDate     = ea.StartDate,
                    DueDate       = ea.DueDate,
                    Course        = ea.Enrollment.Course
                }).ToList();

            var learnerEnrollments = enrollments
                .GroupBy(e => e.LearnerCode)
                .Select(g => new
                {
                    LearnerCode  = g.Key,
                    AllCompleted = g.All(e => e.IsCompleted),
                    AnyStarted   = g.Any(e => e.IsCompleted || e.Progress > 0)
                }).ToList();

            int uniqueLearnersCount  = learnerEnrollments.Count;
            int completedCount       = learnerEnrollments.Count(s => s.AllCompleted);
            int inProgressCount      = learnerEnrollments.Count(s => !s.AllCompleted && s.AnyStarted);
            int notStartedCount      = learnerEnrollments.Count(s => !s.AllCompleted && !s.AnyStarted);
            int totalEnrollments     = enrollments.Count;
            int completedEnrollments = enrollments.Count(e => e.IsCompleted);
            double completionRate    = totalEnrollments == 0
                ? 0 : Math.Round((double)completedEnrollments / totalEnrollments * 100);

            var courseSummaries = activeRules.Select(r => new CourseSummaryDto
            {
                AssignmentRuleId  = r.Id,
                CourseCode        = r.Course?.Code ?? "-",
                CourseTitle       = r.Course?.Title ?? "Unknown Course",
                CompletedLearners = enrollments.Count(e => e.AssignmentId == r.Id && e.IsCompleted),
                TotalLearners     = enrollments.Count(e => e.AssignmentId == r.Id),
                IsCourseDeleted   = r.Course?.IsDeleted ?? false
            }).ToList();

            var uniqueCodes  = enrollments.Select(e => e.LearnerCode).Distinct().ToList();
            var learnerNames = await LookupLearnerNamesAsync(uniqueCodes);

            var ruleCourseMap = activeRules.ToDictionary(r => r.Id, r => r.Course);

            var learnersProgress = enrollments.Select(e =>
            {
                var course = e.Course ?? (ruleCourseMap.TryGetValue(e.AssignmentId, out var c) ? c : null);
                var status = AssignmentStatusKeys.GetLearnerStatus(e.IsCompleted, e.Progress);
                return new LearnerProgressDto
                {
                    LearnerCode      = e.LearnerCode,
                    LearnerName      = learnerNames.GetValueOrDefault(e.LearnerCode, e.LearnerCode),
                    AssignmentRuleId = e.AssignmentId,
                    CourseCode       = course?.Code ?? "-",
                    CourseTitle      = course?.Title ?? "Unknown Course",
                    Progress         = e.Progress,
                    IsCompleted      = e.IsCompleted,
                    Status           = status,
                    CompletedDate    = e.CompletedDate,
                    StartDate        = e.StartDate,
                    DueDate          = e.DueDate
                };
            }).ToList();

            bool hasDeletedCourse = courseSummaries.Any(c => c.IsCourseDeleted);

            var createdByName = await LookupCreatedByNameAsync(mainRule.CreatedBy);

            return new AssignmentDashboardDto
            {
                AssignmentNo     = mainRule.AssignmentNo ?? string.Empty,
                Description      = mainRule.Description ?? string.Empty,
                CreatedBy        = mainRule.CreatedBy,
                CreatedByName    = createdByName,
                StartDate        = mainRule.StartDate,
                DueDate          = mainRule.DueDate,
                TotalEmployees   = uniqueLearnersCount,
                TotalCourses     = activeRules.Count,
                CompletionRate   = completionRate,
                HasDeletedCourse = hasDeletedCourse,
                ChartData        = new DashboardChartDto
                {
                    Completed  = completedCount,
                    InProgress = inProgressCount,
                    NotStarted = notStartedCount
                },
                Courses  = courseSummaries,
                Learners = learnersProgress
            };
        }

        public async Task<ValidateBeforeAssignResult> ValidateBeforeAssignAsync(BulkAssignDto dto)
        {
            if (dto.GroupId.HasValue && dto.EmployeeCodes.Count == 0)
            {
                dto.EmployeeCodes = await _learnerGroupService.GetLearnerCodesAsync(dto.GroupId.Value);
                if (dto.EmployeeCodes.Count == 0)
                    return new ValidateBeforeAssignResult { Success = false, Message = "The selected group has no members." };
            }

            var existingLinks = await _enrollmentAssignmentRepo.GetAsync(
                ea => dto.CourseIds.Contains(ea.Assignment != null ? (ea.Assignment.CourseId ?? 0) : 0),
                includeProperties: "Enrollment,Assignment,Assignment.Course"
            );

            var inProgressConflicts = existingLinks
                .Where(ea => ea.Enrollment != null
                          && dto.EmployeeCodes.Contains(ea.Enrollment.LearnerCode)
                          && !(ea.SnapshotCompleted || ea.Enrollment.IsCompleted)
                          && (ea.SnapshotProgress > 0 || ea.Enrollment.Progress > 0))
                .Select(ea => new ConflictDto
                {
                    LearnerCode = ea.Enrollment!.LearnerCode,
                    CourseTitle  = ea.Assignment?.Course?.Title ?? "Unknown",
                    DueDate      = ea.DueDate
                }).ToList();

            var completedConflicts = existingLinks
                .Where(ea => ea.Enrollment != null
                          && dto.EmployeeCodes.Contains(ea.Enrollment.LearnerCode)
                          && (ea.SnapshotCompleted || ea.Enrollment.IsCompleted))
                .Select(ea => new CompletedConflictDto
                {
                    LearnerCode   = ea.Enrollment!.LearnerCode,
                    CourseTitle    = ea.Assignment?.Course?.Title ?? "Unknown",
                    CompletedDate = ea.SnapshotCompleted ? ea.SnapshotCompletedDate : ea.Enrollment.CompletedDate
                }).ToList();

            return new ValidateBeforeAssignResult
            {
                Success             = true,
                InProgressConflicts = inProgressConflicts,
                CompletedConflicts  = completedConflicts,
                ResolvedCount       = dto.EmployeeCodes.Count
            };
        }

        public async Task<PagedResult<AssignmentHistoryDto>> GetAssignmentHistoryPagedAsync(PaginationParams p)
        {
            var divisionId = _currentUser.DivisionId;
            var assignments = await _assignmentRepo.GetAsync(
                  filter: a => !divisionId.HasValue || a.DivisionId == divisionId.Value,
                  includeProperties: "Course",
                  ignoreQueryFilters: false
              );

            var allCoursesIncludeDeleted = await GetCoursesIncludingDeletedAsync(assignments);
            var courseMap = allCoursesIncludeDeleted.ToDictionary(c => c.Id);

            var links = await _enrollmentAssignmentRepo.GetAsync(
                filter: null,
                includeProperties: "Enrollment"
            );

            var currentDate = _dateTime.Now;

            var grouped = assignments
                .GroupBy(r => _assignmentBatchService.GetBatchKey(r))
                .Select(g => MapToHistoryDto(g, links, currentDate, courseMap))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                string search = p.Search.Trim();
                grouped = grouped.Where(h =>
                    (h.AssignmentNo != null && h.AssignmentNo.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (h.Description  != null && h.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (h.CourseNames  != null && h.CourseNames.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(p.Status))
            {
                grouped = grouped.Where(h =>
                    string.Equals(h.Status, p.Status, StringComparison.OrdinalIgnoreCase));
            }

            var ordered = grouped.OrderByDescending(x => x.AssignmentNo).ToList();
            int totalCount = ordered.Count;

            var paged = ordered
                .Skip((p.Page - 1) * p.PageSize)
                .Take(p.PageSize)
                .ToList();

            return new PagedResult<AssignmentHistoryDto>
            {
                Data       = paged,
                TotalCount = totalCount,
                Page       = p.Page,
                PageSize   = p.PageSize
            };
        }

        public static string CalculateStatus(
            bool hasEnrollments,
            bool allCompleted,
            DateTime? startDate,
            DateTime? dueDate,
            DateTime currentDate)
        {
            return AssignmentStatusKeys.GetBatchStatus(hasEnrollments, allCompleted, startDate, dueDate, currentDate);
        }

        private static AssignmentHistoryDto MapToHistoryDto(
            IGrouping<string?, Assignment> g,
            IReadOnlyList<EnrollmentAssignment> allLinks,
            DateTime currentDate,
            Dictionary<int, Course> courseMap)
        {
            var first         = g.First();
            var assignmentIds = g.Select(a => a.Id).ToList();

            var relatedLinks = allLinks
                .Where(ea => assignmentIds.Contains(ea.AssignmentId) && ea.Enrollment != null)
                .ToList();

            bool allCompleted = relatedLinks.Count > 0
                && relatedLinks.All(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);

            string status = CalculateStatus(
                relatedLinks.Count > 0,
                allCompleted,
                first.StartDate,
                first.DueDate,
                currentDate);

            var courseEntries = g
                .Select(a => a.CourseId.HasValue && courseMap.TryGetValue(a.CourseId.Value, out var c)
                    ? c
                    : a.Course)
                .Where(c => c != null)
                .DistinctBy(c => c!.Id)
                .ToList();

            var deletedCourses  = courseEntries.Where(c => c!.IsDeleted).ToList();
            var activeCourses   = courseEntries.Where(c => !c!.IsDeleted).ToList();

            var allCourseNameParts = activeCourses
                .Select(c => c!.Title ?? "Unknown Course")
                .Concat(deletedCourses.Select(c => $"{c!.Title ?? "Unknown Course"} [Deleted]"));

            return new AssignmentHistoryDto
            {
                Id           = first.Id,
                AssignmentNo = g.Key ?? string.Empty,
                Description  = first.Description ?? string.Empty,
                EmployeeCodes = first.EmployeeCodes ?? string.Empty,
                StartDate    = first.StartDate,
                DueDate      = first.DueDate,
                CourseNames  = string.Join(", ", allCourseNameParts),
                Status       = status,
                CreatedBy    = first.CreatedBy,
                CreatedAt    = first.CreatedAt,
                CourseCount  = g.Select(a => a.CourseId).Distinct().Count(),
                LearnerCount = string.IsNullOrEmpty(first.EmployeeCodes)
                    ? 0
                    : first.EmployeeCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
                CompletedEnrollmentCount = relatedLinks.Count(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted),
                TotalEnrollmentCount     = relatedLinks.Count,
                HasDeletedCourse   = deletedCourses.Count > 0,
                DeletedCourseNames = deletedCourses.Count > 0
                    ? string.Join(", ", deletedCourses.Select(c => c!.Title ?? "Unknown"))
                    : null
            };
        }

        private async Task<string?> LookupCreatedByNameAsync(string? createdByNid)
        {
            if (string.IsNullOrWhiteSpace(createdByNid)) return null;

            try
            {
                var result = await _learnerApiService.GetEmployeesByNidsAsync(new[] { createdByNid });
                if (result.TryGetValue(createdByNid, out var employee))
                {
                    var fullName = employee.FullName;
                    return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<Dictionary<string, string>> LookupLearnerNamesAsync(List<string> codes)
        {
            if (codes.Count == 0) return new Dictionary<string, string>();

            try
            {
                var bulk = await _learnerApiService.GetLearnersByCodesAsync(codes);
                return bulk.ToDictionary(kv => kv.Key, kv => kv.Value.Name ?? kv.Key);
            }
            catch
            {
                var dict = new Dictionary<string, string>();
                foreach (var code in codes)
                {
                    try
                    {
                        var s = await _learnerApiService.GetLearnerByCodeAsync(code);
                        dict[code] = s?.Name ?? code;
                    }
                    catch
                    {
                        dict[code] = code;
                    }
                }
                return dict;
            }
        }

        private sealed class EnrollmentProjection
        {
            public string LearnerCode { get; set; } = string.Empty;
            public int AssignmentId { get; set; }
            public double Progress { get; set; }
            public bool IsCompleted { get; set; }
            public DateTime? CompletedDate { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
            public Course? Course { get; set; }
        }

        public async Task<List<AssignmentGroupHistoryDto>> GetGroupHistoryAsync(int groupId)
        {
            var divisionId = _currentUser.DivisionId;
            var assignments = await _assignmentRepo.GetAsync(
                r => r.LearnerGroupId == groupId &&
                (!divisionId.HasValue || r.DivisionId == divisionId.Value),
                includeProperties: "Course"
            );

            if (!assignments.Any())
                return [];

            var allIds = assignments.Select(a => a.Id).ToList();

            var links = await _enrollmentAssignmentRepo.GetAsync(
                ea => allIds.Contains(ea.AssignmentId),
                includeProperties: "Enrollment"
            );

            var now = _dateTime.Now;

            return assignments
                .GroupBy(r => _assignmentBatchService.GetBatchKey(r))
                .Select(g =>
                {
                    var first   = g.First();
                    var ruleIds = g.Select(a => a.Id).ToList();

                    var relatedLinks = links
                        .Where(ea => ruleIds.Contains(ea.AssignmentId) && ea.Enrollment != null)
                        .ToList();

                    bool allDone = relatedLinks.Any()
                        && relatedLinks.All(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);

                    string status = CalculateStatus(
                        relatedLinks.Any(), allDone, first.StartDate, first.DueDate, now);

                    var done  = relatedLinks.Count(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);
                    var total = relatedLinks.Count;
                    var pct   = total > 0 ? Math.Round((double)done / total * 100) : 0;

                    return new AssignmentGroupHistoryDto
                    {
                        Id                       = first.Id,
                        AssignmentNo             = g.Key,
                        Description              = first.Description,
                        CourseNames              = string.Join(", ", g
                            .Select(c => c.Course != null ? c.Course.Title : "Unknown").Distinct()),
                        CourseCount              = g.Select(a => a.CourseId).Distinct().Count(),
                        StartDate                = first.StartDate,
                        DueDate                  = first.DueDate,
                        Status                   = status,
                        CompletedEnrollmentCount = done,
                        TotalEnrollmentCount     = total,
                        CompletionPct            = pct
                    };
                })
                .OrderByDescending(x => x.AssignmentNo)
                .ToList();
        }

        public async Task ExtendDueDateAsync(int assignmentId, DateTime newDueDate)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId);
            if (mainRule == null)
                throw new KeyNotFoundException("Assignment not found");

            if (!IsAccessibleToCurrentDivision(mainRule.DivisionId))
                throw new UnauthorizedAccessException("Assignment is not accessible in the current division.");

            if (mainRule.StartDate.HasValue && newDueDate <= mainRule.StartDate.Value)
                throw new ArgumentException("Due date must be after the start date.");

            var allRules = await _assignmentBatchService.LoadBatchAsync(mainRule);

            foreach (var rule in allRules)
            {
                rule.DueDate = newDueDate;
            }

            var ruleIds = allRules.Select(r => r.Id).ToList();
            var activeLinks = await _enrollmentAssignmentRepo.GetAsync(
                ea => ruleIds.Contains(ea.AssignmentId),
                includeProperties: "Enrollment"
            );
            foreach (var link in activeLinks.Where(ea => ea.Enrollment != null && !(ea.SnapshotCompleted || ea.Enrollment.IsCompleted)))
            {
                link.DueDate = newDueDate;
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<LookupCourseDto>> GetLookupCoursesAsync()
        {
            var divisionId = _currentUser.DivisionId;
            var courses = await _courseRepo.GetAsync(
                c => c.Status == CourseStatus.Open && (!divisionId.HasValue || c.Category != null && c.Category.DivisionId == divisionId.Value),
                includeProperties: "Category,CourseType");

            return courses.Select(c => new LookupCourseDto
            {
                Id            = c.Id,
                Code          = c.Code,
                Title         = c.Title,
                CategoryId    = c.CategoryId,
                DivisionId    = c.Category?.DivisionId,
                CourseTypeId  = c.CourseTypeId,
                CourseTypeName = c.CourseType?.Name
            }).ToList();
        }

        private bool IsAccessibleToCurrentDivision(int? divisionId)
        {
            return !_currentUser.DivisionId.HasValue || divisionId == _currentUser.DivisionId.Value;
        }

        private async Task<IReadOnlyList<Course>> GetCoursesIncludingDeletedAsync(IEnumerable<Assignment> assignments)
        {
            var allCourseIds = assignments
                .Select(a => a.CourseId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (allCourseIds.Count == 0)
                return [];

            return await _courseRepo.GetAsync(
                filter: c => allCourseIds.Contains(c.Id),
                ignoreQueryFilters: true);
        }
    }
}
