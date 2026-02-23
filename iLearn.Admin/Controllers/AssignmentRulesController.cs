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
            ViewBag.CourseId = courseId;
            return View();
        }

        // --- เพิ่มฟังก์ชันนี้เข้าไปใหม่ ---
        public IActionResult Progress(int id)
        {
            ViewBag.AssignmentRuleId = id; // ส่ง ID ของกฎไปให้หน้า View ใช้งาน
            return View();
        }
    }
}