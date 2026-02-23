using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentRulesController : ControllerBase
    {
        private readonly IGenericRepository<AssignmentRule> _repo;
        private readonly ICourseAssignmentService _assignmentService;

        public AssignmentRulesController(
            IGenericRepository<AssignmentRule> repo,
            ICourseAssignmentService assignmentService)
        {
            _repo = repo;
            _assignmentService = assignmentService;
        }

        // --- [NEW] API สำหรับดึงประวัติมาแสดงที่หน้า Index แบบยุบรวม Row ---
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            // 1. ดึงข้อมูล Rule ทั้งหมด และบอก Repository ให้ Include ตาราง Course มาด้วย
            var rules = await _repo.GetAsync(includeProperties: "Course");

            // 2. จัดกลุ่มข้อมูล (Group By) ด้วย AssignmentNo
            var groupedHistory = rules
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo)) // ป้องกันรายการที่ไม่มีเลขที่
                .GroupBy(r => r.AssignmentNo)
                .Select(g => new
                {
                    // ใช้ Id ของวิชาแรกในกลุ่ม เป็นตัวแทนเพื่อส่งไปหน้า Progress Dashboard
                    Id = g.First().Id,
                    AssignmentNo = g.Key,
                    Description = g.First().Description,
                    EmployeeCodes = g.First().EmployeeCodes,
                    StartDate = g.First().StartDate,
                    DueDate = g.First().DueDate,

                    // 3. เอาชื่อ Course ของทุก Row ในกลุ่มนี้มาต่อกันด้วยเครื่องหมายจุลภาค (Comma)
                    CourseNames = string.Join(", ", g.Select(c => c.Course?.Title ?? "Unknown Course").Distinct())
                })
                .OrderByDescending(x => x.AssignmentNo) // เรียงจากล่าสุดไปเก่าสุด
                .ToList();

            // ส่งกลับในรูปแบบที่ DevExtreme ต้องการ
            return Ok(new { data = groupedHistory, totalCount = groupedHistory.Count });
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var rules = await _repo.GetAsync(r => r.CourseId == courseId);
            return Ok(rules.Select(r => new { r.Id, r.CourseId })); // ปรับให้คืนค่าตามความเหมาะสม
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _repo.GetByIdAsync(id);
            if (rule == null) return NotFound();

            // Optional: ถ้าจะลบ ควรเขียน Logic ลบทุก Rule ที่มี AssignmentNo เดียวกันด้วย
            var relatedRules = await _repo.GetAsync(r => r.AssignmentNo == rule.AssignmentNo);
            foreach (var r in relatedRules)
            {
                await _repo.DeleteAsync(r);
            }

            return NoContent();
        }
    }
}