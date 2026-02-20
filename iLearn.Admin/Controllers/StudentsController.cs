using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class StudentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}