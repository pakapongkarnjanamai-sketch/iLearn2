using iLearn.Application.DTOs;
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
        private readonly IStudentApiService _studentApiService;
        private readonly ICurrentUserService _currentUser;

        public StudentGroupService(
            IGenericRepository<StudentGroup> groupRepo,
            IGenericRepository<StudentGroupMember> memberRepo,
            IStudentApiService studentApiService,
            ICurrentUserService currentUser)
        {
            _groupRepo = groupRepo;
            _memberRepo = memberRepo;
            _studentApiService = studentApiService;
            _currentUser = currentUser;
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
            var group = await _groupRepo.GetByIdAsync(groupId)
                ?? throw new KeyNotFoundException($"StudentGroup id={groupId} not found");

            // 💡 Data Isolation: ป้องกันเพิ่มสมาชิกข้ามแผนก
            if (_currentUser.DivisionId.HasValue && group.DivisionId != _currentUser.DivisionId.Value)
                throw new UnauthorizedAccessException("Cannot modify a group from another division.");

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
    }
}
