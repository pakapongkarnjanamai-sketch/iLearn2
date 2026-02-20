using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentApiService _studentService;

        public StudentsController(IStudentApiService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet("GetStudentbyEID/{employeeCode}")]
        public async Task<IActionResult> GetStudentbyEID(string employeeCode)
        {
            // เช็คว่ามีการส่งรหัสพนักงานมาหรือไม่
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                return BadRequest(new { message = "รหัสพนักงานต้องไม่เป็นค่าว่างครับ" });
            }

            var student = await _studentService.GetStudentByCodeAsync(employeeCode);

            // ถ้าหาข้อมูลไม่เจอ ให้ส่ง 404 Not Found กลับไป
            if (student == null)
            {
                return NotFound(new { message = $"ไม่พบข้อมูลพนักงานรหัส {employeeCode} ครับ" });
            }

            // ถ้าสำเร็จ ส่งข้อมูลพร้อม Status 200 OK
            return Ok(student);
        }

        [HttpGet("GetStudentAsync")]
        public async Task<IActionResult> GetStudentAsync()
        {
            var student = await _studentService.GetStudentAsync();

            if (student == null)
            {
                return NotFound(new { message = "ไม่พบข้อมูลพนักงานครับ" });
            }

            return Ok(student);
        }

        // เพิ่ม Endpoint ใหม่สำหรับดึงข้อมูลตามแผนก (Divisions)
        [HttpGet("divisions")]
        public async Task<IActionResult> GetStudentsByDivisions(
            [FromQuery] string[] divisions,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            if (divisions == null || divisions.Length == 0)
            {
                return BadRequest(new { message = "กรุณาระบุ Divisions อย่างน้อย 1 แผนกครับ" });
            }

            var result = await _studentService.GetStudentsByDivisionsAsync(divisions, skip, take);

            if (result == null)
            {
                // ส่ง 500 Internal Server Error ถ้าระบบหลังบ้านดึงข้อมูลจาก API ต้นทางไม่สำเร็จ
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลจากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }
    }
}