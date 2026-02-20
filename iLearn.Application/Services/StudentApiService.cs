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

        public StudentApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ExternalStudentDto> GetStudentByCodeAsync(string Code)
        {
            try
            {
                var url = $"https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/StudentLookup/{Code}";

                var response = await _httpClient.GetFromJsonAsync<ExternalStudentDto>(url);
                return response;
            }
            catch
            {
                return null;
            }
        }

        public async Task<StudentDto> GetStudentAsync()
        {
            try
            {
                var url = $"https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/Student";

                var response = await _httpClient.GetFromJsonAsync<StudentDto>(url);
                return response;
            }
            catch
            {
                return null;
            }
        }

        // ฟังก์ชันใหม่สำหรับเรียก API Divisions
        public async Task<DivisionApiResponse> GetStudentsByDivisionsAsync(string[] divisions, int skip = 0, int take = 20)
        {
            try
            {
                // 1. แปลงตัวแปร divisions array ให้เป็น Object ตามโครงสร้างที่ API คุณคาดหวัง
                // ตัวอย่าง: จะได้รูปแบบเป็น {"divisions":["PD1","PD2","NLC"]}
                var keyObj = new { divisions = divisions };
                var keyJson = JsonSerializer.Serialize(keyObj);

                // 2. เข้ารหัส JSON string ให้ปลอดภัยสำหรับการส่งผ่าน URL ป้องกันไม่ให้เครื่องหมาย " หรือ { ทำให้ URL พัง
                var encodedKey = Uri.EscapeDataString(keyJson);
                var encodedSummary = Uri.EscapeDataString("[{\"selector\":\"EId\",\"summaryType\":\"count\"}]");

                // 3. ประกอบ URL โดยใส่ Parameter สำหรับแบ่งหน้า (skip, take) และตัวกรองอื่นๆ
                var url = $"https://AP-NTC2137-PRWB/Utility/EmployeeServiceV2/api/Student/divisions?key={encodedKey}&skip={skip}&take={take}&requireTotalCount=true&totalSummary={encodedSummary}";

                // 4. ยิง Request ไปยัง API และแปลงผลลัพธ์ (Deserialize) กลับมาใส่ใน DTO ของเรา
                var response = await _httpClient.GetFromJsonAsync<DivisionApiResponse>(url);

                return response;
            }
            catch (Exception ex)
            {
                // หากเชื่อมต่อไม่ได้หรือเกิดข้อผิดพลาด จะส่ง null กลับไป 
                // คุณสามารถนำ ex.Message ไปบันทึกลง Logger ได้ในอนาคตครับ
                Console.WriteLine($"Error fetching divisions: {ex.Message}");
                return null;
            }
        }
    }
}