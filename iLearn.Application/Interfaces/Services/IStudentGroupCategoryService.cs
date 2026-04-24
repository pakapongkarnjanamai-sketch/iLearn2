using iLearn.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface IStudentGroupCategoryService
    {
        Task<List<StudentGroupCategoryDto>> GetAllAsync();
        Task<StudentGroupCategoryDetailDto?> GetByIdAsync(int id);
        Task<StudentGroupCategoryDto> CreateAsync(CreateStudentGroupCategoryDto dto);
        Task UpdateAsync(int id, UpdateStudentGroupCategoryDto dto);
        Task DeleteAsync(int id);
    }
}
