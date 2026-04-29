using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class ContentItemsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
