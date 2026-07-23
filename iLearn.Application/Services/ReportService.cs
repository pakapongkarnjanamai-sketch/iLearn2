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
                .Select(c => new {
                    c.Id,
                    c.Code,
                    c.Title,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    DivisionName = c.Category != null && c.Category.Division != null ? c.Category.Division.Name : null,
                    CourseTypeName = c.CourseType != null ? c.CourseType.Name : null
                })
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

            // 3. Count assignments per course across all sources (single-course, multi-course batch, and linked enrollment assignments)
            var singleCourseAssignments = _assignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(a => a.CourseId.HasValue && courseIds.Contains(a.CourseId.Value))
                .Where(a => !divisionId.HasValue || a.DivisionId == divisionId.Value)
                .Select(a => new { CourseId = a.CourseId!.Value, AssignmentId = a.Id });

            var multiCourseAssignments = _assignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(a => !divisionId.HasValue || a.DivisionId == divisionId.Value)
                .SelectMany(a => a.AssignmentCourses.Select(ac => new { CourseId = ac.CourseId, AssignmentId = a.Id }))
                .Where(x => courseIds.Contains(x.CourseId));

            var linkedAssignments = _enrollmentAssignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(ea => ea.AssignmentId > 0 && ea.Enrollment != null && ea.Enrollment.CourseId.HasValue && courseIds.Contains(ea.Enrollment.CourseId.Value))
                .Where(ea => ea.Assignment != null)
                .Where(ea => !divisionId.HasValue || ea.Assignment!.DivisionId == divisionId.Value)
                .Select(ea => new { CourseId = ea.Enrollment!.CourseId!.Value, AssignmentId = ea.AssignmentId });

            var assignmentCounts = await singleCourseAssignments
                .Union(multiCourseAssignments)
                .Union(linkedAssignments)
                .GroupBy(x => x.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Select(x => x.AssignmentId).Distinct().Count() })
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
                    DivisionName = course.DivisionName,
                    CourseTypeName = course.CourseTypeName,
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

        public async Task<AssignmentSummaryReportDto> GetAssignmentSummaryReportAsync(
            int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default)
        {
            var assignmentRows = await _assignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(a => !divisionId.HasValue || a.DivisionId == divisionId.Value)
                .Select(a => new
                {
                    a.Id,
                    a.AssignmentNo,
                    a.Description,
                    a.StartDate,
                    a.DueDate,
                    a.CreatedAt,
                    a.CourseId,
                    DivisionName = a.DivisionNavigation != null ? a.DivisionNavigation.Name : a.Division,
                })
                .ToListAsync(cancellationToken);

            if (assignmentRows.Count == 0)
            {
                return new AssignmentSummaryReportDto { GeneratedAt = currentDate };
            }

            var assignmentIds = assignmentRows.Select(row => row.Id).ToList();
            var linkRows = await _enrollmentAssignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(link => assignmentIds.Contains(link.AssignmentId) && link.Enrollment != null)
                .Select(link => new
                {
                    link.AssignmentId,
                    LearnerCode = link.Enrollment!.LearnerCode,
                    IsCompleted = link.SnapshotCompleted || link.Enrollment.IsCompleted,
                    DueDate = link.DueDate,
                })
                .ToListAsync(cancellationToken);

            var linksByAssignmentId = linkRows.ToLookup(row => row.AssignmentId);

            var rows = assignmentRows
                .GroupBy(row => string.IsNullOrWhiteSpace(row.AssignmentNo) ? $"assignment:{row.Id}" : row.AssignmentNo!)
                .Select(group =>
                {
                    var orderedAssignments = group.OrderBy(row => row.Id).ToList();
                    var first = orderedAssignments[0];
                    var links = orderedAssignments
                        .SelectMany(row => linksByAssignmentId[row.Id])
                        .ToList();
                    var hasEnrollments = links.Count > 0;
                    var completedCount = links.Count(link => link.IsCompleted);
                    var overdueCount = links.Count(link => !link.IsCompleted && link.DueDate.HasValue && link.DueDate.Value < currentDate);
                    var learnerCount = links
                        .Select(link => link.LearnerCode)
                        .Where(code => !string.IsNullOrWhiteSpace(code))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    var startDate = orderedAssignments
                        .Where(row => row.StartDate.HasValue)
                        .Select(row => row.StartDate)
                        .DefaultIfEmpty(first.StartDate)
                        .Min();
                    var dueDate = orderedAssignments
                        .Where(row => row.DueDate.HasValue)
                        .Select(row => row.DueDate)
                        .DefaultIfEmpty(first.DueDate)
                        .Max();

                    return new AssignmentSummaryRow
                    {
                        AssignmentId = first.Id,
                        AssignmentNo = string.IsNullOrWhiteSpace(first.AssignmentNo) ? $"Assignment {first.Id}" : first.AssignmentNo!,
                        Description = first.Description,
                        DivisionName = first.DivisionName,
                        StartDate = startDate,
                        DueDate = dueDate,
                        CreatedAt = orderedAssignments.Min(row => row.CreatedAt),
                        CourseCount = orderedAssignments
                            .Where(row => row.CourseId.HasValue)
                            .Select(row => row.CourseId!.Value)
                            .Distinct()
                            .Count(),
                        LearnerCount = learnerCount,
                        EnrollmentCount = links.Count,
                        CompletedCount = completedCount,
                        OverdueCount = overdueCount,
                        CompletionRate = links.Count == 0 ? 0 : (double)completedCount / links.Count * 100,
                        Status = AssignmentStatusKeys.GetBatchStatus(
                            hasEnrollments,
                            hasEnrollments && completedCount == links.Count,
                            startDate,
                            dueDate,
                            currentDate),
                    };
                })
                .OrderByDescending(row => row.CreatedAt)
                .ThenBy(row => row.AssignmentNo)
                .ToList();

            var totalEnrollments = rows.Sum(row => row.EnrollmentCount);
            var totalCompleted = rows.Sum(row => row.CompletedCount);
            var totalLearners = linkRows
                .Select(row => row.LearnerCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            return new AssignmentSummaryReportDto
            {
                GeneratedAt = currentDate,
                TotalAssignments = rows.Count,
                ActiveAssignments = rows.Count(row => row.Status == AssignmentStatusKeys.Batch.InProgress),
                CompletedAssignments = rows.Count(row => row.Status == AssignmentStatusKeys.Batch.Completed),
                OverdueAssignments = rows.Count(row => row.Status == AssignmentStatusKeys.Batch.Expired),
                TotalLearners = totalLearners,
                TotalEnrollments = totalEnrollments,
                CompletionRate = totalEnrollments == 0 ? 0 : (double)totalCompleted / totalEnrollments * 100,
                Rows = rows,
            };
        }

        public async Task<LearnerGroupSummaryReportDto> GetLearnerGroupSummaryReportAsync(
            int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default)
        {
            var groupRows = await _learnerGroupRepo.GetQuery()
                .AsNoTracking()
                .Where(g => !divisionId.HasValue || g.DivisionId == divisionId.Value)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    g.CreatedAt,
                    DivisionName = g.Division != null ? g.Division.Name : null,
                    CategoryName = g.Category != null ? g.Category.Name : null,
                })
                .ToListAsync(cancellationToken);

            if (groupRows.Count == 0)
            {
                return new LearnerGroupSummaryReportDto { GeneratedAt = currentDate };
            }

            var groupIds = groupRows.Select(row => row.Id).ToList();
            var memberRows = await _learnerGroupMemberRepo.GetQuery()
                .AsNoTracking()
                .Where(member => groupIds.Contains(member.LearnerGroupId))
                .Select(member => new
                {
                    member.LearnerGroupId,
                    member.LearnerCode,
                })
                .ToListAsync(cancellationToken);

            var learnerCodes = memberRows
                .Select(row => row.LearnerCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var visibleEnrollments = learnerCodes.Count == 0
                ? new List<EnrollmentReportRow>()
                : await BuildVisibleEnrollmentRowsQuery(divisionId)
                    .Where(enrollment => learnerCodes.Contains(enrollment.LearnerCode))
                    .ToListAsync(cancellationToken);

            var assignmentRows = await _assignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(assignment => assignment.LearnerGroupId.HasValue && groupIds.Contains(assignment.LearnerGroupId.Value))
                .Where(assignment => !divisionId.HasValue || assignment.DivisionId == divisionId.Value)
                .Select(assignment => new
                {
                    LearnerGroupId = assignment.LearnerGroupId!.Value,
                    assignment.Id,
                    assignment.AssignmentNo,
                })
                .ToListAsync(cancellationToken);

            var rows = groupRows
                .Select(group =>
                {
                    var groupMembers = memberRows
                        .Where(member => member.LearnerGroupId == group.Id)
                        .Select(member => member.LearnerCode)
                        .Where(code => !string.IsNullOrWhiteSpace(code))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var memberSet = groupMembers.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var groupEnrollments = visibleEnrollments
                        .Where(enrollment => memberSet.Contains(enrollment.LearnerCode))
                        .ToList();
                    var groupAssignments = assignmentRows
                        .Where(assignment => assignment.LearnerGroupId == group.Id)
                        .Select(assignment => string.IsNullOrWhiteSpace(assignment.AssignmentNo)
                            ? $"assignment:{assignment.Id}"
                            : assignment.AssignmentNo!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    var completedCount = groupEnrollments.Count(enrollment => enrollment.IsCompleted);

                    return new LearnerGroupSummaryRow
                    {
                        LearnerGroupId = group.Id,
                        Name = group.Name,
                        Description = group.Description,
                        DivisionName = group.DivisionName,
                        CategoryName = group.CategoryName,
                        CreatedAt = group.CreatedAt,
                        MemberCount = groupMembers.Count,
                        AssignmentCount = groupAssignments,
                        CourseCount = groupEnrollments
                            .Where(enrollment => enrollment.CourseId.HasValue)
                            .Select(enrollment => enrollment.CourseId!.Value)
                            .Distinct()
                            .Count(),
                        DueDate = groupEnrollments.Any(enrollment => enrollment.DueDate.HasValue)
                            ? groupEnrollments
                                .Where(enrollment => enrollment.DueDate.HasValue)
                                .Max(enrollment => enrollment.DueDate)
                            : null,
                        EnrollmentCount = groupEnrollments.Count,
                        CompletedCount = completedCount,
                        OverdueCount = groupEnrollments.Count(enrollment => !enrollment.IsCompleted && enrollment.DueDate.HasValue && enrollment.DueDate.Value < currentDate),
                        AvgProgress = groupEnrollments.Count == 0 ? 0 : groupEnrollments.Average(enrollment => enrollment.Progress),
                        CompletionRate = groupEnrollments.Count == 0 ? 0 : (double)completedCount / groupEnrollments.Count * 100,
                    };
                })
                .OrderBy(row => row.Name)
                .ToList();

            var totalEnrollments = rows.Sum(row => row.EnrollmentCount);
            var totalCompleted = rows.Sum(row => row.CompletedCount);

            return new LearnerGroupSummaryReportDto
            {
                GeneratedAt = currentDate,
                TotalGroups = rows.Count,
                TotalMembers = rows.Sum(row => row.MemberCount),
                GroupsWithAssignments = rows.Count(row => row.AssignmentCount > 0),
                TotalAssignments = rows.Sum(row => row.AssignmentCount),
                TotalEnrollments = totalEnrollments,
                CompletionRate = totalEnrollments == 0 ? 0 : (double)totalCompleted / totalEnrollments * 100,
                Rows = rows,
            };
        }

        public async Task<AssignmentReportExportDto> GetAssignmentReportExportAsync(
            int? divisionId, DateTime? from, DateTime? to, DateTime currentDate, CancellationToken cancellationToken = default)
        {
            var normalizedFrom = NormalizeDate(from);
            var normalizedTo = NormalizeDate(to);
            var summary = await GetAssignmentSummaryReportAsync(divisionId, currentDate, cancellationToken);
            summary.Rows = summary.Rows
                .Where(row => IsDueDateInRange(row.DueDate, normalizedFrom, normalizedTo))
                .ToList();

            var detailRows = await BuildAssignmentExportDetailRowsAsync(
                divisionId, summary.Rows, normalizedFrom, normalizedTo, currentDate, cancellationToken);

            ApplyAssignmentExportDetailCounts(summary, detailRows, currentDate);

            return new AssignmentReportExportDto
            {
                GeneratedAt = currentDate,
                From = normalizedFrom,
                To = normalizedTo,
                Summary = summary,
                DetailRows = detailRows,
            };
        }

        public async Task<LearnerGroupReportExportDto> GetLearnerGroupReportExportAsync(
            int? divisionId, DateTime? from, DateTime? to, DateTime currentDate, CancellationToken cancellationToken = default)
        {
            var normalizedFrom = NormalizeDate(from);
            var normalizedTo = NormalizeDate(to);
            var hasDateFilter = normalizedFrom.HasValue || normalizedTo.HasValue;
            var summary = await GetLearnerGroupSummaryReportAsync(divisionId, currentDate, cancellationToken);
            var candidateGroupIds = summary.Rows.Select(row => row.LearnerGroupId).ToList();

            var detailRows = await BuildLearnerGroupExportDetailRowsAsync(
                divisionId, candidateGroupIds, normalizedFrom, normalizedTo, currentDate, cancellationToken);

            var includedGroupIds = hasDateFilter
                ? detailRows.Select(row => row.LearnerGroupId).Distinct().ToHashSet()
                : candidateGroupIds.ToHashSet();

            summary.Rows = summary.Rows
                .Where(row => includedGroupIds.Contains(row.LearnerGroupId))
                .ToList();

            ApplyLearnerGroupExportDetailCounts(summary, detailRows);

            var memberRows = await BuildLearnerGroupExportMemberRowsAsync(
                summary.Rows.Select(row => row.LearnerGroupId).ToList(), cancellationToken);

            return new LearnerGroupReportExportDto
            {
                GeneratedAt = currentDate,
                From = normalizedFrom,
                To = normalizedTo,
                Summary = summary,
                MemberRows = memberRows,
                DetailRows = detailRows,
            };
        }

        public async Task<byte[]> BuildAssignmentReportExcelAsync(
            int? divisionId, DateTime? from, DateTime? to, string? lang, DateTime currentDate, CancellationToken cancellationToken = default)
        {
            var export = await GetAssignmentReportExportAsync(divisionId, from, to, currentDate, cancellationToken);
            return ReportExcelBuilder.BuildAssignmentWorkbook(export, lang);
        }

        public async Task<byte[]> BuildLearnerGroupReportExcelAsync(
            int? divisionId, DateTime? from, DateTime? to, string? lang, DateTime currentDate, CancellationToken cancellationToken = default)
        {
            var export = await GetLearnerGroupReportExportAsync(divisionId, from, to, currentDate, cancellationToken);
            return ReportExcelBuilder.BuildLearnerGroupWorkbook(export, lang);
        }


        public async Task<ActivityReportDto> GetActivityReportAsync(
            int months, int? divisionId, CancellationToken cancellationToken = default)
        {
            var now = _dateTime.Now;
            var clampedMonths = Math.Clamp(months, 3, 24);
            var cutoff = new DateTime(now.Year, now.Month, 1).AddMonths(-clampedMonths + 1);

            // Completions: enrollments with CompletedDate in range, scoped by division
            // (same visibility rule as other reports: skip enrollments whose only assignments are deleted)
            var enrollmentQuery = ApplyVisibleEnrollmentFilter(BuildDivisionScopedEnrollmentQuery(divisionId));

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

        private async Task<List<AssignmentReportDetailRow>> BuildAssignmentExportDetailRowsAsync(
            int? divisionId,
            IReadOnlyCollection<AssignmentSummaryRow> summaryRows,
            DateTime? from,
            DateTime? to,
            DateTime currentDate,
            CancellationToken cancellationToken)
        {
            if (summaryRows.Count == 0)
            {
                return [];
            }

            var assignmentRows = await _assignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(assignment => !divisionId.HasValue || assignment.DivisionId == divisionId.Value)
                .Select(assignment => new
                {
                    assignment.Id,
                    assignment.AssignmentNo,
                })
                .ToListAsync(cancellationToken);

            var includedFirstAssignmentIds = summaryRows.Select(row => row.AssignmentId).ToHashSet();
            var includedBatchKeys = assignmentRows
                .Where(row => includedFirstAssignmentIds.Contains(row.Id))
                .Select(row => GetAssignmentBatchKey(row.Id, row.AssignmentNo))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var includedAssignmentIds = assignmentRows
                .Where(row => includedBatchKeys.Contains(GetAssignmentBatchKey(row.Id, row.AssignmentNo)))
                .Select(row => row.Id)
                .ToList();

            if (includedAssignmentIds.Count == 0)
            {
                return [];
            }

            var assignmentMap = assignmentRows.ToDictionary(row => row.Id);
            var linkRows = await _enrollmentAssignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(link => includedAssignmentIds.Contains(link.AssignmentId) && link.Enrollment != null)
                .Select(link => new
                {
                    link.AssignmentId,
                    link.EnrollmentId,
                })
                .ToListAsync(cancellationToken);

            var enrollmentIds = linkRows.Select(row => row.EnrollmentId).Distinct().ToList();
            if (enrollmentIds.Count == 0)
            {
                return [];
            }

            var enrollmentRows = await BuildVisibleEnrollmentRowsQuery(divisionId)
                .Where(enrollment => enrollmentIds.Contains(enrollment.Id))
                .ToListAsync(cancellationToken);
            var enrollmentMap = enrollmentRows.ToDictionary(row => row.Id);
            var learnerMap = await LoadLearnerMapAsync(enrollmentRows.Select(row => row.LearnerCode));

            return linkRows
                .Select(row =>
                {
                    if (!enrollmentMap.TryGetValue(row.EnrollmentId, out var enrollment))
                    {
                        return null;
                    }

                    if (!IsDueDateInRange(enrollment.DueDate, from, to))
                    {
                        return null;
                    }

                    assignmentMap.TryGetValue(row.AssignmentId, out var assignment);
                    learnerMap.TryGetValue(enrollment.LearnerCode, out var learner);
                    return new AssignmentReportDetailRow
                    {
                        AssignmentNo = BuildAssignmentDisplayNo(row.AssignmentId, assignment?.AssignmentNo),
                        LearnerCode = enrollment.LearnerCode,
                        LearnerName = learner?.Name,
                        LearnerDivision = learner?.Division,
                        CourseCode = enrollment.CourseCode,
                        CourseTitle = enrollment.CourseTitle,
                        StartDate = enrollment.StartDate,
                        DueDate = enrollment.DueDate,
                        Status = AssignmentStatusKeys.GetScheduledLearnerStatus(
                            enrollment.IsCompleted, enrollment.Progress, enrollment.StartDate, enrollment.DueDate, currentDate),
                        Progress = enrollment.Progress,
                        CompletedDate = enrollment.CompletedDate,
                        DaysOverdue = GetDaysOverdue(enrollment.IsCompleted, enrollment.DueDate, currentDate),
                    };
                })
                .Where(row => row != null)
                .Cast<AssignmentReportDetailRow>()
                .OrderBy(row => row.AssignmentNo)
                .ThenBy(row => row.LearnerCode)
                .ThenBy(row => row.CourseTitle)
                .ToList();
        }

        private async Task<List<LearnerGroupReportMemberRow>> BuildLearnerGroupExportMemberRowsAsync(
            IReadOnlyCollection<int> learnerGroupIds,
            CancellationToken cancellationToken)
        {
            if (learnerGroupIds.Count == 0)
            {
                return [];
            }

            var groupMap = await _learnerGroupRepo.GetQuery()
                .AsNoTracking()
                .Where(group => learnerGroupIds.Contains(group.Id))
                .Select(group => new { group.Id, group.Name })
                .ToDictionaryAsync(group => group.Id, cancellationToken);
            var memberRows = await _learnerGroupMemberRepo.GetQuery()
                .AsNoTracking()
                .Where(member => learnerGroupIds.Contains(member.LearnerGroupId))
                .Select(member => new
                {
                    member.LearnerGroupId,
                    member.LearnerCode,
                    member.CreatedAt,
                })
                .ToListAsync(cancellationToken);
            var learnerMap = await LoadLearnerMapAsync(memberRows.Select(row => row.LearnerCode));

            return memberRows
                .Select(row =>
                {
                    groupMap.TryGetValue(row.LearnerGroupId, out var group);
                    learnerMap.TryGetValue(row.LearnerCode, out var learner);
                    return new LearnerGroupReportMemberRow
                    {
                        LearnerGroupId = row.LearnerGroupId,
                        GroupName = group?.Name ?? string.Empty,
                        LearnerCode = row.LearnerCode,
                        LearnerName = learner?.Name,
                        LearnerDivision = learner?.Division,
                        CreatedAt = row.CreatedAt,
                    };
                })
                .OrderBy(row => row.GroupName)
                .ThenBy(row => row.LearnerCode)
                .ToList();
        }

        private async Task<List<LearnerGroupReportDetailRow>> BuildLearnerGroupExportDetailRowsAsync(
            int? divisionId,
            IReadOnlyCollection<int> learnerGroupIds,
            DateTime? from,
            DateTime? to,
            DateTime currentDate,
            CancellationToken cancellationToken)
        {
            if (learnerGroupIds.Count == 0)
            {
                return [];
            }

            var groupMap = await _learnerGroupRepo.GetQuery()
                .AsNoTracking()
                .Where(group => learnerGroupIds.Contains(group.Id))
                .Select(group => new { group.Id, group.Name })
                .ToDictionaryAsync(group => group.Id, cancellationToken);
            var memberRows = await _learnerGroupMemberRepo.GetQuery()
                .AsNoTracking()
                .Where(member => learnerGroupIds.Contains(member.LearnerGroupId))
                .Select(member => new
                {
                    member.LearnerGroupId,
                    member.LearnerCode,
                })
                .ToListAsync(cancellationToken);
            var learnerCodes = memberRows
                .Select(row => row.LearnerCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (learnerCodes.Count == 0)
            {
                return [];
            }

            var enrollmentRows = await BuildVisibleEnrollmentRowsQuery(divisionId)
                .Where(enrollment => learnerCodes.Contains(enrollment.LearnerCode))
                .ToListAsync(cancellationToken);
            var enrollmentsByLearner = enrollmentRows.ToLookup(row => row.LearnerCode, StringComparer.OrdinalIgnoreCase);
            var learnerMap = await LoadLearnerMapAsync(learnerCodes);

            return memberRows
                .SelectMany(member => enrollmentsByLearner[member.LearnerCode]
                    .Where(enrollment => IsDueDateInRange(enrollment.DueDate, from, to))
                    .Select(enrollment =>
                    {
                        groupMap.TryGetValue(member.LearnerGroupId, out var group);
                        learnerMap.TryGetValue(member.LearnerCode, out var learner);
                        return new LearnerGroupReportDetailRow
                        {
                            LearnerGroupId = member.LearnerGroupId,
                            GroupName = group?.Name ?? string.Empty,
                            LearnerCode = member.LearnerCode,
                            LearnerName = learner?.Name,
                            CourseCode = enrollment.CourseCode,
                            CourseTitle = enrollment.CourseTitle,
                            AssignmentNo = enrollment.AssignmentNo,
                            StartDate = enrollment.StartDate,
                            DueDate = enrollment.DueDate,
                            Status = AssignmentStatusKeys.GetScheduledLearnerStatus(
                                enrollment.IsCompleted, enrollment.Progress, enrollment.StartDate, enrollment.DueDate, currentDate),
                            Progress = enrollment.Progress,
                            CompletedDate = enrollment.CompletedDate,
                            DaysOverdue = GetDaysOverdue(enrollment.IsCompleted, enrollment.DueDate, currentDate),
                        };
                    }))
                .OrderBy(row => row.GroupName)
                .ThenBy(row => row.LearnerCode)
                .ThenBy(row => row.CourseTitle)
                .ToList();
        }

        private async Task<Dictionary<string, ExternalLearnerDto>> LoadLearnerMapAsync(IEnumerable<string> learnerCodes)
        {
            var codes = learnerCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return codes.Count == 0
                ? new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase)
                : await _learnerApiService.GetLearnersByCodesAsync(codes);
        }

        private static void ApplyAssignmentExportDetailCounts(
            AssignmentSummaryReportDto summary,
            IReadOnlyCollection<AssignmentReportDetailRow> detailRows,
            DateTime currentDate)
        {
            var detailsByAssignment = detailRows.ToLookup(row => row.AssignmentNo, StringComparer.OrdinalIgnoreCase);
            foreach (var row in summary.Rows)
            {
                var rows = detailsByAssignment[row.AssignmentNo].ToList();
                row.LearnerCount = rows.Select(detail => detail.LearnerCode).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                row.CourseCount = rows
                    .Select(detail => !string.IsNullOrWhiteSpace(detail.CourseCode) ? detail.CourseCode : detail.CourseTitle)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                row.EnrollmentCount = rows.Count;
                row.CompletedCount = rows.Count(detail => detail.Status == AssignmentStatusKeys.Learner.Completed);
                row.OverdueCount = rows.Count(detail => detail.DaysOverdue > 0);
                row.CompletionRate = rows.Count == 0 ? 0 : (double)row.CompletedCount / rows.Count * 100;
                row.Status = AssignmentStatusKeys.GetBatchStatus(
                    rows.Count > 0,
                    rows.Count > 0 && row.CompletedCount == rows.Count,
                    row.StartDate,
                    row.DueDate,
                    currentDate);
            }

            summary.TotalAssignments = summary.Rows.Count;
            summary.ActiveAssignments = summary.Rows.Count(row => row.Status == AssignmentStatusKeys.Batch.InProgress);
            summary.CompletedAssignments = summary.Rows.Count(row => row.Status == AssignmentStatusKeys.Batch.Completed);
            summary.OverdueAssignments = summary.Rows.Count(row => row.Status == AssignmentStatusKeys.Batch.Expired);
            summary.TotalLearners = detailRows.Select(row => row.LearnerCode).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            summary.TotalEnrollments = detailRows.Count;
            summary.CompletionRate = detailRows.Count == 0
                ? 0
                : (double)detailRows.Count(row => row.Status == AssignmentStatusKeys.Learner.Completed) / detailRows.Count * 100;
        }

        private static void ApplyLearnerGroupExportDetailCounts(
            LearnerGroupSummaryReportDto summary,
            IReadOnlyCollection<LearnerGroupReportDetailRow> detailRows)
        {
            var detailsByGroup = detailRows.ToLookup(row => row.LearnerGroupId);
            foreach (var row in summary.Rows)
            {
                var rows = detailsByGroup[row.LearnerGroupId].ToList();
                row.CourseCount = rows
                    .Select(detail => !string.IsNullOrWhiteSpace(detail.CourseCode) ? detail.CourseCode : detail.CourseTitle)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                row.EnrollmentCount = rows.Count;
                row.CompletedCount = rows.Count(detail => detail.Status == AssignmentStatusKeys.Learner.Completed);
                row.OverdueCount = rows.Count(detail => detail.DaysOverdue > 0);
                row.AvgProgress = rows.Count == 0 ? 0 : rows.Average(detail => detail.Progress);
                row.CompletionRate = rows.Count == 0 ? 0 : (double)row.CompletedCount / rows.Count * 100;
                row.DueDate = rows.Any(detail => detail.DueDate.HasValue)
                    ? rows.Where(detail => detail.DueDate.HasValue).Max(detail => detail.DueDate)
                    : null;
            }

            summary.TotalGroups = summary.Rows.Count;
            summary.TotalMembers = summary.Rows.Sum(row => row.MemberCount);
            summary.GroupsWithAssignments = summary.Rows.Count(row => row.AssignmentCount > 0);
            summary.TotalAssignments = summary.Rows.Sum(row => row.AssignmentCount);
            summary.TotalEnrollments = detailRows.Count;
            summary.CompletionRate = detailRows.Count == 0
                ? 0
                : (double)detailRows.Count(row => row.Status == AssignmentStatusKeys.Learner.Completed) / detailRows.Count * 100;
        }

        private static DateTime? NormalizeDate(DateTime? value)
        {
            return value?.Date;
        }

        private static bool IsDueDateInRange(DateTime? dueDate, DateTime? from, DateTime? to)
        {
            if (!from.HasValue && !to.HasValue)
            {
                return true;
            }

            if (!dueDate.HasValue)
            {
                return false;
            }

            var dueDateValue = dueDate.Value.Date;
            return (!from.HasValue || dueDateValue >= from.Value.Date)
                && (!to.HasValue || dueDateValue <= to.Value.Date);
        }

        private static string GetAssignmentBatchKey(int assignmentId, string? assignmentNo)
        {
            return string.IsNullOrWhiteSpace(assignmentNo) ? $"assignment:{assignmentId}" : assignmentNo!;
        }

        private static string BuildAssignmentDisplayNo(int assignmentId, string? assignmentNo)
        {
            return string.IsNullOrWhiteSpace(assignmentNo) ? $"Assignment {assignmentId}" : assignmentNo!;
        }

        private static int GetDaysOverdue(bool isCompleted, DateTime? dueDate, DateTime currentDate)
        {
            return !isCompleted && dueDate.HasValue && dueDate.Value.Date < currentDate.Date
                ? (int)(currentDate.Date - dueDate.Value.Date).TotalDays
                : 0;
        }

        /// <summary>
        /// Visibility rule shared with the learner side (EnrollmentsController.GetEffectiveSchedule):
        /// include enrollments that have at least one active assignment link, or have never had links.
        /// Exclude enrollments whose links existed but are now all soft-deleted/deleted assignments.
        /// </summary>
        private IQueryable<Enrollment> ApplyVisibleEnrollmentFilter(IQueryable<Enrollment> query)
        {
            var allLinks = _enrollmentAssignmentRepo.GetQuery()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Select(ea => ea.EnrollmentId)
                .Distinct();

            var activeLinks = _enrollmentAssignmentRepo.GetQuery()
                .AsNoTracking()
                .Join(
                    _assignmentRepo.GetQuery()
                        .AsNoTracking()
                        .Where(a => !a.IsDeleted),
                    ea => ea.AssignmentId,
                    a => a.Id,
                    (ea, _) => ea.EnrollmentId)
                .Distinct();

            return query.Where(e =>
                activeLinks.Contains(e.Id)
                || !allLinks.Contains(e.Id));
        }

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

            return ApplyVisibleEnrollmentFilter(query)
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
