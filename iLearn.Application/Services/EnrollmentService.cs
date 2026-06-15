using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly ICourseAssignmentService _courseAssignmentService;
        private readonly IAssignmentDashboardService _assignmentDashboardService;
        private readonly ICurrentUserService _currentUser;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly ILearnerGroupService _learnerGroupService;
        private readonly IAssignmentNoGenerator _assignmentNoGen;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;

        public EnrollmentService(
            IGenericRepository<Enrollment> enrollmentRepo,
            ICourseAssignmentService courseAssignmentService,
            IAssignmentDashboardService assignmentDashboardService,
            ICurrentUserService currentUser,
            IGenericRepository<Course> courseRepo,
            ILearnerGroupService learnerGroupService,
            IAssignmentNoGenerator assignmentNoGen,
            IDateTime dateTime,
            IUnitOfWork unitOfWork)
        {
            _enrollmentRepo = enrollmentRepo;
            _courseAssignmentService = courseAssignmentService;
            _assignmentDashboardService = assignmentDashboardService;
            _currentUser = currentUser;
            _courseRepo = courseRepo;
            _learnerGroupService = learnerGroupService;
            _assignmentNoGen = assignmentNoGen;
            _dateTime = dateTime;
            _unitOfWork = unitOfWork;
        }

        public async Task<EnrollmentDto?> ResetStatusAsync(int enrollmentId)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(enrollmentId);
            if (enrollment == null)
                return null;

            // Reset enrollment summary and set ResetAt while preserving history logs.
            enrollment.IsCompleted = false;
            enrollment.CompletedDate = null;
            enrollment.Progress = 0;
            enrollment.ResetAt = _dateTime.Now;

            await _enrollmentRepo.UpdateAsync(enrollment);
            return enrollment.ToDto();
        }

        public async Task<EnrollmentDto?> GetByIdAsync(int enrollmentId)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(enrollmentId);
            if (enrollment == null) return null;
            return enrollment.ToDto();
        }

        public async Task<EnrollmentDto?> UpdateCompletionAsync(int enrollmentId, bool isComplete)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(enrollmentId);
            if (enrollment == null) return null;

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
            return enrollment.ToDto();
        }

        public async Task<BulkAssignResultDto> BulkAssignAsync(BulkAssignDto dto)
        {
            dto.EmployeeCodes = await ResolveEmployeeCodesAsync(dto);

            if (dto.CourseIds == null || !dto.CourseIds.Any() || dto.EmployeeCodes == null || !dto.EmployeeCodes.Any())
            {
                return new BulkAssignResultDto
                {
                    Success = false,
                    ErrorMessage = "Courses and Employees are required.",
                    ErrorType = "BadRequest"
                };
            }

            var accessibleCourses = await GetAccessibleCoursesAsync(dto.CourseIds);
            if (HasUnauthorizedCourses(dto.CourseIds, accessibleCourses))
            {
                return new BulkAssignResultDto
                {
                    Success = false,
                    ErrorType = "Forbid"
                };
            }

            var validation = await _assignmentDashboardService.ValidateBeforeAssignAsync(dto);
            if (!validation.Success)
            {
                return new BulkAssignResultDto
                {
                    Success = false,
                    ErrorMessage = validation.Message,
                    ErrorType = "BadRequest"
                };
            }

            if (validation.InProgressConflicts.Count > 0 && !dto.ConfirmReassignInProgress)
            {
                return new BulkAssignResultDto
                {
                    Success = false,
                    ErrorMessage = "Confirmation is required before resetting learners with in-progress assignments.",
                    ErrorType = "Conflict",
                    InProgressConflicts = validation.InProgressConflicts,
                    CompletedConflicts = validation.CompletedConflicts
                };
            }

            if (validation.CompletedConflicts.Count > 0 && !dto.ConfirmReassignCompleted)
            {
                return new BulkAssignResultDto
                {
                    Success = false,
                    ErrorMessage = "Confirmation is required before reassigning learners who already completed the course.",
                    ErrorType = "Conflict",
                    InProgressConflicts = validation.InProgressConflicts,
                    CompletedConflicts = validation.CompletedConflicts
                };
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                string assignmentNo = await _assignmentNoGen.NextAsync();
                string employeesStr = string.Join(",", dto.EmployeeCodes);

                var rules = dto.CourseIds.Select(courseId => new Assignment
                {
                    AssignmentNo = assignmentNo,
                    Description = dto.Description,
                    CourseId = courseId,
                    EmployeeCodes = employeesStr,
                    StartDate = dto.StartDate,
                    DueDate = dto.DueDate,
                    Division = dto.Division,
                    LearnerGroupId = dto.GroupId,
                    DivisionId = _currentUser.DivisionId
                }).ToList();

                await _unitOfWork.AddRangeAsync(rules);
                await _unitOfWork.SaveChangesAsync();

                var assignmentRuleIdsByCourseId = rules
                    .Where(rule => rule.CourseId.HasValue)
                    .ToDictionary(rule => rule.CourseId!.Value, rule => rule.Id);

                await _courseAssignmentService.AssignCoursesToEmployees(
                    assignmentRuleIdsByCourseId,
                    dto.EmployeeCodes,
                    dto.StartDate,
                    dto.DueDate,
                    forceReset: true);

                await transaction.CommitAsync();

                return new BulkAssignResultDto
                {
                    Success = true,
                    AssignmentNo = assignmentNo,
                    AssignmentId = rules.FirstOrDefault()?.Id ?? 0
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<List<string>> ResolveEmployeeCodesAsync(BulkAssignDto dto)
        {
            if (!dto.GroupId.HasValue || dto.EmployeeCodes.Count > 0)
                return dto.EmployeeCodes;

            return await _learnerGroupService.GetLearnerCodesAsync(dto.GroupId.Value);
        }

        private async Task<IReadOnlyList<Course>> GetAccessibleCoursesAsync(IEnumerable<int> courseIds)
        {
            var targetCourseIds = courseIds.Distinct().ToList();
            var courses = await _courseRepo.GetAsync(
                c => targetCourseIds.Contains(c.Id)
                    && c.Status == CourseStatus.Open
                    && (!_currentUser.DivisionId.HasValue || c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value),
                includeProperties: "Category,Versions.CourseContentItems.ContentItem"
            );

            return courses
                .Where(CourseContentReadiness.HasReadyActiveVersion)
                .ToList();
        }

        private static bool HasUnauthorizedCourses(IEnumerable<int> requestedCourseIds, IEnumerable<Course> accessibleCourses)
        {
            var accessibleCourseIds = accessibleCourses
                .Select(c => c.Id)
                .Distinct()
                .ToHashSet();

            return requestedCourseIds.Any(courseId => !accessibleCourseIds.Contains(courseId));
        }
    }
}
