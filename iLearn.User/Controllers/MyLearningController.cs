using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.User.Controllers
{
    [Authorize]
    public class MyLearningController : Controller
    {
        private readonly IConfiguration _configuration;

        public MyLearningController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        // เปลี่ยนรับ parameter จาก enrollmentId เป็น courseId
        public IActionResult Player(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }
    }
}