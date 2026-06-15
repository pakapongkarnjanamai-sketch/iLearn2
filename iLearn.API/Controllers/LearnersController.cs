using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LearnersController : ControllerBase
    {
        private readonly ILearnerApiService _learnerService;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly ICurrentUserService _currentUser;

        public LearnersController(
            ILearnerApiService learnerService,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            ICurrentUserService currentUser)
        {
            _learnerService = learnerService;
            _enrollmentRepo = enrollmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _currentUser = currentUser;
        }

        [AllowAnonymous]
        [HttpGet("GetLearnerbyEID/{employeeCode}")]
        public async Task<IActionResult> GetLearnerbyEID(string employeeCode)
        {
            // เช็คว่ามีการส่งรหัสพนักงานมาหรือไม่
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                return BadRequest(new { message = "รหัสพนักงานต้องไม่เป็นค่าว่างครับ" });
            }

            var learner = await _learnerService.GetLearnerByCodeAsync(employeeCode);

            // ถ้าหาข้อมูลไม่เจอ ให้ส่ง 404 Not Found กลับไป
            if (learner == null)
            {
                return NotFound(new { message = $"ไม่พบข้อมูลพนักงานรหัส {employeeCode} ครับ" });
            }

            // ถ้าสำเร็จ ส่งข้อมูลพร้อม Status 200 OK
            return Ok(learner);
        }

        // Endpoint สำหรับดึงข้อมูลตามแผนก (Divisions)
        [Authorize(Policy = "AdminOnly")]
        [HttpGet("divisions")]
        public async Task<IActionResult> GetLearnersByDivisions(
            [FromQuery] string[] divisions,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            if (divisions == null || divisions.Length == 0)
            {
                return BadRequest(new { message = "กรุณาระบุ Divisions อย่างน้อย 1 แผนกครับ" });
            }

            var result = await _learnerService.GetLearnersByDivisionsAsync(divisions, skip, take);

            if (result == null)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลจากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }

        // -------------------------------------------------------------------------
        // 🚀 ปรับปรุง: เปลี่ยนจากการรับ DataSourceLoadOptions เป็นดึง Query String ตรงๆ
        // -------------------------------------------------------------------------

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("GetDivisions")]
        public async Task<IActionResult> GetDivisions()
        {
            // Data isolation: non-SuperAdmin users can only see their own division.
            if (_currentUser.DivisionId.HasValue && !string.IsNullOrEmpty(_currentUser.DivisionName))
            {
                return Ok(new[] { new { Name = _currentUser.DivisionName } });
            }

            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            var result = await _learnerService.GetDivisionsAsync(queryString);

            if (result == null)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลแผนก (Divisions) จากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("GetDepartments")]
        public async Task<IActionResult> GetDepartments()
        {
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            var result = await _learnerService.GetDepartmentsAsync(queryString);

            if (result == null)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลฝ่าย (Departments) จากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("GetSections")]
        public async Task<IActionResult> GetSections()
        {
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            var result = await _learnerService.GetSectionsAsync(queryString);

            if (result == null)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลส่วนงาน (Sections) จากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("GetPositions")]
        public async Task<IActionResult> GetPositions()
        {
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            var result = await _learnerService.GetPositionsAsync(queryString);

            if (result == null)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลตำแหน่ง (Positions) จากเซิร์ฟเวอร์หลักครับ" });
            }

            return Ok(result);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("Get")]
        public async Task<IActionResult> Get()
        {
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;

            // Map camelCase fields from frontend to PascalCase for external employee API
            queryString = MapFilterFieldNames(queryString);

            // Data isolation: restrict learner data to the current user's division.
            if (_currentUser.DivisionId.HasValue && !string.IsNullOrEmpty(_currentUser.DivisionName))
            {
                queryString = InjectDivisionFilter(queryString, _currentUser.DivisionName);
            }

            var resultJson = await _learnerService.GetLearnersDxGridAsync(queryString);

            try
            {
                var response = JsonSerializer.Deserialize<LearnersGridResponse>(resultJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Fallback to raw Content in case external schema is unexpectedly different
                return Content(resultJson, "application/json");
            }
        }

        // ── Learner Profile: ข้อมูลส่วนตัว + ประวัติการเรียน ────────────────────
        [Authorize(Policy = "AdminOnly")]
        [HttpGet("profile/{code}")]
        public async Task<IActionResult> GetProfile(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { message = "Employee code is required." });

            // 1. ข้อมูลส่วนตัวจาก External API
            var learnerInfo = await _learnerService.GetLearnerByCodeAsync(code);

            if (_currentUser.DivisionId.HasValue)
            {
                if (learnerInfo == null)
                    return NotFound(new { message = "Learner not found." });

                if (string.IsNullOrWhiteSpace(_currentUser.DivisionName)
                    || !string.Equals(learnerInfo.Division, _currentUser.DivisionName, StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { message = "Learner not found." });
                }
            }

            // 2. Enrollment ทั้งหมดของ learner พร้อม Course
            //    ใช้ ignoreQueryFilters เพื่อให้โหลด Course ที่ถูก Soft Delete ได้ด้วย
            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.LearnerCode == code,
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
                    code = learnerInfo != null ? learnerInfo.Code : code,
                    name = learnerInfo != null ? learnerInfo.Name : code,
                    division = learnerInfo != null ? learnerInfo.Division : null,
                    department = learnerInfo != null ? learnerInfo.Department : null,
                    section = learnerInfo != null ? learnerInfo.Section : null,
                    position = learnerInfo != null ? learnerInfo.Position : null,
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

        private static readonly System.Collections.Generic.Dictionary<string, string> FieldMapping = new(System.StringComparer.OrdinalIgnoreCase)
        {
            { "nid", "NID" },
            { "eId", "EId" },
            { "englishFirstName", "EnglishFirstName" },
            { "englishLastName", "EnglishLastName" },
            { "division", "Division" },
            { "department", "Department" },
            { "section", "Section" },
            { "position", "Position" }
        };

        private static string MapFilterFieldNames(string queryString)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                return queryString;
            }

            var filterMatch = Regex.Match(queryString, @"([?&])filter=([^&]*)");
            if (!filterMatch.Success)
            {
                return queryString;
            }

            var existingFilter = Uri.UnescapeDataString(filterMatch.Groups[2].Value);

            // Matches field name when it is the first element of an array: ["field",
            // Lookbehind matches "[" then optional whitespace and quote.
            // Lookahead matches quote then optional whitespace and comma.
            var regex = new Regex(@"(?<=\[\s*"")\b(nid|eId|englishFirstName|englishLastName|division|department|section|position)\b(?=""\s*,)", RegexOptions.IgnoreCase);

            var mappedFilter = regex.Replace(existingFilter, match =>
            {
                var field = match.Value;
                return FieldMapping.TryGetValue(field, out var mapped) ? mapped : field;
            });

            return queryString.Replace(filterMatch.Value,
                $"{filterMatch.Groups[1].Value}filter={Uri.EscapeDataString(mappedFilter)}");
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

    public class LearnersGridResponse
    {
        public List<LearnerGridRowDto> Data { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class LearnerGridRowDto
    {
        public int Id { get; set; }
        public string EId { get; set; } = string.Empty;
        public string NID { get; set; } = string.Empty;
        public string EnglishFirstName { get; set; } = string.Empty;
        public string EnglishLastName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
    }
}