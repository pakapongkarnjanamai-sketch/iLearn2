using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
