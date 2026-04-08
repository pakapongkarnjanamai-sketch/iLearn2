using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class AssignmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Form(int? id, int? courseId, int? groupId)
        {
            ViewBag.Id = id;
            ViewBag.CourseId = courseId;
            ViewBag.GroupId = groupId;
            return View();
        }

        // --- เพิ่มฟังก์ชันนี้เข้าไปใหม่ ---
        public IActionResult Detail(int id)
        {
            ViewBag.AssignmentId = id; // ส่ง ID ของกฎไปให้หน้า View ใช้งาน
            return View();
        }

        [HttpGet]
        public IActionResult Report(int id)
        {
            ViewBag.AssignmentId = id;
            return View("Report");
        }

        public IActionResult Gantt()
        {
            return View();
        }
    }
}