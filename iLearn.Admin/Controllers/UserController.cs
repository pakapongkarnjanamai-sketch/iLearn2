using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
