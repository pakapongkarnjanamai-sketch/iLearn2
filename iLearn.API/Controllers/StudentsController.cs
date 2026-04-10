using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.RegularExpressions;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentApiService _studentService;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly ICurrentUserService _currentUser;

        public StudentsController(
            IStudentApiService studentService,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            ICurrentUserService currentUser)
        {
            _studentService = studentService;
            _enrollmentRepo = enrollmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _currentUser = currentUser;
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
            // Data isolation: non-SuperAdmin users can only see their own division.
            if (_currentUser.DivisionId.HasValue && !string.IsNullOrEmpty(_currentUser.DivisionName))
            {
                return Ok(new[] { new { Name = _currentUser.DivisionName } });
            }

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
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;

            // Data isolation: restrict student data to the current user's division.
            if (_currentUser.DivisionId.HasValue && !string.IsNullOrEmpty(_currentUser.DivisionName))
            {
                queryString = InjectDivisionFilter(queryString, _currentUser.DivisionName);
            }

            var resultJson = await _studentService.GetStudentsDxGridAsync(queryString);

            if (resultJson == null)
            {
                return StatusCode(500, new { message = "Failed to connect to the employee data source." });
            }

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

            if (_currentUser.DivisionId.HasValue)
            {
                if (studentInfo == null)
                    return NotFound(new { message = "Student not found." });

                if (string.IsNullOrWhiteSpace(_currentUser.DivisionName)
                    || !string.Equals(studentInfo.Division, _currentUser.DivisionName, StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { message = "Student not found." });
                }
            }

            // 2. Enrollment ทั้งหมดของ student พร้อม Course
            //    ใช้ ignoreQueryFilters เพื่อให้โหลด Course ที่ถูก Soft Delete ได้ด้วย
            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.StudentCode == code,
                includeProperties: "Course,AssignmentLinks",
                ignoreQueryFilters: true
            );

            // กรอง Enrollment ที่ถูก Soft Delete ออก (เพราะ ignoreQueryFilters ปิด filter ทุก entity)
            var activeEnrollments = enrollments.Where(e => !e.IsDeleted).ToList();

            // 3. สร้าง history — 1 Enrollment = 1 row ใน grid
            //    isAssignmentCancelled = เคยถูก Assign แต่ link ถูกลบทั้งหมดแล้ว
            var history = activeEnrollments
                .OrderByDescending(e => e.StartDate ?? e.CompletedDate)
                .Select(e => new
                {
                    enrollmentId = e.Id,
                    courseId = e.CourseId,
                    courseCode = e.Course != null ? e.Course.Code : "-",
                    courseTitle = e.Course != null ? e.Course.Title : "Unknown Course",
                    isCourseDeleted = e.Course != null && e.Course.IsDeleted,
                    progress = e.Progress,
                    isCompleted = e.IsCompleted,
                    startDate = e.StartDate,
                    dueDate = e.DueDate,
                    completedDate = e.CompletedDate,
                    totalScore = e.TotalScore,
                    totalTimeSpent = e.TotalTimeSpent,
                    hasActiveAssignment = e.AssignmentLinks.Any(),
                    // Enrollment ที่ไม่มี link เหลือ, ยังไม่จบ และเคยมี StartDate/DueDate
                    // = เคยถูก Assign แต่ Assignment ถูกลบไปแล้ว
                    isAssignmentCancelled = !e.AssignmentLinks.Any()
                                           && !e.IsCompleted
                                           && (e.StartDate.HasValue || e.DueDate.HasValue)
                }).ToList();

            // 4. KPI — คิดเฉพาะ Course ที่ยังใช้งานอยู่ (ไม่ถูก Soft Delete)
            var activeCourseHistory = history.Where(e => !e.isCourseDeleted).ToList();
            var totalCourses = activeCourseHistory.Count;
            var completedCourses = activeCourseHistory.Count(e => e.isCompleted);
            var inProgressCourses = activeCourseHistory.Count(e => !e.isCompleted && e.progress > 0);
            var totalTimeSpent = activeCourseHistory.Sum(e => e.totalTimeSpent);

            return Ok(new
            {
                success = true,
                data = new
                {
                    code = studentInfo != null ? studentInfo.Code : code,
                    name = studentInfo != null ? studentInfo.Name : code,
                    division = studentInfo != null ? studentInfo.Division : null,
                    department = studentInfo != null ? studentInfo.Department : null,
                    section = studentInfo != null ? studentInfo.Section : null,
                    position = studentInfo != null ? studentInfo.Position : null,
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

        /// <summary>
        /// Injects a DevExtreme-compatible Division filter into the proxy query string
        /// so that non-SuperAdmin users only see employees within their own division.
        /// </summary>
        private static string InjectDivisionFilter(string queryString, string divisionName)
        {
            var divFilter = System.Text.Json.JsonSerializer.Serialize(
                new object[] { "Division", "=", divisionName }
            );

            var filterMatch = Regex.Match(queryString, @"([?&])filter=([^&]*)");
            if (filterMatch.Success)
            {
                var existingFilter = Uri.UnescapeDataString(filterMatch.Groups[2].Value);
                var combined = $"[{existingFilter},\"and\",{divFilter}]";
                return queryString.Replace(filterMatch.Value,
                    $"{filterMatch.Groups[1].Value}filter={Uri.EscapeDataString(combined)}");
            }

            var separator = queryString.Contains('?') ? "&" : "?";
            return $"{queryString}{separator}filter={Uri.EscapeDataString(divFilter)}";
        }
    }
}