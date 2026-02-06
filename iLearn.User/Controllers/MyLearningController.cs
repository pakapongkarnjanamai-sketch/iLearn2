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

        public IActionResult Player(int enrollmentId)
        {
            ViewBag.EnrollmentId = enrollmentId;
          
            return View();
        }
    }
}