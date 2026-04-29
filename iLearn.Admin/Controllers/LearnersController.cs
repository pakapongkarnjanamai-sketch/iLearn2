using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class LearnersController : Controller
    {
        [Authorize(Policy = "SuperAdminOnly")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Learners/Report")]
        [HttpGet("Learners/Report/{code}")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Report(string code)
        {
            ViewBag.LearnerCode = code ?? "";
            return View();
        }
    }
}