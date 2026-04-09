using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class StudentGroupService : IStudentGroupService
    {
        private readonly IGenericRepository<StudentGroup> _groupRepo;
        private readonly IGenericRepository<StudentGroupMember> _memberRepo;
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly Lazy<ICourseAssignmentService> _courseAssignmentService;
        private readonly IAssignmentBatchService _assignmentBatchService;
        private readonly IStudentApiService _studentApiService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;

        private static readonly HashSet<string> AllowedAssignmentStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Completed",
            "InProgress",
            "Upcoming",
            "Expired"
        };

        public StudentGroupService(
            IGenericRepository<StudentGroup> groupRepo,
            IGenericRepository<StudentGroupMember> memberRepo,
            IGenericRepository<Assignment> assignmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            Lazy<ICourseAssignmentService> courseAssignmentService,
            IAssignmentBatchService assignmentBatchService,
            IStudentApiService studentApiService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            IUnitOfWork unitOfWork)
        {
            _groupRepo = groupRepo;
            _memberRepo = memberRepo;
            _assignmentRepo = assignmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _courseAssignmentService = courseAssignmentService;
            _assignmentBatchService = assignmentBatchService;
            _studentApiService = studentApiService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<StudentGroupDto>> GetAllAsync()
        {
            // 💡 Data Isolation: กรอง DivisionId ตั้งแต่ระดับ Query
            var groups = _currentUser.DivisionId.HasValue
                ? await _groupRepo.GetAsync(
                    filter: g => g.DivisionId == _currentUser.DivisionId.Value,
                    includeProperties: "Members")
                : await _groupRepo.GetAsync(includeProperties: "Members");

            return groups.Select(g => new StudentGroupDto
            {
                Id          = g.Id,
                Name        = g.Name,
                Description = g.Description,
                MemberCount = g.Members.Count,
                DivisionId  = g.DivisionId,    // 🆕 ส่ง DivisionId ออกไปด้วย
                CreatedAt   = g.CreatedAt,
                CreatedBy   = g.CreatedBy
            }).ToList();
        }

        public async Task<StudentGroupDetailDto?> GetByIdAsync(int id)
        {
            var groups = await _groupRepo.GetAsync(
                filter: g => g.Id == id,
                includeProperties: "Members"
            );
            var group = groups.FirstOrDefault();
            if (group == null) return null;

            // 💡 Data Isolation: ตรวจสอบว่า group เป็นของ Division ตัวเอง
            if (_currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
                return null;

            // ⚡ Bulk lookup: 1 HTTP call แทน N calls
            var codes      = group.Members.Select(m => m.StudentCode);
            var profileMap = await _studentApiService.GetStudentsByCodesAsync(codes);

            var memberDtos = group.Members.Select(m =>
            {
                profileMap.TryGetValue(m.StudentCode, out var s);
                return new StudentGroupMemberDto
                {
                    Id          = m.Id,
                    StudentCode = m.StudentCode,
                    StudentName = s?.Name       ?? m.StudentCode,
                    Division    = s?.Division,
                    Department  = s?.Department,
                    Section     = s?.Section,
                    Position    = s?.Position
                };
            }).ToList();

            return new StudentGroupDetailDto
            {
                Id          = group.Id,
                Name        = group.Name,
                Description = group.Description,
                CreatedBy   = group.CreatedBy,
                Members     = memberDtos
            };
        }

        public async Task<StudentGroupDto> CreateAsync(CreateStudentGroupDto dto)
        {
            var group = new StudentGroup
            {
                Name = dto.Name,
                Description = dto.Description,
                DivisionId = _currentUser.DivisionId
            };
            var created = await _groupRepo.AddAsync(group);

            foreach (var code in dto.StudentCodes.Distinct())
            {
                await _memberRepo.AddAsync(new StudentGroupMember
                {
                    StudentGroupId = created.Id,
                    StudentCode    = code
                });
            }

            return new StudentGroupDto
            {
                Id          = created.Id,
                Name        = created.Name,
                Description = created.Description,
                MemberCount = dto.StudentCodes.Distinct().Count(),
                DivisionId  = created.DivisionId,    // 🆕
                CreatedAt   = created.CreatedAt,
                CreatedBy   = created.CreatedBy
            };
        }

        public async Task UpdateAsync(int id, UpdateStudentGroupDto dto)
        {
            var group = await _groupRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"StudentGroup id={id} not found");

            // 💡 Data Isolation: ป้องกันแก้ไขข้ามแผนก
            if (_currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
                throw new UnauthorizedAccessException("Cannot update a group from another division.");

            group.Name        = dto.Name;
            group.Description = dto.Description;
            await _groupRepo.UpdateAsync(group);
        }

        public async Task DeleteAsync(int id)
        {
            var group = await _groupRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"StudentGroup id={id} not found");

            // 💡 Data Isolation: ป้องกันลบข้ามแผนก
            if (_currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
                throw new UnauthorizedAccessException("Cannot delete a group from another division.");

            // Soft-delete members ก่อน
            var members = await _memberRepo.GetAsync(m => m.StudentGroupId == id);
            foreach (var member in members)
                await _memberRepo.DeleteAsync(member);

            await _groupRepo.DeleteAsync(group);
        }

        public async Task AddMembersAsync(int groupId, AddGroupMembersDto dto)
        {
            var group = await GetAccessibleGroupAsync(groupId);

            var existing = await _memberRepo.GetAsync(m => m.StudentGroupId == groupId);
            var existingCodes = existing.Select(m => m.StudentCode).ToHashSet();

            foreach (var code in dto.StudentCodes.Distinct().Where(c => !existingCodes.Contains(c)))
            {
                await _memberRepo.AddAsync(new StudentGroupMember
                {
                    StudentGroupId = groupId,
                    StudentCode    = code
                });
            }
        }

        public async Task<StudentGroupAddMembersPreviewDto> PreviewAddMembersAsync(int groupId, StudentGroupAddMembersOptionsDto dto)
        {
            var normalizedCodes = NormalizeStudentCodes(dto.StudentCodes);
            ValidateAddMembersOptions(dto.EnrollToRelatedAssignments, dto.AssignmentStatuses, normalizedCodes);

            var group = await GetAccessibleGroupAsync(groupId, includeProperties: "Members");
            var existingMemberCodes = group.Members
                .Select(member => member.StudentCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var studentProfiles = await _studentApiService.GetStudentsByCodesAsync(normalizedCodes);
            var assignmentContexts = dto.EnrollToRelatedAssignments
                ? await LoadRelatedAssignmentContextsAsync(groupId, dto.AssignmentStatuses)
                : [];

            return new StudentGroupAddMembersPreviewDto
            {
                GroupId = group.Id,
                GroupName = group.Name,
                GroupDescription = group.Description,
                EnrollToRelatedAssignments = dto.EnrollToRelatedAssignments,
                SelectedStudentCount = normalizedCodes.Count,
                NewMemberCount = normalizedCodes.Count(code => !existingMemberCodes.Contains(code)),
                ExistingMemberCount = normalizedCodes.Count(code => existingMemberCodes.Contains(code)),
                SelectedAssignmentCount = assignmentContexts.Count,
                SelectedCourseCount = assignmentContexts.Sum(context => context.CourseCount),
                EstimatedEnrollmentCount = assignmentContexts.Sum(context => context.CourseCount * normalizedCodes.Count),
                Students = normalizedCodes.Select(code =>
                {
                    studentProfiles.TryGetValue(code, out var profile);

                    return new StudentGroupAddMembersStudentPreviewDto
                    {
                        StudentCode = code,
                        StudentName = profile?.Name ?? code,
                        Division = profile?.Division,
                        Department = profile?.Department,
                        Section = profile?.Section,
                        Position = profile?.Position,
                        IsAlreadyMember = existingMemberCodes.Contains(code)
                    };
                }).ToList(),
                Assignments = assignmentContexts.Select(context =>
                {
                    context.Preview.EstimatedEnrollmentCount = context.CourseCount * normalizedCodes.Count;
                    return context.Preview;
                }).ToList()
            };
        }

        public async Task<StudentGroupAddMembersResultDto> AddMembersWithAssignmentsAsync(int groupId, StudentGroupAddMembersOptionsDto dto)
        {
            var normalizedCodes = NormalizeStudentCodes(dto.StudentCodes);
            ValidateAddMembersOptions(dto.EnrollToRelatedAssignments, dto.AssignmentStatuses, normalizedCodes);

            var group = await GetAccessibleGroupAsync(groupId, includeProperties: "Members");
            var existingMemberCodes = group.Members
                .Select(member => member.StudentCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var addedStudentCodes = normalizedCodes
                .Where(code => !existingMemberCodes.Contains(code))
                .ToList();

            foreach (var code in addedStudentCodes)
            {
                await _memberRepo.AddWithoutSaveAsync(new StudentGroupMember
                {
                    StudentGroupId = groupId,
                    StudentCode = code
                });
            }

            var assignmentContexts = dto.EnrollToRelatedAssignments
                ? await LoadRelatedAssignmentContextsAsync(groupId, dto.AssignmentStatuses)
                : [];

            if (assignmentContexts.Count == 0)
            {
                if (addedStudentCodes.Count > 0)
                {
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            else
            {
                foreach (var context in assignmentContexts)
                {
                    var assignmentRuleIdsByCourseId = context.Rules
                        .Where(rule => rule.CourseId.HasValue)
                        .GroupBy(rule => rule.CourseId!.Value)
                        .ToDictionary(grouping => grouping.Key, grouping => grouping.First().Id);

                    if (assignmentRuleIdsByCourseId.Count == 0)
                    {
                        continue;
                    }

                    await _courseAssignmentService.Value.AssignCoursesToEmployees(
                        assignmentRuleIdsByCourseId,
                        normalizedCodes,
                        context.StartDate,
                        context.DueDate,
                        forceReset: false);
                }
            }

            return new StudentGroupAddMembersResultDto
            {
                GroupId = group.Id,
                GroupName = group.Name,
                SelectedStudentCount = normalizedCodes.Count,
                AddedMemberCount = addedStudentCodes.Count,
                ExistingMemberCount = normalizedCodes.Count - addedStudentCodes.Count,
                AssignmentCount = assignmentContexts.Count,
                CourseCount = assignmentContexts.Sum(context => context.CourseCount),
                EstimatedEnrollmentCount = assignmentContexts.Sum(context => context.CourseCount * normalizedCodes.Count),
                AddedStudentCodes = addedStudentCodes
            };
        }

        public async Task RemoveMemberAsync(int groupId, int memberId)
        {
            var members = await _memberRepo.GetAsync(m => m.Id == memberId && m.StudentGroupId == groupId);
            var member = members.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Member id={memberId} not found in group id={groupId}");

            // 💡 Data Isolation: ตรวจสอบ ownership ของ group
            var group = await _groupRepo.GetByIdAsync(groupId);
            if (group != null && _currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
                throw new UnauthorizedAccessException("Cannot modify a group from another division.");

            await _memberRepo.DeleteAsync(member);
        }

        public async Task<List<string>> GetStudentCodesAsync(int groupId)
        {
            var members = await _memberRepo.GetAsync(m => m.StudentGroupId == groupId);
            return members.Select(m => m.StudentCode).ToList();
        }

        private async Task<StudentGroup> GetAccessibleGroupAsync(int groupId, string? includeProperties = null)
        {
            IReadOnlyList<StudentGroup> groups = string.IsNullOrWhiteSpace(includeProperties)
                ? await _groupRepo.GetAsync(g => g.Id == groupId)
                : await _groupRepo.GetAsync(g => g.Id == groupId, includeProperties: includeProperties);

            var group = groups.FirstOrDefault()
                ?? throw new KeyNotFoundException($"StudentGroup id={groupId} not found");

            if (_currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
            {
                throw new UnauthorizedAccessException("Cannot modify a group from another division.");
            }

            return group;
        }

        private static List<string> NormalizeStudentCodes(IEnumerable<string>? studentCodes)
        {
            return studentCodes?
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }

        private static void ValidateAddMembersOptions(bool enrollToRelatedAssignments, IEnumerable<string>? assignmentStatuses, IReadOnlyCollection<string> normalizedCodes)
        {
            if (normalizedCodes.Count == 0)
            {
                throw new ArgumentException("At least one student code is required.");
            }

            var statuses = assignmentStatuses?
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Select(status => status.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            var invalidStatuses = statuses
                .Where(status => !AllowedAssignmentStatuses.Contains(status))
                .ToList();

            if (invalidStatuses.Count > 0)
            {
                throw new ArgumentException($"Unsupported assignment status: {string.Join(", ", invalidStatuses)}.");
            }

            if (enrollToRelatedAssignments && statuses.Count == 0)
            {
                throw new ArgumentException("Select at least one assignment status when enrollment is enabled.");
            }
        }

        private async Task<List<RelatedAssignmentContext>> LoadRelatedAssignmentContextsAsync(int groupId, IEnumerable<string>? selectedStatuses)
        {
            var selectedStatusSet = selectedStatuses?
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Select(status => status.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

            var assignments = await _assignmentRepo.GetAsync(
                assignment => assignment.StudentGroupId == groupId
                    && (!_currentUser.DivisionId.HasValue || assignment.DivisionId == _currentUser.DivisionId.Value),
                includeProperties: "Course");

            if (assignments.Count == 0)
            {
                return [];
            }

            var assignmentIds = assignments.Select(assignment => assignment.Id).ToList();
            var links = await _enrollmentAssignmentRepo.GetAsync(
                link => assignmentIds.Contains(link.AssignmentId),
                includeProperties: "Enrollment");

            var now = _dateTime.Now;

            return assignments
                .GroupBy(_assignmentBatchService.GetBatchKey)
                .Select(grouping =>
                {
                    var first = grouping.First();
                    var ruleIds = grouping.Select(rule => rule.Id).ToHashSet();
                    var relatedLinks = links
                        .Where(link => ruleIds.Contains(link.AssignmentId) && link.Enrollment != null)
                        .ToList();

                    var allCompleted = relatedLinks.Any()
                        && relatedLinks.All(link => link.SnapshotCompleted || link.Enrollment!.IsCompleted);

                    var status = AssignmentDashboardService.CalculateStatus(
                        relatedLinks.Any(),
                        allCompleted,
                        first.StartDate,
                        first.DueDate,
                        now);

                    return new RelatedAssignmentContext
                    {
                        Rules = grouping.ToList(),
                        StartDate = first.StartDate,
                        DueDate = first.DueDate,
                        Status = status,
                        CourseCount = grouping.Select(rule => rule.CourseId).Where(courseId => courseId.HasValue).Distinct().Count(),
                        Preview = new StudentGroupRelatedAssignmentPreviewDto
                        {
                            Id = first.Id,
                            AssignmentNo = grouping.Key,
                            Description = first.Description,
                            CourseNames = string.Join(", ", grouping
                                .Select(rule => rule.Course != null ? rule.Course.Title : "Unknown")
                                .Distinct()),
                            CourseCount = grouping.Select(rule => rule.CourseId).Where(courseId => courseId.HasValue).Distinct().Count(),
                            Status = status,
                            StartDate = first.StartDate,
                            DueDate = first.DueDate,
                            CurrentLearnerCount = relatedLinks
                                .Select(link => link.Enrollment!.StudentCode)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .Count(),
                            EstimatedEnrollmentCount = 0
                        }
                    };
                })
                .Where(context => selectedStatusSet.Contains(context.Status))
                .OrderByDescending(context => context.StartDate ?? DateTime.MinValue)
                .ThenByDescending(context => context.Preview.AssignmentNo)
                .ToList();
        }

        private sealed class RelatedAssignmentContext
        {
            public List<Assignment> Rules { get; set; } = new();
            public StudentGroupRelatedAssignmentPreviewDto Preview { get; set; } = new();
            public string Status { get; set; } = string.Empty;
            public int CourseCount { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
        }
    }
}
