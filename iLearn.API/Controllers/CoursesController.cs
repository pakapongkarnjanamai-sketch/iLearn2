using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IGenericRepository<Resource> _resourceRepository;
        private readonly IGenericRepository<CourseResource> _courseResourceRepository;
        private readonly IGenericRepository<FileStorage> _fileStorageRepository;
        private readonly IGenericRepository<CourseVersion> _courseVersionRepository;
        private readonly ICourseAssignmentService _assignmentService;

        public CoursesController(
            ICourseRepository courseRepository,
            IGenericRepository<Resource> resourceRepository,
            IGenericRepository<CourseResource> courseResourceRepository,
            IGenericRepository<FileStorage> fileStorageRepository,
            IGenericRepository<CourseVersion> courseVersionRepository,
            ICourseAssignmentService assignmentService)
        {
            _courseRepo = courseRepository;
            _resourceRepository = resourceRepository;
            _courseResourceRepository = courseResourceRepository;
            _fileStorageRepository = fileStorageRepository;
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

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CourseCreateDto model)
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
                    // CategoryId = model.CategoryId, // [ลบออก] เพราะ Entity Course ไม่มี CategoryId
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                await _courseRepo.AddAsync(course);

                // 3. สร้าง CourseVersion แรก
                var courseVersion = new CourseVersion
                {
                    CourseId = course.Id,
                    VersionNumber = 1,
                    Note = "Initial Create", // [แก้ไข] เปลี่ยนจาก VersionNote เป็น Note
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                await _courseVersionRepository.AddAsync(courseVersion);

                // 4. จัดการไฟล์แนบ
                if (model.Resources != null && model.Resources.Count > 0)
                {
                    foreach (var file in model.Resources)
                    {
                        if (file.Length > 0)
                        {
                            byte[] fileData;
                            using (var ms = new MemoryStream())
                            {
                                await file.CopyToAsync(ms);
                                fileData = ms.ToArray();
                            }

                            var fileStorage = new FileStorage
                            {
                                Name = file.FileName,
                                ContentType = file.ContentType,
                                Data = fileData,
                                Length = file.Length,
                                CreatedAt = DateTime.Now
                            };
                            await _fileStorageRepository.AddAsync(fileStorage);

                            var resource = new Resource
                            {
                                Name = file.FileName,
                                IsActive = true,
                                TypeId = 1,
                                FileStorageId = fileStorage.Id,
                                CreatedAt = DateTime.Now
                            };
                            await _resourceRepository.AddAsync(resource);

                            var courseResource = new CourseResource
                            {
                                CourseVersionId = courseVersion.Id,
                                ResourceId = resource.Id,
                                CreatedAt = DateTime.Now
                            };
                            await _courseResourceRepository.AddAsync(courseResource);
                        }
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
        public async Task<IActionResult> Update(int id, [FromForm] CourseCreateDto dto)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            course.Title = dto.CourseName;
            course.Description = dto.Description;
            // course.CategoryId = dto.CategoryId; // [ลบออก] เพราะ Entity Course ไม่มี CategoryId

            if (course.Type != (CourseType)dto.CourseType)
            {
                course.Type = (CourseType)dto.CourseType;
            }

            await _courseRepo.UpdateAsync(course);
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