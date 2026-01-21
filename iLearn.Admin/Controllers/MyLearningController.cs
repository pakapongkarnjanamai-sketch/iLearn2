using iLearn.Admin.Models;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace iLearn.Admin.Controllers
{
    [Authorize]
    public class MyLearningController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        public MyLearningController(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Action นี้จะถูกเรียกเมื่อกดปุ่ม "เข้าเรียน"
        public async Task<IActionResult> Player(int enrollmentId)
        {
            // 1. สร้าง Client (ชื่อต้องตรงกับที่ลงทะเบียนใน Program.cs)
            var client = _clientFactory.CreateClient("iLearnAPI");

            try
            {
                // 2. ยิง API ไปขอข้อมูล Player (URL, IDs) แทนการ Query Database เอง
                var response = await client.GetAsync($"api/LearningLogs/player-info/{enrollmentId}");

                if (!response.IsSuccessStatusCode)
                {
                    return View("Error", new ErrorViewModel { RequestId = "API Error: " + response.ReasonPhrase });
                }

                // 3. แปลงข้อมูล JSON กลับมาเป็น Object
                var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<PlayerInfoDto>>();

                if (apiResult == null || !apiResult.Success || apiResult.Data == null)
                {
                    return NotFound("ไม่พบข้อมูลเนื้อหาเรียน (Content not found)");
                }

                var data = apiResult.Data;

                // 4. ส่งข้อมูลเข้า ViewBag ให้ JavaScript (scorm-adapter.js) ใช้งาน
                ViewBag.CourseVersionId = data.CourseVersionId;
                ViewBag.ResourceId = data.ResourceId;
                ViewBag.LaunchUrl = data.LaunchUrl;
                ViewBag.StudentCode = data.StudentCode;

                // ดึง URL ของ API จาก Config เพื่อส่งให้ JS ใช้ยิง Commit/Initialize
                ViewBag.ServiceUrl = _configuration.GetValue<string>("ApiSettings:BaseUrl");

                return View();
            }
            catch (Exception ex)
            {
                // กรณีต่อ API ไม่ติด
                return View("Error", new ErrorViewModel { RequestId = "Connection Error: " + ex.Message });
            }
        }
    }
}