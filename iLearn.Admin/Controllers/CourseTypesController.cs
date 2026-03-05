using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class CourseTypesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
