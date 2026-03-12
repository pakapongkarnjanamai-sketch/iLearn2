using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;

namespace iLearn.Application.Services
{
    public class AssignmentDashboardService : IAssignmentDashboardService
    {
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IStudentApiService _studentApiService;
        private readonly IStudentGroupService _studentGroupService;
        private readonly ICurrentUserService _currentUser;
        public AssignmentDashboardService(
            IGenericRepository<Assignment> assignmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Course> courseRepo,
            IStudentApiService studentApiService,
            IStudentGroupService studentGroupService,
            ICurrentUserService currentUser)
        {
            _assignmentRepo = assignmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _courseRepo = courseRepo;
            _studentApiService = studentApiService;
            _studentGroupService = studentGroupService;
            _currentUser = currentUser;
        }

        // ── Dashboard ─────────────────────────────────────────────────────────────
        public async Task<AssignmentDashboardDto?> GetDashboardAsync(int assignmentId)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId);
            if (mainRule == null) return null;

            // 💡 เพิ่มการตรวจสอบ Data Isolation: ถ้าไม่ใช่ Division ตัวเอง ให้คืนค่า null (ไม่ให้ดู)
            if (_currentUser.DivisionId.HasValue && mainRule.DivisionId != _currentUser.DivisionId.Value)
            {
                return null;
            }

            // ✅ ignoreQueryFilters: true — ดึง Assignment ที่มี Course ถูก soft-delete ได้ด้วย
            var allRules = await _assignmentRepo.GetAsync(
                r => r.AssignmentNo == mainRule.AssignmentNo,
                includeProperties: "Course",
                ignoreQueryFilters: true
            );
            var ruleIds = allRules.Select(r => r.Id).ToList();

            var links = await _enrollmentAssignmentRepo.GetAsync(
                ea => ruleIds.Contains(ea.AssignmentId),
                includeProperties: "Enrollment,Enrollment.Course"
            );

            var enrollments = links
                .Where(ea => ea.Enrollment != null)
                .Select(ea => new EnrollmentProjection
                {
                    StudentCode   = ea.Enrollment!.StudentCode,
                    AssignmentId  = ea.AssignmentId,
                    Progress      = ea.SnapshotCompleted ? ea.SnapshotProgress : ea.Enrollment.Progress,
                    IsCompleted   = ea.SnapshotCompleted || ea.Enrollment.IsCompleted,
                    CompletedDate = ea.SnapshotCompleted ? ea.SnapshotCompletedDate : ea.Enrollment.CompletedDate,
                    StartDate     = ea.StartDate,
                    DueDate       = ea.DueDate,
                    Course        = ea.Enrollment.Course
                }).ToList();

            var studentEnrollments = enrollments
                .GroupBy(e => e.StudentCode)
                .Select(g => new
                {
                    StudentCode  = g.Key,
                    AllCompleted = g.All(e => e.IsCompleted),
                    AnyStarted   = g.Any(e => e.IsCompleted || e.Progress > 0)
                }).ToList();

            int uniqueStudentsCount  = studentEnrollments.Count;
            int completedCount       = studentEnrollments.Count(s => s.AllCompleted);
            int inProgressCount      = studentEnrollments.Count(s => !s.AllCompleted && s.AnyStarted);
            int notStartedCount      = studentEnrollments.Count(s => !s.AllCompleted && !s.AnyStarted);
            int totalEnrollments     = enrollments.Count;
            int completedEnrollments = enrollments.Count(e => e.IsCompleted);
            double completionRate    = totalEnrollments == 0
                ? 0 : Math.Round((double)completedEnrollments / totalEnrollments * 100);

            // ✅ ตรวจสอบ course ที่ถูก soft-delete
            var courseSummaries = allRules.Select(r => new CourseSummaryDto
            {
                AssignmentRuleId  = r.Id,
                CourseCode        = r.Course?.Code ?? "-",
                CourseTitle       = r.Course?.Title ?? "Unknown Course",
                CompletedStudents = enrollments.Count(e => e.AssignmentId == r.Id && e.IsCompleted),
                TotalStudents     = enrollments.Count(e => e.AssignmentId == r.Id),
                IsCourseDeleted   = r.Course?.IsDeleted ?? false   // ✅ บอก UI ว่า course นี้ถูกลบแล้ว
            }).ToList();

