using iLearn.Application.DTOs;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface IEmployeeApiService
    {
        Task<ExternalEmployeeDto> GetEmployeeByCodeAsync(string employeeCode);
    }
}