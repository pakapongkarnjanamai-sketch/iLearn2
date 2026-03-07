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

        public StudentGroupService(
            IGenericRepository<StudentGroup> groupRepo,
            IGenericRepository<StudentGroupMember> memberRepo,
            IStudentApiService studentApiService)
        {
            _groupRepo = groupRepo;
            _memberRepo = memberRepo;
            _studentApiService = studentApiService;
        }

        public async Task<List<StudentGroupDto>> GetAllAsync()
        {
            var groups = await _groupRepo.GetAsync(includeProperties: "Members");
            return groups.Select(g => new StudentGroupDto
            {
                Id          = g.Id,
                Name        = g.Name,
                Description = g.Description,
                MemberCount = g.Members.Count,
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

            // ?????????????????? External API (parallel)
            var nameTasks = group.Members.Select(async m =>
            {
                try
                {
                    var student = await _studentApiService.GetStudentByCodeAsync(m.StudentCode);
                    return (m.Id, m.StudentCode, Name: student?.Name ?? m.StudentCode);
                }
                catch
                {
                    return (m.Id, m.StudentCode, Name: m.StudentCode);
                }
            });
            var nameResults = await Task.WhenAll(nameTasks);

            return new StudentGroupDetailDto
            {
                Id          = group.Id,
                Name        = group.Name,
                Description = group.Description,
                Members     = nameResults.Select(r => new StudentGroupMemberDto
                {
                    Id          = r.Id,
                    StudentCode = r.StudentCode,
                    StudentName = r.Name
                }).ToList()
            };
        }

        public async Task<StudentGroupDto> CreateAsync(CreateStudentGroupDto dto)
        {
            var group = new StudentGroup
            {
                Name        = dto.Name,
                Description = dto.Description
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
                CreatedAt   = created.CreatedAt,
                CreatedBy   = created.CreatedBy
            };
        }

        public async Task UpdateAsync(int id, UpdateStudentGroupDto dto)
        {
            var group = await _groupRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"StudentGroup id={id} not found");

            group.Name        = dto.Name;
            group.Description = dto.Description;
            await _groupRepo.UpdateAsync(group);
        }

        public async Task DeleteAsync(int id)
        {
            var group = await _groupRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"StudentGroup id={id} not found");

            // Soft-delete ????????????????? ???????? Soft-delete ?????
            var members = await _memberRepo.GetAsync(m => m.StudentGroupId == id);
            foreach (var member in members)
                await _memberRepo.DeleteAsync(member);

            await _groupRepo.DeleteAsync(group);
        }

        public async Task AddMembersAsync(int groupId, AddGroupMembersDto dto)
        {
            var group = await _groupRepo.GetByIdAsync(groupId)
                ?? throw new KeyNotFoundException($"StudentGroup id={groupId} not found");

            // ?????????????????????????????????? duplicate
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

            await _memberRepo.DeleteAsync(member);
        }

        public async Task<List<string>> GetStudentCodesAsync(int groupId)
        {
            var members = await _memberRepo.GetAsync(m => m.StudentGroupId == groupId);
            return members.Select(m => m.StudentCode).ToList();
        }
    }
}
