using DevExtreme.AspNet.Mvc;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace iLearn.Infrastructure.Services
{
    public class StudentApiService : IStudentApiService
    {
        private readonly HttpClient _httpClient;
        // กำหนด Base URL ให้เรียกใช้ซ้ำได้ง่ายและลดการพิมพ์ผิด
        private const string BaseStudentLookupUrl = "https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/StudentLookup";

        public StudentApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetStudentsDxGridAsync(string queryString)
        {
            try
            {
                var url = $"https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/Student{queryString}";
                var response = await _httpClient.GetStringAsync(url);
                return response;
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
                var url = $"https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/Student/all";
                var response = await _httpClient.GetFromJsonAsync<AllStudentsApiResponse>(url);
                return response;
            }
            catch
            {
                return null;
            }
        }

        public async Task<DivisionApiResponse> GetStudentsByDivisionsAsync(string[] divisions, int skip = 0, int take = 20)
        {
            try
            {
                var keyObj = new { divisions = divisions };
                var keyJson = JsonSerializer.Serialize(keyObj);

                var encodedKey = Uri.EscapeDataString(keyJson);
                var encodedSummary = Uri.EscapeDataString("[{\"selector\":\"EId\",\"summaryType\":\"count\"}]");

                var url = $"https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/Student/divisions?key={encodedKey}&skip={skip}&take={take}&requireTotalCount=true&totalSummary={encodedSummary}";

                var response = await _httpClient.GetFromJsonAsync<DivisionApiResponse>(url);

                return response;
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