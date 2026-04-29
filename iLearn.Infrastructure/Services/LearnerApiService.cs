using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;

namespace iLearn.Infrastructure.Services
{
    public class LearnerApiService : ILearnerApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly string _baseLearnerLookupUrl;
        private readonly string _baseLearnerUrl;
        private readonly string _baseEmployeeCsvUrl;
        private const string EmployeeCsvCacheKey = "employee_csv_directory";

        public LearnerApiService(
            HttpClient httpClient,
            IOptions<EmployeeServiceSettings> settings,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
            _baseLearnerLookupUrl = settings.Value.BaseLearnerLookupUrl;
            _baseLearnerUrl = settings.Value.BaseLearnerUrl;
            _baseEmployeeCsvUrl = settings.Value.BaseEmployeeCsvUrl;
        }

        public async Task<string> GetLearnersDxGridAsync(string queryString)
        {
            try
            {
                return await _httpClient.GetStringAsync($"{_baseLearnerUrl}{queryString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching DevExtreme grid data: {ex.Message}");
                return null;
            }
        }

        public async Task<ExternalLearnerDto> GetLearnerByCodeAsync(string Code)
        {
            try
            {
                var url = $"{_baseLearnerLookupUrl}/{Code}";
                var response = await _httpClient.GetFromJsonAsync<ExternalLearnerDto>(url);
                return response;
            }
            catch
            {
                return null;
            }
        }

        public async Task<AllLearnersApiResponse> GetLearnerAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<AllLearnersApiResponse>(
                    $"{_baseLearnerUrl}/all");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Bulk lookup: ??? HTTP 1 ????????? /api/Learner/all (Server cache 24h)
        /// ???? filter ????? codes ???????????? memory
        /// ????????? GetLearnerByCodeAsync ?????? (N+1 problem)
        /// </summary>
        public async Task<Dictionary<string, ExternalLearnerDto>> GetLearnersByCodesAsync(
            IEnumerable<string> codes)
        {
            try
            {
                var codeSet = codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var response = await GetLearnerAsync(); // reuse � server ?? MemoryCache 24h

                if (response?.data == null)
                    return new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase);

                return response.data
                    .Where(s => !string.IsNullOrEmpty(s.EId) && codeSet.Contains(s.EId))
                    .ToDictionary(
                        s => s.EId,
                        s => new ExternalLearnerDto
                        {
                            Code = s.EId,
                            Name = $"{s.EnglishFirstName} {s.EnglishLastName}".Trim(),
                            Division = s.Division,
                            Department = s.Department,
                            Section = s.Section,
                            Position = s.Position
                        },
                        StringComparer.OrdinalIgnoreCase
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLearnersByCodesAsync: {ex.Message}");
                return new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase);
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

        public async Task<DivisionApiResponse> GetLearnersByDivisionsAsync(string[] divisions, int skip = 0, int take = 20)
        {
            try
            {
                var keyObj = new { divisions };
                var encodedKey = Uri.EscapeDataString(JsonSerializer.Serialize(keyObj));
                var encodedSummary = Uri.EscapeDataString("[{\"selector\":\"EId\",\"summaryType\":\"count\"}]");
                var url = $"{_baseLearnerUrl}/divisions?key={encodedKey}&skip={skip}&take={take}" +
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
                var url = $"{_baseLearnerLookupUrl}/GetDistinctSections{queryString}";
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
                var url = $"{_baseLearnerLookupUrl}/GetDistinctDivisions{queryString}";
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
                var url = $"{_baseLearnerLookupUrl}/GetDistinctDepartments{queryString}";
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
                var url = $"{_baseLearnerLookupUrl}/GetDistinctPositions{queryString}";
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
