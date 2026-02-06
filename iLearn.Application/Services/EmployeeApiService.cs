using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace iLearn.Infrastructure.Services
{
    public class EmployeeApiService : IEmployeeApiService
    {
        private readonly HttpClient _httpClient;

        public EmployeeApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ExternalEmployeeDto> GetEmployeeByCodeAsync(string employeeCode)
        {
            try
            {
                // URL ของ API ภายนอก
                var url = $"https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/StudentLookup/{employeeCode}";

                // เรียก API และ Map เข้า DTO ทันที
                var response = await _httpClient.GetFromJsonAsync<ExternalEmployeeDto>(url);
                return response;
            }
            catch
            {
                // จัดการ Error เช่น พนักงานไม่พบ หรือ API ล่ม
                return null;
            }
        }
    }
}