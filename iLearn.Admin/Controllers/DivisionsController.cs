using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class DivisionsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
