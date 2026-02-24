using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace iLearn.Admin.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ILogger<CoursesController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(int categoryId)
        {
            ViewBag.categoryId = categoryId;
            return View();
        }

        // [เพิ่ม] Action สำหรับหน้าจัดการ Course Versions
        [HttpGet]
        public IActionResult Version(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }
        [HttpGet]
        public IActionResult Player(int versionId)
        {
            ViewBag.VersionId = versionId;
            return View();
        }
    
        [HttpGet]
        public IActionResult Form(int? id)
        {
            ViewBag.Id = id; // ถ้าเป็น null คือ Create, ถ้ามีค่าคือ Edit
            return View();
        }

        // [เพิ่มส่วนนี้] Action สำหรับหน้าฟอร์มจัดการ Version
        [HttpGet]
        public IActionResult VersionForm(int? id, int courseId)
        {
            ViewBag.Id = id;
            ViewBag.CourseId = courseId;
            return View();
        }
        public IActionResult Drafts()
        {
            ViewBag.Title = "จัดการแบบร่าง (Draft Courses)";
            return View();
        }
        [HttpGet]
        public IActionResult Dashboard(int id)
        {
            ViewBag.CourseId = id;
            // ทริค: คุณอาจจะดึงข้อมูล Title ของคอร์สมาแสดงบน Title Bar ด้วยก็ได้
            // var course = _courseService.GetById(id);
            // ViewBag.CourseTitle = course.Title;

            return View();
        }
    }
}