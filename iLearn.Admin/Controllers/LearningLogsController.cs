using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class LearningLogsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
