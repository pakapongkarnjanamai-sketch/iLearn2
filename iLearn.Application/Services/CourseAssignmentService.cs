using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class CourseAssignmentService : ICourseAssignmentService
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IAssignmentDashboardService _assignmentDashboardService;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;

        public CourseAssignmentService(
            ICourseRepository courseRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Assignment> assignmentRepo,
            IAssignmentDashboardService assignmentDashboardService,
            IGenericRepository<CourseVersion> versionRepo,
            IDateTime dateTime,
            IUnitOfWork unitOfWork)
        {
            _courseRepo = courseRepo;
            _enrollmentRepo = enrollmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _assignmentRepo = assignmentRepo;
            _assignmentDashboardService = assignmentDashboardService;
            _versionRepo = versionRepo;
            _dateTime = dateTime;
            _unitOfWork = unitOfWork;
        }

     
        public async Task AssignGeneralCoursesToNewUserAsync(string employeeId)
        {
            var activeCourses = await _courseRepo.GetActiveCoursesAsync();
            var generalCourses = activeCourses.Where(c => c.CourseType != null && c.CourseType.Name == "General");
            var activeVersions = await GetActiveVersionMapAsync(generalCourses.Select(c => c.Id));

            foreach (var course in generalCourses)
            {
                if (!activeVersions.TryGetValue(course.Id, out var activeVersion))
                    continue;

                await CreateOrUpdateEnrollment(employeeId, course, activeVersion);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task AssignCourseToEmployees(int courseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, int? assignmentRuleId = null, bool forceReset = false)
        {
            if (employeeCodes == null || !employeeCodes.Any()) return;

            employeeCodes = NormalizeEmployeeCodes(employeeCodes);

            if (!employeeCodes.Any()) return;

            ValidateAssignmentWindow(startDate, dueDate);

            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null || course.Status != CourseStatus.Open) return;

            var activeVersion = await GetActiveVersionAsync(course.Id);
            if (activeVersion == null) return;

            foreach (var empCode in employeeCodes)
            {
                await CreateOrUpdateEnrollment(empCode, course, activeVersion, assignmentRuleId, startDate, dueDate, forceReset);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task AssignCoursesToEmployees(IReadOnlyDictionary<int, int> assignmentRuleIdsByCourseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, bool forceReset = false)
        {
            if (assignmentRuleIdsByCourseId == null || assignmentRuleIdsByCourseId.Count == 0) return;
            if (employeeCodes == null || !employeeCodes.Any()) return;

            employeeCodes = NormalizeEmployeeCodes(employeeCodes);
            if (!employeeCodes.Any()) return;

            ValidateAssignmentWindow(startDate, dueDate);

            var courseIds = assignmentRuleIdsByCourseId.Keys.Distinct().ToList();
            var courses = await _courseRepo.GetAsync(c => courseIds.Contains(c.Id) && c.Status == CourseStatus.Open);
            var activeCourses = courses.ToDictionary(c => c.Id);
            var activeVersions = await GetActiveVersionMapAsync(courseIds);
            var existingEnrollments = await GetExistingEnrollmentMapAsync(courseIds, employeeCodes);
            var existingEnrollmentIds = existingEnrollments.Values
                .Where(e => e.Id > 0)
                .Select(e => e.Id)
                .Distinct()
                .ToList();
            var existingLinks = await GetExistingLinkMapAsync(existingEnrollmentIds);

            foreach (var assignmentRule in assignmentRuleIdsByCourseId)
            {
                if (!activeCourses.TryGetValue(assignmentRule.Key, out var course))
                    continue;

                if (!activeVersions.TryGetValue(assignmentRule.Key, out var activeVersion))
                    continue;

                foreach (var empCode in employeeCodes)
                {
                    var enrollmentKey = BuildEnrollmentKey(empCode, course.Id);
                    existingEnrollments.TryGetValue(enrollmentKey, out var existingEnrollment);

                    var enrollmentLinks = existingEnrollment != null && existingEnrollment.Id > 0 && existingLinks.TryGetValue(existingEnrollment.Id, out var links)
                        ? links
                        : [];

                    var updatedEnrollment = await CreateOrUpdateEnrollment(
                        empCode,
                        course,
                        activeVersion,
                        assignmentRule.Value,
                        startDate,
                        dueDate,
                        forceReset,
                        existingEnrollment,
                        enrollmentLinks);

                    existingEnrollments[enrollmentKey] = updatedEnrollment;
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<Enrollment> CreateOrUpdateEnrollment(
            string learnerCode,
            Course course,
            CourseVersion activeVersion,
            int? assignmentRuleId = null,
            DateTime? startDate = null,
            DateTime? dueDate = null,
            bool forceReset = false,
            Enrollment? existingEnrollment = null,
            List<EnrollmentAssignment>? existingLinks = null)
        {
            var existing = existingEnrollment;
            existingLinks ??= [];

            if (existing == null)
            {
                var existingEnrollments = await _enrollmentRepo.GetAsync(e =>
                    e.LearnerCode == learnerCode &&
                    e.CourseId == course.Id);

                existing = existingEnrollments.FirstOrDefault();
            }

            if (existing == null)
            {
                existing = new Enrollment
                {
                    LearnerCode           = learnerCode,
                    CourseId              = course.Id,
                    EnrolledCourseVersion = activeVersion.Id,
                    IsCompleted           = false,
                    StartDate             = startDate,
                    DueDate               = dueDate
                };
                await _enrollmentRepo.AddWithoutSaveAsync(existing);
            }
            else if (forceReset || existing.EnrolledCourseVersion != activeVersion.Id)
            {
                if (existing.IsCompleted)
                {
                    if (existingLinks.Count == 0 && existing.Id > 0)
                    {
                        existingLinks = (await _enrollmentAssignmentRepo.GetAsync(ea => ea.EnrollmentId == existing.Id)).ToList();
                    }

                    foreach (var eaLink in existingLinks)
                    {
                        eaLink.SnapshotCompleted     = existing.IsCompleted;
                        eaLink.SnapshotCompletedDate = existing.CompletedDate;
                        eaLink.SnapshotProgress      = existing.Progress;
                    }
                }

                existing.ResetAt               = _dateTime.Now;
                existing.EnrolledCourseVersion = activeVersion.Id;
                existing.IsCompleted           = false;
                existing.CompletedDate         = null;
                existing.Progress              = 0;
                existing.TotalScore            = 0;
                existing.StartDate             = startDate ?? existing.StartDate;
                existing.DueDate               = dueDate   ?? existing.DueDate;
            }

            if (assignmentRuleId.HasValue)
            {
                var linkRepo = _enrollmentAssignmentRepo;
                var matchingLinks = existingLinks
                    .Where(ea => ea.AssignmentId == assignmentRuleId.Value)
                    .ToList();

                if (matchingLinks.Count == 0 && existing.Id > 0)
                {
                    matchingLinks = (await linkRepo.GetAsync(ea =>
                        ea.EnrollmentId == existing.Id &&
                        ea.AssignmentId == assignmentRuleId.Value)).ToList();
                }

                if (matchingLinks.Count == 0)
                {
                    var link = new EnrollmentAssignment
                    {
                        Enrollment = existing,
                        AssignmentId = assignmentRuleId.Value,
                        StartDate    = startDate,
                        DueDate      = dueDate
                    };

                    await linkRepo.AddWithoutSaveAsync(link);
                    existingLinks.Add(link);
                }
                else
                {
                    var link = matchingLinks.First();
                    link.StartDate = startDate ?? link.StartDate;
                    link.DueDate   = dueDate   ?? link.DueDate;
                }
            }

            return existing;
        }

        public async Task<List<AssignmentHistoryDto>> GetAssignmentHistoryAsync()
        {
            var assignments = await _assignmentRepo.GetAsync(includeProperties: "Course");

            var links = await _enrollmentAssignmentRepo.GetAsync(
                filter: null,
                includeProperties: "Enrollment"
            );

            var currentDate = _dateTime.Now;

            var groupedHistory = assignments
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo))
                .GroupBy(r => r.AssignmentNo)
                .Select(g =>
                {
                    var first         = g.First();
                    var assignmentIds = g.Select(a => a.Id).ToList();

                    var relatedLinks = links
                        .Where(ea => assignmentIds.Contains(ea.AssignmentId) && ea.Enrollment != null)
                        .ToList();

                    bool isCompleted = relatedLinks.Any()
                        && relatedLinks.All(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);

                    string status = AssignmentDashboardService.CalculateStatus(
                        relatedLinks.Any(), isCompleted, first.StartDate, first.DueDate, currentDate);

                    return new AssignmentHistoryDto
                    {
                        Id           = first.Id,
                        AssignmentNo = g.Key,
                        Description  = first.Description,
                        EmployeeCodes = first.EmployeeCodes,
                        StartDate    = first.StartDate,
                        DueDate      = first.DueDate,
                        CourseNames  = string.Join(", ", g.Select(c => c.Course?.Title ?? "Unknown Course").Distinct()),
                        Status       = status,
                        CreatedBy    = first.CreatedBy,
                        CreatedAt    = first.CreatedAt,
                        CourseCount  = g.Select(a => a.CourseId).Distinct().Count(),
                        LearnerCount = string.IsNullOrEmpty(first.EmployeeCodes)
                            ? 0
                            : first.EmployeeCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
                        CompletedEnrollmentCount = relatedLinks.Count(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted),
                        TotalEnrollmentCount     = relatedLinks.Count
                    };
                })
                .OrderByDescending(x => x.AssignmentNo)
                .ToList();

            return groupedHistory;
        }

        public async Task<AssignmentConflictDto> CheckAssignmentConflictsAsync(int courseId, List<string> employeeCodes, DateTime startDate, DateTime dueDate)
        {
            var validation = await _assignmentDashboardService.ValidateBeforeAssignAsync(new BulkAssignDto
            {
                CourseIds = [courseId],
                EmployeeCodes = employeeCodes,
                StartDate = startDate,
                DueDate = dueDate
            });

            var result = new AssignmentConflictDto
            {
                HasConflict = validation.InProgressConflicts.Count > 0 || validation.CompletedConflicts.Count > 0
            };

            if (!validation.Success)
            {
                result.HasConflict = true;
                if (!string.IsNullOrWhiteSpace(validation.Message))
                {
                    result.ConflictMessages.Add(validation.Message);
                }
                return result;
            }

            result.ValidEmployeeCodes = employeeCodes
                .Except(validation.InProgressConflicts.Select(x => x.LearnerCode))
                .Except(validation.CompletedConflicts.Select(x => x.LearnerCode))
                .Distinct()
                .ToList();

            result.ConflictMessages.AddRange(validation.InProgressConflicts
                .Select(x => $"Learner {x.LearnerCode} is already in progress for {x.CourseTitle}."));
            result.ConflictMessages.AddRange(validation.CompletedConflicts
                .Select(x => $"Learner {x.LearnerCode} has already completed {x.CourseTitle}."));

            return result;
        }

        private static List<string> NormalizeEmployeeCodes(IEnumerable<string> employeeCodes)
        {
            return employeeCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void ValidateAssignmentWindow(DateTime? startDate, DateTime? dueDate)
        {
            if (startDate.HasValue && dueDate.HasValue && startDate.Value > dueDate.Value)
                throw new ArgumentException("StartDate must be on or before DueDate.");
        }

        private async Task<CourseVersion?> GetActiveVersionAsync(int courseId)
        {
            var versions = await _versionRepo.GetAsync(v => v.CourseId == courseId && v.IsActive);
            return versions.FirstOrDefault();
        }

        private async Task<Dictionary<int, CourseVersion>> GetActiveVersionMapAsync(IEnumerable<int> courseIds)
        {
            var versions = await _versionRepo.GetAsync(v => courseIds.Contains(v.CourseId) && v.IsActive);
            return versions
                .GroupBy(v => v.CourseId)
                .Select(g => g.OrderByDescending(v => v.VersionNumber).First())
                .ToDictionary(v => v.CourseId);
        }

        private async Task<Dictionary<string, Enrollment>> GetExistingEnrollmentMapAsync(IEnumerable<int> courseIds, IEnumerable<string> employeeCodes)
        {
            var enrollments = await _enrollmentRepo.GetAsync(e =>
                courseIds.Contains(e.CourseId ?? 0) &&
                employeeCodes.Contains(e.LearnerCode));

            return enrollments.ToDictionary(e => BuildEnrollmentKey(e.LearnerCode, e.CourseId ?? 0));
        }

        private async Task<Dictionary<int, List<EnrollmentAssignment>>> GetExistingLinkMapAsync(IEnumerable<int> enrollmentIds)
        {
            var ids = enrollmentIds.Distinct().ToList();
            if (ids.Count == 0)
                return [];

            var links = await _enrollmentAssignmentRepo.GetAsync(ea => ids.Contains(ea.EnrollmentId));
            return links
                .GroupBy(ea => ea.EnrollmentId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private static string BuildEnrollmentKey(string learnerCode, int courseId)
        {
            return $"{learnerCode.Trim().ToUpperInvariant()}::{courseId}";
        }
    }
}