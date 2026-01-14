using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class EnrollmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
