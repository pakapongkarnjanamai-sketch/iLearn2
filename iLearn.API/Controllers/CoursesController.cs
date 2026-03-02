using iLearn.Application.DTOs;

using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ICourseVersionService _versionService;

        public CoursesController(ICourseService courseService, ICourseVersionService versionService)
        {
            _courseService = courseService;
            _versionService = versionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool isActive = true)
        {
            var courses = await _courseService.GetAllCoursesAsync(isActive);
            return Ok(new { success = true, data = courses });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound(new { success = false, message = "ไม่พบหลักสูตร" });

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
                    new { success = true, message = "สร้างหลักสูตรสำเร็จ", data = course });
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
                return Ok(new { success = true, message = "อัปเดตข้อมูลและเอกสารสำเร็จ", data = course });
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
                return Ok(new { success = true, message = "ลบหลักสูตรและไฟล์ที่เกี่ยวข้องสำเร็จ" });
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
                return StatusCode(500, new { success = false, message = "เกิดข้อผิดพลาด", error = ex.Message });
            }
        }

        [HttpPost("{id}/assign-now")]
        public async Task<IActionResult> TriggerAssignment(int id)
        {
            try
            {
                await _courseService.TriggerAssignmentAsync(id);
                return Ok(new { success = true, message = "เริ่มกระบวนการมอบหมายหลักสูตรแล้ว" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("create-scorm")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCourseWithScorm([FromForm] CourseCreateDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var course = await _courseService.CreateCourseWithScormAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = course.Id },
                    new { success = true, message = "สร้างหลักสูตรพร้อม SCORM สำเร็จ", data = course });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล", error = ex.Message });
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

        [HttpPost("{courseId}/versions")]
        [Consumes("multipart/form-data")]
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
                    new { success = true, message = "สร้างเวอร์ชันใหม่สำเร็จ", data = version });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "เกิดข้อผิดพลาดในการสร้างเวอร์ชัน", error = ex.Message });
            }
        }

        [HttpPut("versions/{versionId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateVersion(int versionId, [FromForm] CreateCourseVersionDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var files = Request.Form.Files.ToList();
                var version = await _versionService.UpdateVersionAsync(versionId, model, files);
                return Ok(new { success = true, message = "อัปเดตเวอร์ชันสำเร็จ", data = version });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "เกิดข้อผิดพลาดในการอัปเดตเวอร์ชัน", error = ex.Message });
            }
        }

        [HttpDelete("versions/{versionId}")]
        public async Task<IActionResult> DeleteVersion(int versionId)
        {
            try
            {
                await _versionService.DeleteVersionAsync(versionId);
                return Ok(new { success = true, message = "ลบเวอร์ชันสำเร็จ" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{courseId}/versions/{versionId}/set-active")]
        public async Task<IActionResult> SetActiveVersion(int courseId, int versionId)
        {
            try
            {
                await _versionService.SetActiveVersionAsync(courseId, versionId);
                return Ok(new { success = true, message = "เปลี่ยนเวอร์ชันที่ใช้งานสำเร็จ" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        // คลาสที่เราสร้างไว้รับค่าชั่วคราวจาก JSON body ของคำขอ
        public class CourseStatusUpdateDto
        {
            public bool IsActive { get; set; }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] CourseStatusUpdateDto statusObj)
        {
            try
            {
                var newStatus = await _courseService.UpdateCourseStatusAsync(id, statusObj.IsActive);
                return Ok(new { success = true, message = "อัปเดตสถานะสำเร็จ", isActive = newStatus });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "เกิดข้อผิดพลาดในการอัปเดตสถานะ", error = ex.Message });
            }
        }
    }
}
