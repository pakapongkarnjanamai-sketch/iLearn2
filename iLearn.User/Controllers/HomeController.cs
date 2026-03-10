using iLearn.User.Models;
using iLearn.Application.DTOs;
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
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "MyLearning");

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
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return Json(new { success = true, data = employee });
            }

            return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
        }

        [HttpGet]
        public IActionResult GetCurrentUser()
        {
            if (User.Identity?.IsAuthenticated != true)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                employeeCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                name = User.FindFirst(ClaimTypes.Name)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                department = User.FindFirst("Department")?.Value,
                division = User.FindFirst("Division")?.Value,
                profileImage = User.FindFirst("ProfileImage")?.Value
            });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
    }
}