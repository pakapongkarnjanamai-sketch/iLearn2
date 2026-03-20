using iLearn.API.Services;
using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.API.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMaintenanceStatusService _maintenanceStatusService;
        private readonly IAdminActivityService _adminActivityService;
        private readonly IMemoryCache _cache;

        public ResourcesController(
            IGenericRepository<Resource> resourceRepo,
            IGenericRepository<FileStorage> fileRepo,
            IScormService scormService,
            ILogger<ResourcesController> logger,
            IMaintenanceStatusService maintenanceStatusService,
            IAdminActivityService adminActivityService,
            IMemoryCache cache) // ✅ เพิ่ม Logger ใน DI
        {
            _resourceRepo = resourceRepo;
            _fileRepo = fileRepo;
            _scormService = scormService;
            _logger = logger; // ✅ กำหนดค่า Logger
            _maintenanceStatusService = maintenanceStatusService;
            _adminActivityService = adminActivityService;
            _cache = cache;
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

            if (resource.IsActive && !string.IsNullOrEmpty(resource.URL) && !string.IsNullOrEmpty(resource.ResourceHref))
            {
                string URL = _scormService.GetScormUrl(resource.URL, resource.ResourceHref);
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
            ResourceStatsCache.Invalidate(_cache);

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
                ResourceStatsCache.Invalidate(_cache);
                await _adminActivityService.LogAsync(
                    actionType: "PublishResource",
                    entityType: nameof(Resource),
                    entityId: resource.Id,
                    title: $"Published resource {resource.Name}",
                    description: $"Published resource '{resource.Name}'.");
                return Ok(resource.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating resource");
                return StatusCode(500, $"Error activating resource: {ex.Message}");
            }
        }

        [HttpPost("Unpublish")]
        public async Task<IActionResult> Unpublish([FromQuery] int key)
        {
            try
            {
                var resource = await _resourceRepo.GetQuery()
                    .Include(r => r.CourseResources)
                    .FirstOrDefaultAsync(r => r.Id == key);

                if (resource == null)
                    return NotFound(new { message = "Resource not found." });

                if (!resource.IsActive)
                    return BadRequest(new { message = "Resource is not published." });

                if (resource.CourseResources.Any())
                    return BadRequest(new { message = "Cannot unpublish a resource that is assigned to courses. Remove all course assignments first." });

                if (!string.IsNullOrEmpty(resource.URL))
                {
                    _scormService.DeleteScormFolder(resource.URL);
                    _logger.LogInformation("SCORM folder deleted for unpublish: {Folder}", resource.URL);
                }

                resource.IsActive = false;
                resource.URL = null;
                resource.ResourceHref = null;
                resource.SchemaVersion = null;

                await _resourceRepo.UpdateAsync(resource);
                ResourceStatsCache.Invalidate(_cache);
                return Ok(new { message = "Resource unpublished. Extracted files removed from server." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpublishing resource {Key}", key);
                return StatusCode(500, new { message = $"Error unpublishing resource: {ex.Message}" });
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

            // Soft Delete Resource — LearningLog.ResourceId ยังอ้างอิงได้
            await _resourceRepo.DeleteAsync(resource);
            ResourceStatsCache.Invalidate(_cache);

            if (resource.FileStorageId.HasValue)
            {
                var file = await _fileRepo.GetByIdAsync(resource.FileStorageId.Value);
                // Hard Delete FileStorage — ลบ binary data จริง ไม่มี FK อ้างอิงมา
                if (file != null) await _fileRepo.HardDeleteAsync(file);
            }

            return NoContent();
        }

        /// <summary>
        /// Analyze resources for optimization — find unused published and unpublished-but-needed resources.
        /// </summary>
        [HttpGet("Admin/OptimizeAnalysis")]
        public async Task<IActionResult> OptimizeAnalysis()
        {
            // 1) Published resources NOT linked to any active CourseVersion
            var unusedPublished = await _resourceRepo.GetQuery()
                .Where(r => r.IsActive)
                .Where(r => !r.CourseResources.Any(cr => cr.CourseVersion != null && cr.CourseVersion.IsActive))
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.TypeId,
                    r.URL,
                    fileLength = r.FileStorage != null ? r.FileStorage.Length : 0,
                    totalCourseCount = r.CourseResources.Count()
                })
                .ToListAsync();

            var unusedList = unusedPublished.Select(r =>
            {
                (int FileCount, long TotalSize) info = !string.IsNullOrEmpty(r.URL)
                    ? _scormService.GetFolderInfo(r.URL)
                    : (0, 0L);
                return new
                {
                    r.Id,
                    r.Name,
                    r.TypeId,
                    r.fileLength,
                    r.totalCourseCount,
                    serverFileCount = info.FileCount,
                    serverSize = info.TotalSize
                };
            }).ToList();

            // 2) Draft resources linked to active CourseVersions (should be published)
            var shouldPublishRaw = await _resourceRepo.GetQuery()
                .Where(r => !r.IsActive && r.FileStorageId != null)
                .Where(r => r.CourseResources.Any(cr => cr.CourseVersion != null && cr.CourseVersion.IsActive))
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.TypeId,
                    fileLength = r.FileStorage != null ? r.FileStorage.Length : 0
                })
                .ToListAsync();

            // Load active course version details for the matched resources
            var shouldPublishIds = shouldPublishRaw.Select(r => r.Id).ToList();
            var courseDetails = shouldPublishIds.Count > 0
                ? await _resourceRepo.GetQuery()
                    .Where(r => shouldPublishIds.Contains(r.Id))
                    .SelectMany(r => r.CourseResources
                        .Where(cr => cr.CourseVersion != null && cr.CourseVersion.IsActive)
                        .Select(cr => new
                        {
                            resourceId = r.Id,
                            courseId = cr.CourseVersion!.CourseId,
                            courseCode = cr.CourseVersion.Course != null ? cr.CourseVersion.Course.Code : "",
                            versionNumber = cr.CourseVersion.VersionNumber
                        }))
                    .ToListAsync()
                : [];

            var courseDetailsGrouped = courseDetails
                .GroupBy(x => x.resourceId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new { x.courseId, x.courseCode, x.versionNumber })
                          .DistinctBy(x => new { x.courseId, x.versionNumber })
                          .ToList()
                );

            var shouldPublish = shouldPublishRaw.Select(r => new
            {
                r.Id,
                r.Name,
                r.TypeId,
                r.fileLength,
                activeCourseVersions = courseDetailsGrouped.TryGetValue(r.Id, out var cvs) ? cvs : []
            }).ToList();

            long totalReclaimable = unusedList.Sum(r => r.serverSize);

            return Ok(new
            {
                unusedPublished = unusedList,
                shouldPublish,
                summary = new
                {
                    unusedCount = unusedList.Count,
                    shouldPublishCount = shouldPublish.Count,
                    totalReclaimableSize = totalReclaimable
                }
            });
        }

        /// <summary>
        /// Batch unpublish resources by IDs — removes extracted files from server.
        /// </summary>
        [HttpPost("Admin/BatchUnpublish")]
        public async Task<IActionResult> BatchUnpublish([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { message = "No resource IDs provided." });

            int success = 0;
            int failed = 0;
            var errors = new List<object>();

            foreach (var id in ids)
            {
                try
                {
                    var resource = await _resourceRepo.GetByIdAsync(id);
                    if (resource == null || !resource.IsActive) { failed++; continue; }

                    if (!string.IsNullOrEmpty(resource.URL))
                        _scormService.DeleteScormFolder(resource.URL);

                    resource.IsActive = false;
                    resource.URL = null;
                    resource.ResourceHref = null;
                    resource.SchemaVersion = null;

                    await _resourceRepo.UpdateAsync(resource);
                    ResourceStatsCache.Invalidate(_cache);
                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add(new { id, error = ex.Message });
                    _logger.LogError(ex, "BatchUnpublish failed for resource {Id}", id);
                }
            }

            return Ok(new { success, failed, errors, message = $"Unpublished {success} resource(s). {failed} failed." });
        }

        /// <summary>
        /// Batch publish resources by IDs — extracts SCORM packages to server.
        /// </summary>
        [HttpPost("Admin/BatchPublish")]
        public async Task<IActionResult> BatchPublish([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { message = "No resource IDs provided." });

            int success = 0;
            int failed = 0;
            var errors = new List<object>();

            foreach (var id in ids)
            {
                try
                {
                    var resource = await _resourceRepo.GetByIdAsync(id);
                    if (resource == null || resource.IsActive) { failed++; continue; }

                    var fileStorage = await _fileRepo.GetByIdAsync(resource.FileStorageId ?? 0);
                    if (fileStorage?.Data == null) { failed++; errors.Add(new { id, error = "No file data" }); continue; }

                    string extension = Path.GetExtension(resource.Name).ToLower();
                    if (extension == ".zip")
                    {
                        string folderName = Guid.NewGuid().ToString();
                        var scormInfo = await _scormService.ExtractAndParseScormAsync(fileStorage.Data, folderName);
                        resource.ResourceHref = scormInfo.ResourceHref;
                        resource.SchemaVersion = scormInfo.SchemaVersion;
                        resource.URL = scormInfo.FolderName;
                    }

                    resource.IsActive = true;
                    await _resourceRepo.UpdateAsync(resource);
                    ResourceStatsCache.Invalidate(_cache);
                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add(new { id, error = ex.Message });
                    _logger.LogError(ex, "BatchPublish failed for resource {Id}", id);
                }
            }

            return Ok(new { success, failed, errors, message = $"Published {success} resource(s). {failed} failed." });
        }

        [HttpPost("Admin/BatchPublishStream")]
        public async Task BatchPublishStream([FromBody] List<int> ids)
        {
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            Response.Headers.Append("Content-Type", "text/event-stream; charset=utf-8");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            await Response.StartAsync();

            var stopwatch = Stopwatch.StartNew();
            var success = 0;
            var failed = 0;
            var operationId = _maintenanceStatusService.BeginOperation(
                "Batch Publish Resources",
                ids?.Count ?? 0,
                User.Identity?.Name ?? "SYSTEM");

            await WriteProgressAsync(new BulkOperationProgressDto
            {
                CurrentItem = 0,
                TotalItems = ids?.Count ?? 0,
                SuccessCount = 0,
                FailureCount = 0,
                CurrentStep = "Starting batch publish",
                ElapsedTime = stopwatch.Elapsed
            });
            _maintenanceStatusService.UpdateOperation(operationId, "Starting batch publish", currentItem: 0, successCount: 0, failureCount: 0);

            if (ids == null || ids.Count == 0)
            {
                _maintenanceStatusService.CompleteOperation(operationId, false, "No resource IDs provided", success, failed);
                await WriteProgressAsync(new BulkOperationProgressDto
                {
                    IsComplete = true,
                    CurrentStep = "No resource IDs provided.",
                    LatestResult = new BulkOperationItemDto
                    {
                        Success = false,
                        ErrorMessage = "No resource IDs provided."
                    },
                    ElapsedTime = stopwatch.Elapsed
                });
                return;
            }

            for (var index = 0; index < ids.Count; index++)
            {
                var resourceId = ids[index];
                var progress = new BulkOperationProgressDto
                {
                    CurrentItem = index + 1,
                    TotalItems = ids.Count,
                    SuccessCount = success,
                    FailureCount = failed,
                    ElapsedTime = stopwatch.Elapsed
                };

                try
                {
                    var resource = await _resourceRepo.GetByIdAsync(resourceId);
                    if (resource == null)
                    {
                        failed++;
                        progress.FailureCount = failed;
                        progress.CurrentStep = "Resource not found";
                        progress.LatestResult = new BulkOperationItemDto
                        {
                            ResourceId = resourceId,
                            Success = false,
                            ErrorMessage = "Resource not found."
                        };
                        _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                        await WriteProgressAsync(progress);
                        continue;
                    }

                    progress.CurrentResourceName = resource.Name;
                    progress.CurrentStep = "Loading resource";
                    _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                    await WriteProgressAsync(progress);

                    if (resource.IsActive)
                    {
                        failed++;
                        progress.FailureCount = failed;
                        progress.CurrentStep = "Skipped because the resource is already published";
                        progress.LatestResult = new BulkOperationItemDto
                        {
                            ResourceId = resource.Id,
                            ResourceName = resource.Name,
                            Success = false,
                            ErrorMessage = "Resource is already published."
                        };
                        _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                        await WriteProgressAsync(progress);
                        continue;
                    }

                    progress.CurrentStep = "Loading file from database";
                    _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                    await WriteProgressAsync(progress);

                    var fileStorage = await _fileRepo.GetByIdAsync(resource.FileStorageId ?? 0);
                    if (fileStorage?.Data == null)
                    {
                        failed++;
                        progress.FailureCount = failed;
                        progress.CurrentStep = "File data was not found";
                        progress.LatestResult = new BulkOperationItemDto
                        {
                            ResourceId = resource.Id,
                            ResourceName = resource.Name,
                            Success = false,
                            ErrorMessage = "No file data."
                        };
                        _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                        await WriteProgressAsync(progress);
                        continue;
                    }

                    var extension = Path.GetExtension(resource.Name).ToLowerInvariant();
                    var itemResult = new BulkOperationItemDto
                    {
                        ResourceId = resource.Id,
                        ResourceName = resource.Name
                    };

                    if (extension == ".zip")
                    {
                        progress.CurrentStep = "Extracting and validating SCORM package";
                        _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                        await WriteProgressAsync(progress);

                        var folderName = Guid.NewGuid().ToString();

                        try
                        {
                            var scormInfo = await _scormService.ExtractAndParseScormAsync(fileStorage.Data, folderName);
                            resource.ResourceHref = scormInfo.ResourceHref;
                            resource.SchemaVersion = scormInfo.SchemaVersion;
                            resource.URL = scormInfo.FolderName;
                            itemResult.Details = $"SCORM {scormInfo.SchemaVersion} - {scormInfo.ResourceHref}";
                        }
                        catch (InvalidScormPackageException ex)
                        {
                            _scormService.DeleteScormFolder(folderName);
                            failed++;
                            progress.FailureCount = failed;
                            progress.CurrentStep = "SCORM validation failed";
                            progress.LatestResult = new BulkOperationItemDto
                            {
                                ResourceId = resource.Id,
                                ResourceName = resource.Name,
                                Success = false,
                                ErrorMessage = $"Invalid SCORM package: {ex.Message}"
                            };
                            _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                            await WriteProgressAsync(progress);
                            continue;
                        }
                    }
                    else
                    {
                        progress.CurrentStep = "Preparing non-SCORM resource";
                        _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                        await WriteProgressAsync(progress);
                        itemResult.Details = $"File type {extension}";
                    }

                    progress.CurrentStep = "Saving publish status";
                    _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                    await WriteProgressAsync(progress);

                    resource.IsActive = true;
                    await _resourceRepo.UpdateAsync(resource);

                    success++;
                    ResourceStatsCache.Invalidate(_cache);
                    progress.SuccessCount = success;
                    progress.CurrentStep = "Completed";
                    progress.LatestResult = new BulkOperationItemDto
                    {
                        ResourceId = itemResult.ResourceId,
                        ResourceName = itemResult.ResourceName,
                        Success = true,
                        Details = itemResult.Details
                    };
                    progress.ElapsedTime = stopwatch.Elapsed;
                    _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentResourceName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                    await WriteProgressAsync(progress);
                }
                catch (Exception ex)
                {
                    failed++;
                    var errorProgress = new BulkOperationProgressDto
                    {
                        CurrentItem = index + 1,
                        TotalItems = ids.Count,
                        SuccessCount = success,
                        FailureCount = failed,
                        CurrentStep = "Unexpected error",
                        ElapsedTime = stopwatch.Elapsed,
                        LatestResult = new BulkOperationItemDto
                        {
                            ResourceId = resourceId,
                            Success = false,
                            ErrorMessage = ex.Message
                        }
                    };
                    _maintenanceStatusService.UpdateOperation(operationId, errorProgress.CurrentStep, errorProgress.CurrentResourceName, errorProgress.CurrentItem, errorProgress.SuccessCount, errorProgress.FailureCount);
                    await WriteProgressAsync(errorProgress);

                    _logger.LogError(ex, "BatchPublishStream failed for resource {Id}", resourceId);
                }
            }

            _maintenanceStatusService.CompleteOperation(operationId, failed == 0, "Batch publish completed", success, failed);
            await _adminActivityService.LogAsync(
                actionType: "BatchPublishResources",
                entityType: nameof(Resource),
                entityId: null,
                title: "Completed batch publish",
                description: $"Batch published {success} resource(s) with {failed} failure(s).");
            await WriteProgressAsync(new BulkOperationProgressDto
            {
                CurrentItem = ids.Count,
                TotalItems = ids.Count,
                SuccessCount = success,
                FailureCount = failed,
                IsComplete = true,
                CurrentStep = "Batch publish completed",
                ElapsedTime = stopwatch.Elapsed
            });
        }


        /// <summary>
        /// 🔓 [ADMIN] SetPublic ทีละไฟล์แบบ Streaming (ไม่ต้องรอให้เสร็จทั้งหมด)
        /// </summary>
        [HttpPost("Admin/BulkSetPublic")]
        //[Authorize(Roles = "Admin")]
        public async Task BulkSetPublicStreaming()
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            var stopwatch = Stopwatch.StartNew();
            int currentItem = 0;
            int successCount = 0;
            int failureCount = 0;

            try
            {
                // ✅ นับจำนวนทั้งหมดก่อน (query เบาๆ)
                var totalCount = await _resourceRepo.CountAsync(r => r.IsActive == false && r.FileStorageId != null);

                if (totalCount == 0)
                {
                    await WriteProgressAsync(new BulkOperationProgressDto
                    {
                        IsComplete = true,
                        CurrentItem = 0,
                        TotalItems = 0,
                        ElapsedTime = stopwatch.Elapsed
                    });
                    return;
                }

                _logger.LogInformation($"🚀 Starting Streaming Bulk SetPublic for {totalCount} resources");

                // ✅ ดึงทีละ ID (ไม่โหลดข้อมูลทั้งหมด)
                var resourceIds = await _resourceRepo.GetAsync(
                    filter: r => r.IsActive == false && r.FileStorageId != null,
                    selector: r => r.Id
                );

                // ✅ ประมวลผลทีละไฟล์
                foreach (var resourceId in resourceIds)
                {
                    currentItem++;

                    // โหลดเฉพาะไฟล์ที่กำลังประมวลผล
                    var resource = await _resourceRepo.GetByIdAsync(resourceId);
                    if (resource == null) continue;

                    var itemResult = new BulkOperationItemDto
                    {
                        ResourceId = resource.Id,
                        ResourceName = resource.Name
                    };

                    try
                    {
                        // โหลด FileStorage เฉพาะตอนต้องใช้
                        var fileStorage = await _fileRepo.GetByIdAsync(resource.FileStorageId ?? 0);

                        if (fileStorage == null || fileStorage.Data == null)
                        {
                            itemResult.Success = false;
                            itemResult.ErrorMessage = "ไม่พบไฟล์ที่เชื่อมโยง";
                            failureCount++;

                            await WriteProgressAsync(new BulkOperationProgressDto
                            {
                                CurrentItem = currentItem,
                                TotalItems = totalCount,
                                SuccessCount = successCount,
                                FailureCount = failureCount,
                                CurrentResourceName = resource.Name,
                                LatestResult = itemResult,
                                ElapsedTime = stopwatch.Elapsed
                            });

                            _logger.LogWarning($"❌ [{currentItem}/{totalCount}] {resource.Name} - No file attached");
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
                                failureCount++;

                                await WriteProgressAsync(new BulkOperationProgressDto
                                {
                                    CurrentItem = currentItem,
                                    TotalItems = totalCount,
                                    SuccessCount = successCount,
                                    FailureCount = failureCount,
                                    CurrentResourceName = resource.Name,
                                    LatestResult = itemResult,
                                    ElapsedTime = stopwatch.Elapsed
                                });

                                _logger.LogError($"❌ [{currentItem}/{totalCount}] {resource.Name} - Invalid SCORM: {ex.Message}");
                                continue;
                            }
                        }
                        else
                        {
                            resource.IsActive = true;
                            itemResult.Details = $"ไฟล์ประเภท {extension}";
                        }

                        await _resourceRepo.UpdateAsync(resource);

                        itemResult.Success = true;
                        successCount++;

                        // ส่ง Progress แบบ Real-time
                        await WriteProgressAsync(new BulkOperationProgressDto
                        {
                            CurrentItem = currentItem,
                            TotalItems = totalCount,
                            SuccessCount = successCount,
                            FailureCount = failureCount,
                            CurrentResourceName = resource.Name,
                            LatestResult = itemResult,
                            ElapsedTime = stopwatch.Elapsed
                        });

                        _logger.LogInformation($"✅ [{currentItem}/{totalCount}] {resource.Name} - {itemResult.Details}");
                    }
                    catch (Exception ex)
                    {
                        itemResult.Success = false;
                        itemResult.ErrorMessage = ex.Message;
                        failureCount++;

                        await WriteProgressAsync(new BulkOperationProgressDto
                        {
                            CurrentItem = currentItem,
                            TotalItems = totalCount,
                            SuccessCount = successCount,
                            FailureCount = failureCount,
                            CurrentResourceName = resource.Name,
                            LatestResult = itemResult,
                            ElapsedTime = stopwatch.Elapsed
                        });

                        _logger.LogError(ex, $"❌ [{currentItem}/{totalCount}] Error processing {resource.Name}");
                    }

                    // ✅ ให้ GC ทำงาน (ลด memory)
                    if (currentItem % 10 == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }

                // ส่งสถานะสุดท้าย
                stopwatch.Stop();
                await WriteProgressAsync(new BulkOperationProgressDto
                {
                    CurrentItem = currentItem,
                    TotalItems = totalCount,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    IsComplete = true,
                    ElapsedTime = stopwatch.Elapsed
                });

                _logger.LogInformation($"🎉 Streaming Bulk SetPublic Completed: ✅ {successCount}/{totalCount} (❌ {failureCount}) in {stopwatch.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "💥 Streaming Bulk SetPublic failed");

                await WriteProgressAsync(new BulkOperationProgressDto
                {
                    IsComplete = true,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    ElapsedTime = stopwatch.Elapsed,
                    LatestResult = new BulkOperationItemDto
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    }
                });
            }
        }

        private async Task WriteProgressAsync(BulkOperationProgressDto progress)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(progress);
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.BodyWriter.FlushAsync();
            await Response.Body.FlushAsync();
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
            var operationId = Guid.Empty;

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
                operationId = _maintenanceStatusService.BeginOperation(
                    "Unpublish All Published Resources",
                    result.TotalProcessed,
                    User.Identity?.Name ?? "SYSTEM");
                _maintenanceStatusService.UpdateOperation(operationId, "Starting unpublish all", currentItem: 0, successCount: 0, failureCount: 0);

                var currentItem = 0;
                foreach (var resource in resourcesList)
                {
                    currentItem++;
                    var itemResult = new BulkOperationItemDto
                    {
                        ResourceId = resource.Id, // ✅ แก้
                        ResourceName = resource.Name
                    };

                    try
                    {
                        _maintenanceStatusService.UpdateOperation(operationId, "Removing published files", resource.Name, currentItem, result.SuccessCount, result.FailureCount);
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
                        ResourceStatsCache.Invalidate(_cache);
                        result.Results.Add(itemResult);
                        _maintenanceStatusService.UpdateOperation(operationId, "Updating resource to draft", resource.Name, currentItem, result.SuccessCount, result.FailureCount);

                        _logger.LogInformation($"✅ [{result.SuccessCount}/{result.TotalProcessed}] {resource.Name} - {itemResult.Details}");
                    }
                    catch (Exception ex)
                    {
                        itemResult.Success = false;
                        itemResult.ErrorMessage = ex.Message;
                        result.FailureCount++;
                        result.Results.Add(itemResult);
                        _maintenanceStatusService.UpdateOperation(operationId, "Unexpected error", resource.Name, currentItem, result.SuccessCount, result.FailureCount);

                        _logger.LogError(ex, $"❌ [{result.SuccessCount + result.FailureCount}/{result.TotalProcessed}] Error deleting {resource.Name}");
                    }
                }

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Summary = $"✅ ลบสำเร็จ {result.SuccessCount}/{result.TotalProcessed} รายการ " +
                                $"(❌ ล้มเหลว {result.FailureCount}) ⏱️ ใช้เวลา {result.Duration.TotalSeconds:F2} วินาที";
                if (operationId != Guid.Empty)
                    _maintenanceStatusService.CompleteOperation(operationId, result.FailureCount == 0, "Unpublish all completed", result.SuccessCount, result.FailureCount);
                await _adminActivityService.LogAsync(
                    actionType: "UnpublishAllResources",
                    entityType: nameof(Resource),
                    entityId: null,
                    title: "Completed unpublish all published resources",
                    description: $"Unpublished {result.SuccessCount} resource(s) with {result.FailureCount} failure(s).");

                _logger.LogInformation($"🎉 Bulk Delete Completed: {result.Summary}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                _logger.LogError(ex, "💥 Bulk Delete operation failed");
                if (operationId != Guid.Empty)
                    _maintenanceStatusService.CompleteOperation(operationId, false, "Unpublish all failed", result.SuccessCount, result.FailureCount + 1);

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