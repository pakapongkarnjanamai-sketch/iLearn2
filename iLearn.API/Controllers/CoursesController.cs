using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
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
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notificationService;

        public CoursesController(
            ICourseService courseService,
            ICourseVersionService versionService,
            IGenericRepository<Course> courseRepo,
            IGenericRepository<CourseType> courseTypeRepo,
            ICurrentUserService currentUser,
            INotificationService notificationService)
        {
            _courseService = courseService;
            _versionService = versionService;
            _courseRepo = courseRepo;
            _courseTypeRepo = courseTypeRepo;
            _currentUser = currentUser;
            _notificationService = notificationService;
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

            return Ok(new ApiResponse<IEnumerable<CourseDto>>
            {
                Success = true,
                Data = courses
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound(new ApiResponse<CourseDetailDto>
                {
                    Success = false,
                    Message = "Course not found."
                });

            return Ok(new ApiResponse<CourseDetailDto>
            {
                Success = true,
                Data = course
            });
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
                    new ApiResponse<CourseDto>
                    {
                        Success = true,
                        Message = "Course created successfully.",
                        Data = course
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<CourseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
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
                return Ok(new ApiResponse<CourseDto>
                {
                    Success = true,
                    Message = "Course updated successfully.",
                    Data = course
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<CourseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _courseService.DeleteCourseAsync(id);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Course and related files deleted successfully."
                });
            }
            catch (InvalidOperationException ex) // 🌟 ดักจับเคสที่ลบไม่ได้เพราะมีคนเรียนแล้ว
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An internal server error occurred.",
                    ErrorCode = ex.Message
                });
            }
        }

        [HttpPost("create-scorm")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ScormPackageLimits.MaxRequestEnvelopeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ScormPackageLimits.MaxRequestEnvelopeBytes)]
        public async Task<IActionResult> CreateCourseWithScorm([FromForm] CourseCreateDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var course = await _courseService.CreateCourseWithScormAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = course.Id },
                    new ApiResponse<CourseDto>
                    {
                        Success = true,
                        Message = "Course with SCORM created successfully.",
                        Data = course
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<CourseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CourseDto>
                {
                    Success = false,
                    Message = "An error occurred while saving data.",
                    ErrorCode = ex.Message
                });
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
                return Ok(new ApiResponse<IEnumerable<CourseVersionDto>>
                {
                    Success = true,
                    Data = versions
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<IEnumerable<CourseVersionDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("{courseId}/version-impact")]
        public async Task<IActionResult> GetVersionLearnerImpact(int courseId)
        {
            try
            {
                var impact = await _versionService.GetVersionLearnerImpactAsync(courseId);
                return Ok(new ApiResponse<CourseVersionLearnerImpactDto>
                {
                    Success = true,
                    Data = impact
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<CourseVersionLearnerImpactDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("versions/{versionId}")]
        public async Task<IActionResult> GetVersion(int versionId)
        {
            try
            {
                var version = await _versionService.GetVersionByIdAsync(versionId);
                return Ok(new ApiResponse<CreateCourseVersionDto>
                {
                    Success = true,
                    Data = version
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<CreateCourseVersionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("versions/{versionId}/readiness")]
        public async Task<IActionResult> GetVersionReadiness(int versionId)
        {
            try
            {
                var readiness = await _versionService.GetVersionReadinessAsync(versionId);
                return Ok(new ApiResponse<CourseVersionReadinessDto>
                {
                    Success = true,
                    Data = readiness
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<CourseVersionReadinessDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost("{courseId}/versions")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ScormPackageLimits.MaxRequestEnvelopeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ScormPackageLimits.MaxRequestEnvelopeBytes)]
        public async Task<IActionResult> CreateVersion(int courseId, [FromForm] CreateCourseVersionDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Get uploaded files from request
                var files = Request.Form.Files.ToList();

                var version = await _versionService.CreateVersionAsync(courseId, model, files);

                await _notificationService.NotifyAsync(
                    _currentUser.UserId,
                    NotificationTypes.ScormUploadSucceeded,
                    NotificationLevels.Success,
                    "อัปโหลด SCORM สำเร็จ",
                    message: $"เวอร์ชัน {version.VersionNumber} ของคอร์ส #{courseId}",
                    linkPath: $"/courses/{courseId}",
                    entityType: "CourseVersion",
                    entityId: version.Id);

                return CreatedAtAction(nameof(GetVersion), new { versionId = version.Id },
                    new ApiResponse<CourseVersionDto>
                    {
                        Success = true,
                        Message = "New version created successfully.",
                        Data = version
                    });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<CourseVersionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidScormPackageException ex)
            {
                await _notificationService.NotifyAsync(
                    _currentUser.UserId,
                    NotificationTypes.ScormUploadFailed,
                    NotificationLevels.Error,
                    "อัปโหลด SCORM ล้มเหลว",
                    message: ex.Message,
                    linkPath: $"/courses/{courseId}",
                    entityType: "Course",
                    entityId: courseId);

                return BadRequest(new ApiResponse<CourseVersionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<CourseVersionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CourseVersionDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the version.",
                    ErrorCode = ex.Message
                });
            }
        }

        [HttpPut("versions/{versionId}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ScormPackageLimits.MaxRequestEnvelopeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ScormPackageLimits.MaxRequestEnvelopeBytes)]
        public async Task<IActionResult> UpdateVersion(int versionId, [FromForm] CreateCourseVersionDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var files = Request.Form.Files.ToList();
                var version = await _versionService.UpdateVersionAsync(versionId, model, files);

                await _notificationService.NotifyAsync(
                    _currentUser.UserId,
                    NotificationTypes.ScormUploadSucceeded,
                    NotificationLevels.Success,
                    "อัปโหลด SCORM สำเร็จ",
                    message: $"อัปเดตเวอร์ชัน {version.VersionNumber} สำเร็จ",
                    linkPath: $"/courses/{version.CourseId}",
                    entityType: "CourseVersion",
                    entityId: version.Id);

                return Ok(new ApiResponse<CourseVersionDto>
                {
                    Success = true,
                    Message = "Version updated successfully.",
                    Data = version
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<CourseVersionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidScormPackageException ex)
            {
                await _notificationService.NotifyAsync(
                    _currentUser.UserId,
                    NotificationTypes.ScormUploadFailed,
                    NotificationLevels.Error,
                    "อัปโหลด SCORM ล้มเหลว",
                    message: ex.Message,
                    entityType: "CourseVersion",
                    entityId: versionId);

                return BadRequest(new ApiResponse<CourseVersionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<CourseVersionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CourseVersionDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the version.",
                    ErrorCode = ex.Message
                });
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
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Version deleted successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
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
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Active version changed successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // ── Learners enrolled in a specific course ─────────────────────────
        [HttpGet("{courseId}/learners")]
        public async Task<IActionResult> GetCourseLearners(int courseId)
        {
            var learners = await _courseService.GetCourseLearnersAsync(courseId);
            return Ok(new ApiResponse<List<CourseLearnerDto>>
            {
                Success = true,
                Data = learners
            });
        }

        // ── Assignment history for a specific course ─────────────────────────
        [HttpGet("{courseId}/assignments")]
        public async Task<IActionResult> GetCourseAssignments(int courseId)
        {
            var assignments = await _courseService.GetCourseAssignmentsAsync(courseId);
            return Ok(new ApiResponse<List<CourseAssignmentHistoryDto>>
            {
                Success = true,
                Data = assignments
            });
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
                return Ok(new ApiResponse<CourseStatusImpactDto>
                {
                    Success = true,
                    Data = impact
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<CourseStatusImpactDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // ── Consolidated Dashboard endpoint ───────────────────────────────
        // Returns course info, versions with contentItems, and KPI counts in one call
        [HttpGet("{courseId}/dashboard")]
        public async Task<IActionResult> GetDashboard(int courseId)
        {
            var dashboard = await _courseService.GetCourseDashboardAsync(courseId);
            if (dashboard == null)
            {
                return NotFound(new ApiResponse<CourseDashboardDto>
                {
                    Success = false,
                    Message = "Course not found."
                });
            }

            return Ok(new ApiResponse<CourseDashboardDto>
            {
                Success = true,
                Data = dashboard
            });
        }
    }
}