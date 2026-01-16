using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class CoursesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Wizard()
        {
            return View();
        }
    }
}
