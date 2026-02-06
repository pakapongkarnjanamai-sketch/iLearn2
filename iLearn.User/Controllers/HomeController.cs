using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using iLearn.Application.Interfaces.Services; // สมมติว่ามี Interface นี้จากขั้นตอนก่อนหน้า

namespace iLearn.User.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEmployeeApiService _employeeService;

        public HomeController(IEmployeeApiService employeeService)
        {
            _employeeService = employeeService;
        }

        public IActionResult Index()
        {
            // ถ้า Login อยู่แล้ว ให้ข้ามไปหน้า MyLearning เลย
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "MyLearning");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmployee(string employeeCode)
        {
            var employee = await _employeeService.GetEmployeeByCodeAsync(employeeCode);

            if (employee != null)
            {
                // เก็บข้อมูลเข้า Claims เพื่อใช้ในหน้า MyLearning
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, employee.Code),
                    new Claim(ClaimTypes.Name, employee.Name),
                    new Claim("Department", employee.Department ?? "-"),
                    new Claim("Division", employee.Division ?? "-")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return Json(new { success = true, data = employee });
            }

            return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
    }
}