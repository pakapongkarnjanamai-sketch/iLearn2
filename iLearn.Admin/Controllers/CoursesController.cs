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
    }
}