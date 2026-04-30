using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    internal sealed record ContentItemFolderStats(int FileCount, long TotalSize);
    internal sealed record ContentItemSummaryStats(
        int TotalCount,
        int PublishedCount,
        int DraftCount,
        long TotalDbSize,
        int TotalServerFiles,
        long TotalServerSize);

    public class ContentItemsCRUDController : GenericController<ContentItem>
    {
        private readonly IGenericRepository<CourseContentItem> _courseContentItemRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IScormService _scormService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ContentItemsCRUDController> _logger;

        public ContentItemsCRUDController(
            IGenericRepository<ContentItem> repository,
            ICurrentUserService currentUser,
            IGenericRepository<CourseContentItem> courseContentItemRepo,
            IGenericRepository<Course> courseRepo,
            IGenericRepository<FileStorage> fileRepo,
            IScormService scormService,
            IMemoryCache cache,
            ILogger<ContentItemsCRUDController> logger) : base(repository, currentUser)
        {
            _courseContentItemRepo = courseContentItemRepo;
            _courseRepo         = courseRepo;
            _fileRepo           = fileRepo;
            _scormService       = scormService;
            _cache              = cache;
            _logger             = logger;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            return await GetFiltered(loadOptions, courseId: null);
        }

        [HttpGet("GetByCourse")]
        public async Task<IActionResult> GetByCourse(DataSourceLoadOptions loadOptions, [FromQuery] int courseId)
        {
            return await GetFiltered(loadOptions, courseId);
        }

        [HttpGet("Get/{id}")]
        public override async Task<IActionResult> Get(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            return Ok(entity.ToDto());
        }

        private async Task<IActionResult> GetFiltered(DataSourceLoadOptions loadOptions, int? courseId)
        {
            var baseQuery = _repository.GetQuery().AsQueryable();

            if (courseId.HasValue)
                baseQuery = baseQuery.Where(r => r.CourseContentItems.Any(cr => cr.CourseVersion.CourseId == courseId.Value));

            var query = baseQuery.Select(r => new
            {
                r.Id,
                r.Name,
                r.TypeId,
                r.IsActive,
                IsPublished = r.IsActive,
                PublishState = r.IsActive ? "Published" : "Unpublished",
                r.URL,
                r.FileStorageId,
                r.CreatedAt,
                fileLength = r.FileStorage != null ? r.FileStorage.Length : 0,
                courseContentItems = r.CourseContentItems.Select(cr => new
                {
                    courseId = cr.CourseVersion.CourseId
                }).ToList(),
                courseIdsCount = r.CourseContentItems.Select(cr => cr.CourseVersion.CourseId).Distinct().Count()
            });

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpGet("GetServerStats")]
        public async Task<IActionResult> GetServerStats(CancellationToken cancellationToken)
        {
            return Ok(await GetCachedServerStatsAsync(cancellationToken));
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats(CancellationToken cancellationToken)
        {
            return Ok(await GetCachedSummaryStatsAsync(cancellationToken));
        }

        [HttpGet("GetDashboardStats")]
        public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
        {
            var summary = await GetCachedSummaryStatsAsync(cancellationToken);
            var serverStats = await GetCachedServerStatsAsync(cancellationToken);
            return Ok(new { summary, serverStats });
        }

        private async Task<Dictionary<int, ContentItemFolderStats>> GetPublishedFolderStatsAsync(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(ContentItemStatsCache.FolderStatsKey, out Dictionary<int, ContentItemFolderStats>? cachedStats) && cachedStats != null)
            {
                return cachedStats;
            }

            var publishedContentItems = await _repository.GetQuery()
                .Where(r => r.IsActive && !string.IsNullOrEmpty(r.URL))
                .Select(r => new { r.Id, r.URL })
                .ToListAsync(cancellationToken);

            var folderStats = new Dictionary<int, ContentItemFolderStats>(publishedContentItems.Count);
            foreach (var contentItem in publishedContentItems)
            {
                var info = _scormService.GetFolderInfo(contentItem.URL!);
                folderStats[contentItem.Id] = new ContentItemFolderStats(info.FileCount, info.TotalSize);
            }

            _cache.Set(ContentItemStatsCache.FolderStatsKey, folderStats, ContentItemStatsCache.FolderOptions);

            return folderStats;
        }

        private async Task<Dictionary<int, ContentItemFolderStats>> GetCachedServerStatsAsync(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(ContentItemStatsCache.ServerStatsKey, out Dictionary<int, ContentItemFolderStats>? cachedStats) && cachedStats != null)
            {
                return cachedStats;
            }

            var folderStats = await GetPublishedFolderStatsAsync(cancellationToken);
            var serverStats = new Dictionary<int, ContentItemFolderStats>(folderStats);
            _cache.Set(ContentItemStatsCache.ServerStatsKey, serverStats, ContentItemStatsCache.ServerOptions);
            return serverStats;
        }

        private async Task<ContentItemSummaryStats> GetCachedSummaryStatsAsync(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(ContentItemStatsCache.SummaryStatsKey, out ContentItemSummaryStats cachedSummary))
            {
                return cachedSummary;
            }

            var dbAggregate = await _repository.GetQuery()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    totalCount = g.Count(),
                    publishedCount = g.Count(r => r.IsActive),
                    totalDbSize = g.Sum(r => (long?)((r.FileStorage != null ? r.FileStorage.Length : 0))) ?? 0
                })
                .FirstOrDefaultAsync(cancellationToken);

            int totalCount = dbAggregate?.totalCount ?? 0;
            int publishedCount = dbAggregate?.publishedCount ?? 0;
            int draftCount = totalCount - publishedCount;
            long totalDbSize = dbAggregate?.totalDbSize ?? 0;

            var folderStats = await GetPublishedFolderStatsAsync(cancellationToken);
            int totalServerFiles = folderStats.Values.Sum(s => s.FileCount);
            long totalServerSize = folderStats.Values.Sum(s => s.TotalSize);

            var summary = new ContentItemSummaryStats(
                totalCount,
                publishedCount,
                draftCount,
                totalDbSize,
                totalServerFiles,
                totalServerSize);

            _cache.Set(ContentItemStatsCache.SummaryStatsKey, summary, ContentItemStatsCache.SummaryOptions);

            return summary;
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var contentItem = await _repository.GetQuery()
                                .Include(r => r.CourseContentItems)
                                    .ThenInclude(cr => cr.CourseVersion)
                                .FirstOrDefaultAsync(r => r.Id == key);

            if (contentItem == null) return NotFound();

            JsonConvert.PopulateObject(values, contentItem);

            var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);
            if (valuesDict.ContainsKey("CourseIds"))
            {
                var courseIdsJson      = valuesDict["CourseIds"].ToString();
                var selectedCourseIds  = JsonConvert.DeserializeObject<List<int>>(courseIdsJson) ?? new List<int>();
                var currentLinks       = contentItem.CourseContentItems.ToList();

                foreach (var link in currentLinks)
                {
                    if (link.CourseVersion != null && !selectedCourseIds.Contains(link.CourseVersion.CourseId))
                        await _courseContentItemRepo.DeleteAsync(link);
                }

                foreach (var courseId in selectedCourseIds)
                {
                    bool alreadyLinked = currentLinks.Any(cr => cr.CourseVersion != null && cr.CourseVersion.CourseId == courseId);
                    if (!alreadyLinked)
                    {
                        var course = await _courseRepo.GetQuery()
                            .Include(c => c.Versions)
                            .FirstOrDefaultAsync(c => c.Id == courseId);

                        if (course != null && course.Versions.Any())
                        {
                            var latestVersion = course.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
                            if (latestVersion != null)
                            {
                                await _courseContentItemRepo.AddAsync(new CourseContentItem
                                {
                                    ContentItemId      = key,
                                    CourseVersionId = latestVersion.Id
                                });
                            }
                        }
                    }
                }
            }

            await _repository.UpdateAsync(contentItem);
            ContentItemStatsCache.Invalidate(_cache);
            return Ok(contentItem);
        }

        [HttpDelete("Delete")]
        public override async Task<IActionResult> Delete([FromForm] int key)
        {
            var contentItem = await _repository.GetByIdAsync(key);
            if (contentItem == null) return NotFound();

            try
            {
                if (contentItem.IsActive && !string.IsNullOrEmpty(contentItem.URL) && contentItem.URL.StartsWith("scorm/"))
                {
                    var parts = contentItem.URL.Split('/');
                    if (parts.Length >= 2)
                        _scormService.DeleteScormFolder(parts[1]);
                }

                if (contentItem.FileStorageId.HasValue)
                {
                    var file = await _fileRepo.GetByIdAsync(contentItem.FileStorageId.Value);
                    if (file != null)
                        await _fileRepo.HardDeleteAsync(file);
                }
            }
            catch (Exception ex)
            {
                // SCORM/file cleanup is best-effort. The DB row will still be deleted
                // below — log the failure so orphaned files can be reconciled later.
                _logger.LogWarning(
                    ex,
                    "ContentItemsCRUDController.Delete: cleanup failed for contentItem {ContentItemId} ({ContentItemName})",
                    contentItem.Id,
                    contentItem.Name);
            }

            await _repository.DeleteAsync(contentItem);
            return Ok();
        }
    }
}
