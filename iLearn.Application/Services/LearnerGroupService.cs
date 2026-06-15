using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class LearnerGroupService : ILearnerGroupService
    {
        private readonly IGenericRepository<LearnerGroup> _groupRepo;
        private readonly IGenericRepository<LearnerGroupCategory> _categoryRepo;
        private readonly IGenericRepository<LearnerGroupMember> _memberRepo;
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly Lazy<ICourseAssignmentService> _courseAssignmentService;
        private readonly IAssignmentBatchService _assignmentBatchService;
        private readonly ILearnerApiService _learnerApiService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly IUnitOfWork _unitOfWork;

        private static readonly HashSet<string> AllowedAssignmentStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            AssignmentStatusKeys.Batch.Completed,
            AssignmentStatusKeys.Batch.InProgress,
            AssignmentStatusKeys.Batch.Upcoming,
            AssignmentStatusKeys.Batch.Expired
        };

        public LearnerGroupService(
            IGenericRepository<LearnerGroup> groupRepo,
            IGenericRepository<LearnerGroupCategory> categoryRepo,
            IGenericRepository<LearnerGroupMember> memberRepo,
            IGenericRepository<Assignment> assignmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            Lazy<ICourseAssignmentService> courseAssignmentService,
            IAssignmentBatchService assignmentBatchService,
            ILearnerApiService learnerApiService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            IUnitOfWork unitOfWork)
        {
            _groupRepo = groupRepo;
            _categoryRepo = categoryRepo;
            _memberRepo = memberRepo;
            _assignmentRepo = assignmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _courseAssignmentService = courseAssignmentService;
            _assignmentBatchService = assignmentBatchService;
            _learnerApiService = learnerApiService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<LearnerGroupDto>> GetAllAsync()
        {
            var query = _groupRepo.GetQuery().AsNoTracking();
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(g => g.DivisionId == _currentUser.DivisionId.Value);

            var rows = await query.Select(g => new LearnerGroupDto
            {
                Id           = g.Id,
                Name         = g.Name,
                Description  = g.Description,
                MemberCount  = g.Members.Count,
                DivisionId   = g.DivisionId,
                CategoryId   = g.CategoryId,
                CategoryName = g.Category != null ? g.Category.Name : null,
                CreatedAt    = g.CreatedAt,
                CreatedBy    = g.CreatedBy
            }).ToListAsync();

            return rows;
        }

        public async Task<PagedResult<LearnerGroupDto>> GetPagedAsync(PaginationParams p)
        {
            var query = _groupRepo.GetQuery().AsNoTracking();

            if (_currentUser.DivisionId.HasValue)
                query = query.Where(g => g.DivisionId == _currentUser.DivisionId.Value);

            if (p.RootCategoryOnly == true)
            {
                query = query.Where(g => !g.CategoryId.HasValue);
            }
            else if (p.CategoryId is { Count: > 0 })
            {
                var categoryIds = p.CategoryId
                    .Where(id => id > 0)
                    .Distinct()
                    .ToArray();

                if (categoryIds.Length > 0)
                    query = query.Where(g => g.CategoryId.HasValue && categoryIds.Contains(g.CategoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var term = p.Search.Trim().ToLower();
                query = query.Where(g =>
                    g.Name.ToLower().Contains(term) ||
                    (g.Description != null && g.Description.ToLower().Contains(term)) ||
                    (g.CreatedBy != null && g.CreatedBy.ToLower().Contains(term)));
            }

            query = (p.SortBy?.ToLower(), p.SortDescending) switch
            {
                ("name",        true)  => query.OrderByDescending(g => g.Name),
                ("name",        false) => query.OrderBy(g => g.Name),
                ("membercount", true)  => query.OrderByDescending(g => g.Members.Count),
                ("membercount", false) => query.OrderBy(g => g.Members.Count),
                ("createdby",   true)  => query.OrderByDescending(g => g.CreatedBy),
                ("createdby",   false) => query.OrderBy(g => g.CreatedBy),
                (_,             true)  => query.OrderByDescending(g => g.Id),
                _                      => query.OrderBy(g => g.Name),
            };

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, p.Page);
            var pageSize = Math.Clamp(p.PageSize, 1, 100);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new LearnerGroupDto
                {
                    Id           = g.Id,
                    Name         = g.Name,
                    Description  = g.Description,
                    MemberCount  = g.Members.Count,
                    DivisionId   = g.DivisionId,
                    CategoryId   = g.CategoryId,
                    CategoryName = g.Category != null ? g.Category.Name : null,
                    CreatedAt    = g.CreatedAt,
                    CreatedBy    = g.CreatedBy
                })
                .ToListAsync();

            return new PagedResult<LearnerGroupDto>
            {
                Data       = items,
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            };
        }

        public async Task<LearnerGroupDetailDto?> GetByIdAsync(int id)
        {
            var groups = await _groupRepo.GetAsync(
                filter: g => g.Id == id,
                includeProperties: "Members,Category"
            );
            var group = groups.FirstOrDefault();
            if (group == null) return null;

            // 💡 Data Isolation: ตรวจสอบว่า group เป็นของ Division ตัวเอง
            if (_currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
                return null;

            // ⚡ Bulk lookup: 1 HTTP call แทน N calls
            var codes      = group.Members.Select(m => m.LearnerCode);
            var profileMap = await _learnerApiService.GetLearnersByCodesAsync(codes);

            var memberDtos = group.Members.Select(m =>
            {
                profileMap.TryGetValue(m.LearnerCode, out var s);
                return new LearnerGroupMemberDto
                {
                    Id          = m.Id,
                    LearnerCode = m.LearnerCode,
                    LearnerName = s?.Name       ?? m.LearnerCode,
                    Division    = s?.Division,
                    Department  = s?.Department,
                    Section     = s?.Section,
                    Position    = s?.Position
                };
            }).ToList();

            var categoryAncestors = await LoadCategoryAncestorsAsync(group.Category);

            return new LearnerGroupDetailDto
            {
                Id                = group.Id,
                Name              = group.Name,
                Description       = group.Description,
                CreatedBy         = group.CreatedBy,
                CategoryId        = group.CategoryId,
                CategoryName      = group.Category?.Name,
                CategoryAncestors = categoryAncestors,
                Members           = memberDtos
            };
        }

        public async Task<LearnerGroupDto> CreateAsync(CreateLearnerGroupDto dto)
        {
            var normalizedDescription = NormalizeRequiredDescription(dto.Description);
            var category = await ValidateCategoryAsync(dto.CategoryId);

            var group = new LearnerGroup
            {
                Name = dto.Name,
                Description = normalizedDescription,
                DivisionId = _currentUser.IsSuperAdmin ? dto.DivisionId : _currentUser.DivisionId,
                CategoryId = category?.Id
            };
            var created = await _groupRepo.AddAsync(group);

            foreach (var code in dto.LearnerCodes.Distinct())
            {
                await _memberRepo.AddAsync(new LearnerGroupMember
                {
                    LearnerGroupId = created.Id,
                    LearnerCode    = code
                });
            }

            return new LearnerGroupDto
            {
                Id           = created.Id,
                Name         = created.Name,
                Description  = created.Description,
                MemberCount  = dto.LearnerCodes.Distinct().Count(),
                DivisionId   = created.DivisionId,
                CategoryId   = created.CategoryId,
                CategoryName = category?.Name,
                CreatedAt    = created.CreatedAt,
                CreatedBy    = created.CreatedBy
            };
        }

        public async Task UpdateAsync(int id, UpdateLearnerGroupDto dto)
        {
            var group = await _groupRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"LearnerGroup id={id} not found");

            // 💡 Data Isolation: ป้องกันแก้ไขข้ามแผนก
            if (_currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
                throw new UnauthorizedAccessException("Cannot update a group from another division.");

            group.Name        = dto.Name;
            group.Description = dto.Description?.Trim() ?? string.Empty;

            if (group.CategoryId != dto.CategoryId)
            {
                var category = await ValidateCategoryAsync(dto.CategoryId);
                group.CategoryId = category?.Id;
            }

            await _groupRepo.UpdateAsync(group);
        }

        public async Task DeleteAsync(int id)
        {
            var group = await _groupRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"LearnerGroup id={id} not found");

            // 💡 Data Isolation: ป้องกันลบข้ามแผนก
            if (_currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
                throw new UnauthorizedAccessException("Cannot delete a group from another division.");

            // Soft-delete members ก่อน
            var members = await _memberRepo.GetAsync(m => m.LearnerGroupId == id);
            foreach (var member in members)
                await _memberRepo.DeleteAsync(member);

            await _groupRepo.DeleteAsync(group);
        }

        public async Task AddMembersAsync(int groupId, AddGroupMembersDto dto)
        {
            var group = await GetAccessibleGroupAsync(groupId);

            var existing = await _memberRepo.GetAsync(m => m.LearnerGroupId == groupId);
            var existingCodes = existing.Select(m => m.LearnerCode).ToHashSet();

            foreach (var code in dto.LearnerCodes.Distinct().Where(c => !existingCodes.Contains(c)))
            {
                await _memberRepo.AddAsync(new LearnerGroupMember
                {
                    LearnerGroupId = groupId,
                    LearnerCode    = code
                });
            }
        }

        public async Task<LearnerGroupAddMembersPreviewDto> PreviewAddMembersAsync(int groupId, LearnerGroupAddMembersOptionsDto dto)
        {
            var normalizedCodes = NormalizeLearnerCodes(dto.LearnerCodes);
            ValidateAddMembersOptions(dto.EnrollToRelatedAssignments, dto.AssignmentStatuses, normalizedCodes);

            var group = await GetAccessibleGroupAsync(groupId, includeProperties: "Members");
            var existingMemberCodes = group.Members
                .Select(member => member.LearnerCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var learnerProfiles = await _learnerApiService.GetLearnersByCodesAsync(normalizedCodes);
            var assignmentContexts = dto.EnrollToRelatedAssignments
                ? await LoadRelatedAssignmentContextsAsync(groupId, dto.AssignmentStatuses, dto.AssignmentIds)
                : [];

            return new LearnerGroupAddMembersPreviewDto
            {
                GroupId = group.Id,
                GroupName = group.Name,
                GroupDescription = group.Description,
                EnrollToRelatedAssignments = dto.EnrollToRelatedAssignments,
                SelectedLearnerCount = normalizedCodes.Count,
                NewMemberCount = normalizedCodes.Count(code => !existingMemberCodes.Contains(code)),
                ExistingMemberCount = normalizedCodes.Count(code => existingMemberCodes.Contains(code)),
                SelectedAssignmentCount = assignmentContexts.Count,
                SelectedCourseCount = assignmentContexts.Sum(context => context.CourseCount),
                EstimatedEnrollmentCount = assignmentContexts.Sum(context => context.CourseCount * normalizedCodes.Count),
                Learners = normalizedCodes.Select(code =>
                {
                    learnerProfiles.TryGetValue(code, out var profile);

                    return new LearnerGroupAddMembersLearnerPreviewDto
                    {
                        LearnerCode = code,
                        LearnerName = profile?.Name ?? code,
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

        public async Task<LearnerGroupAddMembersResultDto> AddMembersWithAssignmentsAsync(int groupId, LearnerGroupAddMembersOptionsDto dto)
        {
            var normalizedCodes = NormalizeLearnerCodes(dto.LearnerCodes);
            ValidateAddMembersOptions(dto.EnrollToRelatedAssignments, dto.AssignmentStatuses, normalizedCodes);

            var group = await GetAccessibleGroupAsync(groupId, includeProperties: "Members");
            var existingMemberCodes = group.Members
                .Select(member => member.LearnerCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var addedLearnerCodes = normalizedCodes
                .Where(code => !existingMemberCodes.Contains(code))
                .ToList();

            foreach (var code in addedLearnerCodes)
            {
                await _memberRepo.AddWithoutSaveAsync(new LearnerGroupMember
                {
                    LearnerGroupId = groupId,
                    LearnerCode = code
                });
            }

            var assignmentContexts = dto.EnrollToRelatedAssignments
                ? await LoadRelatedAssignmentContextsAsync(groupId, dto.AssignmentStatuses, dto.AssignmentIds)
                : [];

            if (assignmentContexts.Count == 0)
            {
                if (addedLearnerCodes.Count > 0)
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

            return new LearnerGroupAddMembersResultDto
            {
                GroupId = group.Id,
                GroupName = group.Name,
                SelectedLearnerCount = normalizedCodes.Count,
                AddedMemberCount = addedLearnerCodes.Count,
                ExistingMemberCount = normalizedCodes.Count - addedLearnerCodes.Count,
                AssignmentCount = assignmentContexts.Count,
                CourseCount = assignmentContexts.Sum(context => context.CourseCount),
                EstimatedEnrollmentCount = assignmentContexts.Sum(context => context.CourseCount * normalizedCodes.Count),
                AddedLearnerCodes = addedLearnerCodes
            };
        }

        public async Task RemoveMemberAsync(int groupId, int memberId)
        {
            var members = await _memberRepo.GetAsync(m => m.Id == memberId && m.LearnerGroupId == groupId);
            var member = members.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Member id={memberId} not found in group id={groupId}");

            // 💡 Data Isolation: ตรวจสอบ ownership ของ group
            var group = await _groupRepo.GetByIdAsync(groupId);
            if (group != null && _currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
                throw new UnauthorizedAccessException("Cannot modify a group from another division.");

            await _memberRepo.DeleteAsync(member);
        }

        public async Task<LearnerGroupRemoveMembersPreviewDto> PreviewRemoveMembersAsync(int groupId, LearnerGroupRemoveMembersOptionsDto dto)
        {
            var (group, selectedMembers) = await ResolveRemoveMembersScopeAsync(groupId, dto);

            var assignmentContexts = dto.UnenrollFromRelatedAssignments
                ? await LoadRelatedAssignmentContextsAsync(groupId, dto.AssignmentStatuses, dto.AssignmentIds)
                : [];

            var selectedCodes = selectedMembers
                .Select(m => m.LearnerCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var profiles = await _learnerApiService.GetLearnersByCodesAsync(selectedCodes);

            var memberPerLearnerCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var totalUnenrollLinks = 0;

            if (assignmentContexts.Count > 0)
            {
                var assignmentIds = assignmentContexts.SelectMany(c => c.Rules.Select(r => r.Id)).ToHashSet();
                var links = await _enrollmentAssignmentRepo.GetAsync(
                    link => assignmentIds.Contains(link.AssignmentId) && link.Enrollment != null,
                    includeProperties: "Enrollment");

                foreach (var link in links)
                {
                    var code = link.Enrollment!.LearnerCode;
                    if (!selectedCodes.Contains(code))
                    {
                        continue;
                    }

                    totalUnenrollLinks++;
                    memberPerLearnerCounts.TryGetValue(code, out var current);
                    memberPerLearnerCounts[code] = current + 1;
                }
            }

            return new LearnerGroupRemoveMembersPreviewDto
            {
                GroupId = group.Id,
                GroupName = group.Name,
                GroupDescription = group.Description,
                UnenrollFromRelatedAssignments = dto.UnenrollFromRelatedAssignments,
                SelectedMemberCount = selectedMembers.Count,
                SelectedAssignmentCount = assignmentContexts.Count,
                SelectedCourseCount = assignmentContexts.Sum(c => c.CourseCount),
                EstimatedUnenrollmentCount = totalUnenrollLinks,
                Members = selectedMembers.Select(member =>
                {
                    profiles.TryGetValue(member.LearnerCode, out var profile);
                    memberPerLearnerCounts.TryGetValue(member.LearnerCode, out var unenrollCount);

                    return new LearnerGroupRemoveMembersLearnerPreviewDto
                    {
                        MemberId = member.Id,
                        LearnerCode = member.LearnerCode,
                        LearnerName = profile?.Name ?? member.LearnerCode,
                        Division = profile?.Division,
                        Department = profile?.Department,
                        Section = profile?.Section,
                        Position = profile?.Position,
                        CurrentAssignmentEnrollmentCount = unenrollCount
                    };
                }).ToList(),
                Assignments = assignmentContexts.Select(context =>
                {
                    context.Preview.EstimatedEnrollmentCount = 0;
                    return context.Preview;
                }).ToList()
            };
        }

        public async Task<LearnerGroupRemoveMembersResultDto> RemoveMembersWithAssignmentsAsync(int groupId, LearnerGroupRemoveMembersOptionsDto dto)
        {
            var (group, selectedMembers) = await ResolveRemoveMembersScopeAsync(groupId, dto);

            var selectedCodes = selectedMembers
                .Select(m => m.LearnerCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var assignmentContexts = dto.UnenrollFromRelatedAssignments
                ? await LoadRelatedAssignmentContextsAsync(groupId, dto.AssignmentStatuses, dto.AssignmentIds)
                : [];

            var unenrolledLinkCount = 0;

            if (assignmentContexts.Count > 0)
            {
                var assignmentIds = assignmentContexts.SelectMany(c => c.Rules.Select(r => r.Id)).ToHashSet();
                var links = await _enrollmentAssignmentRepo.GetAsync(
                    link => assignmentIds.Contains(link.AssignmentId) && link.Enrollment != null,
                    includeProperties: "Enrollment");

                foreach (var link in links)
                {
                    if (!selectedCodes.Contains(link.Enrollment!.LearnerCode))
                    {
                        continue;
                    }

                    _enrollmentAssignmentRepo.DeleteWithoutSave(link);
                    unenrolledLinkCount++;
                }
            }

            foreach (var member in selectedMembers)
            {
                _memberRepo.DeleteWithoutSave(member);
            }

            await _unitOfWork.SaveChangesAsync();

            return new LearnerGroupRemoveMembersResultDto
            {
                GroupId = group.Id,
                GroupName = group.Name,
                SelectedMemberCount = selectedMembers.Count,
                RemovedMemberCount = selectedMembers.Count,
                AssignmentCount = assignmentContexts.Count,
                UnenrolledLinkCount = unenrolledLinkCount,
                RemovedLearnerCodes = selectedMembers.Select(m => m.LearnerCode).ToList()
            };
        }

        private async Task<(LearnerGroup group, List<LearnerGroupMember> members)> ResolveRemoveMembersScopeAsync(int groupId, LearnerGroupRemoveMembersOptionsDto dto)
        {
            var memberIdSet = dto.MemberIds?
                .Where(id => id > 0)
                .ToHashSet() ?? [];

            if (memberIdSet.Count == 0)
            {
                throw new ArgumentException("At least one member is required.");
            }

            ValidateAssignmentStatuses(dto.UnenrollFromRelatedAssignments, dto.AssignmentStatuses);

            var group = await GetAccessibleGroupAsync(groupId, includeProperties: "Members");

            var selected = group.Members
                .Where(m => memberIdSet.Contains(m.Id))
                .ToList();

            if (selected.Count == 0)
            {
                throw new ArgumentException("Selected members were not found in this group.");
            }

            return (group, selected);
        }

        private static void ValidateAssignmentStatuses(bool isEnabled, IEnumerable<string>? assignmentStatuses)
        {
            var statuses = assignmentStatuses?
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Select(status => status.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            var invalid = statuses
                .Where(status => !AllowedAssignmentStatuses.Contains(status))
                .ToList();

            if (invalid.Count > 0)
            {
                throw new ArgumentException($"Unsupported assignment status: {string.Join(", ", invalid)}.");
            }

            if (isEnabled && statuses.Count == 0)
            {
                throw new ArgumentException("Select at least one assignment status when this option is enabled.");
            }
        }

        public async Task<List<string>> GetLearnerCodesAsync(int groupId)
        {
            var members = await _memberRepo.GetAsync(m => m.LearnerGroupId == groupId);
            return members.Select(m => m.LearnerCode).ToList();
        }

        private async Task<LearnerGroup> GetAccessibleGroupAsync(int groupId, string? includeProperties = null)
        {
            IReadOnlyList<LearnerGroup> groups = string.IsNullOrWhiteSpace(includeProperties)
                ? await _groupRepo.GetAsync(g => g.Id == groupId)
                : await _groupRepo.GetAsync(g => g.Id == groupId, includeProperties: includeProperties);

            var group = groups.FirstOrDefault()
                ?? throw new KeyNotFoundException($"LearnerGroup id={groupId} not found");

            if (_currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
            {
                throw new UnauthorizedAccessException("Cannot modify a group from another division.");
            }

            return group;
        }

        private static List<string> NormalizeLearnerCodes(IEnumerable<string>? learnerCodes)
        {
            return learnerCodes?
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }

        private static string NormalizeRequiredDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Description is required.");
            }

            return description.Trim();
        }

        // ── Category helpers ──────────────────────────────────────────
        private async Task<LearnerGroupCategory?> ValidateCategoryAsync(int? categoryId)
        {
            if (!categoryId.HasValue) return null;

            var category = await _categoryRepo.GetByIdAsync(categoryId.Value)
                ?? throw new ArgumentException("Category not found.");

            if (_currentUser.DivisionId.HasValue && category.DivisionId != _currentUser.DivisionId.Value)
                throw new ArgumentException("Category must belong to the same division.");

            return category;
        }

        private async Task<List<LearnerGroupCategoryAncestorDto>> LoadCategoryAncestorsAsync(LearnerGroupCategory? category)
        {
            if (category == null) return new();

            var ids = (category.Path ?? "/")
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var n) ? n : 0)
                .Where(n => n > 0)
                .ToList();

            ids.Add(category.Id);

            if (ids.Count == 0) return new();

            var loaded = await _categoryRepo.GetQuery()
                .AsNoTracking()
                .Where(c => ids.Contains(c.Id))
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return ids
                .Select(id => loaded.FirstOrDefault(a => a.Id == id))
                .Where(a => a != null)
                .Select(a => new LearnerGroupCategoryAncestorDto { Id = a!.Id, Name = a.Name })
                .ToList();
        }

        private static void ValidateAddMembersOptions(bool enrollToRelatedAssignments, IEnumerable<string>? assignmentStatuses, IReadOnlyCollection<string> normalizedCodes)
        {
            if (normalizedCodes.Count == 0)
            {
                throw new ArgumentException("At least one learner code is required.");
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

        private async Task<List<RelatedAssignmentContext>> LoadRelatedAssignmentContextsAsync(int groupId, IEnumerable<string>? selectedStatuses, IEnumerable<int>? selectedAssignmentIds = null)
        {
            var selectedStatusSet = selectedStatuses?
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Select(status => status.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

            var selectedIdSet = selectedAssignmentIds?
                .Where(id => id > 0)
                .ToHashSet() ?? [];

            var assignments = await _assignmentRepo.GetAsync(
                assignment => assignment.LearnerGroupId == groupId
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
                        Preview = new LearnerGroupRelatedAssignmentPreviewDto
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
                                .Select(link => link.Enrollment!.LearnerCode)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .Count(),
                            EstimatedEnrollmentCount = 0
                        }
                    };
                })
                .Where(context => selectedStatusSet.Contains(context.Status))
                .Where(context => selectedIdSet.Count == 0 || context.Rules.Any(rule => selectedIdSet.Contains(rule.Id)))
                .OrderByDescending(context => context.StartDate ?? DateTime.MinValue)
                .ThenByDescending(context => context.Preview.AssignmentNo)
                .ToList();
        }

        private sealed class RelatedAssignmentContext
        {
            public List<Assignment> Rules { get; set; } = new();
            public LearnerGroupRelatedAssignmentPreviewDto Preview { get; set; } = new();
            public string Status { get; set; } = string.Empty;
            public int CourseCount { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? DueDate { get; set; }
        }
    }
}
