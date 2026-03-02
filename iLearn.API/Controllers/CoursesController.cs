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
            // [ตอน Publish] ตรวจสอบความพร้อมของหลักสูตร
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

                    // เช็คว่ามี Resource ที่พร้อมใช้งาน (Active) อย่างน้อย 1 รายการหรือไม่
                    var activeResources = courseResources.Where(cr => cr.Resource != null && cr.Resource.IsActive).ToList();

                    if (activeResources.Any())
                    {
                        isReadyToPublish = true;
                        break; // เจอว่าพร้อมใช้งานแล้ว สามารถหยุดเช็คและเตรียม Publish ได้เลย
                    }
                }

                if (!isReadyToPublish)
                {
                    return BadRequest(new { success = false, message = "ไม่สามารถ Publish ได้! หลักสูตรนี้ต้องมี Version และเนื้อหา (Resource) ที่เปิดใช้งาน (Active) อย่างน้อย 1 รายการ" });
                }
            }

            // ========================================================
            // [ตอน Unpublish] ปิดการใช้งานเนื้อหาลูก (Resource)
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

                    // ปิดสถานะ Active ของ Resource ทั้งหมดเมื่อคอร์สถูก Unpublish
                    foreach (var cr in courseResources)
                    {
                        if (cr.Resource != null && cr.Resource.IsActive)
                        {
                            cr.Resource.IsActive = false;
                            await _resourceRepository.UpdateAsync(cr.Resource);
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

            // ========================================================
            // 🧹 ขั้นตอนที่ 1: ปิด Version เก่า และ "เก็บกวาด" Resource ที่ไม่ได้ใช้งานแล้ว
            // ========================================================
            foreach (var version in versions.Where(v => v.Id != versionId && v.IsActive))
            {
                // ปิดสถานะ Version เดิม
                version.IsActive = false;
                await _courseVersionRepository.UpdateAsync(version);

                // ดึง Resource ของ Version เดิมที่เพิ่งถูกปิดไป
                var oldCourseResources = await _courseResourceRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == version.Id,
                    includeProperties: "Resource"
                );

                foreach (var cr in oldCourseResources)
                {
                    var res = cr.Resource;
                    if (res != null && res.IsActive)
                    {
                        // ตรวจสอบว่า Resource ตัวนี้กำลังจะถูกใช้งานใน Version ใหม่ (versionId) ที่กำลังจะเปิดหรือไม่
                        // หรือถูกใช้งานใน CourseVersion อื่นๆ ที่ยัง Active อยู่หรือไม่ (เผื่อมีการแชร์ไฟล์ข้ามคอร์ส)
                        var allUsages = await _courseResourceRepository.GetAsync(
                            filter: x => x.ResourceId == res.Id,
                            includeProperties: "CourseVersion"
                        );

                        bool isUsedElsewhere = allUsages.Any(x =>
                            x.CourseVersionId == versionId || // กำลังจะถูกเปิดใช้ใน Version ใหม่
                            (x.CourseVersion != null && x.CourseVersion.IsActive) // หรือถูกใช้ในเวอร์ชันอื่นที่ Active อยู่
                        );

                        // ถ้า "ไม่มีใครใช้งานไฟล์นี้แล้ว" ให้ทำการ เคลียร์ค่า และ ลบโฟลเดอร์ทิ้ง
                        if (!isUsedElsewhere)
                        {
                            if (res.TypeId == 1 && !string.IsNullOrEmpty(res.URL))
                            {
                                // ลบโฟลเดอร์ SCORM ออกจาก Server (คืนพื้นที่)
                                _scormService.DeleteScormFolder(res.URL);

                                // เคลียร์ค่ากลับเป็นสถานะก่อนแตกไฟล์ (ย้อนกลับกระบวนการ Extract)
                                res.URL = res.Name; // คืนค่า URL ให้กลับเป็นชื่อไฟล์ต้นฉบับ
                                res.ResourceHref = null;
                                res.SchemaVersion = null;
                            }

                            // ปิดสถานะ Resource
                            res.IsActive = false;
                            await _resourceRepository.UpdateAsync(res);
                        }
                    }
                }
            }

            // ========================================================
            // 🚀 ขั้นตอนที่ 2: เปิด Version ใหม่ และ "แตกไฟล์" Resource ที่เพิ่งเข้ามา
            // ========================================================
            var targetVersion = versions.First(v => v.Id == versionId);
            if (!targetVersion.IsActive)
            {
                targetVersion.IsActive = true;
                await _courseVersionRepository.UpdateAsync(targetVersion);

                // ดึง Resource ของ Version ที่กำลังจะถูกเปิด
                var newCourseResources = await _courseResourceRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == targetVersion.Id,
                    includeProperties: "Resource"
                );

                foreach (var cr in newCourseResources)
                {
                    var res = cr.Resource;
                    if (res != null)
                    {
                        // เช็คว่าเป็น SCORM (TypeId = 1), มีไฟล์ใน DB และ "ยังไม่ได้ Active"
                        if (res.TypeId == 1 && res.FileStorageId.HasValue && !res.IsActive)
                        {
                            var fileStorage = await _fileStorageRepository.GetByIdAsync(res.FileStorageId.Value);
                            if (fileStorage != null && fileStorage.Data != null)
                            {
                                // ใช้ชื่อไฟล์เดิม (ตัดนามสกุล .zip ออก) เป็นชื่อโฟลเดอร์ เพื่อความเป็นระเบียบ
                                var folderName = Path.GetFileNameWithoutExtension(res.Name);

                                try
                                {
                                    // แตกไฟล์และอ่าน Manifest
                                    var scormInfo = await _scormService.ExtractAndParseScormAsync(fileStorage.Data, folderName);

                                    // อัปเดตข้อมูลลิงก์ SCORM และเปิดสถานะ
                                    res.ResourceHref = scormInfo.ResourceHref;
                                    res.SchemaVersion = scormInfo.SchemaVersion;
                                    res.URL = scormInfo.FolderName;
                                    res.IsActive = true;

                                    await _resourceRepository.UpdateAsync(res);
                                }
                                catch (InvalidScormPackageException ex)
                                {
                                    // กรณี Error แตกไฟล์ไม่ผ่าน ให้ลบโฟลเดอร์ขยะทิ้ง
                                    _scormService.DeleteScormFolder(folderName);
                                    return BadRequest(new { success = false, message = $"เกิดข้อผิดพลาดในการเตรียมไฟล์ SCORM '{res.Name}': {ex.Message}" });
                                }
                            }
                        }
                        else if (res.TypeId != 1 && !res.IsActive)
                        {
                            // สำหรับไฟล์ธรรมดา (ที่ไม่ใช่ SCORM) ก็เปิด Active เฉยๆ
                            res.IsActive = true;
                            await _resourceRepository.UpdateAsync(res);
                        }
                    }
                }
            }

            return Ok(new { success = true, message = "เปลี่ยนเวอร์ชันที่ใช้งาน พร้อมเตรียมและเคลียร์ไฟล์สำเร็จ" });
        }

        [HttpPost("CreateVersion")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateVersion([FromForm] CreateCourseVersionDto dto)
        {
            // 🛡️ กางโล่ Database Transaction
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
                await _context.SaveChangesAsync(); // บังคับ Save เพื่อเอา newVersion.Id

                // ==========================================
                // 4. จัดการเนื้อหา (Resource) แบบเรียงลำดับ
                // ==========================================
                if (dto.ResourceIds != null && dto.ResourceIds.Any())
                {
                    int fileUploadIndex = 0; // ตัวนับว่าถึงไฟล์อัปโหลดตัวไหนแล้ว
                    int sequenceOrder = 1;   // ลำดับที่ของเนื้อหา (ถ้าในอนาคตมีฟิลด์ Order ใน DB)

                    // วนลูปตามที่หน้าเว็บส่งลำดับมา
                    foreach (var incomingId in dto.ResourceIds)
                    {
                        int finalResourceId = incomingId;

                        // 🌟 ถ้าเจอเลข 0 แปลว่าตำแหน่งนี้คือ "ไฟล์ใหม่"
                        if (incomingId == 0)
                        {
                            // เช็กว่ามีไฟล์แนบมาให้หยิบใช้ไหม
                            if (dto.Files != null && fileUploadIndex < dto.Files.Count)
                            {
                                var file = dto.Files[fileUploadIndex];
                                fileUploadIndex++; // ขยับไปไฟล์ถัดไป

                                if (file.Length > 0)
                                {
                                    using var ms = new MemoryStream();
                                    await file.CopyToAsync(ms);
                                    var fileBytes = ms.ToArray();

                                    // A. สร้าง FileStorage
                                    var fileStorage = new FileStorage
                                    {
                                        Name = file.FileName,
                                        ContentType = file.ContentType,
                                        Data = fileBytes,
                                        Length = file.Length,
                                        CreatedAt = DateTime.Now
                                    };
                                    await _fileStorageRepository.AddAsync(fileStorage);
                                    await _context.SaveChangesAsync(); // Save เอา fileStorage.Id

                                    // B. ✨ สร้าง Resource ใหม่ (แก้ปัญหาที่คุณแจ้งมา)
                                    var newResource = new Resource
                                    {
                                        Name = file.FileName,
                                        TypeId = 1, // 1 = Learn/SCORM (ถ้าต้องแยก Exam อาจจะต้องส่ง TypeId มาเพิ่ม)
                                        IsActive = true,
                                        FileStorageId = fileStorage.Id,
                                        URL = file.FileName,
                                        CreatedAt = DateTime.Now
                                    };
                                    await _resourceRepository.AddAsync(newResource);
                                    await _context.SaveChangesAsync(); // Save เอา newResource.Id

                                    // เอา ID ของ Resource ที่เพิ่งสร้างใหม่ไปใช้ในขั้นตอน C
                                    finalResourceId = newResource.Id;
                                }
                            }
                        }

                        // C. ผูกเข้ากับ CourseVersion ด้วย CourseResource
                        if (finalResourceId > 0)
                        {
                            await _courseResourceRepository.AddAsync(new CourseResource
                            {
                                CourseVersionId = newVersion.Id,
                                ResourceId = finalResourceId,
                                // OrderIndex = sequenceOrder, // 💡 ถ้าใน DB คุณสร้างคอลัมน์เก็บลำดับ เปิดคอมเมนต์บรรทัดนี้ได้เลยครับ
                                CreatedAt = DateTime.Now
                            });
                            sequenceOrder++;
                        }
                    }
                }

                // ==========================================
                // 5. เซฟและยืนยันการทำ Transaction
                // ==========================================
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "สร้างเวอร์ชันใหม่และจัดลำดับเนื้อหาสำเร็จ!",
                    versionId = newVersion.Id
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }
    }
}
