using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface ILearnerApiService
    {
        Task<ExternalLearnerDto> GetLearnerByCodeAsync(string Code);
        Task<AllLearnersApiResponse> GetLearnerAsync();
        Task<DivisionApiResponse> GetLearnersByDivisionsAsync(string[] divisions, int skip = 0, int take = 20);
        Task<string> GetLearnersDxGridAsync(string queryString);
        Task<object> GetSectionsAsync(string queryString);
        Task<object> GetDivisionsAsync(string queryString);
        Task<object> GetDepartmentsAsync(string queryString);
        Task<object> GetPositionsAsync(string queryString);

        /// <summary>
        /// Bulk lookup — ยิง HTTP 1 ครั้งผ่าน /api/Learner/all (Server cache 24h)
        /// แล้ว filter + map ใน memory แทน GetLearnerByCodeAsync ทีละคน (N+1 problem)
        /// </summary>
        Task<Dictionary<string, ExternalLearnerDto>> GetLearnersByCodesAsync(IEnumerable<string> codes);

        Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids);
    }
}