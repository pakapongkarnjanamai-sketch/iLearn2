// File: iLearn.API/Controllers/DashboardController.cs
using iLearn.Application.Interfaces.Repositories;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<Resource> _resourceRepo;

        public DashboardController(
            IGenericRepository<Course> courseRepo,
            IGenericRepository<User> userRepo,
            IGenericRepository<Resource> resourceRepo)
        {
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _resourceRepo = resourceRepo;
        }

        [HttpGet("Stats")]
        public async Task<IActionResult> GetStats()
        {
            // ใช้ CountAsync ซึ่งจะไป Gen SQL "SELECT COUNT(*)..." ที่เร็วมาก และไม่ดึง Data ออกมา
            var activeCourses = await _courseRepo.CountAsync(c => c.IsActive);
            var draftCourses = await _courseRepo.CountAsync(c => !c.IsActive);
            var totalUsers = await _userRepo.CountAsync(); // นับทั้งหมด หรือจะกรอง IsActive ก็ได้
            var totalResources = await _resourceRepo.CountAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    activeCourses,
                    draftCourses,
                    totalUsers,
                    totalResources
                }
            });
        }
    }
}