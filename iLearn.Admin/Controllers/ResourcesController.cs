using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class ResourcesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
