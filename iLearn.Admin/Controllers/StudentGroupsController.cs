using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class StudentGroupsController : Controller
    {
        public IActionResult Index() => View();

        public IActionResult Detail(int id)
        {
            ViewBag.GroupId = id;
            return View();
        }
    }
}
