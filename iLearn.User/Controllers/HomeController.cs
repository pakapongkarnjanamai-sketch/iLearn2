using Microsoft.AspNetCore.Mvc;

namespace iLearn.User.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
