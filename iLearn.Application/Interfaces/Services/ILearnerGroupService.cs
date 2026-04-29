using iLearn.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface ILearnerGroupService
    {
        Task<List<LearnerGroupDto>> GetAllAsync();
        Task<PagedResult<LearnerGroupDto>> GetPagedAsync(PaginationParams p);
        Task<LearnerGroupDetailDto?> GetByIdAsync(int id);
        Task<LearnerGroupDto> CreateAsync(CreateLearnerGroupDto dto);
        Task UpdateAsync(int id, UpdateLearnerGroupDto dto);
        Task DeleteAsync(int id);
        Task AddMembersAsync(int groupId, AddGroupMembersDto dto);
        Task<LearnerGroupAddMembersPreviewDto> PreviewAddMembersAsync(int groupId, LearnerGroupAddMembersOptionsDto dto);
        Task<LearnerGroupAddMembersResultDto> AddMembersWithAssignmentsAsync(int groupId, LearnerGroupAddMembersOptionsDto dto);
        Task<LearnerGroupRemoveMembersPreviewDto> PreviewRemoveMembersAsync(int groupId, LearnerGroupRemoveMembersOptionsDto dto);
        Task<LearnerGroupRemoveMembersResultDto> RemoveMembersWithAssignmentsAsync(int groupId, LearnerGroupRemoveMembersOptionsDto dto);
        Task RemoveMemberAsync(int groupId, int memberId);
        Task<List<string>> GetLearnerCodesAsync(int groupId);
    }
}
