using iLearn.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface IStudentGroupService
    {
        Task<List<StudentGroupDto>> GetAllAsync();
        Task<StudentGroupDetailDto?> GetByIdAsync(int id);
        Task<StudentGroupDto> CreateAsync(CreateStudentGroupDto dto);
        Task UpdateAsync(int id, UpdateStudentGroupDto dto);
        Task DeleteAsync(int id);
        Task AddMembersAsync(int groupId, AddGroupMembersDto dto);
        Task<StudentGroupAddMembersPreviewDto> PreviewAddMembersAsync(int groupId, StudentGroupAddMembersOptionsDto dto);
        Task<StudentGroupAddMembersResultDto> AddMembersWithAssignmentsAsync(int groupId, StudentGroupAddMembersOptionsDto dto);
        Task RemoveMemberAsync(int groupId, int memberId);
        Task<List<string>> GetStudentCodesAsync(int groupId);
    }
}
