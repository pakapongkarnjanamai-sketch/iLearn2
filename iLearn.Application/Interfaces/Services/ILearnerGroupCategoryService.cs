using iLearn.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface ILearnerGroupCategoryService
    {
        Task<List<LearnerGroupCategoryDto>> GetAllAsync();
        Task<LearnerGroupCategoryDetailDto?> GetByIdAsync(int id);
        Task<LearnerGroupCategoryDto> CreateAsync(CreateLearnerGroupCategoryDto dto);
        Task UpdateAsync(int id, UpdateLearnerGroupCategoryDto dto);
        Task DeleteAsync(int id);
    }
}
