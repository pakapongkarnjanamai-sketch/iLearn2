using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class StudentGroupCategoriesController : Controller
    {
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Editor(int? id, int? parentId)
        {
            ViewBag.Id = id;
            ViewBag.ParentId = parentId;
            return View();
        }

        public IActionResult Detail(int id)
        {
            ViewBag.CategoryId = id;
            return View();
        }
    }
}
