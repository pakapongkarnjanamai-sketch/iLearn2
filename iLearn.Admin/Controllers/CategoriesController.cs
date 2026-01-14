using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class CategoriesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
