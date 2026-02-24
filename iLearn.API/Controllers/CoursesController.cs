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
        private readonly IGenericRepository<Resource> _resourceRepository;
        private readonly IGenericRepository<FileStorage> _fileStorageRepository; // [เพิ่ม] สำหรับบันทึกไฟล์ลง DB
        private readonly ICourseAssignmentService _assignmentService;
        private readonly IScormService _scormService;

        public CoursesController(
            ICourseRepository courseRepository,
            IGenericRepository<CourseResource> courseResourceRepository,
            IGenericRepository<CourseVersion> courseVersionRepository,
            IGenericRepository<Resource> resourceRepository,
                IGenericRepository<FileStorage> fileStorageRepository,
            ICourseAssignmentService assignmentService,
            IScormService scormService)
        {
            _courseRepo = courseRepository;
            _courseResourceRepository = courseResourceRepository;
            _courseVersionRepository = courseVersionRepository;
            _resourceRepository = resourceRepository;
            _fileStorageRepository = fileStorageRepository;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            var versions = await _courseVersionRepository.GetAllAsync();

            // 1. ลองหา Version ที่ Active ก่อน
            var targetVersion = versions.FirstOrDefault(v => v.CourseId == id && v.IsActive);

            // 2. ถ้าไม่มี Active (เช่น เป็น Draft อยู่) ให้เอา Version ล่าสุดมาแสดงแทน
            if (targetVersion == null)
            {
                targetVersion = versions
                    .Where(v => v.CourseId == id)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefault();
            }

            var resourceList = new List<object>();
            if (targetVersion != null)
            {
                // ดึงข้อมูล Resource ของ Version นั้นๆ
                var courseResources = await _courseResourceRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == targetVersion.Id,
                    includeProperties: "Resource"
                );

                resourceList = courseResources.Select(cr => new
                {
                    cr.Resource.Id,
                    cr.Resource.Name,
                    cr.Resource.TypeId,
                    TypeName = cr.Resource.TypeId == 2 ? "Exam" : "Learn",
                    cr.Resource.IsActive,
                    // ส่ง URL หรือ ID ไฟล์กลับไปเผื่อใช้ดาวน์โหลด
                    cr.Resource.URL
                }).ToList<object>();
            }

            return Ok(new
            {
                course.Id,
                CourseCode = course.Code,
                CourseName = course.Title,
                course.Description,
                CourseType = (int)course.Type,
                course.CategoryId,
                course.IsActive,
                Resources = resourceList // ✅ ตอนนี้จะมีข้อมูลแม้เป็น Draft
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

            return Ok(new { message = "เริ่มกระบวนการมอบหมายหลักสูตรแล้ว (Assignments Process Started)" });
        }

        [HttpPost("create-scorm")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCourseWithScorm([FromForm] CourseCreateDto model)
        {
            try
            {
                // 1. สร้าง Course (บังคับเป็น Inactive/Draft)
                var course = new Course
                {
                    Code = model.CourseCode,
                    Title = model.CourseName,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    Type = (CourseType)model.CourseType,
                    IsActive = false,
                    CreatedAt = DateTime.Now
                };

                await _courseRepo.AddAsync(course);

                // 2. สร้าง Version แรก (Draft)
                var version = new CourseVersion
                {
                    CourseId = course.Id,
                    VersionNumber = 1,
                    Note = "Draft (Initial Upload)",
                    IsActive = false,
                    CreatedAt = DateTime.Now
                };
                await _courseVersionRepository.AddAsync(version);

                // 3. จัดการไฟล์ (บันทึกลง DB โดยตรง)
                if (model.Files != null && model.Files.Count > 0)
                {
                    foreach (var file in model.Files)
                    {
                        if (file.Length > 0)
                        {
                            // A. อ่านไฟล์เป็น Byte Array
                            using (var ms = new MemoryStream())
                            {
                                await file.CopyToAsync(ms);
                                var fileBytes = ms.ToArray();

                                // B. บันทึกเข้าตาราง FileStorage
                                var fileStorage = new FileStorage
                                {
                                    Name = file.FileName,
                                    ContentType = file.ContentType,
                                    Data = fileBytes, // เก็บ Binary ของไฟล์
                                    Length = file.Length,
                                    CreatedAt = DateTime.Now
                                };

                                await _fileStorageRepository.AddAsync(fileStorage);

                                // C. สร้าง Resource เชื่อมโยงกับ FileStorage
                                var resource = new Resource
                                {
                                    Name = file.FileName,
                                    TypeId = 1, // 1 = Learn/SCORM
                                    IsActive = true,
                                    FileStorageId = fileStorage.Id, // เชื่อม FK ไปหาไฟล์ที่เพิ่งบันทึก
                                    // URL/Href อาจไม่ต้องใส่เพราะไฟล์อยู่ใน DB หรือใส่เป็นชื่อไฟล์ไว้ก่อน
                                    URL = file.FileName,
                                    CreatedAt = DateTime.Now
                                };

                                await _resourceRepository.AddAsync(resource);

                                // D. สร้างความสัมพันธ์ CourseResource
                                var courseResource = new CourseResource
                                {
                                    CourseVersionId = version.Id,
                                    ResourceId = resource.Id,
                                    CreatedAt = DateTime.Now
                                };
                                await _courseResourceRepository.AddAsync(courseResource);
                            }
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "สร้างหลักสูตรและบันทึกไฟล์เรียบร้อยแล้ว",
                    courseId = course.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost("update-scorm/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateCourseWithScorm(int id, [FromForm] CourseCreateDto model)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            // 1. อัปเดตข้อมูลทั่วไป
            course.Title = model.CourseName;
            course.Description = model.Description;
            course.Code = model.CourseCode;
            course.CategoryId = model.CategoryId;
            course.Type = (CourseType)model.CourseType;
            await _courseRepo.UpdateAsync(course);

            // 2. เพิ่มไฟล์ใหม่ (ถ้ามี)
            if (model.Files != null && model.Files.Count > 0)
            {
                var versions = await _courseVersionRepository.GetAllAsync();
                var activeVersion = versions.FirstOrDefault(v => v.CourseId == id && v.IsActive);

                if (activeVersion == null)
                {
                    activeVersion = versions.Where(v => v.CourseId == id).OrderByDescending(v => v.VersionNumber).FirstOrDefault();
                }

                if (activeVersion != null)
                {
                    foreach (var file in model.Files)
                    {
                        if (file.Length > 0)
                        {
                            using (var ms = new MemoryStream())
                            {
                                await file.CopyToAsync(ms);
                                var fileBytes = ms.ToArray();

                                // Save FileStorage
                                var fileStorage = new FileStorage
                                {
                                    Name = file.FileName,
                                    ContentType = file.ContentType,
                                    Data = fileBytes,
                                    Length = file.Length,
                                    CreatedAt = DateTime.Now
                                };
                                await _fileStorageRepository.AddAsync(fileStorage);

                                // Save Resource
                                var resource = new Resource
                                {
                                    Name = file.FileName,
                                    TypeId = 1,
                                    IsActive = true,
                                    FileStorageId = fileStorage.Id,
                                    URL = file.FileName,
                                    CreatedAt = DateTime.Now
                                };
                                await _resourceRepository.AddAsync(resource);

                                // Link Resource
                                await _courseResourceRepository.AddAsync(new CourseResource
                                {
                                    CourseVersionId = activeVersion.Id,
                                    ResourceId = resource.Id,
                                    CreatedAt = DateTime.Now
                                });
                            }
                        }
                    }
                }
            }

            return Ok(new { success = true, message = "อัปเดตข้อมูลสำเร็จ" });
        }

        [HttpDelete("resource/{resourceId}")]
        public async Task<IActionResult> DeleteResource(int resourceId)
        {
            try
            {
                // 1. ตรวจสอบว่ามี Resource นี้อยู่จริง
                var resource = await _resourceRepository.GetByIdAsync(resourceId);
                if (resource == null) return NotFound(new { message = "ไม่พบไฟล์ที่ต้องการลบ" });

                // 2. ลบความเชื่อมโยงกับ Course (CourseResource)
                var courseResources = await _courseResourceRepository.GetAsync(cr => cr.ResourceId == resourceId);
                foreach (var cr in courseResources)
                {
                    await _courseResourceRepository.DeleteAsync(cr);
                }

                // 3. ลบข้อมูล Resource
                await _resourceRepository.DeleteAsync(resource);

                // 4. ลบไฟล์จริงใน FileStorage (เพื่อคืนพื้นที่)
                if (resource.FileStorageId.HasValue)
                {
                    var fileStorage = await _fileStorageRepository.GetByIdAsync(resource.FileStorageId.Value);
                    if (fileStorage != null)
                    {
                        await _fileStorageRepository.DeleteAsync(fileStorage);
                    }
                }

                return Ok(new { success = true, message = "ลบไฟล์เรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }
        [HttpPut("{id}/info")]
        public async Task<IActionResult> UpdateCourseInfo(int id, [FromBody] UpdateCourseInfoDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound(new { success = false, message = "ไม่พบหลักสูตรที่ต้องการแก้ไข" });

            // อัปเดตฟิลด์ที่ได้รับมา
            course.Code = dto.CourseCode;       // <--- บรรทัดที่ต้องเพิ่มสำหรับบันทึกรหัสวิชาใหม่
            course.Title = dto.CourseName;
            course.Description = dto.Description;
            course.CategoryId = dto.CategoryId;
            course.Type = (CourseType)dto.CourseType;
            // [เพิ่มใหม่] แมปค่า CourseType (แปลงจาก int กลับเป็น Enum)
            course.Type = (CourseType)dto.CourseType;

            await _courseRepo.UpdateAsync(course);

            return Ok(new { success = true, message = "อัปเดตข้อมูลทั่วไปสำเร็จ" });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateCourseStatus(int id, [FromBody] UpdateCourseStatusDto dto)
        {
            // 1. ค้นหาคอร์สหลัก
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound(new { success = false, message = "ไม่พบหลักสูตรที่ต้องการแก้ไข" });

            // ========================================================
            // [ตรวจสอบก่อน Publish] ถ้า IsActive = true ต้องมีข้อมูลพร้อมใช้งาน
            // ========================================================
            if (dto.IsActive == true)
            {
                // ดึง Version ของคอร์สนี้ที่ IsActive = true มาเช็ค
                var activeVersions = await _courseVersionRepository.GetAsync(v => v.CourseId == id && v.IsActive);
                bool isReadyToPublish = false;

                foreach (var version in activeVersions)
                {
                    // ดึงข้อมูล CourseResource พร้อม Include Resource เข้ามาด้วย
                    var courseResources = await _courseResourceRepository.GetAsync(
                        filter: cr => cr.CourseVersionId == version.Id,
                        includeProperties: "Resource"
                    );

                    // ตรวจสอบว่ามี Resource อย่างน้อย 1 ตัวที่เป็น IsActive = true หรือไม่
                    if (courseResources.Any(cr => cr.Resource != null && cr.Resource.IsActive))
                    {
                        isReadyToPublish = true;
                        break; // เจอแค่ 1 ตัวก็ถือว่าผ่านเงื่อนไขแล้ว หยุดลูปได้เลย
                    }
                }

                // ถ้าลูปหาจนจบแล้วไม่เจอเนื้อหาที่พร้อมใช้งานเลย ให้ตีกลับ (Return BadRequest)
                if (!isReadyToPublish)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "ไม่สามารถ Publish ได้! หลักสูตรนี้ต้องมี Version และเนื้อหา (Resource) ที่เปิดใช้งาน (Active) อย่างน้อย 1 รายการ"
                    });
                }
            }

            // ========================================================
            // ถ้าเป็นการ Set to Closed (IsActive = false) ให้ไปลบและปิดการใช้งานตัวลูกๆ
            // ========================================================
            if (dto.IsActive == false)
            {
                var versions = await _courseVersionRepository.GetAsync(v => v.CourseId == id);
                foreach (var version in versions)
                {
                    //if (version.IsActive)
                    //{
                    //    version.IsActive = false;
                    //    await _courseVersionRepository.UpdateAsync(version);
                    //}

                    var courseResources = await _courseResourceRepository.GetAsync(
                        filter: cr => cr.CourseVersionId == version.Id,
                        includeProperties: "Resource"
                    );

                    foreach (var cr in courseResources)
                    {
                        if (cr.Resource != null)
                        {
                            if (cr.Resource.IsActive)
                            {
                                cr.Resource.IsActive = false;
                                await _resourceRepository.UpdateAsync(cr.Resource);
                            }

                            // ลบโฟลเดอร์แตกไฟล์ SCORM
                            if (!string.IsNullOrEmpty(cr.Resource.URL))
                            {
                                try
                                {
                                    var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                                    var extractedFolderPath = Path.Combine(webRootPath, "scorm", cr.Resource.URL);

                                    if (Directory.Exists(extractedFolderPath))
                                    {
                                        Directory.Delete(extractedFolderPath, true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error deleting extracted folder: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }

            // ========================================================
            // ถ้าผ่านเงื่อนไขทั้งหมด ค่อยบันทึกสถานะของ Course หลักลง DB
            // ========================================================
            course.IsActive = dto.IsActive;
            await _courseRepo.UpdateAsync(course);

            return Ok(new { success = true, isActive = course.IsActive, message = "อัปเดตสถานะสำเร็จ" });
        }
        [HttpPatch("{courseId}/versions/{versionId}/set-active")]
        public async Task<IActionResult> SetActiveVersion(int courseId, int versionId)
        {
            // 1. ตรวจสอบคอร์ส
            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null) return NotFound(new { success = false, message = "ไม่พบหลักสูตร" });

            // 2. ดึง Version ทั้งหมดของคอร์สนี้
            var versions = await _courseVersionRepository.GetAsync(v => v.CourseId == courseId);

            // 3. ตรวจสอบว่า Version ที่ส่งมามีอยู่จริง
            if (!versions.Any(v => v.Id == versionId))
                return NotFound(new { success = false, message = "ไม่พบ Version ที่ระบุในหลักสูตรนี้" });

            // 4. วนลูปอัปเดต: ให้ตัวที่เลือกเป็น true ตัวอื่นบังคับเป็น false ทั้งหมด
            foreach (var version in versions)
            {
                if (version.Id == versionId)
                {
                    if (!version.IsActive)
                    {
                        version.IsActive = true;
                        await _courseVersionRepository.UpdateAsync(version);
                    }
                }
                else
                {
                    if (version.IsActive)
                    {
                        version.IsActive = false;
                        await _courseVersionRepository.UpdateAsync(version);
                    }
                }
            }

            return Ok(new { success = true, message = "เปลี่ยนเวอร์ชันที่ใช้งานสำเร็จ" });
        }
    }
}
