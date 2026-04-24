using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class StudentGroupCategoriesController : Controller
    {
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Editor(int? id)
        {
            ViewBag.Id = id;
            return View();
        }

        public IActionResult Detail(int id)
        {
            ViewBag.CategoryId = id;
            return View();
        }
    }
}
