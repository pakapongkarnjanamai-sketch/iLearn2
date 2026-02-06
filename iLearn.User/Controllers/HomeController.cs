using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
                // สร้างรายการ Claims (ข้อมูลพนักงานที่จะฝังไว้ใน Cookie)
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, employee.Code),
            new Claim(ClaimTypes.Name, employee.Name),
            new Claim("Department", employee.Department),
            new Claim("Division", employee.Division),
            new Claim("Section", employee.Section)
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // ทำการ Sign In เข้าสู่ระบบ
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return Json(new { success = true, data = employee });
            }

            return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
        }

        public async Task<IActionResult> Logout()
        {
            // ล้าง Cookie Authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // ส่งกลับไปหน้าแรก
            return RedirectToAction("Index", "Home");
        }
    }
}