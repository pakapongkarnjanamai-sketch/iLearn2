using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

    }
}
