using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace iLearn.API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ICourseVersionService _versionService;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<CourseType> _courseTypeRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly ILearnerApiService _learnerApiService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;

        public CoursesController(
            ICourseService courseService,
            ICourseVersionService versionService,
            IGenericRepository<Course> courseRepo,
            IGenericRepository<CourseType> courseTypeRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<Assignment> assignmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            ILearnerApiService learnerApiService,
            ICurrentUserService currentUser,
            IDateTime dateTime)
        {
            _courseService = courseService;
            _versionService = versionService;
            _courseRepo = courseRepo;
            _courseTypeRepo = courseTypeRepo;
            _enrollmentRepo = enrollmentRepo;
            _assignmentRepo = assignmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _learnerApiService = learnerApiService;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup(DataSourceLoadOptions loadOptions)
        {
            var query = _courseRepo.GetQuery()
                .AsNoTracking()
                .Where(c => !c.IsDeleted);

            if (_currentUser.DivisionId.HasValue)
            {
                query = query.Where(c => c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value);
            }

            var lookupQuery = query
                .OrderBy(c => c.Code)
                .ThenBy(c => c.Title)
                .Select(c => new LookupCourseDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Title = c.Title,
                    CategoryId = c.CategoryId,
                    DivisionId = c.Category != null ? c.Category.DivisionId : null,
                    CourseTypeId = c.CourseTypeId,
                    CourseTypeName = c.CourseType != null ? c.CourseType.Name : null
                });

            return Ok(await DataSourceLoader.LoadAsync(lookupQuery, loadOptions));
        }

        [HttpGet("course-types-lookup")]
        public IActionResult GetCourseTypesLookup()
        {
            var courseTypes = _courseTypeRepo.GetQuery()
                .Select(ct => new
                {
                    ct.Id,
                    ct.Name,
                    ct.IsActive,
                    ct.CreatedAt
                })
                .OrderBy(ct => ct.Name)
                .ToList();

            return Ok(courseTypes);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool isActive = true, [FromQuery] string? divisionName = null)
        {
            IEnumerable<CourseDto> courses;

            // ── Data Isolation สำหรับ Learner: กรองตาม divisionName จาก Query ──
            if (!string.IsNullOrWhiteSpace(divisionName))
            {
                courses = await _courseService.GetCoursesByDivisionNameAsync(divisionName, isActive);
            }
            else
            {
                courses = await _courseService.GetAllCoursesAsync(isActive);

                // ── Data Isolation สำหรับ Admin: กรอง Category.DivisionId ──
                if (_currentUser.DivisionId.HasValue)
                {
                    courses = courses.Where(c => c.DivisionId == _currentUser.DivisionId.Value);
                }
            }

            return Ok(new { success = true, data = courses });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound(new { success = false, message = "Course not found." });

            return Ok(new { success = true, data = course });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CourseCreateDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var course = await _courseService.CreateCourseAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = course.Id },
                    new { success = true, message = "Course created successfully.", data = course });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var course = await _courseService.UpdateCourseAsync(id, dto);
                return Ok(new { success = true, message = "Course updated successfully.", data = course });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _courseService.DeleteCourseAsync(id);
                return Ok(new { success = true, message = "Course and related files deleted successfully." });
            }
            catch (InvalidOperationException ex) // 🌟 ดักจับเคสที่ลบไม่ได้เพราะมีคนเรียนแล้ว
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An internal server error occurred.", error = ex.Message });
            }
        }

        [HttpPost("create-scorm")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ScormPackageLimits.MaxCompressedPackageBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ScormPackageLimits.MaxCompressedPackageBytes)]
        public async Task<IActionResult> CreateCourseWithScorm([FromForm] CourseCreateDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var course = await _courseService.CreateCourseWithScormAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = course.Id },
                    new { success = true, message = "Course with SCORM created successfully.", data = course });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while saving data.", error = ex.Message });
            }
        }

        // ============================================================
        // Version Management Endpoints
        // ============================================================

        [HttpGet("{courseId}/versions")]
        public async Task<IActionResult> GetCourseVersions(int courseId)
        {
            try
            {
                var versions = await _versionService.GetCourseVersionsAsync(courseId);
                return Ok(new { success = true, data = versions });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{courseId}/version-impact")]
        public async Task<IActionResult> GetVersionLearnerImpact(int courseId)
        {
            try
            {
                var impact = await _versionService.GetVersionLearnerImpactAsync(courseId);
                return Ok(new { success = true, data = impact });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("versions/{versionId}")]
        public async Task<IActionResult> GetVersion(int versionId)
        {
            try
            {
                var version = await _versionService.GetVersionByIdAsync(versionId);
                return Ok(new { success = true, data = version });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("versions/{versionId}/readiness")]
        public async Task<IActionResult> GetVersionReadiness(int versionId)
        {
            try
            {
                var readiness = await _versionService.GetVersionReadinessAsync(versionId);
                return Ok(new { success = true, data = readiness });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{courseId}/versions")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ScormPackageLimits.MaxCompressedPackageBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ScormPackageLimits.MaxCompressedPackageBytes)]
        public async Task<IActionResult> CreateVersion(int courseId, [FromForm] CreateCourseVersionDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Get uploaded files from request
                var files = Request.Form.Files.ToList();

                var version = await _versionService.CreateVersionAsync(courseId, model, files);
                return CreatedAtAction(nameof(GetVersion), new { versionId = version.Id },
                    new { success = true, message = "New version created successfully.", data = version });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidScormPackageException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while creating the version.", error = ex.Message });
            }
        }

        [HttpPut("versions/{versionId}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ScormPackageLimits.MaxCompressedPackageBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ScormPackageLimits.MaxCompressedPackageBytes)]
        public async Task<IActionResult> UpdateVersion(int versionId, [FromForm] CreateCourseVersionDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var files = Request.Form.Files.ToList();
                var version = await _versionService.UpdateVersionAsync(versionId, model, files);
                return Ok(new { success = true, message = "Version updated successfully.", data = version });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidScormPackageException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating the version.", error = ex.Message });
            }
        }

        [HttpDelete("versions/{versionId}")]
        public async Task<IActionResult> DeleteVersion(int versionId)
        {
            // 🔧 แก้ไข: ลบโค้ด _repo / id ที่ถูกวางผิดที่ออก
            // Version ไม่มี DivisionId โดยตรง — ให้ Service layer จัดการ ownership ผ่าน Course -> Category -> Division
            try
            {
                await _versionService.DeleteVersionAsync(versionId);
                return Ok(new { success = true, message = "Version deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{courseId}/versions/{versionId}/set-active")]
        public async Task<IActionResult> SetActiveVersion(
            int courseId,
            int versionId,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CourseVersionLearnerPolicyDto? dto)
        {
            try
            {
                await _versionService.SetActiveVersionAsync(
                    courseId,
                    versionId,
                    dto?.Policy ?? CourseVersionLearnerPolicy.NewLearnersOnly);
                return Ok(new { success = true, message = "Active version changed successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        // ── Learners enrolled in a specific course ─────────────────────────
        [HttpGet("{courseId}/learners")]
        public async Task<IActionResult> GetCourseLearners(int courseId)
        {
            var enrollments = await _enrollmentRepo.GetAsync(
                e => e.CourseId == courseId,
                includeProperties: "AssignmentLinks"
            );

            if (!enrollments.Any())
                return Ok(new { success = true, data = new List<object>() });

            var codes = enrollments.Select(e => e.LearnerCode).Distinct().ToList();
            Dictionary<string, ExternalLearnerDto> learnerMap;
            try
            {
                learnerMap = await _learnerApiService.GetLearnersByCodesAsync(codes);
            }
            catch
            {
                learnerMap = new Dictionary<string, ExternalLearnerDto>();
            }

            var now = _dateTime.Now;

            var result = enrollments.Select(e =>
            {
                var learner = learnerMap.GetValueOrDefault(e.LearnerCode);
                var effectiveStart = e.AssignmentLinks.Any() ? e.AssignmentLinks.Min(a => a.StartDate) : e.StartDate;
                var effectiveDue   = e.AssignmentLinks.Any() ? e.AssignmentLinks.Max(a => a.DueDate)   : e.DueDate;

                var status = AssignmentStatusKeys.GetScheduledLearnerStatus(
                    e.IsCompleted,
                    e.Progress,
                    effectiveStart,
                    effectiveDue,
                    now);

                return new
                {
                    id            = e.Id,
                    learnerCode   = e.LearnerCode,
                    learnerName   = learner?.Name ?? e.LearnerCode,
                    division      = learner?.Division,
                    department    = learner?.Department,
                    position      = learner?.Position,
                    progress      = Math.Round(e.Progress),
                    isCompleted   = e.IsCompleted,
                    completedDate = e.CompletedDate,
                    startDate     = effectiveStart,
                    dueDate       = effectiveDue,
                    status
                };
            })
            .OrderBy(x => x.isCompleted)
            .ThenByDescending(x => x.progress)
            .ToList();

            return Ok(new { success = true, data = result });
        }

        // ── Assignment history for a specific course ─────────────────────────
        [HttpGet("{courseId}/assignments")]
        public async Task<IActionResult> GetCourseAssignments(int courseId)
        {
            var assignments = await _assignmentRepo.GetAsync(
                r => r.CourseId == courseId
                  && (!_currentUser.DivisionId.HasValue || r.DivisionId == _currentUser.DivisionId.Value),
                includeProperties: "Course"
            );

            if (!assignments.Any())
                return Ok(new { success = true, data = new List<object>() });

            var allIds = assignments.Select(a => a.Id).ToList();
            var links = await _enrollmentAssignmentRepo.GetAsync(
                ea => allIds.Contains(ea.AssignmentId),
                includeProperties: "Enrollment"
            );

            var now = _dateTime.Now;

            var history = assignments
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo))
                .GroupBy(r => r.AssignmentNo)
                .Select(g =>
                {
                    var first   = g.First();
                    var ruleIds = g.Select(a => a.Id).ToList();

                    var relatedLinks = links
                        .Where(ea => ruleIds.Contains(ea.AssignmentId) && ea.Enrollment != null)
                        .ToList();

                    bool allDone = relatedLinks.Any()
                        && relatedLinks.All(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);

                    string status = AssignmentDashboardService.CalculateStatus(
                        relatedLinks.Any(), allDone, first.StartDate, first.DueDate, now);

                    var done  = relatedLinks.Count(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);
                    var total = relatedLinks.Count;
                    var pct   = total > 0 ? Math.Round((double)done / total * 100) : 0;

                    return new
                    {
                        id            = first.Id,
                        assignmentNo  = g.Key,
                        description   = first.Description,
                        startDate     = first.StartDate,
                        dueDate       = first.DueDate,
                        status,
                        completedEnrollmentCount = done,
                        totalEnrollmentCount     = total,
                        completionPct            = pct,
                        learnerGroupId           = first.LearnerGroupId
                    };
                })
                .OrderByDescending(x => x.assignmentNo)
                .ToList();

            return Ok(new { success = true, data = history });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] Application.DTOs.CourseStatusUpdateDto dto)
        {
            try
            {
                CourseStatus targetStatus;
                if (dto.Status.HasValue)
                {
                    targetStatus = dto.Status.Value;
                }
                else if (dto.IsActive.HasValue)
                {
                    targetStatus = dto.IsActive.Value ? CourseStatus.Open : CourseStatus.Closed;
                }
                else
                {
                    return BadRequest(new ApiResponse<CourseStatusResultDto>
                    {
                        Success = false,
                        Message = "Status or isActive is required."
                    });
                }

                var result = await _courseService.UpdateCourseStatusAsync(id, targetStatus);
                return Ok(new ApiResponse<CourseStatusResultDto>
                {
                    Success = true,
                    Message = $"Course status changed to {result.StatusName} successfully.",
                    Data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<CourseStatusResultDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<CourseStatusResultDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("{id}/status-impact")]
        public async Task<IActionResult> GetStatusImpact(int id)
        {
            try
            {
                var impact = await _courseService.GetCourseStatusImpactAsync(id);
                return Ok(new { success = true, data = impact });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        // ── Consolidated Dashboard endpoint ───────────────────────────────
        // Returns course info, versions with contentItems, and KPI counts in one call
        [HttpGet("{courseId}/dashboard")]
        public async Task<IActionResult> GetDashboard(int courseId)
        {
            var course = await _courseService.GetCourseByIdAsync(courseId);
            if (course == null)
                return NotFound(new { success = false, message = "Course not found." });

            // DbContext is not thread-safe — run queries sequentially
            var versions = await _versionService.GetCourseVersionsAsync(courseId);
            var enrollments = await _enrollmentRepo.GetAsync(
                e => e.CourseId == courseId
            );
            var assignments = await _assignmentRepo.GetAsync(
                r => r.CourseId == courseId
                  && (!_currentUser.DivisionId.HasValue || r.DivisionId == _currentUser.DivisionId.Value)
            );

            // KPI counts
            var learnerCount = enrollments.Count;
            var completedCount = enrollments.Count(e => e.IsCompleted);

            var assignmentGroups = assignments
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo))
                .GroupBy(r => r.AssignmentNo)
                .Count();

            return Ok(new
            {
                success = true,
                data = new
                {
                    course,
                    versions,
                    kpi = new
                    {
                        versionCount = versions.Count(),
                        learnerCount,
                        completedCount,
                        assignmentCount = assignmentGroups
                    }
                }
            });
        }
    }
}