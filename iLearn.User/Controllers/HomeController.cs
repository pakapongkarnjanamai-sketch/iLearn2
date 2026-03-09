using iLearn.User.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace iLearn.User.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "MyLearning");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmployee(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return Json(new { success = false, message = "กรุณาระบุรหัสพนักงาน" });

            ExternalStudentDto? employee = null;
            try
            {
                var client = _httpClientFactory.CreateClient("iLearnAPI");
                var response = await client.GetAsync($"Students/GetStudentbyEID/{Uri.EscapeDataString(employeeCode)}");

                if (response.IsSuccessStatusCode)
                    employee = await response.Content.ReadFromJsonAsync<ExternalStudentDto>();
            }
            catch
            {
                return Json(new { success = false, message = "ไม่สามารถเชื่อมต่อระบบได้ กรุณาลองใหม่อีกครั้ง" });
            }

            if (employee != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, employee.Code),
                    new Claim(ClaimTypes.Name, employee.Name),
                    new Claim("Department", employee.Department ?? "-"),
                    new Claim("Division", employee.Division ?? "-"),
                    // เพิ่ม Email และ ProfileImage ถ้ามี
                    //new Claim(ClaimTypes.Email, employee.Email ?? ""),
                    //new Claim("ProfileImage", employee.ProfileImageUrl ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return Json(new { success = true, data = employee });
            }

            return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
        }

        // ✨ เพิ่ม Action สำหรับดึงข้อมูล User ปัจจุบัน
        [HttpGet]
        public IActionResult GetCurrentUser()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false });
            }

            var userData = new
            {
                success = true,
                employeeCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                name = User.FindFirst(ClaimTypes.Name)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                department = User.FindFirst("Department")?.Value,
                division = User.FindFirst("Division")?.Value,
                profileImage = User.FindFirst("ProfileImage")?.Value
            };

            return Json(userData);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
    }
}