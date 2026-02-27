using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
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
using iLearn.Infrastructure.Persistence;

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
        private readonly AppDbContext _context;
        public CoursesController(
     ICourseRepository courseRepository,
     IGenericRepository<CourseResource> courseResourceRepository,
     IGenericRepository<CourseVersion> courseVersionRepository,
     IGenericRepository<Resource> resourceRepository,
     IGenericRepository<FileStorage> fileStorageRepository,
     ICourseAssignmentService assignmentService,
     IScormService scormService,
     AppDbContext context) // <--- เพิ่มตรงนี้
        {
            _courseRepo = courseRepository;
            _courseResourceRepository = courseResourceRepository;
            _courseVersionRepository = courseVersionRepository;
            _resourceRepository = resourceRepository;
            _fileStorageRepository = fileStorageRepository;
            _assignmentService = assignmentService;
            _scormService = scormService;
            _context = context; // <--- เพิ่มตรงนี้
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
            // ==========================================
            // 🛡️ 1. ดักจับการใช้รหัสวิชาซ้ำ (ป้องกันบั๊กข้อมูลชนกัน)
            // ==========================================
            if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
            {
                return BadRequest(new { success = false, message = $"รหัสวิชา '{model.CourseCode}' มีอยู่ในระบบแล้ว กรุณาใช้รหัสอื่น" });
            }

            // ==========================================
            // 🛡️ 2. กางโล่ Database Transaction
            // ==========================================
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 3. สร้าง Course (บังคับเป็น Inactive/Draft)
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

                // 4. สร้าง Version แรก (Draft)
                var version = new CourseVersion
                {
                    CourseId = course.Id,
                    VersionNumber = 1, // สร้างคอร์สใหม่ เลขเวอร์ชันจะเป็น 1 เสมอ (ไม่ต้องรันเลขหน้าเว็บแล้ว)
                    Note = "Draft (Initial Upload)",
                    IsActive = false,
                    CreatedAt = DateTime.Now
                };
                await _courseVersionRepository.AddAsync(version);

                // 5. จัดการไฟล์และบันทึกลง DB
                if (model.Files != null && model.Files.Count > 0)
                {
                    foreach (var file in model.Files)
                    {
                        if (file.Length > 0)
                        {
                            using (var ms = new MemoryStream())
                            {
                                await file.CopyToAsync(ms);
                                var fileBytes = ms.ToArray();

                                // A. บันทึกเข้าตาราง FileStorage
                                var fileStorage = new FileStorage
                                {
                                    Name = file.FileName,
                                    ContentType = file.ContentType,
                                    Data = fileBytes,
                                    Length = file.Length,
                                    CreatedAt = DateTime.Now
                                };
                                await _fileStorageRepository.AddAsync(fileStorage);

                                // B. สร้าง Resource
                                var resource = new Resource
                                {
                                    Name = file.FileName,
                                    TypeId = 1, // 1 = Learn/SCORM
                                    IsActive = true,
                                    FileStorageId = fileStorage.Id,
                                    URL = file.FileName,
                                    CreatedAt = DateTime.Now
                                };
                                await _resourceRepository.AddAsync(resource);

                                // C. สร้างความสัมพันธ์ CourseResource
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

                // ==========================================
                // 🛡️ 6. บันทึกข้อมูลทั้งหมดลงฐานข้อมูล (Commit)
                // ถ้ารอดมาถึงบรรทัดนี้ได้ แสดงว่าทุกอย่างสมบูรณ์!
                // ==========================================
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "สร้างหลักสูตรและบันทึกไฟล์เรียบร้อยแล้ว",
                    courseId = course.Id
                });
            }
            catch (Exception ex)
            {
                // ==========================================
                // 🛡️ 7. ยกเลิกการทำงานทั้งหมด (Rollback)
                // ถ้าเกิด Error เช่น ไฟดับ หรือเซฟไฟล์ไม่ได้ ข้อมูลตารางแรกๆ จะถูกดึงกลับทั้งหมด
                // ==========================================
                await transaction.RollbackAsync();

                return StatusCode(500, new { success = false, message = $"เกิดข้อผิดพลาดในการบันทึกข้อมูล: {ex.Message}" });
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
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound(new { success = false, message = "ไม่พบหลักสูตรที่ต้องการแก้ไข" });

            // ========================================================
            // [ตอน Publish] ตรวจสอบความพร้อม และ "แตกไฟล์ SCORM"
            // ========================================================
            if (dto.IsActive == true)
            {
                var activeVersions = await _courseVersionRepository.GetAsync(v => v.CourseId == id && v.IsActive);
                bool isReadyToPublish = false;

                foreach (var version in activeVersions)
                {
                    var courseResources = await _courseResourceRepository.GetAsync(
                        filter: cr => cr.CourseVersionId == version.Id,
                        includeProperties: "Resource"
                    );

                    var activeResources = courseResources.Where(cr => cr.Resource != null && cr.Resource.IsActive).ToList();

                    if (activeResources.Any())
                    {
                        isReadyToPublish = true;

                        // --- ใช้งาน ScormService ของคุณในการแตกไฟล์ ---
                        foreach (var cr in activeResources)
                        {
                            // ถ้าเป็น SCORM (สมมติ TypeId == 1) และมี FileStorageId
                            if (cr.Resource.TypeId == 1 && cr.Resource.FileStorageId.HasValue)
                            {
                                var fileStorage = await _fileStorageRepository.GetByIdAsync(cr.Resource.FileStorageId.Value);
                                if (fileStorage != null && fileStorage.Data != null)
                                {
                                    var folderName = Path.GetFileNameWithoutExtension(cr.Resource.URL);

                                    try
                                    {
                                        // โยน byte[] ให้ Service แตกไฟล์และตรวจสอบ Manifest
                                        var manifestInfo = await _scormService.ExtractAndParseScormAsync(fileStorage.Data, folderName);

                                        // (ทริคเสริม) คุณสามารถเก็บ ResourceHref ลง DB ได้เลย เพื่อให้หน้า Player เรียกไฟล์ได้ถูกต้อง
                                        // cr.Resource.Href = manifestInfo.ResourceHref;
                                        // await _resourceRepository.UpdateAsync(cr.Resource);
                                    }
                                    catch (InvalidScormPackageException ex)
                                    {
                                        // ดักจับ Error จาก Exception ที่คุณเขียนไว้ใน Service
                                        return BadRequest(new { success = false, message = $"ไฟล์ {cr.Resource.Name} มีปัญหา: {ex.Message}" });
                                    }
                                }
                            }
                        }
                        // ---------------------------------------------
                    }
                }

                if (!isReadyToPublish)
                {
                    return BadRequest(new { success = false, message = "ไม่สามารถ Publish ได้! หลักสูตรนี้ต้องมี Version และเนื้อหา (Resource) ที่เปิดใช้งาน (Active) อย่างน้อย 1 รายการ" });
                }
            }

            // ========================================================
            // [ตอน Unpublish] ปิดการใช้งานตัวลูก และ "ลบโฟลเดอร์ SCORM"
            // ========================================================
            if (dto.IsActive == false)
            {
                var versions = await _courseVersionRepository.GetAsync(v => v.CourseId == id);
                foreach (var version in versions)
                {
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

                            // --- ใช้งาน ScormService ในการลบโฟลเดอร์ ---
                            var folderName = Path.GetFileNameWithoutExtension(cr.Resource.URL);
                            _scormService.DeleteScormFolder(folderName);
                        }
                    }
                }
            }

            // ========================================================
            // อัปเดตสถานะของ Course
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

        [HttpPost("CreateVersion")]
        [Consumes("multipart/form-data")] // สำคัญมาก: บอก API ว่าจะรับข้อมูลแบบ FormData ที่มีไฟล์แนบ
        public async Task<IActionResult> CreateVersion([FromForm] CreateCourseVersionDto dto)
        {
            // 🛡️ กางโล่ Database Transaction ป้องกันข้อมูลพังกลางทาง
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ==========================================
                // 1. คำนวณเลข Version ใหม่ให้โดยอัตโนมัติ
                // ==========================================
                var existingVersions = await _courseVersionRepository.GetAsync(v => v.CourseId == dto.CourseId);

                int nextVersionNumber = existingVersions.Any()
                    ? existingVersions.Max(v => v.VersionNumber) + 1
                    : 1;

                // ==========================================
                // 2. จัดการสถานะ IsActive
                // ถ้าเวอร์ชันใหม่ถูกตั้งให้ Active ต้องไปปิดเวอร์ชันเก่าๆ ก่อน
                // ==========================================
                if (dto.IsActive)
                {
                    foreach (var oldVersion in existingVersions.Where(v => v.IsActive))
                    {
                        oldVersion.IsActive = false;
                        await _courseVersionRepository.UpdateAsync(oldVersion);
                    }
                }

                // ==========================================
                // 3. สร้าง CourseVersion ตัวใหม่
                // ==========================================
                var newVersion = new CourseVersion
                {
                    CourseId = dto.CourseId,
                    VersionNumber = nextVersionNumber,
                    Note = dto.Note,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.Now
                };
                await _courseVersionRepository.AddAsync(newVersion);

                // ==========================================
                // 4. บันทึกไฟล์ที่อัปโหลดมาใหม่ (ถ้ามี)
                // ==========================================
                if (dto.Files != null && dto.Files.Count > 0)
                {
                    foreach (var file in dto.Files)
                    {
                        if (file.Length > 0)
                        {
                            using (var ms = new MemoryStream())
                            {
                                await file.CopyToAsync(ms);
                                var fileBytes = ms.ToArray();

                                // A. บันทึกลง FileStorage
                                var fileStorage = new FileStorage
                                {
                                    Name = file.FileName,
                                    ContentType = file.ContentType,
                                    Data = fileBytes,
                                    Length = file.Length,
                                    CreatedAt = DateTime.Now
                                };
                                await _fileStorageRepository.AddAsync(fileStorage);

                                // B. สร้าง Resource ผูกกับ FileStorage
                                var resource = new Resource
                                {
                                    Name = file.FileName,
                                    TypeId = 1, // 1 = Learn/SCORM
                                    IsActive = true,
                                    FileStorageId = fileStorage.Id,
                                    URL = file.FileName,
                                    CreatedAt = DateTime.Now
                                };
                                await _resourceRepository.AddAsync(resource);

                                // C. สร้างความสัมพันธ์ CourseResource ให้ Version นี้
                                await _courseResourceRepository.AddAsync(new CourseResource
                                {
                                    CourseVersionId = newVersion.Id,
                                    ResourceId = resource.Id,
                                    CreatedAt = DateTime.Now
                                });
                            }
                        }
                    }
                }

                // ==========================================
                // 5. เชื่อมโยงไฟล์เดิม (ResourceIds) ที่เลือกมา
                // ==========================================
                if (dto.ResourceIds != null && dto.ResourceIds.Any())
                {
                    foreach (var resourceId in dto.ResourceIds)
                    {
                        await _courseResourceRepository.AddAsync(new CourseResource
                        {
                            CourseVersionId = newVersion.Id,
                            ResourceId = resourceId, // ใช้ ID ไฟล์เก่าที่ส่งมา
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                // ==========================================
                // 6. เซฟทุกอย่างลง Database
                // ==========================================
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "สร้างเวอร์ชันใหม่พร้อมเนื้อหาสำเร็จ!",
                    versionId = newVersion.Id
                });
            }
            catch (Exception ex)
            {
                // ถ้าระหว่างทางเกิด Error เช่น ไฟล์ใหญ่เกิน เซฟลง DB ไม่ผ่าน ให้ Rollback ดึงข้อมูลกลับทั้งหมด
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }
    }
}
