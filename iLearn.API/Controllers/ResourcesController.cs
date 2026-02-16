using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResourcesController : ControllerBase
    {
        private readonly IGenericRepository<Resource> _resourceRepo;
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IScormService _scormService;
        private readonly ILogger<ResourcesController> _logger; // ✅ เพิ่ม Logger

        public ResourcesController(
            IGenericRepository<Resource> resourceRepo,
            IGenericRepository<FileStorage> fileRepo,
            IScormService scormService,
            ILogger<ResourcesController> logger) // ✅ เพิ่ม Logger ใน DI
        {
            _resourceRepo = resourceRepo;
            _fileRepo = fileRepo;
            _scormService = scormService;
            _logger = logger; // ✅ กำหนดค่า Logger
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var resources = await _resourceRepo.GetAllAsync();
            return Ok(resources.Select(r => r.ToDto()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var resource = await _resourceRepo.GetByIdAsync(id);
            if (resource == null) return NotFound();
            return Ok(resource.ToDto());
        }

        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetContent(int id)
        {
            var resources = await _resourceRepo.GetAsync(r => r.Id == id);
            var resource = resources.FirstOrDefault();

            if (resource == null) return NotFound();

            if (resource.IsActive && !string.IsNullOrEmpty(resource.URL))
            {
                string URL = _scormService.GetScormUrl(resource.URL,resource.ResourceHref);
                return Ok(new { url = URL });
            }

            var fileStorage = await _fileRepo.GetByIdAsync(resource.FileStorageId ?? 0);
            if (fileStorage == null || fileStorage.Data == null)
                return NotFound("File content missing");

           

            return File(fileStorage.Data, fileStorage.ContentType, fileStorage.Name);
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Upload(IFormFile file, int typeId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var fileStorage = new FileStorage
            {
                Name = file.FileName,
                ContentType = file.ContentType,
                Length = file.Length
            };

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileStorage.Data = ms.ToArray();
            }

            var savedFile = await _fileRepo.AddAsync(fileStorage);

            var resource = new Resource
            {
                Name = file.FileName,
                TypeId = typeId,
                IsActive = false,
                FileStorageId = savedFile.Id
            };

            var savedResource = await _resourceRepo.AddAsync(resource);

            return Ok(savedResource.ToDto());
        }

        [HttpPost("SetPublic")]
        public async Task<IActionResult> SetPublic([FromQuery] int key)
        {
            try
            {
                var resource = await _resourceRepo.GetByIdAsync(key);
                if (resource == null) return NotFound("Resource not found");

                var fileStorage = await _fileRepo.GetByIdAsync(resource.FileStorageId ?? 0);
                if (fileStorage == null || fileStorage.Data == null)
                    return NotFound("Associated file not found");

                string extension = Path.GetExtension(resource.Name).ToLower();

                if (extension == ".zip")
                {
                    string folderName = Guid.NewGuid().ToString();

                    try
                    {
                        var scormInfo = await _scormService.ExtractAndParseScormAsync(
                            fileStorage.Data,
                            folderName
                        );

                        resource.ResourceHref = scormInfo.ResourceHref;
                        resource.SchemaVersion = scormInfo.SchemaVersion;
                        resource.URL = scormInfo.FolderName;
                        resource.IsActive = true;

                        _logger.LogInformation($"✅ SCORM Activated: {scormInfo.FolderName}");
                    }
                    catch (InvalidScormPackageException ex)
                    {
                        _scormService.DeleteScormFolder(folderName);
                        return BadRequest(new
                        {
                            error = "Invalid SCORM Package",
                            message = ex.Message
                        });
                    }
                }
                else
                {
                    resource.IsActive = true;
                }

                await _resourceRepo.UpdateAsync(resource);
                return Ok(resource.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating resource");
                return StatusCode(500, $"Error activating resource: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var resource = await _resourceRepo.GetByIdAsync(id);
            if (resource == null) return NotFound();

            if (resource.IsActive && !string.IsNullOrEmpty(resource.URL))
            {
                var parts = resource.URL.Split('/');
                if (parts.Length >= 2)
                {
                    _scormService.DeleteScormFolder(parts[1]);
                }
            }

            await _resourceRepo.DeleteAsync(resource);

            if (resource.FileStorageId.HasValue)
            {
                var file = await _fileRepo.GetByIdAsync(resource.FileStorageId.Value);
                if (file != null) await _fileRepo.DeleteAsync(file);
            }

            return NoContent();
        }

        /// <summary>
        /// 🔓 [ADMIN] SetPublic ทั้งหมดที่ยังไม่ได้ SetPublic
        /// </summary>
        [HttpPost("Admin/BulkSetPublic")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkSetPublic()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new BulkOperationResultDto();

            try
            {
                // ✅ แก้จาก _resourceRepository เป็น _resourceRepo
                // ✅ ใช้ GetAsync แทน FindAsync (ตาม IGenericRepository)
                var inactiveResources = await _resourceRepo.GetAsync(
                    filter: r => r.IsActive == false && r.FileStorageId != null,
                    includeProperties: "FileStorage" // ✅ ใช้ string แทน include expression
                );

                var resourcesList = inactiveResources.ToList();
                result.TotalProcessed = resourcesList.Count;

                if (result.TotalProcessed == 0)
                {
                    result.Summary = "ไม่มี Resource ที่ต้อง SetPublic";
                    return Ok(result);
                }

                _logger.LogInformation($"🚀 Starting Bulk SetPublic for {result.TotalProcessed} resources");

                foreach (var resource in resourcesList)
                {
                    var itemResult = new BulkOperationItemDto
                    {
                        ResourceId = resource.Id, // ✅ แก้จาก ResourceId เป็น Id
                        ResourceName = resource.Name
                    };

                    try
                    {
                        // ✅ ต้อง Load FileStorage ด้วยตัวเองถ้า includeProperties ไม่ทำงาน
                        var fileStorage = await _fileRepo.GetByIdAsync(resource.FileStorageId ?? 0);

                        if (fileStorage == null || fileStorage.Data == null)
                        {
                            itemResult.Success = false;
                            itemResult.ErrorMessage = "ไม่พบไฟล์ที่เชื่อมโยง";
                            result.FailureCount++;
                            result.Results.Add(itemResult);
                            _logger.LogWarning($"❌ [{result.SuccessCount + result.FailureCount}/{result.TotalProcessed}] {resource.Name} - No file attached");
                            continue;
                        }

                        string extension = Path.GetExtension(resource.Name).ToLower();

                        if (extension == ".zip")
                        {
                            string folderName = Guid.NewGuid().ToString();

                            try
                            {
                                var scormInfo = await _scormService.ExtractAndParseScormAsync(
                                    fileStorage.Data,
                                    folderName
                                );

                                resource.ResourceHref = scormInfo.ResourceHref;
                                resource.SchemaVersion = scormInfo.SchemaVersion;
                                resource.URL = scormInfo.FolderName;
                                resource.IsActive = true;

                                itemResult.Details = $"SCORM {scormInfo.SchemaVersion} - {scormInfo.ResourceHref}";
                            }
                            catch (InvalidScormPackageException ex)
                            {
                                _scormService.DeleteScormFolder(folderName);
                                itemResult.Success = false;
                                itemResult.ErrorMessage = $"SCORM ไม่ถูกต้อง: {ex.Message}";
                                result.FailureCount++;
                                result.Results.Add(itemResult);
                                _logger.LogError($"❌ [{result.SuccessCount + result.FailureCount}/{result.TotalProcessed}] {resource.Name} - Invalid SCORM: {ex.Message}");
                                continue;
                            }
                        }
                        else
                        {
                            resource.IsActive = true;
                            itemResult.Details = $"ไฟล์ประเภท {extension}";
                        }

                        await _resourceRepo.UpdateAsync(resource); // ✅ แก้ตัวแปร

                        itemResult.Success = true;
                        result.SuccessCount++;
                        result.Results.Add(itemResult);

                        _logger.LogInformation($"✅ [{result.SuccessCount}/{result.TotalProcessed}] {resource.Name} - {itemResult.Details}");
                    }
                    catch (Exception ex)
                    {
                        itemResult.Success = false;
                        itemResult.ErrorMessage = ex.Message;
                        result.FailureCount++;
                        result.Results.Add(itemResult);

                        _logger.LogError(ex, $"❌ [{result.SuccessCount + result.FailureCount}/{result.TotalProcessed}] Error processing {resource.Name}");
                    }
                }

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Summary = $"✅ สำเร็จ {result.SuccessCount}/{result.TotalProcessed} รายการ " +
                                $"(❌ ล้มเหลว {result.FailureCount}) ⏱️ ใช้เวลา {result.Duration.TotalSeconds:F2} วินาที";

                _logger.LogInformation($"🎉 Bulk SetPublic Completed: {result.Summary}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                _logger.LogError(ex, "💥 Bulk SetPublic operation failed");

                return StatusCode(500, new
                {
                    error = "Bulk operation failed",
                    message = ex.Message,
                    result
                });
            }
        }

        /// <summary>
        /// 🗑️ [ADMIN] ลบไฟล์ทั้งหมดที่ SetPublic แล้ว (ย้ายกลับเป็น Inactive)
        /// </summary>
        [HttpDelete("Admin/BulkDeletePublished")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkDeletePublished([FromQuery] bool confirmDelete = false)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new BulkOperationResultDto();

            try
            {
                // ✅ แก้ตัวแปรและใช้ GetAsync
                var activeResources = await _resourceRepo.GetAsync(
                    filter: r => r.IsActive == true
                );

                var resourcesList = activeResources.ToList();
                result.TotalProcessed = resourcesList.Count;

                if (result.TotalProcessed == 0)
                {
                    result.Summary = "ไม่มี Resource ที่ SetPublic แล้ว";
                    return Ok(result);
                }

                if (!confirmDelete)
                {
                    var scormCount = resourcesList.Count(r => !string.IsNullOrEmpty(r.ResourceHref));
                    var nonScormCount = result.TotalProcessed - scormCount;

                    return Ok(new
                    {
                        warning = "⚠️ คำเตือน: คุณกำลังจะลบ Resource ที่ SetPublic แล้ว",
                        totalResources = result.TotalProcessed,
                        breakdown = new
                        {
                            scormPackages = scormCount,
                            regularFiles = nonScormCount
                        },
                        message = "⚠️ กรุณาเพิ่ม ?confirmDelete=true เพื่อยืนยันการลบ",
                        note = "การลบจะทำให้ Resource กลับเป็น Inactive และลบโฟลเดอร์ SCORM (ถ้ามี)",
                        resources = resourcesList.Select(r => new
                        {
                            ResourceId = r.Id, // ✅ แก้
                            r.Name,
                            r.URL,
                            IsScorm = !string.IsNullOrEmpty(r.ResourceHref),
                            r.SchemaVersion
                        }).ToList()
                    });
                }

                _logger.LogWarning($"🚨 Starting Bulk Delete for {result.TotalProcessed} published resources");

                foreach (var resource in resourcesList)
                {
                    var itemResult = new BulkOperationItemDto
                    {
                        ResourceId = resource.Id, // ✅ แก้
                        ResourceName = resource.Name
                    };

                    try
                    {
                        if (!string.IsNullOrEmpty(resource.URL) && !string.IsNullOrEmpty(resource.ResourceHref))
                        {
                            try
                            {
                                _scormService.DeleteScormFolder(resource.URL);
                                itemResult.Details = $"ลบโฟลเดอร์ SCORM: {resource.URL}";
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"⚠️ Failed to delete SCORM folder {resource.URL}: {ex.Message}");
                                itemResult.Details = $"⚠️ ลบโฟลเดอร์ไม่สำเร็จ: {ex.Message}";
                            }
                        }
                        else
                        {
                            itemResult.Details = "ไฟล์ธรรมดา";
                        }

                        resource.IsActive = false;
                        resource.URL = null;
                        resource.ResourceHref = null;
                        resource.SchemaVersion = null;

                        await _resourceRepo.UpdateAsync(resource); // ✅ แก้

                        itemResult.Success = true;
                        result.SuccessCount++;
                        result.Results.Add(itemResult);

                        _logger.LogInformation($"✅ [{result.SuccessCount}/{result.TotalProcessed}] {resource.Name} - {itemResult.Details}");
                    }
                    catch (Exception ex)
                    {
                        itemResult.Success = false;
                        itemResult.ErrorMessage = ex.Message;
                        result.FailureCount++;
                        result.Results.Add(itemResult);

                        _logger.LogError(ex, $"❌ [{result.SuccessCount + result.FailureCount}/{result.TotalProcessed}] Error deleting {resource.Name}");
                    }
                }

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Summary = $"✅ ลบสำเร็จ {result.SuccessCount}/{result.TotalProcessed} รายการ " +
                                $"(❌ ล้มเหลว {result.FailureCount}) ⏱️ ใช้เวลา {result.Duration.TotalSeconds:F2} วินาที";

                _logger.LogInformation($"🎉 Bulk Delete Completed: {result.Summary}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                _logger.LogError(ex, "💥 Bulk Delete operation failed");

                return StatusCode(500, new
                {
                    error = "Bulk operation failed",
                    message = ex.Message,
                    result
                });
            }
        }
    }
}