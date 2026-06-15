using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
    public class LearnerApiService : ILearnerApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LearnerApiService> _logger;
        private readonly string _baseLearnerLookupUrl;
        private readonly string _baseLearnerUrl;
        private readonly string _baseEmployeeCsvUrl;
        private const string EmployeeCsvCacheKey = "employee_csv_directory";

        public LearnerApiService(
            HttpClient httpClient,
            IOptions<EmployeeServiceSettings> settings,
            IMemoryCache cache,
            ILogger<LearnerApiService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _baseLearnerLookupUrl = settings.Value.BaseLearnerLookupUrl;
            _baseLearnerUrl = settings.Value.BaseLearnerUrl;
            _baseEmployeeCsvUrl = settings.Value.BaseEmployeeCsvUrl;
        }

        public async Task<string> GetLearnersDxGridAsync(string queryString)
        {
            var url = $"{_baseLearnerUrl}{queryString}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var statusCode = (int)response.StatusCode;
                _logger.LogError("Upstream employee service returned non-success status code {StatusCode} for URL {Url}. Body: {Body}", statusCode, url, responseBody);
                
                if (statusCode >= 400 && statusCode < 500)
                {
                    throw new ArgumentException($"Upstream employee service returned client error ({statusCode}): {responseBody}");
                }
                else
                {
                    throw new HttpRequestException($"Upstream employee service returned error ({statusCode}): {responseBody}", null, response.StatusCode);
                }
            }
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<ExternalLearnerDto> GetLearnerByCodeAsync(string Code)
        {
            var url = $"{_baseLearnerLookupUrl}/{Code}";
            var response = await _httpClient.GetFromJsonAsync<ExternalLearnerDto>(url);
            return response;
        }

        public async Task<AllLearnersApiResponse> GetLearnerAsync()
        {
            return await _httpClient.GetFromJsonAsync<AllLearnersApiResponse>(
                $"{_baseLearnerUrl}/all");
        }

        /// <summary>
        /// Bulk lookup — ดึงข้อมูลพนักงานทั้งหมดด้วย HTTP 1 ครั้งผ่าน /api/Learner/all (มี server cache 24h)
        /// แล้วมา filter + map ใน memory แทนการเรียก GetLearnerByCodeAsync ทีละคน (N+1 problem)
        /// </summary>
        public async Task<Dictionary<string, ExternalLearnerDto>> GetLearnersByCodesAsync(
            IEnumerable<string> codes)
        {
            try
            {
                var codeSet = codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var response = await GetLearnerAsync(); // ยิงดึงทั้งหมด

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
                // Graceful degradation โดยตั้งใจ — enrichment ล้มไม่ควรทำทั้งหน้าพัง
                _logger.LogWarning(ex, "Error in GetLearnersByCodesAsync (gracefully degraded with empty dictionary): {Message}", ex.Message);
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
                // Graceful degradation โดยตั้งใจ — enrichment ล้มไม่ควรทำทั้งหน้าพัง
                _logger.LogWarning(ex, "Error in GetEmployeesByNidsAsync (gracefully degraded with empty dictionary): {Message}", ex.Message);
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
            var keyObj = new { divisions };
            var encodedKey = Uri.EscapeDataString(JsonSerializer.Serialize(keyObj));
            var encodedSummary = Uri.EscapeDataString("[{\"selector\":\"EId\",\"summaryType\":\"count\"}]");
            var url = $"{_baseLearnerUrl}/divisions?key={encodedKey}&skip={skip}&take={take}" +
                      $"&requireTotalCount=true&totalSummary={encodedSummary}";
            return await _httpClient.GetFromJsonAsync<DivisionApiResponse>(url);
        }

        public async Task<object> GetSectionsAsync(string queryString)
        {
            // เรียกใช้ URL สำหรับ GetDistinctSections
            var url = $"{_baseLearnerLookupUrl}/GetDistinctSections{queryString}";
            var response = await _httpClient.GetFromJsonAsync<object>(url);
            return response;
        }

        public async Task<object> GetDivisionsAsync(string queryString)
        {
            // เรียกใช้ URL สำหรับ GetDistinctDivisions
            var url = $"{_baseLearnerLookupUrl}/GetDistinctDivisions{queryString}";
            var response = await _httpClient.GetFromJsonAsync<object>(url);
            return response;
        }

        public async Task<object> GetDepartmentsAsync(string queryString)
        {
            // เรียกใช้ URL สำหรับ GetDistinctDepartments
            var url = $"{_baseLearnerLookupUrl}/GetDistinctDepartments{queryString}";
            var response = await _httpClient.GetFromJsonAsync<object>(url);
            return response;
        }

        public async Task<object> GetPositionsAsync(string queryString)
        {
            // เรียกใช้ URL สำหรับ GetDistinctPositions
            var url = $"{_baseLearnerLookupUrl}/GetDistinctPositions{queryString}";
            var response = await _httpClient.GetFromJsonAsync<object>(url);
            return response;
        }
    }
}
