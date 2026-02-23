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

        // Endpoint สำหรับดึงข้อมูลตามแผนก (Divisions)
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
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลจากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }

        // -------------------------------------------------------------------------
        // 🚀 ปรับปรุง: เปลี่ยนจากการรับ DataSourceLoadOptions เป็นดึง Query String ตรงๆ
        // -------------------------------------------------------------------------

        [HttpGet("GetDivisions")]
        public async Task<IActionResult> GetDivisions()
        {
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            var result = await _studentService.GetDivisionsAsync(queryString);

            if (result == null)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลแผนก (Divisions) จากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }

        [HttpGet("GetDepartments")]
        public async Task<IActionResult> GetDepartments()
        {
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            var result = await _studentService.GetDepartmentsAsync(queryString);

            if (result == null)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลฝ่าย (Departments) จากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }

        [HttpGet("GetSections")]
        public async Task<IActionResult> GetSections()
        {
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            var result = await _studentService.GetSectionsAsync(queryString);

            if (result == null)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลส่วนงาน (Sections) จากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }

        [HttpGet("Get")]
        public async Task<IActionResult> Get()
        {
            // 1. ดึงค่า Query String ทั้งหมดที่ DataGrid ส่งมา (เช่นการแบ่งหน้า, ค้นหา)
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;

            // 2. ส่งต่อให้ Service ไปคุยกับ API ต้นทาง
            var resultJson = await _studentService.GetStudentsDxGridAsync(queryString);

            if (resultJson == null)
            {
                return StatusCode(500, new { message = "ไม่สามารถเชื่อมต่อดึงข้อมูลจากฐานข้อมูลพนักงานได้ครับ" });
            }

            // 3. ส่ง JSON ที่ได้กลับไปให้หน้าบ้านตรงๆ เลย ด้วย ContentType application/json
            return Content(resultJson, "application/json");
        }
    }
}