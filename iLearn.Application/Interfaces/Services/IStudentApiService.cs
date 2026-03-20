using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IStudentApiService
    {
        Task<ExternalStudentDto> GetStudentByCodeAsync(string Code);
        Task<AllStudentsApiResponse> GetStudentAsync();
        Task<DivisionApiResponse> GetStudentsByDivisionsAsync(string[] divisions, int skip = 0, int take = 20);
        Task<string> GetStudentsDxGridAsync(string queryString);
        Task<object> GetSectionsAsync(string queryString);
        Task<object> GetDivisionsAsync(string queryString);
        Task<object> GetDepartmentsAsync(string queryString);
        Task<object> GetPositionsAsync(string queryString);

        /// <summary>
        /// Bulk lookup — ยิง HTTP 1 ครั้งผ่าน /api/Student/all (Server cache 24h)
        /// แล้ว filter + map ใน memory แทน GetStudentByCodeAsync ทีละคน (N+1 problem)
        /// </summary>
        Task<Dictionary<string, ExternalStudentDto>> GetStudentsByCodesAsync(IEnumerable<string> codes);

        Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids);
    }
}