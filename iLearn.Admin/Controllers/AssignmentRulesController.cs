using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class AssignmentRulesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Form(int? id, int? courseId)
        {
            ViewBag.Id = id;
            ViewBag.CourseId = courseId; // ส่งค่าวิชาที่ถูกเลือกมาจากหน้าอื่นไปให้ View
            return View();
        }
    }
}
