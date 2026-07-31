using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace iLearn.Application.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly ILearnerApiService _learnerApiService;
        private readonly IAssignmentBatchService _assignmentBatchService;
        private readonly ICourseAssignmentService _courseAssignmentService;
        private readonly IGenericRepository<LearnerGroupMember> _learnerGroupMemberRepo;
        private readonly IScormRuntimeStateService _scormRuntimeStateService;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;

        public AssignmentService(
            IGenericRepository<Assignment> assignmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<Course> courseRepo,
            ILearnerApiService learnerApiService,
            IAssignmentBatchService assignmentBatchService,
            ICourseAssignmentService courseAssignmentService,
            IGenericRepository<LearnerGroupMember> learnerGroupMemberRepo,
            IScormRuntimeStateService scormRuntimeStateService,
            IDateTime dateTime,
            IUnitOfWork unitOfWork)
        {
            _assignmentRepo = assignmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _enrollmentRepo = enrollmentRepo;
            _courseRepo = courseRepo;
            _learnerApiService = learnerApiService;
            _assignmentBatchService = assignmentBatchService;
            _courseAssignmentService = courseAssignmentService;
            _learnerGroupMemberRepo = learnerGroupMemberRepo;
            _scormRuntimeStateService = scormRuntimeStateService;
            _dateTime = dateTime;
            _unitOfWork = unitOfWork;
        }

        public async Task<AssignmentHistoryResponseDto> GetHistoryAsync(
            PaginationParams p,
            int? divisionId,
            DateTime currentDate,
            CancellationToken cancellationToken = default)
        {
            var history = await BuildAssignmentHistoryAsync(divisionId, currentDate, cancellationToken);
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

            return new AssignmentHistoryResponseDto
            {
                Data = paged,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Summary = summary,
            };
        }

        public async Task<List<AssignmentGanttTaskDto>> GetGanttTasksAsync(
            int? divisionId,
            DateTime currentDate,
            CancellationToken cancellationToken = default)
        {
            return await BuildGanttTasksAsync(divisionId, currentDate, cancellationToken);
        }

        public async Task<IReadOnlyList<AssignmentCourseReferenceDto>> GetByCourseAsync(
            int courseId,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var assignments = await _assignmentRepo.GetAsync(r =>
                r.CourseId == courseId && (!divisionId.HasValue || r.DivisionId == divisionId.Value));

            return assignments
                .Select(r => new AssignmentCourseReferenceDto { Id = r.Id, CourseId = r.CourseId })
                .ToList();
        }

        public async Task<AssignmentDashboardDto?> GetDashboardAsync(
            int assignmentId,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            return await BuildAssignmentDashboardAsync(assignmentId, divisionId, cancellationToken);
        }

        public async Task<int?> ResolveAssignmentIdByNoAsync(string assignmentNo, CancellationToken cancellationToken = default)
        {
            return await _assignmentRepo.GetQuery()
                .Where(a => !a.IsDeleted && a.AssignmentNo == assignmentNo)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<AssignmentReassignDataDto?> GetReassignDataAsync(
            int assignmentId,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var mainAssignment = await _assignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(a => a.Id == assignmentId && (!divisionId.HasValue || a.DivisionId == divisionId.Value))
                .Select(a => new
                {
                    a.AssignmentNo,
                    a.LearnerGroupId,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (mainAssignment == null)
            {
                return null;
            }

            var courseIds = await _assignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(a => (!divisionId.HasValue || a.DivisionId == divisionId.Value)
                    && (string.IsNullOrWhiteSpace(mainAssignment.AssignmentNo)
                        ? a.Id == assignmentId
                        : a.AssignmentNo == mainAssignment.AssignmentNo)
                    && a.CourseId.HasValue)
                .Join(_courseRepo.GetQuery().Where(c => !c.IsDeleted),
                    a => a.CourseId,
                    c => c.Id,
                    (_, c) => c.Id)
                .Distinct()
                .ToListAsync(cancellationToken);

            return new AssignmentReassignDataDto
            {
                CourseIds = courseIds,
                LearnerGroupId = mainAssignment.LearnerGroupId,
            };
        }

        public async Task DeleteAssignmentAsync(
            int assignmentId,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var assignment = await _assignmentRepo.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found");

            EnsureDivisionAccess(assignment.DivisionId, divisionId);

            var relatedRules = await _assignmentBatchService.LoadBatchAsync(assignment);
            var relatedIds = relatedRules.Select(r => r.Id).ToList();

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
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

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<AssignmentResetEnrollmentsResponseDto> ResetEnrollmentsAsync(
            int assignmentId,
            ResetEnrollmentsDto dto,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found");

            EnsureDivisionAccess(mainRule.DivisionId, divisionId);

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var targetRuleIds = batchRules.Select(r => r.Id).ToList();

            if (dto.RuleIds is { Count: > 0 })
            {
                targetRuleIds = targetRuleIds.Intersect(dto.RuleIds).ToList();
            }

            if (targetRuleIds.Count == 0)
            {
                throw new ArgumentException("No matching courses found in this assignment.");
            }

            var normalizedLearnerCodes = dto.LearnerCodes is { Count: > 0 }
                ? NormalizeLearnerCodes(dto.LearnerCodes)
                : null;

            var linksQuery = _enrollmentAssignmentRepo.GetQuery()
                .Where(ea => targetRuleIds.Contains(ea.AssignmentId)
                    && !ea.IsDeleted
                    && ea.Enrollment != null);

            if (normalizedLearnerCodes != null)
            {
                linksQuery = linksQuery.Where(ea => normalizedLearnerCodes.Contains(ea.Enrollment!.LearnerCode));
            }

            var enrollmentIds = await linksQuery
                .Select(ea => ea.EnrollmentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (enrollmentIds.Count == 0)
            {
                throw new ArgumentException("No enrollments found matching the selected criteria.");
            }

            var courseIds = batchRules
                .Where(r => targetRuleIds.Contains(r.Id) && r.CourseId.HasValue)
                .Select(r => r.CourseId!.Value)
                .ToList();

            var enrollments = await _enrollmentRepo.GetQuery()
                .Where(e => enrollmentIds.Contains(e.Id)
                    && !e.IsDeleted
                    && (courseIds.Count == 0 || courseIds.Contains(e.CourseId ?? 0)))
                .ToListAsync(cancellationToken);

            if (enrollments.Count == 0)
            {
                throw new ArgumentException("No enrollments found matching the selected criteria.");
            }

            var now = _dateTime.Now;
            var resetCount = 0;

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var enrollmentIdsToReset = enrollments.Select(e => e.Id).ToList();
                var links = await _enrollmentAssignmentRepo.GetQuery()
                    .Where(ea => targetRuleIds.Contains(ea.AssignmentId)
                        && enrollmentIdsToReset.Contains(ea.EnrollmentId)
                        && !ea.IsDeleted)
                    .ToListAsync(cancellationToken);

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

                await _scormRuntimeStateService.ClearForEnrollmentsAsync(
                    enrollmentIdsToReset,
                    saveChanges: false,
                    cancellationToken: cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return new AssignmentResetEnrollmentsResponseDto
            {
                Success = true,
                Message = $"Successfully reset {resetCount} enrollment(s).",
                ResetCount = resetCount,
            };
        }

        public async Task<AssignmentExtendDueDateResponseDto> ExtendDueDateAsync(
            int assignmentId,
            DateTime newDueDate,
            CancellationToken cancellationToken = default)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found");

            if (mainRule.StartDate.HasValue && newDueDate <= mainRule.StartDate.Value)
            {
                throw new ArgumentException("Due date must be after the start date.");
            }

            var allRules = await _assignmentBatchService.LoadBatchAsync(mainRule);

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var rule in allRules)
                {
                    rule.DueDate = newDueDate;
                }

                var ruleIds = allRules.Select(r => r.Id).ToList();
                var activeLinks = await _enrollmentAssignmentRepo.GetAsync(
                    ea => ruleIds.Contains(ea.AssignmentId),
                    includeProperties: "Enrollment");

                foreach (var link in activeLinks.Where(ea => ea.Enrollment != null && !(ea.SnapshotCompleted || ea.Enrollment.IsCompleted)))
                {
                    link.DueDate = newDueDate;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return new AssignmentExtendDueDateResponseDto
            {
                Success = true,
                Message = "Due date extended successfully.",
                NewDueDate = newDueDate,
            };
        }

        public async Task<AssignmentActionResponseDto> UpdateDescriptionAsync(
            int assignmentId,
            UpdateAssignmentDescriptionDto dto,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found");

            if (divisionId.HasValue && mainRule.DivisionId != divisionId.Value)
            {
                throw new KeyNotFoundException("Assignment not found");
            }

            var allRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var normalizedDescription = dto.Description?.Trim() ?? string.Empty;

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var rule in allRules)
                {
                    rule.Description = normalizedDescription;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return new AssignmentActionResponseDto
            {
                Success = true,
                Message = "Description updated successfully.",
            };
        }

        public async Task<AssignmentMutationResponseDto> AddCoursesToAssignmentAsync(
            int assignmentId,
            ManageAssignmentCoursesDto dto,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found");

            EnsureDivisionAccess(mainRule.DivisionId, divisionId);

            var requestedCourseIds = dto.CourseIds?
                .Distinct()
                .ToList() ?? [];

            if (requestedCourseIds.Count == 0)
            {
                throw new ArgumentException("At least one course is required.");
            }

            var accessibleCourses = await GetAccessibleCoursesAsync(requestedCourseIds, divisionId);
            if (HasUnauthorizedCourses(requestedCourseIds, accessibleCourses))
            {
                throw new UnauthorizedAccessException("Forbidden.");
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
                return new AssignmentMutationResponseDto
                {
                    Success = true,
                    Message = "No new courses were added.",
                    AddedCount = 0,
                };
            }

            var learnerCodes = await GetBatchLearnerCodesAsync(batchRules.Select(rule => rule.Id).ToList(), batchRules, cancellationToken);
            var employeeCodesText = string.Join(",", learnerCodes);

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
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
                    DivisionId = mainRule.DivisionId,
                }).ToList();

                await _unitOfWork.AddRangeAsync(newRules, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return new AssignmentMutationResponseDto
            {
                Success = true,
                Message = "Courses added successfully.",
                AddedCount = newCourseIds.Count,
            };
        }

        public async Task<AssignmentRemoveCourseResponseDto> RemoveCourseFromAssignmentAsync(
            int assignmentId,
            int ruleId,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found");

            EnsureDivisionAccess(mainRule.DivisionId, divisionId);

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var targetRule = batchRules.FirstOrDefault(rule => rule.Id == ruleId)
                ?? throw new KeyNotFoundException("Assigned course not found.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var links = await _enrollmentAssignmentRepo.GetAsync(
                    link => link.AssignmentId == targetRule.Id,
                    includeProperties: "Enrollment");

                foreach (var link in links)
                {
                    link.IsDeleted = true;
                    link.DeletedAt = _dateTime.Now;
                }

                targetRule.IsDeleted = true;
                targetRule.DeletedAt = _dateTime.Now;

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            var remainingRules = batchRules.Count(rule => rule.Id != targetRule.Id);
            return new AssignmentRemoveCourseResponseDto
            {
                Success = true,
                Message = "Assigned course removed successfully.",
                AssignmentDeleted = remainingRules == 0,
            };
        }

        public async Task<AssignmentMutationResponseDto> AddLearnersToAssignmentAsync(
            int assignmentId,
            ManageAssignmentLearnersDto dto,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found");

            EnsureDivisionAccess(mainRule.DivisionId, divisionId);

            var requestedLearnerCodes = NormalizeLearnerCodes(dto.EmployeeCodes);
            if (requestedLearnerCodes.Count == 0)
            {
                throw new ArgumentException("At least one learner is required.");
            }

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var ruleIds = batchRules.Select(rule => rule.Id).ToList();
            var currentLearnerCodes = await GetBatchLearnerCodesAsync(ruleIds, batchRules, cancellationToken);
            var newLearnerCodes = requestedLearnerCodes
                .Except(currentLearnerCodes, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (newLearnerCodes.Count == 0)
            {
                return new AssignmentMutationResponseDto
                {
                    Success = true,
                    Message = "No new learners were added.",
                    AddedCount = 0,
                };
            }

            var updatedLearnerCodes = currentLearnerCodes
                .Union(newLearnerCodes, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
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

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return new AssignmentMutationResponseDto
            {
                Success = true,
                Message = "Learners added successfully.",
                AddedCount = newLearnerCodes.Count,
            };
        }

        public async Task<AssignmentActionResponseDto> RemoveLearnerFromAssignmentAsync(
            int assignmentId,
            string learnerCode,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var normalizedLearnerCode = learnerCode?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedLearnerCode))
            {
                throw new ArgumentException("Learner code is required.");
            }

            var result = await RemoveLearnersFromAssignmentAsync(
                assignmentId,
                new ManageAssignmentLearnersDto { EmployeeCodes = [normalizedLearnerCode] },
                divisionId,
                cancellationToken);

            return new AssignmentActionResponseDto
            {
                Success = result.Success,
                Message = "Learner removed successfully.",
            };
        }

        public async Task<AssignmentRemoveLearnersResponseDto> RemoveLearnersFromAssignmentAsync(
            int assignmentId,
            ManageAssignmentLearnersDto dto,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var mainRule = await _assignmentRepo.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found");

            EnsureDivisionAccess(mainRule.DivisionId, divisionId);

            var requestedLearnerCodes = NormalizeLearnerCodes(dto.EmployeeCodes);
            if (requestedLearnerCodes.Count == 0)
            {
                throw new ArgumentException("At least one learner is required.");
            }

            var batchRules = await _assignmentBatchService.LoadBatchAsync(mainRule);
            var ruleIds = batchRules.Select(rule => rule.Id).ToList();
            var currentLearnerCodes = await GetBatchLearnerCodesAsync(ruleIds, batchRules, cancellationToken);

            var codesToRemove = currentLearnerCodes
                .Where(code => requestedLearnerCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (codesToRemove.Count == 0)
            {
                throw new KeyNotFoundException("None of the selected learners are assigned to this assignment.");
            }

            var remainingLearnerCodes = currentLearnerCodes
                .Where(code => !codesToRemove.Contains(code, StringComparer.OrdinalIgnoreCase))
                .ToList();

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var links = await _enrollmentAssignmentRepo.GetAsync(
                    link => ruleIds.Contains(link.AssignmentId)
                        && link.Enrollment != null
                        && codesToRemove.Contains(link.Enrollment.LearnerCode),
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

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return new AssignmentRemoveLearnersResponseDto
            {
                Success = true,
                Message = $"Removed {codesToRemove.Count} learner(s) from this assignment.",
                RemovedCount = codesToRemove.Count,
            };
        }

        public async Task<IReadOnlyList<Course>> GetAccessibleCoursesAsync(
            IEnumerable<int> courseIds,
            int? divisionId,
            bool includeCourseType = false)
        {
            var targetCourseIds = courseIds.Distinct().ToList();
            var includeProperties = includeCourseType ? "Category,CourseType" : "Category";

            return await _courseRepo.GetAsync(
                c => c.Status == CourseStatus.Open
                    && (!targetCourseIds.Any() || targetCourseIds.Contains(c.Id))
                    && (!divisionId.HasValue || c.Category != null && c.Category.DivisionId == divisionId.Value),
                includeProperties: includeProperties);
        }

        public bool HasUnauthorizedCourses(IEnumerable<int> requestedCourseIds, IEnumerable<Course> accessibleCourses)
        {
            var accessibleCourseIds = accessibleCourses
                .Select(c => c.Id)
                .Distinct()
                .ToHashSet();

            return requestedCourseIds.Any(courseId => !accessibleCourseIds.Contains(courseId));
        }

        public async Task<List<string>> GetBatchLearnerCodesAsync(
            List<int> ruleIds,
            IEnumerable<Assignment> batchRules,
            CancellationToken cancellationToken = default)
        {
            var learnerCodesFromLinks = await _enrollmentAssignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(link => ruleIds.Contains(link.AssignmentId) && !link.IsDeleted && link.Enrollment != null)
                .Select(link => link.Enrollment!.LearnerCode)
                .Distinct()
                .ToListAsync(cancellationToken);

            var learnerCodesFromRules = batchRules
                .SelectMany(rule => (rule.EmployeeCodes ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToList();

            return learnerCodesFromLinks
                .Concat(learnerCodesFromRules)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public List<string> NormalizeLearnerCodes(IEnumerable<string>? learnerCodes)
        {
            return learnerCodes?
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }

        private static void EnsureDivisionAccess(int? resourceDivisionId, int? userDivisionId)
        {
            if (userDivisionId.HasValue && resourceDivisionId != userDivisionId.Value)
            {
                throw new UnauthorizedAccessException("Forbidden.");
            }
        }

        private async Task<List<AssignmentHistoryDto>> BuildAssignmentHistoryAsync(
            int? divisionId,
            DateTime currentDate,
            CancellationToken cancellationToken = default)
        {
            var assignmentQuery = _assignmentRepo.GetQuery()
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
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync(cancellationToken);

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
                : await _courseRepo.GetQuery()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(c => courseIds.Contains(c.Id))
                    .Select(c => new AssignmentHistoryCourseRow
                    {
                        Id = c.Id,
                        Title = c.Title,
                        IsDeleted = c.IsDeleted,
                    })
                    .ToDictionaryAsync(c => c.Id, cancellationToken);

            var assignmentIdsQuery = assignmentQuery.Select(a => a.Id);

            var linkRows = await _enrollmentAssignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(ea => assignmentIdsQuery.Contains(ea.AssignmentId))
                .Select(ea => new AssignmentHistoryLinkRow
                {
                    AssignmentId = ea.AssignmentId,
                    LearnerCode = ea.Enrollment != null ? ea.Enrollment.LearnerCode : null,
                    IsCompleted = ea.SnapshotCompleted || (ea.Enrollment != null && ea.Enrollment.IsCompleted),
                })
                .Where(ea => ea.LearnerCode != null)
                .ToListAsync(cancellationToken);

            var linksByAssignmentId = linkRows.ToLookup(link => link.AssignmentId);

            return assignmentRows
                .GroupBy(row => string.IsNullOrWhiteSpace(row.AssignmentNo) ? $"assignment:{row.Id}" : row.AssignmentNo!)
                .Select(group => MapHistoryDto(group, linksByAssignmentId, currentDate, courseMap))
                .ToList();
        }

        private async Task<AssignmentDashboardDto?> BuildAssignmentDashboardAsync(
            int assignmentId,
            int? divisionId,
            CancellationToken cancellationToken = default)
        {
            var mainAssignment = await _assignmentRepo.GetQuery()
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
                    LearnerGroupId = a.LearnerGroupId,
                    LearnerGroupName = a.LearnerGroup != null ? a.LearnerGroup.Name : null,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (mainAssignment == null)
            {
                return null;
            }

            var assignmentRows = await _assignmentRepo.GetQuery()
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
                    LearnerGroupId = a.LearnerGroupId,
                    LearnerGroupName = a.LearnerGroup != null ? a.LearnerGroup.Name : null,
                })
                .ToListAsync(cancellationToken);

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
                : await _courseRepo.GetQuery()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(course => courseIds.Contains(course.Id))
                    .Select(course => new AssignmentHistoryCourseRow
                    {
                        Id = course.Id,
                        Title = course.Title,
                        IsDeleted = course.IsDeleted,
                        Code = course.Code,
                    })
                    .ToDictionaryAsync(course => course.Id, cancellationToken);

            var ruleIds = assignmentRows.Select(row => row.Id).ToList();

            var learnerRows = await _enrollmentAssignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(link => ruleIds.Contains(link.AssignmentId) && link.Enrollment != null)
                .Select(link => new DashboardLearnerRow
                {
                    AssignmentId = link.AssignmentId,
                    LearnerCode = link.Enrollment!.LearnerCode,
                    Progress = link.SnapshotCompleted ? link.SnapshotProgress : link.Enrollment.Progress,
                    IsCompleted = link.SnapshotCompleted || link.Enrollment.IsCompleted,
                    CompletedDate = link.SnapshotCompleted ? link.SnapshotCompletedDate : link.Enrollment.CompletedDate,
                    StartDate = link.StartDate,
                    DueDate = link.DueDate,
                })
                .ToListAsync(cancellationToken);

            var uniqueLearnerCodes = learnerRows
                .Select(row => row.LearnerCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var learnerNames = uniqueLearnerCodes.Count == 0
                ? new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase)
                : await _learnerApiService.GetLearnersByCodesAsync(uniqueLearnerCodes);

            var learnersByCode = learnerRows
                .GroupBy(row => row.LearnerCode)
                .Select(group => new
                {
                    LearnerCode = group.Key,
                    AllCompleted = group.All(row => row.IsCompleted),
                    AnyStarted = group.Any(row => row.IsCompleted || row.Progress > 0),
                })
                .ToList();

            var completedCount = learnersByCode.Count(item => item.AllCompleted);
            var inProgressCount = learnersByCode.Count(item => !item.AllCompleted && item.AnyStarted);
            var notStartedCount = learnersByCode.Count(item => !item.AllCompleted && !item.AnyStarted);
            var totalEnrollments = learnerRows.Count;
            var completedEnrollments = learnerRows.Count(row => row.IsCompleted);
            var completionRate = totalEnrollments == 0
                ? 0
                : Math.Round((double)completedEnrollments / totalEnrollments * 100);

            var learnerCountByRule = learnerRows
                .GroupBy(row => row.AssignmentId)
                .ToDictionary(group => group.Key, group => group.Count());

            var completedCountByRule = learnerRows
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
                        CompletedLearners = completedCountByRule.GetValueOrDefault(row.Id),
                        TotalLearners = learnerCountByRule.GetValueOrDefault(row.Id),
                        IsCourseDeleted = course?.IsDeleted ?? false,
                    };
                })
                .ToList();

            var now = _dateTime.Now;
            var learners = learnerRows
                .Select(row =>
                {
                    var assignment = assignmentRows.FirstOrDefault(item => item.Id == row.AssignmentId);
                    AssignmentHistoryCourseRow? course = null;
                    if (assignment?.CourseId.HasValue == true)
                    {
                        courseMap.TryGetValue(assignment.CourseId.Value, out course);
                    }

                    var status = AssignmentStatusKeys.GetScheduledLearnerStatus(
                        row.IsCompleted, row.Progress, row.StartDate, row.DueDate, now);
                    var learnerInfo = learnerNames.GetValueOrDefault(row.LearnerCode);
                    var learnerGroups = assignment?.LearnerGroupId.HasValue == true
                        && !string.IsNullOrWhiteSpace(assignment.LearnerGroupName)
                        ? new List<string> { assignment.LearnerGroupName }
                        : new List<string>();
                    return new LearnerProgressDto
                    {
                        LearnerCode = row.LearnerCode,
                        LearnerName = learnerInfo?.Name ?? row.LearnerCode,
                        Division = learnerInfo?.Division,
                        Department = learnerInfo?.Department,
                        LearnerGroups = learnerGroups,
                        AssignmentRuleId = row.AssignmentId,
                        CourseCode = course?.Code ?? "-",
                        CourseTitle = course?.Title ?? "Unknown Course",
                        Progress = row.Progress,
                        IsCompleted = row.IsCompleted,
                        Status = status,
                        CompletedDate = row.CompletedDate,
                        StartDate = row.StartDate,
                        DueDate = row.DueDate,
                    };
                })
                .ToList();

            var createdByName = await LookupCreatedByNameAsync(mainAssignment.CreatedBy);

            return new AssignmentDashboardDto
            {
                AssignmentNo = mainAssignment.AssignmentNo ?? string.Empty,
                Description = mainAssignment.Description ?? string.Empty,
                CreatedBy = mainAssignment.CreatedBy,
                CreatedByName = createdByName,
                StartDate = mainAssignment.StartDate,
                DueDate = mainAssignment.DueDate,
                TotalEmployees = learnersByCode.Count,
                TotalCourses = courseSummaries.Count,
                CompletionRate = completionRate,
                LearnerGroupId = mainAssignment.LearnerGroupId,
                LearnerGroupName = mainAssignment.LearnerGroupName,
                HasDeletedCourse = courseSummaries.Any(course => course.IsCourseDeleted),
                ChartData = new DashboardChartDto
                {
                    Completed = completedCount,
                    InProgress = inProgressCount,
                    NotStarted = notStartedCount,
                },
                Courses = courseSummaries,
                Learners = learners,
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

        private async Task<List<AssignmentGanttTaskDto>> BuildGanttTasksAsync(
            int? divisionId,
            DateTime currentDate,
            CancellationToken cancellationToken = default)
        {
            var assignmentRows = await _assignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(a => !divisionId.HasValue || a.DivisionId == divisionId.Value)
                .Select(a => new AssignmentHistoryAssignmentRow
                {
                    Id = a.Id,
                    AssignmentNo = a.AssignmentNo,
                    Description = a.Description,
                    StartDate = a.StartDate,
                    DueDate = a.DueDate,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            if (assignmentRows.Count == 0)
            {
                return [];
            }

            var assignmentIds = assignmentRows.Select(item => item.Id).ToList();

            var linkRows = await _enrollmentAssignmentRepo.GetQuery()
                .AsNoTracking()
                .Where(ea => assignmentIds.Contains(ea.AssignmentId) && ea.Enrollment != null)
                .Select(ea => new GanttLinkRow
                {
                    AssignmentId = ea.AssignmentId,
                    IsCompleted = ea.SnapshotCompleted || ea.Enrollment!.IsCompleted,
                })
                .ToListAsync(cancellationToken);

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
                "learnerCount" => item => item.LearnerCount,
                "progress" or "completedEnrollmentCount" => item => item.TotalEnrollmentCount > 0
                    ? Math.Round((double)item.CompletedEnrollmentCount / item.TotalEnrollmentCount * 100)
                    : 0,
                "startDate" => item => item.StartDate,
                "dueDate" => item => item.DueDate,
                "status" => item => item.Status,
                _ => item => item.AssignmentNo,
            };

            return sortDescending
                ? history.OrderByDescending(keySelector).ThenByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
                : history.OrderBy(keySelector).ThenByDescending(item => item.CreatedAt).ThenBy(item => item.Id);
        }

        private static AssignmentHistorySummaryDto BuildHistorySummary(IEnumerable<AssignmentHistoryDto> history)
        {
            var rows = history.ToList();
            return new AssignmentHistorySummaryDto
            {
                All = rows.Count,
                InProgress = rows.Count(item => item.Status == "InProgress"),
                Upcoming = rows.Count(item => item.Status == "Upcoming"),
                Expired = rows.Count(item => item.Status == "Expired"),
                Completed = rows.Count(item => item.Status == "Completed"),
            };
        }

        private static AssignmentHistoryDto MapHistoryDto(
            IGrouping<string, AssignmentHistoryAssignmentRow> group,
            ILookup<int, AssignmentHistoryLinkRow> linksByAssignmentId,
            DateTime currentDate,
            IReadOnlyDictionary<int, AssignmentHistoryCourseRow> courseMap)
        {
            var first = group.OrderBy(item => item.Id).First();
            var relatedLinks = group
                .SelectMany(item => linksByAssignmentId[item.Id])
                .Where(link => !string.IsNullOrWhiteSpace(link.LearnerCode))
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
                LearnerCount = string.IsNullOrWhiteSpace(first.EmployeeCodes)
                    ? 0
                    : first.EmployeeCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
                CompletedEnrollmentCount = relatedLinks.Count(link => link.IsCompleted),
                TotalEnrollmentCount = relatedLinks.Count,
                HasDeletedCourse = deletedCourses.Count > 0,
                DeletedCourseNames = deletedCourses.Count > 0
                    ? string.Join(", ", deletedCourses.Select(course => course.Title ?? "Unknown"))
                    : null,
            };
        }

        private static AssignmentGanttTaskDto MapGanttTask(
            IGrouping<string, AssignmentHistoryAssignmentRow> group,
            ILookup<int, GanttLinkRow> linksByAssignmentId,
            DateTime currentDate)
        {
            var first = group.OrderBy(item => item.Id).First();
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

            var startDate = group.Min(item => item.StartDate ?? item.CreatedAt);
            var maxDueDate = group
                .Where(item => item.DueDate.HasValue)
                .Select(item => item.DueDate)
                .Max();
            var dueDate = maxDueDate ?? startDate.AddDays(7);
            if (dueDate <= startDate)
            {
                dueDate = startDate.AddDays(1);
            }

            var assignmentNo = string.IsNullOrWhiteSpace(first.AssignmentNo)
                ? $"Assignment {first.Id}"
                : first.AssignmentNo!;
            var title = string.IsNullOrWhiteSpace(first.Description)
                ? assignmentNo
                : first.Description.Trim();

            return new AssignmentGanttTaskDto
            {
                Id = first.Id,
                ParentId = 0,
                AssignmentNo = assignmentNo,
                Title = title,
                StartDate = startDate,
                DueDate = dueDate,
                Progress = progress,
                Color = GetStatusColor(status),
                Status = status,
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
                _ => "#aaaaaa",
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
            public string? LearnerCode { get; set; }
            public bool IsCompleted { get; set; }
        }

        private sealed class GanttLinkRow
        {
            public int AssignmentId { get; set; }
            public bool IsCompleted { get; set; }
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
            public int? LearnerGroupId { get; set; }
            public string? LearnerGroupName { get; set; }
        }

        private sealed class DashboardLearnerRow
        {
            public int AssignmentId { get; set; }
            public string LearnerCode { get; set; } = string.Empty;
            public double Progress { get; set; }
            public bool IsCompleted { get; set; }
            public DateTime? CompletedDate { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
        }
    }
}
