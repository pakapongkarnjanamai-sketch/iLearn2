using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
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

        public CoursesController(
            ICourseRepository courseRepository,
            IGenericRepository<CourseResource> courseResourceRepository,
            IGenericRepository<CourseVersion> courseVersionRepository,
            ICourseAssignmentService assignmentService)
        {
            _courseRepo = courseRepository;
            _courseResourceRepository = courseResourceRepository;
            _courseVersionRepository = courseVersionRepository;
            _assignmentService = assignmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var courses = await _courseRepo.GetAllAsync();
            return Ok(courses);
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
    }
}