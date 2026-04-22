using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class StudentGroupsController : Controller
    {
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Editor(int? id)
        {
            ViewBag.Id = id;
            return View();
        }

        public IActionResult Detail(int id)
        {
            ViewBag.GroupId = id;
            return View();
        }

        [HttpGet]
        public IActionResult AddMembers(int id)
        {
            ViewBag.GroupId = id;
            return View();
        }
    }
}
