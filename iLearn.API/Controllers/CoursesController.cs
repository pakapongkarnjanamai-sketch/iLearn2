using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;

using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IGenericRepository<Resource> _resourceRepository;
        private readonly IGenericRepository<CourseResource> _courseResourceRepository;
        private readonly IGenericRepository<FileStorage> _fileStorageRepository; // เพิ่ม Repo สำหรับเก็บไฟล์
        private readonly IGenericRepository<CourseVersion> _courseVersionRepository; // เพิ่ม Repo สำหรับ Version
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

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CourseCreateDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // 1. ตรวจสอบรหัสซ้ำ (ใช้ model.CourseCode เช็คกับ Database)
                if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
                {
                    return BadRequest(new { message = $"รหัสวิชา '{model.CourseCode}' มีอยู่ในระบบแล้ว" });
                }

                // 2. สร้าง Entity Course (Header)
                // แมปข้อมูลเข้ากับ Property ใหม่: Code, Title, CreatedAt
                var course = new Course
                {
                    Code = model.CourseCode,       // แมปจาก DTO.CourseCode -> Entity.Code
                    Title = model.CourseName,      // แมปจาก DTO.CourseName -> Entity.Title
                    Description = model.Description,
                    Type = (CourseType)model.CourseType,
                    // CategoryId = model.CategoryId, // (ถ้าใน Entity Course ยังมี CategoryId อยู่ให้ uncomment บรรทัดนี้)
                    IsActive = true,
                    CreatedAt = DateTime.Now       // ใช้ CreatedAt ตาม BaseEntity ใหม่
                };

                await _courseRepo.AddAsync(course);
                // หมายเหตุ: ต้องมั่นใจว่า Repository ทำการ SaveChanges() แล้วเพื่อให้ได้ course.Id กลับมา

                // 3. สร้าง CourseVersion แรก (Version 1)
                var courseVersion = new CourseVersion
                {
                    CourseId = course.Id,
                    VersionNumber = 1,
                    
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                await _courseVersionRepository.AddAsync(courseVersion);

                // 4. จัดการไฟล์แนบ (Save to FileStorage in DB -> Resource -> CourseResource)
                if (model.Resources != null && model.Resources.Count > 0)
                {
                    foreach (var file in model.Resources)
                    {
                        if (file.Length > 0)
                        {
                            // A. อ่านไฟล์เป็น byte[] เพื่อเก็บลงตาราง FileStorage
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
                                CreatedAt = DateTime.Now // BaseEntity
                            };
                            await _fileStorageRepository.AddAsync(fileStorage); // ได้ ID กลับมา

                            // B. สร้าง Resource ผูกกับ FileStorage
                            var resource = new Resource
                            {
                                Name = file.FileName,
                                IsActive = true,
                                TypeId = 1, // กำหนด Type Default เช่น 1 = Learning Material
                                FileStorageId = fileStorage.Id, // เชื่อม FK ไปหาไฟล์จริง
                                CreatedAt = DateTime.Now
                            };
                            await _resourceRepository.AddAsync(resource);

                            // C. สร้าง Link CourseResource ผูกกับ "CourseVersion"
                            var courseResource = new CourseResource
                            {
                                CourseVersionId = courseVersion.Id, // ผูกกับ Version ล่าสุด
                                ResourceId = resource.Id,
                                CreatedAt = DateTime.Now
                            };
                            await _courseResourceRepository.AddAsync(courseResource);
                        }
                    }
                }

                // 5. Trigger ระบบ Auto-Assignment (ถ้าเป็น General Course)
                if (course.Type == CourseType.General)
                {
                    await _assignmentService.ProcessAssignmentForCourseAsync(course.Id);
                }

                return CreatedAtAction(nameof(GetById), new { id = course.Id }, new { message = "สร้างหลักสูตรสำเร็จ", data = course });
            }
            catch (Exception ex)
            {
                // ควรมี Logger เก็บ Error จริงไว้
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดภายในเซิร์ฟเวอร์", error = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // ใช้ GetAllAsync หรือ GetActiveCoursesAsync ตามโจทย์
            var courses = await _courseRepo.GetAllAsync();
            var dtos = courses.Select(c => c.ToDto());
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();
            return Ok(course.ToDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCourseDto dto)
        {
            // 1. ตรวจสอบรหัสซ้ำ
            if (!await _courseRepo.IsCourseCodeUniqueAsync(dto.Code))
            {
                return BadRequest($"Course code '{dto.Code}' already exists.");
            }

            // 2. แปลง DTO -> Entity
            var course = dto.ToEntity();

            // 3. บันทึกลง DB
            var createdCourse = await _courseRepo.AddAsync(course);

            // 4. [สำคัญ] Trigger ระบบ Auto-Assignment! 🚀
            // ถ้าเป็น General -> แจกทุกคน
            // ถ้าเป็น Special -> เช็คกฎ (แต่ตอนสร้างใหม่อาจจะยังไม่มีกฎ ต้องไปเพิ่มกฎทีหลังแล้วค่อยกด Assign ก็ได้)
            if (createdCourse.Type == CourseType.General)
            {
                await _assignmentService.ProcessAssignmentForCourseAsync(createdCourse.Id);
            }

            return CreatedAtAction(nameof(GetById), new { id = createdCourse.Id }, createdCourse.ToDto());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateCourseDto dto)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            // Update fields
            course.Title = dto.Title;
            course.Description = dto.Description;
            // course.Code = dto.Code; // ปกติไม่ค่อยให้แก้ Code
            // course.Type = dto.Type; // เปลี่ยน Type อาจต้องคิดเรื่อง Re-assign

            await _courseRepo.UpdateAsync(course);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            await _courseRepo.DeleteAsync(course);
            return NoContent();
        }

        // Endpoint พิเศษสำหรับกด Assign Manual (เผื่อกรณีสร้างกฎทีหลัง)
        [HttpPost("{id}/assign-now")]
        public async Task<IActionResult> TriggerAssignment(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            await _assignmentService.ProcessAssignmentForCourseAsync(id);

            return Ok(new { message = "Assignment process started." });
        }
    }
}