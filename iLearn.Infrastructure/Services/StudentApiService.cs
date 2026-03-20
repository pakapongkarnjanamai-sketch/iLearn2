using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
        private readonly IMemoryCache _cache;
        private readonly string _baseStudentLookupUrl;
        private readonly string _baseStudentUrl;
        private readonly string _baseEmployeeCsvUrl;
        private const string EmployeeCsvCacheKey = "employee_csv_directory";

        public StudentApiService(
            HttpClient httpClient,
            IOptions<EmployeeServiceSettings> settings,
            IMemoryCache cache)
        {
            _httpClient           = httpClient;
            _cache                = cache;
            _baseStudentLookupUrl = settings.Value.BaseStudentLookupUrl;
            _baseStudentUrl       = settings.Value.BaseStudentUrl;
            _baseEmployeeCsvUrl   = settings.Value.BaseEmployeeCsvUrl;
        }

        public async Task<string> GetStudentsDxGridAsync(string queryString)
        {
            try
            {
                return await _httpClient.GetStringAsync($"{_baseStudentUrl}{queryString}");
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
                var url = $"{_baseStudentLookupUrl}/{Code}";
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
                    $"{_baseStudentUrl}/all");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Bulk lookup: ??? HTTP 1 ????????? /api/Student/all (Server cache 24h)
        /// ???? filter ????? codes ???????????? memory
        /// ????????? GetStudentByCodeAsync ?????? (N+1 problem)
        /// </summary>
        public async Task<Dictionary<string, ExternalStudentDto>> GetStudentsByCodesAsync(
            IEnumerable<string> codes)
        {
            try
            {
                var codeSet  = codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var response = await GetStudentAsync(); // reuse — server ?? MemoryCache 24h

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

        public async Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids)
        {
            try
            {
                var nidSet = nids
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (nidSet.Count == 0)
                    return new Dictionary<string, EmployeeCsvDto>(StringComparer.OrdinalIgnoreCase);

                var employees = await GetEmployeeDirectoryAsync();

                return employees
                    .Where(e => !string.IsNullOrWhiteSpace(e.NID) && nidSet.Contains(e.NID))
                    .GroupBy(e => e.NID, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEmployeesByNidsAsync: {ex.Message}");
                return new Dictionary<string, EmployeeCsvDto>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private async Task<List<EmployeeCsvDto>> GetEmployeeDirectoryAsync()
        {
            if (string.IsNullOrWhiteSpace(_baseEmployeeCsvUrl))
                return new List<EmployeeCsvDto>();

            if (_cache.TryGetValue(EmployeeCsvCacheKey, out List<EmployeeCsvDto>? cachedEmployees) && cachedEmployees != null)
                return cachedEmployees;

            var response = await _httpClient.GetFromJsonAsync<EmployeeCsvApiResponse>(_baseEmployeeCsvUrl);
            var employees = response?.data ?? new List<EmployeeCsvDto>();

            _cache.Set(EmployeeCsvCacheKey, employees, TimeSpan.FromMinutes(30));

            return employees;
        }

        public async Task<DivisionApiResponse> GetStudentsByDivisionsAsync(string[] divisions, int skip = 0, int take = 20)
        {
            try
            {
                var keyObj         = new { divisions };
                var encodedKey     = Uri.EscapeDataString(JsonSerializer.Serialize(keyObj));
                var encodedSummary = Uri.EscapeDataString("[{\"selector\":\"EId\",\"summaryType\":\"count\"}]");
                var url = $"{_baseStudentUrl}/divisions?key={encodedKey}&skip={skip}&take={take}" +
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
                // ?????? URL ???? GetDistinctSections
                var url = $"{_baseStudentLookupUrl}/GetDistinctSections{queryString}";
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
                // ?????? URL ???? GetDistinctDivisions
                var url = $"{_baseStudentLookupUrl}/GetDistinctDivisions{queryString}";
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
                var url = $"{_baseStudentLookupUrl}/GetDistinctDepartments{queryString}";
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
                var url = $"{_baseStudentLookupUrl}/GetDistinctPositions{queryString}";
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
