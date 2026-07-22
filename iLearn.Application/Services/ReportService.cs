using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace iLearn.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<LearningLog> _learningLogRepo;
        private readonly IGenericRepository<LearnerGroupMember> _learnerGroupMemberRepo;
        private readonly IGenericRepository<LearnerGroup> _learnerGroupRepo;
        private readonly ILearnerApiService _learnerApiService;
        private readonly IDateTime _dateTime;

        public ReportService(
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Assignment> assignmentRepo,
            IGenericRepository<Course> courseRepo,
            IGenericRepository<LearningLog> learningLogRepo,
            IGenericRepository<LearnerGroupMember> learnerGroupMemberRepo,
            IGenericRepository<LearnerGroup> learnerGroupRepo,
            ILearnerApiService learnerApiService,
            IDateTime dateTime)
        {
            _enrollmentRepo = enrollmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _assignmentRepo = assignmentRepo;
            _courseRepo = courseRepo;
            _learningLogRepo = learningLogRepo;
            _learnerGroupMemberRepo = learnerGroupMemberRepo;
            _learnerGroupRepo = learnerGroupRepo;
            _learnerApiService = learnerApiService;
            _dateTime = dateTime;
        }

        public async Task<ComplianceReportDto> GetComplianceReportAsync(
            int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default)
        {
            var enrollments = await BuildVisibleEnrollmentRowsQuery(divisionId)
                .ToListAsync(cancellationToken);

            var totalLearners = enrollments.Select(e => e.LearnerCode).Distinct().Count();
            var completedCount = enrollments.Count(e => e.IsCompleted);
            var openCount = enrollments.Count(e => !e.IsCompleted);
            var overdueEnrollments = enrollments
                .Where(e => !e.IsCompleted && e.DueDate.HasValue && e.DueDate.Value < currentDate)
                .ToList();
            var overdueLearners = overdueEnrollments.Select(e => e.LearnerCode).Distinct().Count();
            var total = completedCount + openCount;
            var complianceRate = total > 0 ? (double)completedCount / total * 100 : 0;

            // Get learner info for overdue rows and grouping
            var allLearnerCodes = enrollments.Select(e => e.LearnerCode).Distinct().ToList();
            var learnerMap = allLearnerCodes.Count == 0
                ? new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase)
                : await _learnerApiService.GetLearnersByCodesAsync(allLearnerCodes);

            // Build overdue rows
            var overdueRows = overdueEnrollments
                .Select(e =>
                {
                    learnerMap.TryGetValue(e.LearnerCode, out var learner);
                    return new ComplianceOverdueRow
                    {
                        LearnerCode = e.LearnerCode,
                        LearnerName = learner?.Name,
                        Division = learner?.Division,
                        Department = learner?.Department,
                        CourseCode = e.CourseCode,
                        CourseTitle = e.CourseTitle,
                        AssignmentNo = e.AssignmentNo,
                        DueDate = e.DueDate,
                        DaysOverdue = e.DueDate.HasValue ? (int)(currentDate - e.DueDate.Value).TotalDays : 0,
                        Progress = e.Progress,
                    };
                })
                .OrderByDescending(r => r.DaysOverdue)
                .ToList();

            // Group by division
            var byDivision = enrollments
                .GroupBy(e =>
                {
                    learnerMap.TryGetValue(e.LearnerCode, out var l);
                    return l?.Division ?? "Unknown";
                })
                .Select(g => new ComplianceGroupRow
                {
                    GroupName = g.Key,
                    Learners = g.Select(e => e.LearnerCode).Distinct().Count(),
                    Enrollments = g.Count(),
                    Completed = g.Count(e => e.IsCompleted),
                    Overdue = g.Count(e => !e.IsCompleted && e.DueDate.HasValue && e.DueDate.Value < currentDate),
                    CompletionRate = g.Count() > 0 ? (double)g.Count(e => e.IsCompleted) / g.Count() * 100 : 0,
                })
                .OrderBy(r => r.GroupName)
                .ToList();

            // Group by department
            var byDepartment = enrollments
                .GroupBy(e =>
                {
                    learnerMap.TryGetValue(e.LearnerCode, out var l);
                    return new { Department = l?.Department ?? "Unknown", Division = l?.Division ?? "Unknown" };
                })
                .Select(g => new ComplianceGroupRow
                {
                    GroupName = g.Key.Department,
                    ParentDivision = g.Key.Division,
                    Learners = g.Select(e => e.LearnerCode).Distinct().Count(),
                    Enrollments = g.Count(),
                    Completed = g.Count(e => e.IsCompleted),
                    Overdue = g.Count(e => !e.IsCompleted && e.DueDate.HasValue && e.DueDate.Value < currentDate),
                    CompletionRate = g.Count() > 0 ? (double)g.Count(e => e.IsCompleted) / g.Count() * 100 : 0,
                })
                .OrderBy(r => r.ParentDivision)
                .ThenBy(r => r.GroupName)
                .ToList();

            return new ComplianceReportDto
            {
                GeneratedAt = currentDate,
                TotalLearners = totalLearners,
                OpenEnrollments = openCount,
                CompletedEnrollments = completedCount,
                OverdueEnrollments = overdueEnrollments.Count,
                OverdueLearners = overdueLearners,
                ComplianceRate = complianceRate,
                ByDivision = byDivision,
                ByDepartment = byDepartment,
                OverdueRows = overdueRows,
            };
        }

        public async Task<TranscriptReportDto> GetTranscriptReportAsync(
            string learnerCode, int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default)
        {
            // Get enrollments for this learner, scoped by division
            var enrollments = await BuildVisibleEnrollmentRowsQuery(divisionId, learnerCode)
                .ToListAsync(cancellationToken);

            // Verify learner exists (has enrollments or known by EmployeeHub)
            Dictionary<string, ExternalLearnerDto> learnerMap;
            try
            {
                learnerMap = await _learnerApiService.GetLearnersByCodesAsync(new[] { learnerCode });
            }
            catch
            {
                learnerMap = new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase);
            }

            if (enrollments.Count == 0 && !learnerMap.ContainsKey(learnerCode))
            {
                throw new KeyNotFoundException($"Learner '{learnerCode}' not found.");
            }

            learnerMap.TryGetValue(learnerCode, out var learnerInfo);

            // Get learner groups
            var groupNames = await _learnerGroupMemberRepo.GetQuery()
                .AsNoTracking()
                .Where(m => m.LearnerCode == learnerCode)
                .Join(_learnerGroupRepo.GetQuery().AsNoTracking(),
                    m => m.LearnerGroupId,
                    g => g.Id,
                    (_, g) => g.Name)
                .ToListAsync(cancellationToken);

            // Build transcript rows (dates are effective-schedule dates from the projection)
            var rows = enrollments.Select(e => new TranscriptRow
            {
                EnrollmentId = e.Id,
                CourseCode = e.CourseCode,
                CourseTitle = e.CourseTitle,
                AssignmentNo = e.AssignmentNo,
                Status = AssignmentStatusKeys.GetScheduledLearnerStatus(
                    e.IsCompleted, e.Progress, e.StartDate, e.DueDate, currentDate),
                Progress = e.Progress,
                TotalScore = e.TotalScore,
                TotalTimeSpentSeconds = e.TotalTimeSpentSeconds,
                StartDate = e.StartDate,
                DueDate = e.DueDate,
                CompletedDate = e.CompletedDate,
            }).ToList();

            return new TranscriptReportDto
            {
                GeneratedAt = currentDate,
                LearnerCode = learnerCode,
                LearnerName = learnerInfo?.Name,
                Division = learnerInfo?.Division,
                Department = learnerInfo?.Department,
                LearnerGroups = groupNames,
                TotalCourses = enrollments.Count,
                CompletedCourses = enrollments.Count(e => e.IsCompleted),
                Rows = rows,
            };
        }

        public async Task<CourseSummaryReportDto> GetCourseSummaryReportAsync(
            int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default)
        {
            // 1. Get all courses in catalog (scoped by division if specified)
            var coursesQuery = _courseRepo.GetQuery()
                .AsNoTracking();

            if (divisionId.HasValue)
            {
                coursesQuery = coursesQuery.Where(c => c.Category != null && c.Category.DivisionId == divisionId.Value);
            }

            var courses = await coursesQuery
                .Select(c => new { c.Id, c.Code, c.Title, CategoryName = c.Category != null ? c.Category.Name : null })
                .ToListAsync(cancellationToken);

            if (courses.Count == 0)
            {
                return new CourseSummaryReportDto { GeneratedAt = currentDate };
            }

            var courseIds = courses.Select(c => c.Id).ToList();

            // 2. Get enrollments scoped by division (effective-schedule projection), grouped by course
            var courseGroups = await BuildVisibleEnrollmentRowsQuery(divisionId)
                .Where(e => e.CourseId.HasValue && courseIds.Contains(e.CourseId.Value))
                .GroupBy(e => e.CourseId!.Value)
                .Select(g => new
                {
                    CourseId = g.Key,
                    EnrolledLearners = g.Select(e => e.LearnerCode).Distinct().Count(),
                    CompletedCount = g.Count(e => e.IsCompleted),
                    OverdueCount = g.Count(e => !e.IsCompleted && e.DueDate.HasValue && e.DueDate < currentDate),
                    AvgProgress = g.Average(e => e.Progress),
                    TotalEnrollments = g.Count(),
                    AvgScore = g.Where(e => e.TotalScore > 0).Any()
                        ? (double?)g.Where(e => e.TotalScore > 0).Average(e => e.TotalScore)
                        : null,
                })
                .ToListAsync(cancellationToken);

            var groupMap = courseGroups.ToDictionary(g => g.CourseId);

            // 3. Count assignments per course
            var assignmentCounts = await _assignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(a => a.CourseId.HasValue && courseIds.Contains(a.CourseId.Value))
                .Where(a => !divisionId.HasValue || a.DivisionId == divisionId.Value)
                .GroupBy(a => a.CourseId!.Value)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var assignmentCountMap = assignmentCounts.ToDictionary(a => a.CourseId, a => a.Count);

            // 4. Map all courses into summary rows
            var rows = courses.Select(course =>
            {
                groupMap.TryGetValue(course.Id, out var g);
                assignmentCountMap.TryGetValue(course.Id, out var asgCount);

                return new CourseSummaryRow
                {
                    CourseId = course.Id,
                    Code = course.Code,
                    Title = course.Title,
                    CategoryName = course.CategoryName,
                    AssignmentCount = asgCount,
                    EnrolledLearners = g?.EnrolledLearners ?? 0,
                    CompletedCount = g?.CompletedCount ?? 0,
                    OverdueCount = g?.OverdueCount ?? 0,
                    AvgProgress = g?.AvgProgress ?? 0,
                    CompletionRate = (g != null && g.TotalEnrollments > 0) ? (double)g.CompletedCount / g.TotalEnrollments * 100 : 0,
                    AvgScore = g?.AvgScore,
                };
            }).ToList();

            return new CourseSummaryReportDto
            {
                GeneratedAt = currentDate,
                Rows = rows,
            };
        }


        public async Task<ActivityReportDto> GetActivityReportAsync(
            int months, int? divisionId, CancellationToken cancellationToken = default)
        {
            var now = _dateTime.Now;
            var clampedMonths = Math.Clamp(months, 3, 24);
            var cutoff = new DateTime(now.Year, now.Month, 1).AddMonths(-clampedMonths + 1);

            // Completions: enrollments with CompletedDate in range, scoped by division
            // (same visibility rule as other reports: skip enrollments whose only assignments are deleted)
            var enrollmentQuery = BuildDivisionScopedEnrollmentQuery(divisionId)
                .Where(VisibleEnrollmentPredicate);

            var completions = await enrollmentQuery
                .Where(e => e.CompletedDate.HasValue && e.CompletedDate >= cutoff)
                .GroupBy(e => new { e.CompletedDate!.Value.Year, e.CompletedDate!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync(cancellationToken);

            // New enrollments: CreatedAt in range
            var newEnrollments = await enrollmentQuery
                .Where(e => e.CreatedAt >= cutoff)
                .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync(cancellationToken);

            // Active learners + hours played from LearningLog
            // Scope learning logs through enrollment IDs that are division-scoped
            var scopedEnrollmentIds = divisionId.HasValue
                ? await enrollmentQuery.Select(e => e.Id).ToListAsync(cancellationToken)
                : null;

            var logQuery = _learningLogRepo.GetQuery().AsNoTracking()
                .Where(l => l.CreatedAt >= cutoff);

            if (scopedEnrollmentIds != null)
            {
                logQuery = logQuery.Where(l => scopedEnrollmentIds.Contains(l.EnrollmentId));
            }

            var logStats = await logQuery
                .GroupBy(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    ActiveLearners = g.Select(l => l.LearnerCode).Distinct().Count(),
                    TotalSeconds = g.Sum(l => l.TotalSecondsPlayed),
                })
                .ToListAsync(cancellationToken);

            // Build all months (old→new), filling gaps with zeros
            var monthRows = new List<ActivityMonthRow>();
            for (var d = cutoff; d <= new DateTime(now.Year, now.Month, 1); d = d.AddMonths(1))
            {
                var y = d.Year;
                var m = d.Month;
                var comp = completions.FirstOrDefault(c => c.Year == y && c.Month == m);
                var newE = newEnrollments.FirstOrDefault(c => c.Year == y && c.Month == m);
                var log = logStats.FirstOrDefault(c => c.Year == y && c.Month == m);

                monthRows.Add(new ActivityMonthRow
                {
                    Month = $"{y:D4}-{m:D2}",
                    Completions = comp?.Count ?? 0,
                    ActiveLearners = log?.ActiveLearners ?? 0,
                    NewEnrollments = newE?.Count ?? 0,
                    TotalHoursPlayed = (log?.TotalSeconds ?? 0) / 3600.0,
                });
            }

            return new ActivityReportDto
            {
                GeneratedAt = now,
                Months = monthRows,
            };
        }

        /// <summary>
        /// Slim per-enrollment projection shared by the report queries. StartDate/DueDate are
        /// the EFFECTIVE schedule dates (see BuildVisibleEnrollmentRowsQuery), not the raw
        /// enrollment columns.
        /// </summary>
        private sealed class EnrollmentReportRow
        {
            public int Id { get; init; }
            public string LearnerCode { get; init; } = string.Empty;
            public int? CourseId { get; init; }
            public string? CourseCode { get; init; }
            public string? CourseTitle { get; init; }
            public string? AssignmentNo { get; init; }
            public bool IsCompleted { get; init; }
            public double Progress { get; init; }
            public int TotalScore { get; init; }
            public int TotalTimeSpentSeconds { get; init; }
            public DateTime? StartDate { get; init; }
            public DateTime? DueDate { get; init; }
            public DateTime? CompletedDate { get; init; }
        }

        /// <summary>
        /// Visibility rule shared with the learner side (EnrollmentsController.GetEffectiveSchedule):
        /// an enrollment whose only assignment links point to soft-deleted assignments is hidden
        /// from learners, so reports must not count it either.
        /// </summary>
        private static readonly System.Linq.Expressions.Expression<Func<Enrollment, bool>> VisibleEnrollmentPredicate =
            e => !e.AssignmentLinks.Any()
              || e.AssignmentLinks.Any(al => !al.IsDeleted && al.Assignment != null && !al.Assignment.IsDeleted);

        /// <summary>
        /// Projects division-scoped, learner-visible enrollments to report rows using the same
        /// effective-schedule semantics as EnrollmentsController.GetEffectiveSchedule: when active
        /// assignment links exist their Min(StartDate)/Max(DueDate) win over the enrollment-level
        /// columns. This matters because ExtendDueDateAsync updates only Assignment/link DueDate,
        /// never Enrollment.DueDate — reading the raw column would flag extended learners as
        /// Overdue while the assignment pages and the learner player say otherwise.
        /// </summary>
        private IQueryable<EnrollmentReportRow> BuildVisibleEnrollmentRowsQuery(int? divisionId, string? learnerCode = null)
        {
            var query = BuildDivisionScopedEnrollmentQuery(divisionId);

            if (!string.IsNullOrWhiteSpace(learnerCode))
            {
                query = query.Where(e => e.LearnerCode == learnerCode);
            }

            return query
                .Where(VisibleEnrollmentPredicate)
                .Select(e => new EnrollmentReportRow
                {
                    Id = e.Id,
                    LearnerCode = e.LearnerCode,
                    CourseId = e.CourseId,
                    CourseCode = e.Course != null ? e.Course.Code : null,
                    CourseTitle = e.Course != null ? e.Course.Title : null,
                    AssignmentNo = e.AssignmentLinks
                        .Where(al => !al.IsDeleted && al.Assignment != null && !al.Assignment.IsDeleted)
                        .Select(al => al.Assignment!.AssignmentNo)
                        .FirstOrDefault(),
                    IsCompleted = e.IsCompleted,
                    Progress = e.Progress,
                    TotalScore = e.TotalScore,
                    TotalTimeSpentSeconds = e.TotalTimeSpent,
                    CompletedDate = e.CompletedDate,
                    StartDate = e.AssignmentLinks.Any(al => !al.IsDeleted && al.Assignment != null && !al.Assignment.IsDeleted)
                        ? e.AssignmentLinks
                            .Where(al => !al.IsDeleted && al.Assignment != null && !al.Assignment.IsDeleted)
                            .Min(al => al.StartDate)
                        : e.StartDate,
                    DueDate = e.AssignmentLinks.Any(al => !al.IsDeleted && al.Assignment != null && !al.Assignment.IsDeleted)
                        ? e.AssignmentLinks
                            .Where(al => !al.IsDeleted && al.Assignment != null && !al.Assignment.IsDeleted)
                            .Max(al => al.DueDate)
                        : e.DueDate,
                });
        }

        /// <summary>
        /// Builds an enrollment IQueryable scoped by division through AssignmentLinks.
        /// SuperAdmin (divisionId=null) sees all enrollments.
        /// Division-scoped admin sees only enrollments linked to assignments in their division.
        /// </summary>
        private IQueryable<Enrollment> BuildDivisionScopedEnrollmentQuery(int? divisionId)
        {
            var query = _enrollmentRepo.GetQuery().AsNoTracking();

            if (!divisionId.HasValue)
            {
                return query;
            }

            // Enrollment is linked to Assignment via EnrollmentAssignment bridge table.
            // Filter to enrollments that have at least one assignment in the admin's division.
            var scopedEnrollmentIds = _enrollmentAssignmentRepo.GetQuery()
                .AsNoTracking()
                .Join(_assignmentRepo.GetQuery().AsNoTracking()
                        .Where(a => a.DivisionId == divisionId.Value),
                    ea => ea.AssignmentId,
                    a => a.Id,
                    (ea, _) => ea.EnrollmentId)
                .Distinct();

            return query.Where(e => scopedEnrollmentIds.Contains(e.Id));
        }
    }
}
