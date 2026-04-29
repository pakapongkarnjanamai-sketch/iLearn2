using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class LearnerGroupCategoriesController : Controller
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

        [HttpGet]
        public IActionResult SelectCategory(int? selectedId, string? returnUrl, string? returnField)
        {
            ViewBag.SelectedId = selectedId;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.ReturnField = returnField ?? "categoryId";
            return View();
        }
    }
}
