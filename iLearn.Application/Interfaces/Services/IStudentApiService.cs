using iLearn.Application.DTOs;
using System.Threading.Tasks;
using static iLearn.Infrastructure.Services.StudentApiService;

namespace iLearn.Application.Interfaces.Services
{
    public interface IStudentApiService
    {
        Task<ExternalStudentDto> GetStudentByCodeAsync(string employeeCode);
        Task<StudentDto> GetStudentAsync();
    }
}