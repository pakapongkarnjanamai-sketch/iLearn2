// File: iLearn.API/Controllers/DashboardController.cs
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;

namespace iLearn.API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("api/admin/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<ContentItem> _contentItemRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly IGenericRepository<LearningLog> _learningLogRepo;
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IGenericRepository<LearnerGroup> _learnerGroupRepo;
        private readonly IAdminActivityService _adminActivityService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly IMaintenanceStatusService _maintenanceStatusService;

        public DashboardController(
            IGenericRepository<Course> courseRepo,
            IGenericRepository<User> userRepo,
            IGenericRepository<ContentItem> contentItemRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<LearningLog> learningLogRepo,
            IGenericRepository<Assignment> assignmentRepo,
            IGenericRepository<LearnerGroup> learnerGroupRepo,
            IAdminActivityService adminActivityService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            IMaintenanceStatusService maintenanceStatusService)
        {
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _contentItemRepo = contentItemRepo;
            _enrollmentRepo = enrollmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _learningLogRepo = learningLogRepo;
            _assignmentRepo = assignmentRepo;
            _learnerGroupRepo = learnerGroupRepo;
            _adminActivityService = adminActivityService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _maintenanceStatusService = maintenanceStatusService;
        }

        [HttpGet("Overview")]
        public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
        {
            var now = _dateTime.Now;
            var today = now.Date;
            var dueSoonCutoff = today.AddDays(14);
            var recentWindowStart = today.AddDays(-30);
            var previousWindowStart = today.AddDays(-60);

            var coursesQuery = ApplyCourseScope(_courseRepo.GetQuery().AsNoTracking());
            var assignmentsQuery = ApplyAssignmentScope(_assignmentRepo.GetQuery().AsNoTracking());
            var contentItemsQuery = ApplyContentItemScope(_contentItemRepo.GetQuery().AsNoTracking());
            var groupsQuery = ApplyLearnerGroupScope(_learnerGroupRepo.GetQuery().AsNoTracking());
            var learningLogsQuery = ApplyLearningLogScope(_learningLogRepo.GetQuery().AsNoTracking());

            var activeCourses = await coursesQuery.CountAsync(c => c.Status == CourseStatus.Open, cancellationToken);
            var draftCourses = await coursesQuery.CountAsync(c => c.Status == CourseStatus.Draft, cancellationToken);
            var newCourses = await coursesQuery.CountAsync(c => c.CreatedAt >= recentWindowStart, cancellationToken);
            var contentItemCount = await contentItemsQuery.CountAsync(r => r.IsActive, cancellationToken);
            var learnerGroupCount = await groupsQuery.CountAsync(g => g.IsActive, cancellationToken);

            var assignmentRows = await assignmentsQuery
                .Select(a => new DashboardAssignmentRow
                {
                    Id = a.Id,
                    AssignmentNo = a.AssignmentNo,
                    Description = a.Description,
                    CourseId = a.CourseId,
                    StartDate = a.StartDate,
                    DueDate = a.DueDate,
                    CreatedAt = a.CreatedAt,
                    DivisionName = a.DivisionNavigation != null ? a.DivisionNavigation.Name : a.Division
                })
                .ToListAsync(cancellationToken);

            var assignmentIds = assignmentRows.Select(a => a.Id).ToList();
            var taskRows = assignmentIds.Count == 0
                ? new List<DashboardTaskRow>()
                : await _enrollmentAssignmentRepo.GetQuery()
                    .AsNoTracking()
                    .Where(link => assignmentIds.Contains(link.AssignmentId) && link.Enrollment != null)
                    .Select(link => new DashboardTaskRow
                    {
                        AssignmentId = link.AssignmentId,
                        LearnerCode = link.Enrollment!.LearnerCode,
                        CourseId = link.Enrollment.CourseId ?? (link.Assignment != null ? link.Assignment.CourseId : null),
                        IsCompleted = link.SnapshotCompleted || link.Enrollment.IsCompleted,
                        Progress = link.SnapshotCompleted ? link.SnapshotProgress : link.Enrollment.Progress,
                        DueDate = link.DueDate ?? (link.Assignment != null ? link.Assignment.DueDate : null) ?? link.Enrollment.DueDate
                    })
                    .ToListAsync(cancellationToken);

            var completedTasks = taskRows.Count(t => t.IsCompleted);
            var overdueTasks = taskRows.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date < today);
            var dueSoonTasks = taskRows.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date >= today && t.DueDate.Value.Date <= dueSoonCutoff);
            var inProgressTasks = taskRows.Count(t => !t.IsCompleted && t.Progress > 0 && !(t.DueDate.HasValue && t.DueDate.Value.Date < today));
            var notStartedTasks = taskRows.Count(t => !t.IsCompleted && t.Progress <= 0 && !(t.DueDate.HasValue && t.DueDate.Value.Date < today));
            var totalTasks = taskRows.Count;
            var completionRate = totalTasks == 0 ? 0 : Math.Round((double)completedTasks / totalTasks * 100, 1);
            var assignedLearners = taskRows
                .Select(t => t.LearnerCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var learningSessionsLast30 = await learningLogsQuery.CountAsync(l => l.CreatedAt >= recentWindowStart, cancellationToken);
            var learningSessionsPrevious30 = await learningLogsQuery.CountAsync(
                l => l.CreatedAt >= previousWindowStart && l.CreatedAt < recentWindowStart,
                cancellationToken);

            var categoryMix = await coursesQuery
                .Where(c => c.Status == CourseStatus.Open)
                .GroupBy(c => new
                {
                    c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.Name : "Uncategorized"
                })
                .Select(g => new
                {
                    categoryId = g.Key.CategoryId,
                    categoryName = g.Key.CategoryName,
                    courseCount = g.Count()
                })
                .OrderByDescending(x => x.courseCount)
                .ThenBy(x => x.categoryName)
                .Take(6)
                .ToListAsync(cancellationToken);

            var learningActivity = await BuildLearningActivityTrendAsync(learningLogsQuery, today, cancellationToken);
            var assignmentSummaries = BuildPriorityAssignments(assignmentRows, taskRows, today, dueSoonCutoff);
            var priorityAssignments = assignmentSummaries.Take(6).ToList();
            var courseAttention = await BuildCourseAttentionAsync(coursesQuery, taskRows, today, cancellationToken);

            var activeAssignmentBatches = assignmentSummaries.Count(a => a.Status == "Active" || a.Status == "Due Soon" || a.Status == "Overdue");

            return Ok(new
            {
                success = true,
                data = new
                {
                    generatedAt = now,
                    scope = new
                    {
                        isGlobal = _currentUser.IsSuperAdmin,
                        divisionId = _currentUser.DivisionId,
                        divisionName = _currentUser.IsSuperAdmin ? null : _currentUser.DivisionName
                    },
                    kpi = new
                    {
                        activeCourses,
                        draftCourses,
                        newCourses,
                        contentItemCount,
                        learnerGroupCount,
                        activeAssignmentBatches,
                        assignedLearners,
                        completionRate,
                        totalLearningTasks = totalTasks,
                        completedLearningTasks = completedTasks,
                        overdueTasks,
                        dueSoonTasks,
                        learningSessionsLast30,
                        learningSessionsPrevious30,
                        learningSessionDelta = learningSessionsLast30 - learningSessionsPrevious30
                    },
                    taskStatus = new[]
                    {
                        new { status = "Completed", count = completedTasks },
                        new { status = "In Progress", count = inProgressTasks },
                        new { status = "Not Started", count = notStartedTasks },
                        new { status = "Overdue", count = overdueTasks }
                    },
                    learningActivity,
                    categoryMix,
                    priorityAssignments,
                    courseAttention
                }
            });
        }

        [HttpGet("Stats")]
        public async Task<IActionResult> GetStats()
        {
            var activeCourses = await ApplyCourseScope(_courseRepo.GetQuery()).CountAsync(c => c.Status == CourseStatus.Open);
            var draftCourses = await ApplyCourseScope(_courseRepo.GetQuery()).CountAsync(c => c.Status == CourseStatus.Draft);
            var totalContentItems = await ApplyContentItemScope(_contentItemRepo.GetQuery()).CountAsync();

            var now = _dateTime.Now;
            var inProgressAssignments = await ApplyAssignmentScope(_assignmentRepo.GetQuery()).CountAsync(
                a => (!a.StartDate.HasValue || a.StartDate.Value <= now)
                  && (!a.DueDate.HasValue || a.DueDate.Value >= now));

            return Ok(new
            {
                success = true,
                data = new
                {
                    activeCourses,
                    draftCourses,
                    inProgressAssignments,
                    totalContentItems
                }
            });
        }

        [HttpGet("EnrollmentTrends")]
        public IActionResult GetEnrollmentTrends()
        {
            var today = _dateTime.Now.Date;

            var months = Enumerable.Range(0, 6)
                .Select(i => today.AddMonths(-5 + i))
                .Select(d => new { d.Year, d.Month })
                .ToList();

            var cutoff = new DateTime(months[0].Year, months[0].Month, 1);

            var enrollments = ApplyEnrollmentScope(_enrollmentRepo.GetQuery())
                .Where(e => e.StartDate.HasValue && e.StartDate.Value >= cutoff)
                .Select(e => new { e.StartDate!.Value.Year, e.StartDate!.Value.Month })
                .ToList();

            var trends = months.Select(m => new
            {
                month = new DateTime(m.Year, m.Month, 1).ToString("MMM"),
                enrollments = enrollments.Count(e => e.Year == m.Year && e.Month == m.Month)
            });

            return Ok(new { success = true, data = trends });
        }

        [HttpGet("LearningActivityTrends")]
        public IActionResult GetLearningActivityTrends()
        {
            var today = _dateTime.Now.Date;

            // 6 เดือนย้อนหลัง
            var months = Enumerable.Range(0, 6)
                .Select(i => today.AddMonths(-5 + i))
                .Select(d => new { d.Year, d.Month })
                .ToList();

            var cutoff = new DateTime(months[0].Year, months[0].Month, 1);

            // นับ LearningLog (session การเรียน) ตาม CreatedAt
            var logs = ApplyLearningLogScope(_learningLogRepo.GetQuery())
                .Where(l => l.CreatedAt >= cutoff)
                .Select(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
                .ToList();

            var trends = months.Select(m => new
            {
                month = new DateTime(m.Year, m.Month, 1).ToString("MMM yy"),
                sessions = logs.Count(l => l.Year == m.Year && l.Month == m.Month)
            });

            return Ok(new { success = true, data = trends });
        }

        [HttpGet("MaintenanceStatus")]
        public IActionResult GetMaintenanceStatus()
        {
            var operations = _maintenanceStatusService.GetActiveOperations()
                .Select(x => new
                {
                    x.OperationId,
                    x.OperationName,
                    x.CurrentStep,
                    x.CurrentItemName,
                    x.CurrentItem,
                    x.TotalItems,
                    x.SuccessCount,
                    x.FailureCount,
                    x.InitiatedBy,
                    startedAt = x.StartedAt,
                    lastUpdatedAt = x.LastUpdatedAt
                })
                .ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    hasActiveMaintenance = operations.Any(),
                    operations
                }
            });
        }

        [HttpGet("RecentAdminActivities")]
        public async Task<IActionResult> GetRecentAdminActivities([FromQuery] int take = 10)
        {
            var activities = await _adminActivityService.GetRecentActivitiesAsync(take, _currentUser.DivisionId);

            return Ok(new
            {
                success = true,
                data = activities
            });
        }

        private async Task<IEnumerable<object>> BuildLearningActivityTrendAsync(
            IQueryable<LearningLog> learningLogsQuery,
            DateTime today,
            CancellationToken cancellationToken)
        {
            var months = Enumerable.Range(0, 6)
                .Select(i => today.AddMonths(-5 + i))
                .Select(d => new { d.Year, d.Month })
                .ToList();

            var cutoff = new DateTime(months[0].Year, months[0].Month, 1);

            var logs = await learningLogsQuery
                .Where(l => l.CreatedAt >= cutoff)
                .Select(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
                .ToListAsync(cancellationToken);

            return months.Select(m => new
            {
                month = new DateTime(m.Year, m.Month, 1).ToString("MMM yy", CultureInfo.InvariantCulture),
                sessions = logs.Count(l => l.Year == m.Year && l.Month == m.Month)
            });
        }

        private async Task<IEnumerable<object>> BuildCourseAttentionAsync(
            IQueryable<Course> scopedCoursesQuery,
            List<DashboardTaskRow> taskRows,
            DateTime today,
            CancellationToken cancellationToken)
        {
            var courseIds = taskRows
                .Where(t => t.CourseId.HasValue)
                .Select(t => t.CourseId!.Value)
                .Distinct()
                .ToList();

            if (courseIds.Count == 0)
            {
                return [];
            }

            var courseMap = await scopedCoursesQuery
                .IgnoreQueryFilters()
                .Where(c => courseIds.Contains(c.Id))
                .Select(c => new DashboardCourseRow
                {
                    Id = c.Id,
                    Code = c.Code,
                    Title = c.Title,
                    CategoryName = c.Category != null ? c.Category.Name : "Uncategorized"
                })
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            return taskRows
                .Where(t => t.CourseId.HasValue)
                .GroupBy(t => t.CourseId!.Value)
                .Select(g =>
                {
                    courseMap.TryGetValue(g.Key, out var course);
                    var total = g.Count();
                    var completed = g.Count(t => t.IsCompleted);
                    var overdue = g.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date < today);
                    var rate = total == 0 ? 0 : Math.Round((double)completed / total * 100, 1);

                    return new
                    {
                        courseId = g.Key,
                        courseCode = course?.Code ?? "-",
                        courseTitle = course?.Title ?? "Unknown Course",
                        categoryName = course?.CategoryName ?? "Uncategorized",
                        learnerTasks = total,
                        completedTasks = completed,
                        overdueTasks = overdue,
                        completionRate = rate
                    };
                })
                .OrderByDescending(x => x.overdueTasks)
                .ThenBy(x => x.completionRate)
                .ThenByDescending(x => x.learnerTasks)
                .Take(6)
                .ToList();
        }

        private static List<DashboardPriorityAssignment> BuildPriorityAssignments(
            List<DashboardAssignmentRow> assignmentRows,
            List<DashboardTaskRow> taskRows,
            DateTime today,
            DateTime dueSoonCutoff)
        {
            var linksByAssignmentId = taskRows.ToLookup(t => t.AssignmentId);

            return assignmentRows
                .GroupBy(GetAssignmentBatchKey)
                .Select(group =>
                {
                    var rows = group.ToList();
                    var links = rows.SelectMany(row => linksByAssignmentId[row.Id]).ToList();
                    var total = links.Count;
                    var completed = links.Count(t => t.IsCompleted);
                    var overdue = links.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date < today);
                    var dueSoon = links.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date >= today && t.DueDate.Value.Date <= dueSoonCutoff);
                    var rate = total == 0 ? 0 : Math.Round((double)completed / total * 100, 1);
                    var earliestDueDate = links
                        .Select(t => t.DueDate)
                        .Concat(rows.Select(r => r.DueDate))
                        .Where(d => d.HasValue)
                        .OrderBy(d => d!.Value)
                        .FirstOrDefault();
                    var learnerCount = links
                        .Select(t => t.LearnerCode)
                        .Where(code => !string.IsNullOrWhiteSpace(code))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    var firstRow = rows.OrderBy(r => r.Id).First();
                    var status = ResolveAssignmentStatus(rows, total, completed, overdue, dueSoon, today);

                    return new DashboardPriorityAssignment
                    {
                        AssignmentId = firstRow.Id,
                        AssignmentNo = string.IsNullOrWhiteSpace(firstRow.AssignmentNo) ? $"Assignment #{firstRow.Id}" : firstRow.AssignmentNo,
                        Description = string.IsNullOrWhiteSpace(firstRow.Description) ? "No description" : firstRow.Description,
                        DivisionName = firstRow.DivisionName,
                        StartDate = rows.Select(r => r.StartDate).Where(d => d.HasValue).OrderBy(d => d!.Value).FirstOrDefault(),
                        DueDate = earliestDueDate,
                        CourseCount = rows.Where(r => r.CourseId.HasValue).Select(r => r.CourseId!.Value).Distinct().Count(),
                        LearnerCount = learnerCount,
                        TotalTasks = total,
                        CompletedTasks = completed,
                        OverdueTasks = overdue,
                        DueSoonTasks = dueSoon,
                        CompletionRate = rate,
                        Status = status
                    };
                })
                .OrderByDescending(x => x.Status == "Overdue")
                .ThenByDescending(x => x.Status == "Due Soon")
                .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(x => x.TotalTasks)
                .ToList();
        }

        private static string ResolveAssignmentStatus(
            List<DashboardAssignmentRow> rows,
            int totalTasks,
            int completedTasks,
            int overdueTasks,
            int dueSoonTasks,
            DateTime today)
        {
            if (totalTasks > 0 && completedTasks == totalTasks)
            {
                return "Completed";
            }

            if (overdueTasks > 0)
            {
                return "Overdue";
            }

            if (dueSoonTasks > 0)
            {
                return "Due Soon";
            }

            if (rows.All(row => row.StartDate.HasValue && row.StartDate.Value.Date > today))
            {
                return "Upcoming";
            }

            return totalTasks == 0 ? "Unassigned" : "Active";
        }

        private IQueryable<Course> ApplyCourseScope(IQueryable<Course> query)
        {
            var divisionId = _currentUser.DivisionId;
            var divisionName = _currentUser.DivisionName;

            if (divisionId.HasValue)
            {
                return string.IsNullOrWhiteSpace(divisionName)
                    ? query.Where(c => c.Category != null && c.Category.DivisionId == divisionId.Value)
                    : query.Where(c => c.Category != null
                        && (c.Category.DivisionId == divisionId.Value
                            || c.Category.Division != null && c.Category.Division.Name == divisionName));
            }

            if (ShouldFilterByDivisionName())
            {
                return query.Where(c => c.Category != null
                    && c.Category.Division != null
                    && c.Category.Division.Name == divisionName);
            }

            return query;
        }

        private IQueryable<Assignment> ApplyAssignmentScope(IQueryable<Assignment> query)
        {
            var divisionId = _currentUser.DivisionId;
            var divisionName = _currentUser.DivisionName;

            if (divisionId.HasValue)
            {
                return string.IsNullOrWhiteSpace(divisionName)
                    ? query.Where(a => a.DivisionId == divisionId.Value)
                    : query.Where(a => a.DivisionId == divisionId.Value
                        || a.Division == divisionName
                        || a.DivisionNavigation != null && a.DivisionNavigation.Name == divisionName);
            }

            if (ShouldFilterByDivisionName())
            {
                return query.Where(a => a.Division == divisionName
                    || a.DivisionNavigation != null && a.DivisionNavigation.Name == divisionName);
            }

            return query;
        }

        private IQueryable<LearnerGroup> ApplyLearnerGroupScope(IQueryable<LearnerGroup> query)
        {
            var divisionId = _currentUser.DivisionId;
            var divisionName = _currentUser.DivisionName;

            if (divisionId.HasValue)
            {
                return string.IsNullOrWhiteSpace(divisionName)
                    ? query.Where(g => g.DivisionId == divisionId.Value)
                    : query.Where(g => g.DivisionId == divisionId.Value
                        || g.Division != null && g.Division.Name == divisionName);
            }

            if (ShouldFilterByDivisionName())
            {
                return query.Where(g => g.Division != null && g.Division.Name == divisionName);
            }

            return query;
        }

        private IQueryable<ContentItem> ApplyContentItemScope(IQueryable<ContentItem> query)
        {
            var divisionId = _currentUser.DivisionId;
            var divisionName = _currentUser.DivisionName;

            if (divisionId.HasValue)
            {
                return string.IsNullOrWhiteSpace(divisionName)
                    ? query.Where(r => r.CourseContentItems.Any(cr => cr.CourseVersion != null
                        && cr.CourseVersion.Course != null
                        && cr.CourseVersion.Course.Category != null
                        && cr.CourseVersion.Course.Category.DivisionId == divisionId.Value))
                    : query.Where(r => r.CourseContentItems.Any(cr => cr.CourseVersion != null
                        && cr.CourseVersion.Course != null
                        && cr.CourseVersion.Course.Category != null
                        && (cr.CourseVersion.Course.Category.DivisionId == divisionId.Value
                            || cr.CourseVersion.Course.Category.Division != null
                                && cr.CourseVersion.Course.Category.Division.Name == divisionName)));
            }

            if (ShouldFilterByDivisionName())
            {
                return query.Where(r => r.CourseContentItems.Any(cr => cr.CourseVersion != null
                    && cr.CourseVersion.Course != null
                    && cr.CourseVersion.Course.Category != null
                    && cr.CourseVersion.Course.Category.Division != null
                    && cr.CourseVersion.Course.Category.Division.Name == divisionName));
            }

            return query;
        }

        private IQueryable<Enrollment> ApplyEnrollmentScope(IQueryable<Enrollment> query)
        {
            var divisionId = _currentUser.DivisionId;
            var divisionName = _currentUser.DivisionName;

            if (divisionId.HasValue)
            {
                return string.IsNullOrWhiteSpace(divisionName)
                    ? query.Where(e => e.Course != null
                            && e.Course.Category != null
                            && e.Course.Category.DivisionId == divisionId.Value
                        || e.AssignmentLinks.Any(link => link.Assignment != null && link.Assignment.DivisionId == divisionId.Value))
                    : query.Where(e => e.Course != null
                            && e.Course.Category != null
                            && (e.Course.Category.DivisionId == divisionId.Value
                                || e.Course.Category.Division != null && e.Course.Category.Division.Name == divisionName)
                        || e.AssignmentLinks.Any(link => link.Assignment != null
                            && (link.Assignment.DivisionId == divisionId.Value
                                || link.Assignment.Division == divisionName
                                || link.Assignment.DivisionNavigation != null && link.Assignment.DivisionNavigation.Name == divisionName)));
            }

            if (ShouldFilterByDivisionName())
            {
                return query.Where(e => e.Course != null
                        && e.Course.Category != null
                        && e.Course.Category.Division != null
                        && e.Course.Category.Division.Name == divisionName
                    || e.AssignmentLinks.Any(link => link.Assignment != null
                        && (link.Assignment.Division == divisionName
                            || link.Assignment.DivisionNavigation != null && link.Assignment.DivisionNavigation.Name == divisionName)));
            }

            return query;
        }

        private IQueryable<LearningLog> ApplyLearningLogScope(IQueryable<LearningLog> query)
        {
            var divisionId = _currentUser.DivisionId;
            var divisionName = _currentUser.DivisionName;

            if (divisionId.HasValue)
            {
                return string.IsNullOrWhiteSpace(divisionName)
                    ? query.Where(log => log.Enrollment != null
                        && (log.Enrollment.Course != null
                            && log.Enrollment.Course.Category != null
                            && log.Enrollment.Course.Category.DivisionId == divisionId.Value
                            || log.Enrollment.AssignmentLinks.Any(link => link.Assignment != null && link.Assignment.DivisionId == divisionId.Value)))
                    : query.Where(log => log.Enrollment != null
                        && (log.Enrollment.Course != null
                            && log.Enrollment.Course.Category != null
                            && (log.Enrollment.Course.Category.DivisionId == divisionId.Value
                                || log.Enrollment.Course.Category.Division != null && log.Enrollment.Course.Category.Division.Name == divisionName)
                            || log.Enrollment.AssignmentLinks.Any(link => link.Assignment != null
                                && (link.Assignment.DivisionId == divisionId.Value
                                    || link.Assignment.Division == divisionName
                                    || link.Assignment.DivisionNavigation != null && link.Assignment.DivisionNavigation.Name == divisionName))));
            }

            if (ShouldFilterByDivisionName())
            {
                return query.Where(log => log.Enrollment != null
                    && (log.Enrollment.Course != null
                        && log.Enrollment.Course.Category != null
                        && log.Enrollment.Course.Category.Division != null
                        && log.Enrollment.Course.Category.Division.Name == divisionName
                        || log.Enrollment.AssignmentLinks.Any(link => link.Assignment != null
                            && (link.Assignment.Division == divisionName
                                || link.Assignment.DivisionNavigation != null && link.Assignment.DivisionNavigation.Name == divisionName))));
            }

            return query;
        }

        private bool ShouldFilterByDivisionName()
        {
            return !_currentUser.IsSuperAdmin
                && !_currentUser.DivisionId.HasValue
                && !string.IsNullOrWhiteSpace(_currentUser.DivisionName);
        }

        private static string GetAssignmentBatchKey(DashboardAssignmentRow row)
        {
            return string.IsNullOrWhiteSpace(row.AssignmentNo) ? $"assignment:{row.Id}" : row.AssignmentNo!;
        }

        private sealed class DashboardAssignmentRow
        {
            public int Id { get; set; }
            public string? AssignmentNo { get; set; }
            public string? Description { get; set; }
            public int? CourseId { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime CreatedAt { get; set; }
            public string? DivisionName { get; set; }
        }

        private sealed class DashboardTaskRow
        {
            public int AssignmentId { get; set; }
            public string LearnerCode { get; set; } = string.Empty;
            public int? CourseId { get; set; }
            public bool IsCompleted { get; set; }
            public double Progress { get; set; }
            public DateTime? DueDate { get; set; }
        }

        private sealed class DashboardCourseRow
        {
            public int Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }

        private sealed class DashboardPriorityAssignment
        {
            public int AssignmentId { get; set; }
            public string AssignmentNo { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string? DivisionName { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
            public int CourseCount { get; set; }
            public int LearnerCount { get; set; }
            public int TotalTasks { get; set; }
            public int CompletedTasks { get; set; }
            public int OverdueTasks { get; set; }
            public int DueSoonTasks { get; set; }
            public double CompletionRate { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}
