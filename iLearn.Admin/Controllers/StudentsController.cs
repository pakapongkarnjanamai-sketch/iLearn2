using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class StudentsController : Controller
    {
        [Authorize(Policy = "SuperAdminOnly")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Students/Report")]
        [HttpGet("Students/Report/{code}")]
        [Authorize(Policy = "DomainUser")]
        public IActionResult Report(string code)
        {
            ViewBag.StudentCode = code ?? "";
            return View();
        }
    }
}