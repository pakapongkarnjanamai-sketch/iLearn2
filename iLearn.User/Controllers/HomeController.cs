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
            if (HasLearnerCookieSession())
                return RedirectToAction("Index", "MyLearning");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmployee(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return Json(new { success = false, message = "กรุณาระบุรหัสพนักงาน" });

            ExternalLearnerDto? employee = null;
            try
            {
                var client = _httpClientFactory.CreateClient("iLearnAPI");
                var response = await client.GetAsync($"Learners/GetLearnerbyEID/{Uri.EscapeDataString(employeeCode)}");

                if (response.IsSuccessStatusCode)
                    employee = await response.Content.ReadFromJsonAsync<ExternalLearnerDto>();
            }
            catch
            {
                return Json(new { success = false, message = "ไม่สามารถเชื่อมต่อระบบได้ กรุณาลองใหม่อีกครั้ง" });
            }

            if (employee != null)
            {
                var divisionName = employee.Division ?? "-";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, employee.Code),
                    new Claim(ClaimTypes.Name, employee.Name),
                    new Claim("Department", employee.Department ?? "-"),
                    new Claim("Division", divisionName),
                };

                // ── Resolve DivisionId จาก Division Name เพื่อใช้ใน Data Isolation ──
                if (divisionName != "-")
                {
                    try
                    {
                        var client = _httpClientFactory.CreateClient("iLearnAPI");
                        var divResponse = await client.GetAsync($"Divisions/resolve-id?name={Uri.EscapeDataString(divisionName)}");
                        if (divResponse.IsSuccessStatusCode)
                        {
                            var divResult = await divResponse.Content.ReadFromJsonAsync<DivisionResolveResult>();
                            if (divResult?.DivisionId > 0)
                            {
                                claims.Add(new Claim("DivisionId", divResult.DivisionId.ToString()));
                            }
                        }
                    }
                    catch
                    {
                        // ถ้า resolve ไม่ได้ ให้ข้ามไป ระบบจะ fallback ใช้ Division Name แทน
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return Json(new { success = true, data = employee });
            }

            return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
        }

        [HttpGet]
        public IActionResult GetCurrentUser()
        {
            if (!HasLearnerCookieSession())
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                employeeCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                name = User.FindFirst(ClaimTypes.Name)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                department = User.FindFirst("Department")?.Value,
                division = User.FindFirst("Division")?.Value,
                divisionId = User.FindFirst("DivisionId")?.Value,
                profileImage = User.FindFirst("ProfileImage")?.Value
            });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        /// <summary>DTO ภายในสำหรับรับผลจาก API Divisions/resolve-id</summary>
        private class DivisionResolveResult
        {
            public int DivisionId { get; set; }
        }

        private bool HasLearnerCookieSession()
        {
            return User.Identities.Any(identity =>
                identity.IsAuthenticated &&
                string.Equals(identity.AuthenticationType, CookieAuthenticationDefaults.AuthenticationScheme, StringComparison.Ordinal) &&
                identity.HasClaim(claim =>
                    claim.Type == ClaimTypes.NameIdentifier &&
                    !string.IsNullOrWhiteSpace(claim.Value)));
        }
    }
}