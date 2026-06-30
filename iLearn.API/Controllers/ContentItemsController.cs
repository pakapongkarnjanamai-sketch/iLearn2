using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace iLearn.API.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class ContentItemsController : ControllerBase
    {
        private readonly IGenericRepository<ContentItem> _contentItemRepo;
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IContentPublicationService _contentPublicationService;
        private readonly IScormService _scormService;
        private readonly ILogger<ContentItemsController> _logger; // ✅ เพิ่ม Logger
        private readonly IMaintenanceStatusService _maintenanceStatusService;
        private readonly IAdminActivityService _adminActivityService;
        private readonly IMemoryCache _cache;
        private readonly IGenericRepository<CourseContentItem> _courseContentItemRepo;

        public ContentItemsController(
            IGenericRepository<ContentItem> contentItemRepo,
            IGenericRepository<FileStorage> fileRepo,
            IContentPublicationService contentPublicationService,
            IScormService scormService,
            ILogger<ContentItemsController> logger,
            IMaintenanceStatusService maintenanceStatusService,
            IAdminActivityService adminActivityService,
            IMemoryCache cache,
            IGenericRepository<CourseContentItem> courseContentItemRepo) // ✅ เพิ่ม Logger ใน DI
        {
            _contentItemRepo = contentItemRepo;
            _fileRepo = fileRepo;
            _contentPublicationService = contentPublicationService;
            _scormService = scormService;
            _logger = logger; // ✅ กำหนดค่า Logger
            _maintenanceStatusService = maintenanceStatusService;
            _adminActivityService = adminActivityService;
            _cache = cache;
            _courseContentItemRepo = courseContentItemRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var contentItems = await _contentItemRepo.GetAllAsync();
            return Ok(contentItems.Select(r => r.ToDto()));
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationParams p)
        {
            // No Includes here: FileStorage holds the full SCORM ZIP as byte[],
            // so loading entities would pull every package blob into memory per page.
            // Project to the DTO instead so SQL returns only the listed columns.
            var query = _contentItemRepo.GetQuery().AsQueryable();

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var search = p.Search.Trim().ToLower();
                query = query.Where(r => r.Name.ToLower().Contains(search) || 
                                         (r.LaunchHref != null && r.LaunchHref.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(p.Status))
            {
                if (p.Status.Equals("Published", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.IsActive);
                }
                else if (p.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("Unpublished", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => !r.IsActive);
                }
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(p.SortBy))
            {
                var sortBy = p.SortBy.Trim().ToLower();
                if (sortBy == "name")
                {
                    query = p.SortDescending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name);
                }
                else if (sortBy == "typeid")
                {
                    query = p.SortDescending ? query.OrderByDescending(r => r.TypeId) : query.OrderBy(r => r.TypeId);
                }
                else if (sortBy == "schemaversion")
                {
                    query = p.SortDescending ? query.OrderByDescending(r => r.SchemaVersion) : query.OrderBy(r => r.SchemaVersion);
                }
                else if (sortBy == "isactive" || sortBy == "ispublished")
                {
                    query = p.SortDescending ? query.OrderByDescending(r => r.IsActive) : query.OrderBy(r => r.IsActive);
                }
                else if (sortBy == "updatedat")
                {
                    query = p.SortDescending ? query.OrderByDescending(r => r.UpdatedAt) : query.OrderBy(r => r.UpdatedAt);
                }
                else
                {
                    query = p.SortDescending ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id);
                }
            }
            else
            {
                query = query.OrderByDescending(r => r.Id);
            }

            int totalCount = query.Provider is IAsyncQueryProvider
                ? await query.CountAsync()
                : query.Count();

            var projected = query
                .Skip((p.Page - 1) * p.PageSize)
                .Take(p.PageSize)
                .Select(r => new ContentItemDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    TypeId = r.TypeId,
                    IsActive = r.IsActive,
                    LaunchHref = r.LaunchHref,
                    SchemaVersion = r.SchemaVersion,
                    Url = r.URL,
                    FileStorageId = r.FileStorageId,
                    FileLength = r.CachedFileLength ?? 0,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    CourseIdsCount = 0, // Computed in batch below
                });

            var dtos = projected.Provider is IAsyncQueryProvider
                ? await projected.ToListAsync()
                : projected.ToList();

            if (dtos.Count > 0)
            {
                var pagedIds = dtos.Select(d => d.Id).ToList();
                var courseCountQuery = _courseContentItemRepo.GetQuery()
                    .Where(cr => pagedIds.Contains(cr.ContentItemId) && cr.CourseVersion != null)
                    .Select(cr => new { cr.ContentItemId, cr.CourseVersion!.CourseId })
                    .Distinct()
                    .GroupBy(x => x.ContentItemId)
                    .Select(g => new { ContentItemId = g.Key, Count = g.Count() });

                var courseCountMap = courseCountQuery.Provider is IAsyncQueryProvider
                    ? await courseCountQuery.ToDictionaryAsync(g => g.ContentItemId, g => g.Count)
                    : courseCountQuery.ToDictionary(g => g.ContentItemId, g => g.Count);

                foreach (var dto in dtos)
                {
                    if (courseCountMap.TryGetValue(dto.Id, out int count))
                    {
                        dto.CourseIdsCount = count;
                    }
                }
            }

            return Ok(new { success = true, data = dtos, totalCount });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var contentItem = await _contentItemRepo.GetByIdAsync(id);
            if (contentItem == null) return NotFound();
            return Ok(contentItem.ToDto());
        }

        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetContent(int id)
        {
            var contentItems = await _contentItemRepo.GetAsync(r => r.Id == id);
            var contentItem = contentItems.FirstOrDefault();

            if (contentItem == null) return NotFound();

            if (contentItem.IsActive && !string.IsNullOrEmpty(contentItem.URL) && !string.IsNullOrEmpty(contentItem.LaunchHref))
            {
                string URL = _scormService.GetScormUrl(contentItem.URL, contentItem.LaunchHref);
                return Ok(new { url = URL });
            }

            var fileStorage = await _fileRepo.GetByIdAsync(contentItem.FileStorageId ?? 0);
            if (fileStorage == null || fileStorage.Data == null)
                return NotFound("File content missing");

            var safeFileName = ScormUploadValidation.NormalizeUploadedFileName(fileStorage.Name);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = $"contentItem-{contentItem.Id}.bin";
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(fileStorage.Data, "application/octet-stream", safeFileName);
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ScormPackageLimits.MaxRequestEnvelopeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ScormPackageLimits.MaxRequestEnvelopeBytes)]
        public async Task<IActionResult> Upload(IFormFile file, int typeId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                ScormUploadValidation.EnsureValidScormPackageUpload(file);
            }
            catch (InvalidScormPackageException ex)
            {
                return BadRequest(new
                {
                    error = "Invalid SCORM Package",
                    message = ex.Message
                });
            }

            var safeFileName = ScormUploadValidation.NormalizeUploadedFileName(file.FileName);

            var fileStorage = new FileStorage
            {
                Name = safeFileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/zip" : file.ContentType,
                Length = file.Length
            };

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileStorage.Data = ms.ToArray();
            }

            var savedFile = await _fileRepo.AddAsync(fileStorage);

            var contentItem = new ContentItem
            {
                Name = safeFileName,
                TypeId = typeId,
                IsActive = false,
                FileStorageId = savedFile.Id,
                CachedFileLength = savedFile.Length
            };

            var savedContentItem = await _contentItemRepo.AddAsync(contentItem);
            ContentItemStatsCache.Invalidate(_cache);

            return Ok(savedContentItem.ToDto());
        }

        [HttpPost("SetPublic")]
        public async Task<IActionResult> SetPublic([FromQuery] int key)
        {
            try
            {
                var contentItem = await _contentPublicationService.PublishAsync(key);
                ContentItemStatsCache.Invalidate(_cache);
                await _adminActivityService.LogAsync(
                    actionType: "PublishContentItem",
                    entityType: nameof(ContentItem),
                    entityId: contentItem.Id,
                    title: $"Published content item {contentItem.Name}",
                    description: $"Published content item '{contentItem.Name}'.");
                return Ok(contentItem);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidScormPackageException ex)
            {
                return BadRequest(new
                {
                    error = "Invalid SCORM Package",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating contentItem");
                return StatusCode(500, $"Error publishing content item: {ex.Message}");
            }
        }

        [HttpPost("Unpublish")]
        public async Task<IActionResult> Unpublish([FromQuery] int key)
        {
            try
            {
                var contentItem = await _contentPublicationService.UnpublishAsync(key);
                ContentItemStatsCache.Invalidate(_cache);
                _logger.LogInformation("Content item unpublished: {ContentItemId}", contentItem.Id);
                return Ok(new { message = "Content item unpublished. Extracted files removed from server." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpublishing contentItem {Key}", key);
                return StatusCode(500, new { message = $"Error unpublishing contentItem: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var contentItem = await _contentItemRepo.GetByIdAsync(id);
            if (contentItem == null) return NotFound();

            if (contentItem.IsActive && !string.IsNullOrEmpty(contentItem.URL))
            {
                var parts = contentItem.URL.Split('/');
                if (parts.Length >= 2)
                {
                    _scormService.DeleteScormFolder(parts[1]);
                }
            }

            // Soft Delete ContentItem — LearningLog.ContentItemId ยังอ้างอิงได้
            await _contentItemRepo.DeleteAsync(contentItem);
            ContentItemStatsCache.Invalidate(_cache);

            if (contentItem.FileStorageId.HasValue)
            {
                var file = await _fileRepo.GetByIdAsync(contentItem.FileStorageId.Value);
                // Hard Delete FileStorage — ลบ binary data จริง ไม่มี FK อ้างอิงมา
                if (file != null) await _fileRepo.HardDeleteAsync(file);
            }

            return NoContent();
        }

        /// <summary>
        /// Analyze contentItems for optimization — find unused published and unpublished-but-needed contentItems.
        /// </summary>
        [HttpGet("Admin/OptimizeAnalysis")]
        public async Task<IActionResult> OptimizeAnalysis(CancellationToken cancellationToken)
        {
            // 1) Published contentItems NOT linked to any active CourseVersion
            var unusedPublished = await _contentItemRepo.GetQuery()
                .Where(r => r.IsActive)
                .Where(r => !r.CourseContentItems.Any(cr => cr.CourseVersion != null && cr.CourseVersion.IsActive))
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.TypeId,
                    r.URL,
                    fileLength = r.FileStorage != null ? r.FileStorage.Length : 0,
                    totalCourseCount = r.CourseContentItems.Count()
                })
                .ToListAsync(cancellationToken);

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

            // 2) Draft contentItems linked to active CourseVersions (should be published)
            var shouldPublishRaw = await _contentItemRepo.GetQuery()
                .Where(r => !r.IsActive && r.FileStorageId != null)
                .Where(r => r.CourseContentItems.Any(cr => cr.CourseVersion != null && cr.CourseVersion.IsActive))
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.TypeId,
                    fileLength = r.FileStorage != null ? r.FileStorage.Length : 0
                })
                .ToListAsync(cancellationToken);

            // Load active course version details for the matched contentItems
            var shouldPublishIds = shouldPublishRaw.Select(r => r.Id).ToList();
            var courseDetails = shouldPublishIds.Count > 0
                ? await _contentItemRepo.GetQuery()
                    .Where(r => shouldPublishIds.Contains(r.Id))
                    .SelectMany(r => r.CourseContentItems
                        .Where(cr => cr.CourseVersion != null && cr.CourseVersion.IsActive)
                        .Select(cr => new
                        {
                            contentItemId = r.Id,
                            courseId = cr.CourseVersion!.CourseId,
                            courseCode = cr.CourseVersion.Course != null ? cr.CourseVersion.Course.Code : "",
                            versionNumber = cr.CourseVersion.VersionNumber
                        }))
                    .ToListAsync(cancellationToken)
                : [];

            var courseDetailsGrouped = courseDetails
                .GroupBy(x => x.contentItemId)
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
        /// Batch unpublish contentItems by IDs — removes extracted files from server.
        /// </summary>
        [HttpPost("Admin/BatchUnpublish")]
        public async Task<IActionResult> BatchUnpublish([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { message = "No content items selected." });

            var preview = await _contentPublicationService.PreviewBatchUnpublishAsync(ids);
            int success = 0;
            int failed = preview.BlockedCount;
            var errors = new List<object>();

            foreach (var blockedItem in preview.Items.Where(item => !item.CanUnpublish))
            {
                errors.Add(new
                {
                    id = blockedItem.ContentItemId,
                    error = blockedItem.BlockingReason,
                    linkedCourseCodes = blockedItem.LinkedCourseCodes
                });
            }

            foreach (var id in preview.EligibleIds)
            {
                try
                {
                    await _contentPublicationService.UnpublishAsync(id);
                    success++;
                }
                catch (KeyNotFoundException ex)
                {
                    failed++;
                    errors.Add(new { id, error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    failed++;
                    errors.Add(new { id, error = ex.Message });
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add(new { id, error = ex.Message });
                    _logger.LogError(ex, "BatchUnpublish failed for contentItem {Id}", id);
                }
            }

            if (success > 0)
            {
                ContentItemStatsCache.Invalidate(_cache);
            }

            return Ok(new
            {
                success,
                failed,
                blocked = preview.BlockedCount,
                errors,
                eligibleIds = preview.EligibleIds,
                message = $"Unpublished {success} content item(s). {failed} blocked or failed."
            });
        }

        [HttpPost("Admin/PreviewBatchUnpublish")]
        public async Task<IActionResult> PreviewBatchUnpublish([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(new { message = "No content items selected." });
            }

            return Ok(await _contentPublicationService.PreviewBatchUnpublishAsync(ids));
        }

        /// <summary>
        /// Batch publish contentItems by IDs — extracts SCORM packages to server.
        /// </summary>
        [HttpPost("Admin/BatchPublish")]
        public async Task<IActionResult> BatchPublish([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { message = "No content items selected." });

            int success = 0;
            int failed = 0;
            var errors = new List<object>();

            foreach (var id in ids)
            {
                try
                {
                    await _contentPublicationService.PublishAsync(id);
                    success++;
                }
                catch (KeyNotFoundException ex)
                {
                    failed++;
                    errors.Add(new { id, error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    failed++;
                    errors.Add(new { id, error = ex.Message });
                }
                catch (InvalidScormPackageException ex)
                {
                    failed++;
                    errors.Add(new { id, error = ex.Message });
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add(new { id, error = ex.Message });
                    _logger.LogError(ex, "BatchPublish failed for contentItem {Id}", id);
                }
            }

            if (success > 0)
            {
                ContentItemStatsCache.Invalidate(_cache);
            }

            return Ok(new { success, failed, errors, message = $"Published {success} content item(s). {failed} failed." });
        }

        [HttpPost("Admin/BatchPublishStream")]
        public async Task BatchPublishStream([FromBody] List<int> ids, CancellationToken cancellationToken)
        {
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            Response.ContentType = "text/event-stream; charset=utf-8";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            await Response.StartAsync(cancellationToken);

            var stopwatch = Stopwatch.StartNew();
            var success = 0;
            var failed = 0;
            var operationId = _maintenanceStatusService.BeginOperation(
                "Batch Publish Content",
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
                _maintenanceStatusService.CompleteOperation(operationId, false, "No content items selected", success, failed);
                await WriteProgressAsync(new BulkOperationProgressDto
                {
                    IsComplete = true,
                    CurrentStep = "No content items selected.",
                    LatestResult = new BulkOperationItemDto
                    {
                        Success = false,
                        ErrorMessage = "No content items selected."
                    },
                    ElapsedTime = stopwatch.Elapsed
                });
                return;
            }

            for (var index = 0; index < ids.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var contentItemId = ids[index];
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
                    var contentItem = await _contentItemRepo.GetByIdAsync(contentItemId);
                    if (contentItem == null)
                    {
                        failed++;
                        progress.FailureCount = failed;
                        progress.CurrentStep = "Content item not found";
                        progress.LatestResult = new BulkOperationItemDto
                        {
                            ContentItemId = contentItemId,
                            Success = false,
                            ErrorMessage = "Content item not found."
                        };
                        _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentContentItemName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                        await WriteProgressAsync(progress);
                        continue;
                    }

                    progress.CurrentContentItemName = contentItem.Name;
                    progress.CurrentStep = "Loading content item";
                    _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentContentItemName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                    await WriteProgressAsync(progress);
                    progress.CurrentStep = "Applying shared publication policy";
                    _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentContentItemName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                    await WriteProgressAsync(progress);

                    var itemResult = new BulkOperationItemDto
                    {
                        ContentItemId = contentItem.Id,
                        ContentItemName = contentItem.Name
                    };

                    await _contentPublicationService.PublishAsync(contentItemId);
                    itemResult.Details = "Published through shared content publication policy";

                    success++;
                    ContentItemStatsCache.Invalidate(_cache);
                    progress.SuccessCount = success;
                    progress.CurrentStep = "Completed";
                    progress.LatestResult = new BulkOperationItemDto
                    {
                        ContentItemId = itemResult.ContentItemId,
                        ContentItemName = itemResult.ContentItemName,
                        Success = true,
                        Details = itemResult.Details
                    };
                    progress.ElapsedTime = stopwatch.Elapsed;
                    _maintenanceStatusService.UpdateOperation(operationId, progress.CurrentStep, progress.CurrentContentItemName, progress.CurrentItem, progress.SuccessCount, progress.FailureCount);
                    await WriteProgressAsync(progress);
                }
                catch (KeyNotFoundException ex)
                {
                    failed++;
                    var currentContentItemName = progress.CurrentContentItemName ?? string.Empty;
                    var errorProgress = new BulkOperationProgressDto
                    {
                        CurrentItem = index + 1,
                        TotalItems = ids.Count,
                        SuccessCount = success,
                        FailureCount = failed,
                        CurrentContentItemName = currentContentItemName,
                        CurrentStep = "Validation failed",
                        ElapsedTime = stopwatch.Elapsed,
                        LatestResult = new BulkOperationItemDto
                        {
                            ContentItemId = contentItemId,
                            ContentItemName = currentContentItemName,
                            Success = false,
                            ErrorMessage = ex.Message
                        }
                    };
                    _maintenanceStatusService.UpdateOperation(operationId, errorProgress.CurrentStep, errorProgress.CurrentContentItemName, errorProgress.CurrentItem, errorProgress.SuccessCount, errorProgress.FailureCount);
                    await WriteProgressAsync(errorProgress);
                }
                catch (InvalidOperationException ex)
                {
                    failed++;
                    var currentContentItemName = progress.CurrentContentItemName ?? string.Empty;
                    var errorProgress = new BulkOperationProgressDto
                    {
                        CurrentItem = index + 1,
                        TotalItems = ids.Count,
                        SuccessCount = success,
                        FailureCount = failed,
                        CurrentContentItemName = currentContentItemName,
                        CurrentStep = "Validation failed",
                        ElapsedTime = stopwatch.Elapsed,
                        LatestResult = new BulkOperationItemDto
                        {
                            ContentItemId = contentItemId,
                            ContentItemName = currentContentItemName,
                            Success = false,
                            ErrorMessage = ex.Message
                        }
                    };
                    _maintenanceStatusService.UpdateOperation(operationId, errorProgress.CurrentStep, errorProgress.CurrentContentItemName, errorProgress.CurrentItem, errorProgress.SuccessCount, errorProgress.FailureCount);
                    await WriteProgressAsync(errorProgress);
                }
                catch (InvalidScormPackageException ex)
                {
                    failed++;
                    var currentContentItemName = progress.CurrentContentItemName ?? string.Empty;
                    var errorProgress = new BulkOperationProgressDto
                    {
                        CurrentItem = index + 1,
                        TotalItems = ids.Count,
                        SuccessCount = success,
                        FailureCount = failed,
                        CurrentContentItemName = currentContentItemName,
                        CurrentStep = "SCORM validation failed",
                        ElapsedTime = stopwatch.Elapsed,
                        LatestResult = new BulkOperationItemDto
                        {
                            ContentItemId = contentItemId,
                            ContentItemName = currentContentItemName,
                            Success = false,
                            ErrorMessage = $"Invalid SCORM package: {ex.Message}"
                        }
                    };
                    _maintenanceStatusService.UpdateOperation(operationId, errorProgress.CurrentStep, errorProgress.CurrentContentItemName, errorProgress.CurrentItem, errorProgress.SuccessCount, errorProgress.FailureCount);
                    await WriteProgressAsync(errorProgress);
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
                            ContentItemId = contentItemId,
                            Success = false,
                            ErrorMessage = ex.Message
                        }
                    };
                    _maintenanceStatusService.UpdateOperation(operationId, errorProgress.CurrentStep, errorProgress.CurrentContentItemName, errorProgress.CurrentItem, errorProgress.SuccessCount, errorProgress.FailureCount);
                    await WriteProgressAsync(errorProgress);

                    _logger.LogError(ex, "BatchPublishStream failed for contentItem {Id}", contentItemId);
                }
            }

            _maintenanceStatusService.CompleteOperation(operationId, failed == 0, "Batch publish completed", success, failed);
            await _adminActivityService.LogAsync(
                actionType: "BatchPublishContentItems",
                entityType: nameof(ContentItem),
                entityId: null,
                title: "Completed batch publish",
                description: $"Batch published {success} content item(s) with {failed} failure(s).");
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
        public async Task BulkSetPublicStreaming(CancellationToken cancellationToken)
        {
            // SSE response. Use ContentType for the media type and Append for
            // additional headers — Headers.Add throws on duplicate keys.
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            var stopwatch = Stopwatch.StartNew();
            int currentItem = 0;
            int successCount = 0;
            int failureCount = 0;

            try
            {
                // ✅ นับจำนวนทั้งหมดก่อน (query เบาๆ)
                var totalCount = await _contentItemRepo.CountAsync(r => r.IsActive == false && r.FileStorageId != null);

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

                _logger.LogInformation($"🚀 Starting Streaming Bulk SetPublic for {totalCount} contentItems");

                // ✅ ดึงทีละ ID (ไม่โหลดข้อมูลทั้งหมด)
                var contentItemIds = await _contentItemRepo.GetAsync(
                    filter: r => r.IsActive == false && r.FileStorageId != null,
                    selector: r => r.Id
                );

                // Process one file at a time
                foreach (var contentItemId in contentItemIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentItem++;

                    // โหลดเฉพาะไฟล์ที่กำลังประมวลผล
                    var contentItem = await _contentItemRepo.GetByIdAsync(contentItemId);
                    if (contentItem == null) continue;

                    var itemResult = new BulkOperationItemDto
                    {
                        ContentItemId = contentItem.Id,
                        ContentItemName = contentItem.Name
                    };

                    try
                    {
                        // โหลด FileStorage เฉพาะตอนต้องใช้
                        var fileStorage = await _fileRepo.GetByIdAsync(contentItem.FileStorageId ?? 0);

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
                                CurrentContentItemName = contentItem.Name,
                                LatestResult = itemResult,
                                ElapsedTime = stopwatch.Elapsed
                            });

                            _logger.LogWarning($"❌ [{currentItem}/{totalCount}] {contentItem.Name} - No file attached");
                            continue;
                        }

                        string extension = Path.GetExtension(contentItem.Name).ToLower();

                        if (extension == ".zip")
                        {
                            string folderName = Guid.NewGuid().ToString();

                            try
                            {
                                var scormInfo = await _scormService.ExtractAndParseScormAsync(
                                    fileStorage.Data,
                                    folderName
                                );

                                contentItem.LaunchHref = scormInfo.LaunchHref;
                                contentItem.SchemaVersion = scormInfo.SchemaVersion;
                                contentItem.URL = scormInfo.FolderName;
                                contentItem.IsActive = true;

                                itemResult.Details = $"SCORM {scormInfo.SchemaVersion} - {scormInfo.LaunchHref}";
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
                                    CurrentContentItemName = contentItem.Name,
                                    LatestResult = itemResult,
                                    ElapsedTime = stopwatch.Elapsed
                                });

                                _logger.LogError($"❌ [{currentItem}/{totalCount}] {contentItem.Name} - Invalid SCORM: {ex.Message}");
                                continue;
                            }
                        }
                        else
                        {
                            contentItem.IsActive = true;
                            itemResult.Details = $"ไฟล์ประเภท {extension}";
                        }

                        await _contentItemRepo.UpdateAsync(contentItem);

                        itemResult.Success = true;
                        successCount++;

                        // ส่ง Progress แบบ Real-time
                        await WriteProgressAsync(new BulkOperationProgressDto
                        {
                            CurrentItem = currentItem,
                            TotalItems = totalCount,
                            SuccessCount = successCount,
                            FailureCount = failureCount,
                            CurrentContentItemName = contentItem.Name,
                            LatestResult = itemResult,
                            ElapsedTime = stopwatch.Elapsed
                        });

                        _logger.LogInformation($"✅ [{currentItem}/{totalCount}] {contentItem.Name} - {itemResult.Details}");
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
                            CurrentContentItemName = contentItem.Name,
                            LatestResult = itemResult,
                            ElapsedTime = stopwatch.Elapsed
                        });

                        _logger.LogError(ex, $"❌ [{currentItem}/{totalCount}] Error processing {contentItem.Name}");
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

        private async Task WriteProgressAsync(BulkOperationProgressDto progress, CancellationToken cancellationToken = default)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(progress);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.BodyWriter.FlushAsync(cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
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
                var activeContentItems = await _contentItemRepo.GetAsync(
                    filter: r => r.IsActive == true
                );

                var contentItemsList = activeContentItems.ToList();
                var preview = await _contentPublicationService.PreviewBatchUnpublishAsync(contentItemsList.Select(r => r.Id));
                result.TotalProcessed = preview.RequestedCount;

                if (result.TotalProcessed == 0)
                {
                    result.Summary = "ไม่มี ContentItem ที่ SetPublic แล้ว";
                    return Ok(result);
                }

                if (!confirmDelete)
                {
                    return Ok(new
                    {
                        warning = "⚠️ คำเตือน: คุณกำลังจะลบ ContentItem ที่ SetPublic แล้ว",
                        totalContentItems = result.TotalProcessed,
                        eligibleCount = preview.EligibleCount,
                        blockedCount = preview.BlockedCount,
                        breakdown = new
                        {
                            eligibleToUnpublish = preview.EligibleCount,
                            blockedByCourseUsage = preview.BlockedCount
                        },
                        message = "⚠️ กรุณาเพิ่ม ?confirmDelete=true เพื่อยืนยันการลบ",
                        note = "รายการที่ยังถูกใช้โดย course versions จะถูกข้ามตาม shared content publication policy",
                        contentItems = preview.Items.Select(item => new
                        {
                            contentItemId = item.ContentItemId,
                            item.Name,
                            item.CanUnpublish,
                            item.BlockingReason,
                            item.LinkedCourseCodes
                        }).ToList()
                    });
                }

                _logger.LogWarning($"🚨 Starting Bulk Delete for {result.TotalProcessed} published contentItems");
                operationId = _maintenanceStatusService.BeginOperation(
                    "Unpublish All Published Content",
                    preview.EligibleCount,
                    User.Identity?.Name ?? "SYSTEM");
                _maintenanceStatusService.UpdateOperation(operationId, "Starting unpublish all", currentItem: 0, successCount: 0, failureCount: 0);

                foreach (var blockedItem in preview.Items.Where(item => !item.CanUnpublish))
                {
                    result.FailureCount++;
                    result.Results.Add(new BulkOperationItemDto
                    {
                        ContentItemId = blockedItem.ContentItemId,
                        ContentItemName = blockedItem.Name,
                        Success = false,
                        ErrorMessage = blockedItem.BlockingReason,
                        Details = blockedItem.LinkedCourseCodes.Count == 0
                            ? null
                            : $"Linked courses: {string.Join(", ", blockedItem.LinkedCourseCodes)}"
                    });
                }

                var currentItem = 0;
                foreach (var contentItemId in preview.EligibleIds)
                {
                    currentItem++;
                    var contentItem = contentItemsList.First(r => r.Id == contentItemId);
                    var itemResult = new BulkOperationItemDto
                    {
                        ContentItemId = contentItem.Id,
                        ContentItemName = contentItem.Name
                    };

                    try
                    {
                        _maintenanceStatusService.UpdateOperation(operationId, "Applying shared publication policy", contentItem.Name, currentItem, result.SuccessCount, result.FailureCount);
                        await _contentPublicationService.UnpublishAsync(contentItemId);

                        itemResult.Success = true;
                        itemResult.Details = "Unpublished through shared content publication policy";
                        result.SuccessCount++;
                        result.Results.Add(itemResult);
                        _maintenanceStatusService.UpdateOperation(operationId, "Updating content item to draft", contentItem.Name, currentItem, result.SuccessCount, result.FailureCount);

                        _logger.LogInformation($"✅ [{result.SuccessCount}/{result.TotalProcessed}] {contentItem.Name} - {itemResult.Details}");
                    }
                    catch (Exception ex)
                    {
                        itemResult.Success = false;
                        itemResult.ErrorMessage = ex.Message;
                        result.FailureCount++;
                        result.Results.Add(itemResult);
                        _maintenanceStatusService.UpdateOperation(operationId, "Unexpected error", contentItem.Name, currentItem, result.SuccessCount, result.FailureCount);

                        _logger.LogError(ex, $"❌ [{result.SuccessCount + result.FailureCount}/{result.TotalProcessed}] Error deleting {contentItem.Name}");
                    }
                }

                if (result.SuccessCount > 0)
                {
                    ContentItemStatsCache.Invalidate(_cache);
                }

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Summary = $"✅ ลบสำเร็จ {result.SuccessCount}/{result.TotalProcessed} รายการ " +
                                $"(❌ blocked/failed {result.FailureCount}) ⏱️ ใช้เวลา {result.Duration.TotalSeconds:F2} วินาที";
                if (operationId != Guid.Empty)
                    _maintenanceStatusService.CompleteOperation(operationId, result.FailureCount == 0, "Unpublish all completed", result.SuccessCount, result.FailureCount);
                await _adminActivityService.LogAsync(
                    actionType: "UnpublishAllContentItems",
                    entityType: nameof(ContentItem),
                    entityId: null,
                    title: "Completed unpublish all published content",
                    description: $"Unpublished {result.SuccessCount} content item(s) with {result.FailureCount} failure(s).");

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