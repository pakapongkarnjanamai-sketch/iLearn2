using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace iLearn.Infrastructure.Services
{
    public class StudentApiService : IStudentApiService
    {
        private readonly HttpClient _httpClient;

        public StudentApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ExternalStudentDto> GetStudentByCodeAsync(string Code)
        {
            try
            {
                // URL ของ API ภายนอก
                var url = $"https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/StudentLookup/{Code}";

                // เรียก API และ Map เข้า DTO ทันที
                var response = await _httpClient.GetFromJsonAsync<ExternalStudentDto>(url);
                return response;
            }
            catch
            {
                // จัดการ Error เช่น พนักงานไม่พบ หรือ API ล่ม
                return null;
            }
        }
     
        public async Task<StudentDto> GetStudentAsync()
        {
            try
            {
                // URL ของ API ภายนอก
                var url = $"https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/Student";

                // เรียก API และ Map เข้า DTO ทันที
                var response = await _httpClient.GetFromJsonAsync<StudentDto>(url);
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