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
    public class StudentsController : ControllerBase
    {
        private readonly IStudentApiService _studentService;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;

        public StudentsController(
            IStudentApiService studentService,
            IGenericRepository<Enrollment> enrollmentRepo)
        {
            _studentService = studentService;
            _enrollmentRepo = enrollmentRepo;
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

        [HttpGet("GetPositions")]
        public async Task<IActionResult> GetPositions()
        {
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            var result = await _studentService.GetPositionsAsync(queryString);

            if (result == null)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลตำแหน่ง (Positions) จากเซิร์ฟเวอร์หลักครับ" });
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

        // ── Student Profile: ข้อมูลส่วนตัว + ประวัติการเรียน ────────────────────
        [HttpGet("profile/{code}")]
        public async Task<IActionResult> GetProfile(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { message = "Employee code is required." });

            // 1. ข้อมูลส่วนตัวจาก External API
            var studentInfo = await _studentService.GetStudentByCodeAsync(code);

            // 2. Enrollment ทั้งหมดของ student พร้อม Course
            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.StudentCode == code,
                includeProperties: "Course"
            );

            // 3. สร้าง history
            var history = enrollments
                .OrderByDescending(e => e.StartDate ?? e.CompletedDate)
                .Select(e => new
                {
                    enrollmentId          = e.Id,
                    courseId              = e.CourseId,
                    courseCode            = e.Course != null ? e.Course.Code  : "-",
                    courseTitle           = e.Course != null ? e.Course.Title : "Unknown Course",
                    progress              = e.Progress,
                    isCompleted           = e.IsCompleted,
                    startDate             = e.StartDate,
                    dueDate               = e.DueDate,
                    completedDate         = e.CompletedDate,
                    totalScore            = e.TotalScore,
                    totalTimeSpent        = e.TotalTimeSpent,
                    assignmentRuleId      = e.AssignmentRuleId,
                    // Enrollment ที่ไม่มี AssignmentRuleId, ยังไม่จบ และเคยมี StartDate หรือ DueDate
                    // = เคยถูก Assign แต่ Assignment ถูกลบไปแล้ว
                    isAssignmentCancelled = !e.AssignmentRuleId.HasValue
                                           && !e.IsCompleted
                                           && (e.StartDate.HasValue || e.DueDate.HasValue)
                }).ToList();

            // 4. KPI
            var totalCourses      = history.Count;
            var completedCourses  = history.Count(e => e.isCompleted);
            var inProgressCourses = history.Count(e => !e.isCompleted && e.progress > 0);
            var totalTimeSpent    = history.Sum(e => e.totalTimeSpent);

            return Ok(new
            {
                success = true,
                data = new
                {
                    code       = studentInfo != null ? studentInfo.Code       : code,
                    name       = studentInfo != null ? studentInfo.Name       : code,
                    division   = studentInfo != null ? studentInfo.Division   : null,
                    department = studentInfo != null ? studentInfo.Department : null,
                    section    = studentInfo != null ? studentInfo.Section    : null,
                    position   = studentInfo != null ? studentInfo.Position   : null,
                    kpi = new
                    {
                        totalCourses,
                        completedCourses,
                        inProgressCourses,
                        totalTimeSpentSeconds = totalTimeSpent
                    },
                    enrollments = history
                }
            });
        }
    }
}