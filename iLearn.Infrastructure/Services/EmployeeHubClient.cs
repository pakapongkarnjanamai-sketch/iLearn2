using iLearn.Application.Common;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace iLearn.Infrastructure.Services
{
    // Mirrors EmployeeHub Dtos (see .agents/skills/api-employeehub-api-reference)
    public class EmployeeDto
    {
        public string EmpCode { get; set; } = string.Empty;
        public string FirstNameEn { get; set; } = string.Empty;
        public string FirstNameTh { get; set; } = string.Empty;
        public string LastNameEn { get; set; } = string.Empty;
        public string LastNameTh { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public string FullNameTh { get; set; } = string.Empty;
        public string? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? MaritalStatus { get; set; }
        public string Nationality { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Grade { get; set; }
        public int? GradeLevel { get; set; }
        public string? CostCenter { get; set; }
        public string? OrgCode { get; set; }
        public string? Nid { get; set; }
        public string? JoinDate { get; set; }
        public string? SourceUpdatedDate { get; set; }
        public string? SourceUpdatedBy { get; set; }
        public int? LastSyncRunId { get; set; }
    }

    public class EmployeeHubPagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class FindByNidsResultDto
    {
        public int Count { get; set; }
        public List<EmployeeDto> Items { get; set; } = new();
    }

    public class EmployeeHubClient
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public EmployeeHubClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<EmployeeHubPagedResult<EmployeeDto>> GetEmployeesAsync(int page, int pageSize)
        {
            return await _httpClient.GetFromJsonAsync<EmployeeHubPagedResult<EmployeeDto>>($"api/employees?page={page}&pageSize={pageSize}", JsonOptions) ?? new EmployeeHubPagedResult<EmployeeDto>();
        }

        public async Task<EmployeeDto?> GetEmployeeByCodeAsync(string empCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/employees/{empCode}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<EmployeeDto>(JsonOptions);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<FindByNidsResultDto> FindByNidsAsync(IEnumerable<string> nids)
        {
            var response = await _httpClient.PostAsJsonAsync("api/employees/find-by-nids", new { nids }, JsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FindByNidsResultDto>(JsonOptions) ?? new FindByNidsResultDto();
        }

        public async Task<string> CheckHealthAsync()
        {
            return await _httpClient.GetStringAsync("health");
        }
    }
}
