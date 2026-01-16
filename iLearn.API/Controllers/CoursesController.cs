using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic; // เพิ่มสำหรับการใช้ List
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseRepository _courseRepo;
        // private readonly IGenericRepository<Resource> _resourceRepository; // ไม่ได้ใช้สร้าง Resource ใหม่แล้ว
        private readonly IGenericRepository<CourseResource> _courseResourceRepository;
        // private readonly IGenericRepository<FileStorage> _fileStorageRepository; // ไม่ได้ใช้สร้าง FileStorage แล้ว
        private readonly IGenericRepository<CourseVersion> _courseVersionRepository;
        private readonly ICourseAssignmentService _assignmentService;

        public CoursesController(
            ICourseRepository courseRepository,
            IGenericRepository<Resource> resourceRepository, // คงไว้หากมีการใช้งานอื่น หรือลบออกถ้าไม่ใช้
            IGenericRepository<CourseResource> courseResourceRepository,
            IGenericRepository<FileStorage> fileStorageRepository, // คงไว้หากมีการใช้งานอื่น หรือลบออกถ้าไม่ใช้
            IGenericRepository<CourseVersion> courseVersionRepository,
            ICourseAssignmentService assignmentService)
        {
            _courseRepo = courseRepository;
            // _resourceRepository = resourceRepository;
            _courseResourceRepository = courseResourceRepository;
            // _fileStorageRepository = fileStorageRepository;
            _courseVersionRepository = courseVersionRepository;
            _assignmentService = assignmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var courses = await _courseRepo.GetAllAsync();
            return Ok(courses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();
            return Ok(course);
        }

        [HttpPost("Create")]
        // [Consumes("multipart/form-data")] // [ลบออก] ไม่ได้รับไฟล์แล้ว
        public async Task<IActionResult> Create([FromForm] CourseCreateDto model) // [แก้ไข] เปลี่ยนเป็น [FromBody]
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // 1. ตรวจสอบรหัสซ้ำ
                if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
                {
                    return BadRequest(new { message = $"รหัสวิชา '{model.CourseCode}' มีอยู่ในระบบแล้ว" });
                }

                // 2. สร้าง Entity Course
                var course = new Course
                {
                    Code = model.CourseCode,
                    Title = model.CourseName,
                    Description = model.Description,
                    Type = (CourseType)model.CourseType,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                await _courseRepo.AddAsync(course);

                // 3. สร้าง CourseVersion แรก
                var courseVersion = new CourseVersion
                {
                    CourseId = course.Id,
                    VersionNumber = 1,
                    Note = "Initial Create",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                await _courseVersionRepository.AddAsync(courseVersion);

                // 4. [แก้ไข] เชื่อมโยง Resources (จาก ID)
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

                // 5. Trigger Assignment
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseCreateDto dto) // [แก้ไข] เปลี่ยนเป็น [FromBody]
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            course.Title = dto.CourseName;
            course.Description = dto.Description;
            // course.CategoryId = dto.CategoryId;

            if (course.Type != (CourseType)dto.CourseType)
            {
                course.Type = (CourseType)dto.CourseType;
            }

            await _courseRepo.UpdateAsync(course);

            // หมายเหตุ: การ Update ปกติมักจะไม่ยุ่งกับ ResourceIds หรือ CourseVersion 
            // หากต้องการแก้ไข Resource ควรทำผ่าน API แยกหรือการสร้าง Version ใหม่ (ตาม Logic ของ Wizard)

            return Ok(new { success = true, message = "อัปเดตข้อมูลสำเร็จ" });
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