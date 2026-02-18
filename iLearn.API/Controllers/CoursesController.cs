using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using iLearn.Infrastructure.Repositories;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq; // จำเป็นสำหรับ LINQ
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IGenericRepository<CourseResource> _courseResourceRepository;
        private readonly IGenericRepository<CourseVersion> _courseVersionRepository;
        private readonly ICourseAssignmentService _assignmentService;
        private readonly IScormService _scormService;

        public CoursesController(
            ICourseRepository courseRepository,
            IGenericRepository<CourseResource> courseResourceRepository,
            IGenericRepository<CourseVersion> courseVersionRepository,
            ICourseAssignmentService assignmentService,
            IScormService scormService)
        {
            _courseRepo = courseRepository;
            _courseResourceRepository = courseResourceRepository;
            _courseVersionRepository = courseVersionRepository;
            _assignmentService = assignmentService;
            _scormService = scormService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool isActive = true)
        {
            // ดึงข้อมูล Course พร้อม Category และ Versions
            var courses = await _courseRepo.GetAsync(
                filter: c => c.IsActive == isActive,
                includeProperties: "Category,Versions"
            );

            // แปลงเป็น DTO (ตอนนี้จะเรียกใช้ .ToDto() ได้แล้ว)
            var courseDtos = courses.Select(c => c.ToDto()).ToList();

            // ส่งกลับในรูปแบบมาตรฐานที่ Frontend คาดหวัง { success: true, data: [...] }
            return Ok(new { success = true, data = courseDtos });
        }

        // [ปรับปรุง] GetById ให้ส่ง ResourceIds กลับไปแสดงผลด้วย
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            // หา Version ปัจจุบันที่ Active อยู่
            var versions = await _courseVersionRepository.GetAllAsync();
            var activeVersion = versions.FirstOrDefault(v => v.CourseId == id && v.IsActive);

            var resourceIds = new List<int>();
            if (activeVersion != null)
            {
                var allCourseResources = await _courseResourceRepository.GetAllAsync();
                resourceIds = allCourseResources
                    .Where(cr => cr.CourseVersionId == activeVersion.Id)
                    .Select(cr => cr.ResourceId)
                    .ToList();
            }

            // ส่งข้อมูลกลับในรูปแบบเดียวกับ DTO หรือ Anonymous Object ที่ Frontend ใช้ง่ายๆ
            return Ok(new
            {
                course.Id,
                CourseCode = course.Code,
                CourseName = course.Title,
                course.Description,
                CourseType = (int)course.Type,
                course.IsActive,
                ResourceIds = resourceIds // ส่งรายการ ID ของ Resource กลับไป
            });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CourseCreateDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
                {
                    return BadRequest(new { message = $"รหัสวิชา '{model.CourseCode}' มีอยู่ในระบบแล้ว" });
                }

                var course = new Course
                {
                    Code = model.CourseCode,
                    Title = model.CourseName,
                    CategoryId = model.CategoryId,
                    Description = model.Description,
                    Type = (CourseType)model.CourseType,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                await _courseRepo.AddAsync(course);

                var courseVersion = new CourseVersion
                {
                    CourseId = course.Id,
                    VersionNumber = 1,
                    Note = "Initial Create",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                await _courseVersionRepository.AddAsync(courseVersion);

                if (model.ResourceIds != null && model.ResourceIds.Count > 0)
                {
                    foreach (var resourceId in model.ResourceIds)
                    {
                        var courseResource = new CourseResource
                        {
                            CourseVersionId = courseVersion.Id,
                            ResourceId = resourceId,
                            CreatedAt = DateTime.Now
                        };
                        await _courseResourceRepository.AddAsync(courseResource);
                    }
                }

                if (course.Type == CourseType.General)
                {
                    await _assignmentService.ProcessAssignmentForCourseAsync(course.Id);
                }

                return CreatedAtAction(nameof(GetById), new { id = course.Id }, new { success = true, message = "สร้างหลักสูตรสำเร็จ", data = course });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดภายในเซิร์ฟเวอร์", error = ex.Message });
            }
        }

        // [ปรับปรุง] Update ให้รองรับการแก้ไข Resources
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseCreateDto dto)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            // 1. อัปเดตข้อมูลทั่วไป
            course.Title = dto.CourseName;
            course.Description = dto.Description;
            course.Code = dto.CourseCode;
            course.CategoryId = dto.CategoryId;
            if (course.Type != (CourseType)dto.CourseType)
            {
                course.Type = (CourseType)dto.CourseType;
            }

            await _courseRepo.UpdateAsync(course);

            // 2. [เพิ่ม] จัดการ ResourceIds (อัปเดตใน Version ปัจจุบัน)
            // หมายเหตุ: วิธีที่ดีที่สุดคือสร้าง Version ใหม่ แต่ถ้าต้องการแก้ทันทีให้ทำดังนี้:
            var versions = await _courseVersionRepository.GetAllAsync();
            var activeVersion = versions.FirstOrDefault(v => v.CourseId == id && v.IsActive);

            if (activeVersion != null)
            {
                // ดึงรายการเดิม
                var allCourseResources = await _courseResourceRepository.GetAllAsync();
                var currentResources = allCourseResources.Where(cr => cr.CourseVersionId == activeVersion.Id).ToList();

                // ลบรายการเดิมทั้งหมดทิ้ง (หรือจะทำ Diff ก็ได้ แต่วิธีนี้ง่ายกว่าสำหรับข้อมูลไม่เยอะ)
                foreach (var item in currentResources)
                {
                    await _courseResourceRepository.DeleteAsync(item);
                }

                // เพิ่มรายการใหม่ที่เลือกมา
                if (dto.ResourceIds != null && dto.ResourceIds.Count > 0)
                {
                    foreach (var resourceId in dto.ResourceIds)
                    {
                        await _courseResourceRepository.AddAsync(new CourseResource
                        {
                            CourseVersionId = activeVersion.Id,
                            ResourceId = resourceId,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            return Ok(new { success = true, message = "อัปเดตข้อมูลและเอกสารสำเร็จ" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            await _courseRepo.DeleteAsync(course);
            return Ok(new { success = true, message = "ลบหลักสูตรสำเร็จ" });
        }

        [HttpPost("{id}/assign-now")]
        public async Task<IActionResult> TriggerAssignment(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            await _assignmentService.ProcessAssignmentForCourseAsync(id);

            return Ok(new { message = "เริ่มกระบวนการมอบหมายหลักสูตรแล้ว (Assignment Process Started)" });
        }

        [HttpPost("create-scorm")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCourseWithScorm([FromForm] CourseCreateDto model, [FromForm] bool isPublish)
        {
            try
            {
                // 1. Validate ไฟล์
                if (model.File == null || model.File.Length == 0)
                {
                    return BadRequest(new { message = "กรุณาอัปโหลดไฟล์ SCORM (.zip)" });
                }

                // 2. สร้าง Course Object (ใช้ชื่อ Property จาก DTO ให้ถูกต้อง)
                var course = new Course
                {
                    Code = model.CourseCode,       // แก้เป็น CourseCode
                    Title = model.CourseName,      // แก้เป็น CourseName
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    Type = (CourseType)model.CourseType, // แก้เป็น CourseType
                    IsActive = isPublish,          // ถ้า Publish ให้ Active เลย
                    CreatedAt = DateTime.Now
                };

                await _courseRepo.AddAsync(course);

                // 3. สร้าง CourseVersion (Version 1)
                var version = new CourseVersion
                {
                    CourseId = course.Id,
                    VersionNumber = 1,
                    // Status = ... ลบทิ้งเพราะไม่มี Field นี้ใน Entity
                    // ใช้ Note หรือ IsActive แทนสถานะ
                    Note = isPublish ? "Initial Published" : "Draft",
                    IsActive = isPublish, // ใช้ IsActive แทน Status
                    CreatedAt = DateTime.Now
                };

                await _courseVersionRepository.AddAsync(version);

                // 4. Handle SCORM Extraction
                if (isPublish)
                {
                    // แปลง IFormFile เป็น byte[] เพื่อส่งให้ IScormService
                    using (var ms = new MemoryStream())
                    {
                        await model.File.CopyToAsync(ms);
                        var fileBytes = ms.ToArray();

                        // สร้างชื่อโฟลเดอร์สำหรับเก็บไฟล์ (เช่น course_CS101_v1)
                        string folderName = $"course_{course.Code}_v{version.VersionNumber}";

                        // เรียก Service (ส่ง byte[] และ string)
                        await _scormService.ExtractAndParseScormAsync(fileBytes, folderName);
                    }
                }

                return Ok(new { message = "สร้างหลักสูตรสำเร็จ", courseId = course.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }
    }
}