using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class StudentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Profile(string code)
        {
            ViewBag.StudentCode = code ?? "";
            return View();
        }
    }
}