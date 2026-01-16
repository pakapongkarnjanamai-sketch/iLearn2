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

        public IActionResult Index()
        {
            return View();
        }

        // [เพิ่ม] Action สำหรับหน้าจัดการ Course Versions
        [HttpGet]
        public IActionResult Version()
        {
            // ไม่ต้องรับค่า courseId จาก URL แล้ว
            // ปล่อยให้ View จัดการอ่านจาก SessionStorage เอง
            return View();
        }
    }
}