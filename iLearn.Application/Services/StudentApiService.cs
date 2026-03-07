using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace iLearn.Infrastructure.Services
{
    public class StudentApiService : IStudentApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseStudentLookupUrl = "https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/StudentLookup";
        private const string BaseStudentUrl       = "https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/Student";

        public StudentApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetStudentsDxGridAsync(string queryString)
        {
            try
            {
                return await _httpClient.GetStringAsync($"{BaseStudentUrl}{queryString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching DevExtreme grid data: {ex.Message}");
                return null;
            }
        }

        public async Task<ExternalStudentDto> GetStudentByCodeAsync(string Code)
        {
            try
            {
                var url = $"{BaseStudentLookupUrl}/{Code}";
                var response = await _httpClient.GetFromJsonAsync<ExternalStudentDto>(url);
                return response;
            }
            catch
            {
                return null;
            }
        }

        public async Task<AllStudentsApiResponse> GetStudentAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<AllStudentsApiResponse>(
                    $"{BaseStudentUrl}/all");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Bulk lookup: ยิง HTTP 1 ครั้งผ่าน /api/Student/all (Server cache 24h)
        /// แล้ว filter เฉพาะ codes ที่ต้องการใน memory
        /// แทนการยิง GetStudentByCodeAsync ทีละคน (N+1 problem)
        /// </summary>
        public async Task<Dictionary<string, ExternalStudentDto>> GetStudentsByCodesAsync(
            IEnumerable<string> codes)
        {
            try
            {
                var codeSet  = codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var response = await GetStudentAsync(); // reuse — server มี MemoryCache 24h

                if (response?.data == null)
                    return new Dictionary<string, ExternalStudentDto>(StringComparer.OrdinalIgnoreCase);

                return response.data
                    .Where(s => !string.IsNullOrEmpty(s.EId) && codeSet.Contains(s.EId))
                    .ToDictionary(
                        s => s.EId,
                        s => new ExternalStudentDto
                        {
                            Code       = s.EId,
                            Name       = $"{s.EnglishFirstName} {s.EnglishLastName}".Trim(),
                            Division   = s.Division,
                            Department = s.Department,
                            Section    = s.Section,
                            Position   = s.Position
                        },
                        StringComparer.OrdinalIgnoreCase
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStudentsByCodesAsync: {ex.Message}");
                return new Dictionary<string, ExternalStudentDto>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task<DivisionApiResponse> GetStudentsByDivisionsAsync(string[] divisions, int skip = 0, int take = 20)
        {
            try
            {
                var keyObj         = new { divisions };
                var encodedKey     = Uri.EscapeDataString(JsonSerializer.Serialize(keyObj));
                var encodedSummary = Uri.EscapeDataString("[{\"selector\":\"EId\",\"summaryType\":\"count\"}]");
                var url = $"{BaseStudentUrl}/divisions?key={encodedKey}&skip={skip}&take={take}" +
                          $"&requireTotalCount=true&totalSummary={encodedSummary}";
                return await _httpClient.GetFromJsonAsync<DivisionApiResponse>(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching divisions: {ex.Message}");
                return null;
            }
        }

        public async Task<object> GetSectionsAsync(string queryString)
        {
            try
            {
                // อัปเดต URL เป็น GetDistinctSections
                var url = $"{BaseStudentLookupUrl}/GetDistinctSections{queryString}";
                var response = await _httpClient.GetFromJsonAsync<object>(url);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Sections: {ex.Message}");
                return null;
            }
        }

        public async Task<object> GetDivisionsAsync(string queryString)
        {
            try
            {
                // อัปเดต URL เป็น GetDistinctDivisions
                var url = $"{BaseStudentLookupUrl}/GetDistinctDivisions{queryString}";
                var response = await _httpClient.GetFromJsonAsync<object>(url);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Divisions: {ex.Message}");
                return null;
            }
        }

        public async Task<object> GetDepartmentsAsync(string queryString)
        {
            try
            {
                var url = $"{BaseStudentLookupUrl}/GetDistinctDepartments{queryString}";
                var response = await _httpClient.GetFromJsonAsync<object>(url);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Departments: {ex.Message}");
                return null;
            }
        }

        public async Task<object> GetPositionsAsync(string queryString)
        {
            try
            {
                var url = $"{BaseStudentLookupUrl}/GetDistinctPositions{queryString}";
                var response = await _httpClient.GetFromJsonAsync<object>(url);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Positions: {ex.Message}");
                return null;
            }
        }
    }
}