            // Bulk-lookup student names
            var uniqueCodes  = enrollments.Select(e => e.StudentCode).Distinct().ToList();
            var studentNames = await LookupStudentNamesAsync(uniqueCodes);

            var ruleCourseMap = allRules.ToDictionary(r => r.Id, r => r.Course);

            var studentsProgress = enrollments.Select(e =>
            {
                var course = e.Course ?? (ruleCourseMap.TryGetValue(e.AssignmentId, out var c) ? c : null);
                var status = e.IsCompleted ? "Completed" : e.Progress > 0 ? "In Progress" : "Pending";
                return new StudentProgressDto
                {
                    StudentCode      = e.StudentCode,
                    StudentName      = studentNames.GetValueOrDefault(e.StudentCode, e.StudentCode),
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

            return new AssignmentDashboardDto
            {
                AssignmentNo     = mainRule.AssignmentNo ?? string.Empty,
                Description      = mainRule.Description ?? string.Empty,
                CreatedBy        = mainRule.CreatedBy,
                StartDate        = mainRule.StartDate,
                DueDate          = mainRule.DueDate,
                TotalEmployees   = uniqueStudentsCount,
                TotalCourses     = allRules.Count,
                CompletionRate   = completionRate,
                HasDeletedCourse = hasDeletedCourse,   // ✅
                ChartData        = new DashboardChartDto
                {
                    Completed  = completedCount,
                    InProgress = inProgressCount,
                    NotStarted = notStartedCount
                },
                Courses  = courseSummaries,
                Students = studentsProgress
            };
        }

        // ── Validate before assign ───────────────────────────────────────────────
        public async Task<ValidateBeforeAssignResult> ValidateBeforeAssignAsync(BulkAssignDto dto)
        {
            if (dto.GroupId.HasValue && dto.EmployeeCodes.Count == 0)
            {
                dto.EmployeeCodes = await _studentGroupService.GetStudentCodesAsync(dto.GroupId.Value);
                if (dto.EmployeeCodes.Count == 0)
                    return new ValidateBeforeAssignResult { Success = false, Message = "The selected group has no members." };
            }

            var existingLinks = await _enrollmentAssignmentRepo.GetAsync(
                ea => dto.CourseIds.Contains(ea.Assignment != null ? (ea.Assignment.CourseId ?? 0) : 0),
                includeProperties: "Enrollment,Assignment,Assignment.Course"
            );

            var inProgressConflicts = existingLinks
                .Where(ea => ea.Enrollment != null
                          && dto.EmployeeCodes.Contains(ea.Enrollment.StudentCode)
                          && !(ea.SnapshotCompleted || ea.Enrollment.IsCompleted)
                          && (ea.SnapshotProgress > 0 || ea.Enrollment.Progress > 0))
                .Select(ea => new ConflictDto
                {
                    StudentCode = ea.Enrollment!.StudentCode,
                    CourseTitle  = ea.Assignment?.Course?.Title ?? "Unknown",
                    DueDate      = ea.DueDate
                }).ToList();

            var completedConflicts = existingLinks
                .Where(ea => ea.Enrollment != null
                          && dto.EmployeeCodes.Contains(ea.Enrollment.StudentCode)
                          && (ea.SnapshotCompleted || ea.Enrollment.IsCompleted))
                .Select(ea => new CompletedConflictDto
                {
                    StudentCode   = ea.Enrollment!.StudentCode,
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

        // ── Paginated assignment history ─────────────────────────────────────────
        public async Task<PagedResult<AssignmentHistoryDto>> GetAssignmentHistoryPagedAsync(PaginationParams p)
        {
            // ✅ ignoreQueryFilters: true — ดึง Assignment ที่ Course ถูก soft-delete ได้ด้วย
            // Global filter ของ Assignment entity เองยังทำงานปกติ (IsDeleted=false)
            // แต่ navigation property "Course" จะไม่ถูก filter ออกอีกต่อไป
            var assignments = await _assignmentRepo.GetAsync(
                  // 💡 เพิ่ม Filter ตรงนี้เพื่อดึงเฉพาะงานของ Division ตัวเอง
                  filter: a => !_currentUser.DivisionId.HasValue || a.DivisionId == _currentUser.DivisionId.Value,
                  includeProperties: "Course",
                  ignoreQueryFilters: false   // Assignment ที่ถูก soft-delete ไม่ควรโผล่
              );

            // ✅ ดึง Course ที่ถูก soft-delete แยกต่างหากเพื่อ lookup ชื่อ
            var allCourseIds = assignments
                .Select(a => a.CourseId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var allCoursesIncludeDeleted = await _courseRepo.GetAsync(
                filter: c => allCourseIds.Contains(c.Id),
                ignoreQueryFilters: true    // ✅ ดึงแม้ course จะถูกลบ
            );
            var courseMap = allCoursesIncludeDeleted.ToDictionary(c => c.Id);

            var links = await _enrollmentAssignmentRepo.GetAsync(
                filter: null,
                includeProperties: "Enrollment"
            );

            var currentDate = DateTime.UtcNow.AddHours(7);

            var grouped = assignments
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo))
                .GroupBy(r => r.AssignmentNo)
                .Select(g => MapToHistoryDto(g, links, currentDate, courseMap))
                .AsQueryable();

            // Apply filters
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

        // ── Status calculation helper (extracted for testability) ────────────────
        public static string CalculateStatus(
            bool hasEnrollments,
            bool allCompleted,
            DateTime? startDate,
            DateTime? dueDate,
            DateTime currentDate)
        {
            if (hasEnrollments && allCompleted) return "Completed";
            if (startDate.HasValue && startDate.Value > currentDate) return "Upcoming";
            if (dueDate.HasValue && dueDate.Value < currentDate) return "Expired";
            return "InProgress";
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private static AssignmentHistoryDto MapToHistoryDto(
            IGrouping<string?, Assignment> g,
            IReadOnlyList<EnrollmentAssignment> allLinks,
            DateTime currentDate,
            Dictionary<int, Course> courseMap)   // ✅ รับ courseMap ที่รวม deleted courses
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

            // ✅ Resolve course names รวม deleted course โดยใช้ courseMap
            var courseEntries = g
                .Select(a => a.CourseId.HasValue && courseMap.TryGetValue(a.CourseId.Value, out var c)
                    ? c
                    : a.Course)
                .Where(c => c != null)
                .DistinctBy(c => c!.Id)
                .ToList();

            var deletedCourses  = courseEntries.Where(c => c!.IsDeleted).ToList();
            var activeCourses   = courseEntries.Where(c => !c!.IsDeleted).ToList();

            // แสดงชื่อ course ที่ active ก่อน ตามด้วย deleted (ใส่ suffix เพื่อแยกแยะ)
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
                StudentCount = string.IsNullOrEmpty(first.EmployeeCodes)
                    ? 0
                    : first.EmployeeCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
                CompletedEnrollmentCount = relatedLinks.Count(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted),
                TotalEnrollmentCount     = relatedLinks.Count,
                // ✅ Soft-delete awareness fields
                HasDeletedCourse   = deletedCourses.Count > 0,
                DeletedCourseNames = deletedCourses.Count > 0
                    ? string.Join(", ", deletedCourses.Select(c => c!.Title ?? "Unknown"))
                    : null
            };
        }

        private async Task<Dictionary<string, string>> LookupStudentNamesAsync(List<string> codes)
        {
            if (codes.Count == 0) return new Dictionary<string, string>();

            try
            {
                var bulk = await _studentApiService.GetStudentsByCodesAsync(codes);
                return bulk.ToDictionary(kv => kv.Key, kv => kv.Value.Name ?? kv.Key);
            }
            catch
            {
                // Fallback: try one by one
                var dict = new Dictionary<string, string>();
                foreach (var code in codes)
                {
                    try
                    {
                        var s = await _studentApiService.GetStudentByCodeAsync(code);
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
            public string StudentCode { get; set; } = string.Empty;
            public int AssignmentId { get; set; }
            public double Progress { get; set; }
            public bool IsCompleted { get; set; }
            public DateTime? CompletedDate { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
            public Course? Course { get; set; }
        }
    }
}
