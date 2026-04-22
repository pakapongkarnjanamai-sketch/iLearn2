using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class AssignmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult BulkAssign(int? id, int? courseId, int? groupId)
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

        [HttpGet("Assignments/Report")]
        [HttpGet("Assignments/Report/{id:int}")]
        [Authorize(Policy = "DomainUser")]
        public IActionResult Report(int id)
        {
            ViewBag.AssignmentId = id;
            return View("Report");
        }

        [HttpGet(@"Assignments/Report/{assignmentNo:regex(^AS-\d{{8}}-\d+$)}")]
        [Authorize(Policy = "DomainUser")]
        public IActionResult ReportByNo(string assignmentNo)
        {
            ViewBag.AssignmentNo = assignmentNo;
            return View("Report");
        }

        public IActionResult Gantt()
        {
            return View();
        }
    }
}