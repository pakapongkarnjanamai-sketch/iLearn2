using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.Helpers;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace iLearn.Infrastructure.Services
{
    public class EmployeeHubLearnerApiService : ILearnerApiService
    {
        private readonly EmployeeHubClient _client;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EmployeeHubLearnerApiService> _logger;

        private const string EmployeeHubCacheKey = "employeehub_active_directory";

        public EmployeeHubLearnerApiService(
            EmployeeHubClient client,
            IMemoryCache cache,
            ILogger<EmployeeHubLearnerApiService> logger)
        {
            _client = client;
            _cache = cache;
            _logger = logger;
        }

        private static EmployeeDto NormalizeDivision(EmployeeDto e)
        {
            if (e == null) return null!;
            if (string.Equals(e.Company, "NLC", StringComparison.OrdinalIgnoreCase))
                e.Division = "NLC";
            return e;
        }

        private async Task<List<EmployeeDto>> GetActiveEmployeesCachedAsync()
        {
            if (_cache.TryGetValue(EmployeeHubCacheKey, out List<EmployeeDto>? cachedEmployees) && cachedEmployees != null)
                return cachedEmployees;

            var employees = new List<EmployeeDto>();
            int page = 1;
            int pageSize = 200;
            while (true)
            {
                var result = await _client.GetEmployeesAsync(page, pageSize);
                if (result?.Items == null)
                    break;

                employees.AddRange(result.Items.Select(NormalizeDivision));
                if (result.Items.Count < pageSize || employees.Count >= result.Total)
                    break;

                page++;
            }

            _cache.Set(EmployeeHubCacheKey, employees, TimeSpan.FromMinutes(30));
            return employees;
        }

        private DataSourceLoadOptions ParseLoadOptions(string queryString)
        {
            var loadOptions = new DataSourceLoadOptions();
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(queryString))
            {
                var query = queryString.TrimStart('?');
                var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var parts = pair.Split('=', 2);
                    var key = Uri.UnescapeDataString(parts[0]);
                    var val = parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
                    dict[key] = val;
                }
            }

            DataSourceLoadOptionsParser.Parse(loadOptions, key => {
                if (dict.TryGetValue(key, out var val))
                    return val;
                return null;
            });

            return loadOptions;
        }

        public async Task<ExternalLearnerDto> GetLearnerByCodeAsync(string Code)
        {
            var emp = await _client.GetEmployeeByCodeAsync(Code);
            if (emp == null) return null!;

            emp = NormalizeDivision(emp);

            return new ExternalLearnerDto
            {
                Code = emp.EmpCode,
                Name = NameHelper.StripGenderPrefix(emp.FullNameEn),
                Section = emp.Section ?? string.Empty,
                Division = emp.Division ?? string.Empty,
                Department = emp.Department ?? string.Empty,
                Position = emp.Grade ?? string.Empty
            };
        }

        public async Task<AllLearnersApiResponse> GetLearnerAsync()
        {
            var emps = await GetActiveEmployeesCachedAsync();
            return new AllLearnersApiResponse
            {
                success = true,
                data = emps.Select(e => new LearnerDto
                {
                    Id = 0,
                    EId = e.EmpCode,
                    EnglishFirstName = NameHelper.StripGenderPrefix(e.FirstNameEn),
                    EnglishLastName = e.LastNameEn,
                    Section = e.Section ?? string.Empty,
                    Division = e.Division ?? string.Empty,
                    Department = e.Department ?? string.Empty,
                    Position = e.Grade ?? string.Empty
                }).ToList()
            };
        }

        public async Task<DivisionApiResponse> GetLearnersByDivisionsAsync(string[] divisions, int skip = 0, int take = 20)
        {
            var emps = await GetActiveEmployeesCachedAsync();
            var filtered = new List<EmployeeDto>();

            var hasNlc = divisions.Contains("NLC", StringComparer.OrdinalIgnoreCase);
            var otherDivs = divisions.Where(d => !string.Equals(d, "NLC", StringComparison.OrdinalIgnoreCase)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var emp in emps)
            {
                bool matches = false;
                if (hasNlc && string.Equals(emp.Company, "NLC", StringComparison.OrdinalIgnoreCase))
                {
                    matches = true;
                }
                else if (emp.Division != null && otherDivs.Contains(emp.Division))
                {
                    matches = true;
                }

                if (matches)
                {
                    filtered.Add(emp);
                }
            }

            var sorted = filtered.OrderBy(e => e.EmpCode).ToList();
            var totalCount = sorted.Count;

            var paged = sorted.Skip(skip).Take(take).Select(e => new DivisionLearnerDto
            {
                Id = 0,
                EId = e.EmpCode,
                EnglishFirstName = NameHelper.StripGenderPrefix(e.FirstNameEn),
                EnglishLastName = e.LastNameEn,
                Section = e.Section ?? string.Empty,
                Division = e.Division ?? string.Empty,
                Department = e.Department ?? string.Empty,
                Position = e.Grade ?? string.Empty
            }).ToList();

            return new DivisionApiResponse
            {
                data = paged,
                totalCount = totalCount,
                groupCount = 0,
                summary = new List<int> { totalCount }
            };
        }

        public async Task<string> GetLearnersDxGridAsync(string queryString)
        {
            var emps = await GetActiveEmployeesCachedAsync();
            var mappedItems = emps.Select(e => new LearnerGridRowDto
            {
                Id = 0,
                EId = e.EmpCode,
                NID = e.Nid ?? string.Empty,
                EnglishFirstName = NameHelper.StripGenderPrefix(e.FirstNameEn),
                EnglishLastName = e.LastNameEn,
                Division = e.Division ?? string.Empty,
                Department = e.Department ?? string.Empty,
                Section = e.Section ?? string.Empty,
                Position = e.Grade ?? string.Empty
            }).ToList();

            var loadOptions = ParseLoadOptions(queryString);
            var loadResult = DataSourceLoader.Load(mappedItems, loadOptions);

            var responseObj = new
            {
                data = loadResult.data,
                totalCount = loadResult.totalCount
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(responseObj, jsonOptions);
        }

        public async Task<object> GetDivisionsAsync(string queryString)
        {
            var emps = await GetActiveEmployeesCachedAsync();
            var lo = ParseLoadOptions(queryString);
            var filteredObj = DataSourceLoader.Load(emps, new DataSourceLoadOptions { Filter = lo.Filter });
            var filtered = (filteredObj.data as IEnumerable<EmployeeDto>) ?? Enumerable.Empty<EmployeeDto>();

            var divisions = new List<string> { "NLC" };
            var otherDivs = filtered
                .Where(e => !string.Equals(e.Company, "NLC", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Division))
                .Select(e => e.Division!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d);

            divisions.AddRange(otherDivs);

            return divisions.Select(d => new LookupNameDto { Name = d }).ToList();
        }

        public async Task<object> GetSectionsAsync(string queryString)
        {
            var emps = await GetActiveEmployeesCachedAsync();
            var lo = ParseLoadOptions(queryString);
            var filteredObj = DataSourceLoader.Load(emps, new DataSourceLoadOptions { Filter = lo.Filter });
            var filtered = (filteredObj.data as IEnumerable<EmployeeDto>) ?? Enumerable.Empty<EmployeeDto>();

            var sections = filtered
                .Where(e => !string.IsNullOrWhiteSpace(e.Section))
                .Select(e => e.Section!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .Select(s => new LookupNameDto { Name = s })
                .ToList();

            return sections;
        }

        public async Task<object> GetDepartmentsAsync(string queryString)
        {
            var emps = await GetActiveEmployeesCachedAsync();
            var lo = ParseLoadOptions(queryString);
            var filteredObj = DataSourceLoader.Load(emps, new DataSourceLoadOptions { Filter = lo.Filter });
            var filtered = (filteredObj.data as IEnumerable<EmployeeDto>) ?? Enumerable.Empty<EmployeeDto>();

            var departments = filtered
                .Where(e => !string.IsNullOrWhiteSpace(e.Department))
                .Select(e => e.Department!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d)
                .Select(d => new LookupNameDto { Name = d })
                .ToList();

            return departments;
        }

        public async Task<object> GetPositionsAsync(string queryString)
        {
            var emps = await GetActiveEmployeesCachedAsync();
            var lo = ParseLoadOptions(queryString);
            var filteredObj = DataSourceLoader.Load(emps, new DataSourceLoadOptions { Filter = lo.Filter });
            var filtered = (filteredObj.data as IEnumerable<EmployeeDto>) ?? Enumerable.Empty<EmployeeDto>();

            var positions = filtered
                .Where(e => !string.IsNullOrWhiteSpace(e.Grade))
                .Select(e => e.Grade!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p)
                .Select(p => new LookupNameDto { Name = p })
                .ToList();

            return positions;
        }

        public async Task<Dictionary<string, ExternalLearnerDto>> GetLearnersByCodesAsync(IEnumerable<string> codes)
        {
            try
            {
                var codeSet = codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var emps = await GetActiveEmployeesCachedAsync();

                return emps
                    .Where(e => !string.IsNullOrEmpty(e.EmpCode) && codeSet.Contains(e.EmpCode))
                    .ToDictionary(
                        e => e.EmpCode,
                        e => new ExternalLearnerDto
                        {
                            Code = e.EmpCode,
                            Name = NameHelper.StripGenderPrefix(e.FullNameEn),
                            Section = e.Section ?? string.Empty,
                            Division = e.Division ?? string.Empty,
                            Department = e.Department ?? string.Empty,
                            Position = e.Grade ?? string.Empty
                        },
                        StringComparer.OrdinalIgnoreCase
                    );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in GetLearnersByCodesAsync (gracefully degraded with empty dictionary): {Message}", ex.Message);
                return new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids)
        {
            try
            {
                var nidList = nids
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (nidList.Count == 0)
                    return new Dictionary<string, EmployeeCsvDto>(StringComparer.OrdinalIgnoreCase);

                var employees = new List<EmployeeDto>();
                const int batchSize = 200;
                for (int i = 0; i < nidList.Count; i += batchSize)
                {
                    var batch = nidList.Skip(i).Take(batchSize);
                    var result = await _client.FindByNidsAsync(batch);
                    if (result?.Items != null)
                    {
                        employees.AddRange(result.Items.Select(NormalizeDivision));
                    }
                }

                return employees
                    .Where(e => !string.IsNullOrWhiteSpace(e.Nid))
                    .GroupBy(e => e.Nid!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => new EmployeeCsvDto
                        {
                            Id = 0,
                            EId = g.First().EmpCode,
                            NID = g.Key,
                            Email = g.First().Email ?? string.Empty,
                            ThaiFirstName = NameHelper.StripGenderPrefix(g.First().FirstNameTh),
                            ThaiLastName = g.First().LastNameTh,
                            EnglishFirstName = NameHelper.StripGenderPrefix(g.First().FirstNameEn),
                            EnglishLastName = g.First().LastNameEn,
                            Section = g.First().Section ?? string.Empty,
                            Division = g.First().Division ?? string.Empty,
                            Department = g.First().Department ?? string.Empty,
                            Position = g.First().Grade ?? string.Empty
                        },
                        StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in GetEmployeesByNidsAsync (gracefully degraded with empty dictionary): {Message}", ex.Message);
                return new Dictionary<string, EmployeeCsvDto>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
