using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class CategoriesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Detail(int id)
        {
            ViewBag.CategoryId = id;
            return View();
        }

        [HttpGet]
        public IActionResult Report(int id)
        {
            ViewBag.CategoryId = id;
            return View("Report");
        }
    }
}
