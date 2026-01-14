using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class RolesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
