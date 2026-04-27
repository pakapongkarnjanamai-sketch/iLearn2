using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
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
    internal sealed record ResourceFolderStats(int FileCount, long TotalSize);
    internal sealed record ResourceSummaryStats(
        int TotalCount,
        int PublishedCount,
        int DraftCount,
        long TotalDbSize,
        int TotalServerFiles,
        long TotalServerSize);

    public class ResourcesCRUDController : GenericController<Resource>
    {
        private readonly IGenericRepository<CourseResource> _courseResourceRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IScormService _scormService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ResourcesCRUDController> _logger;

        public ResourcesCRUDController(
            IGenericRepository<Resource> repository,
            ICurrentUserService currentUser,
            IGenericRepository<CourseResource> courseResourceRepo,
            IGenericRepository<Course> courseRepo,
            IGenericRepository<FileStorage> fileRepo,
            IScormService scormService,
            IMemoryCache cache,
            ILogger<ResourcesCRUDController> logger) : base(repository, currentUser)
        {
            _courseResourceRepo = courseResourceRepo;
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

        private async Task<IActionResult> GetFiltered(DataSourceLoadOptions loadOptions, int? courseId)
        {
            var baseQuery = _repository.GetQuery().AsQueryable();

            if (courseId.HasValue)
                baseQuery = baseQuery.Where(r => r.CourseResources.Any(cr => cr.CourseVersion.CourseId == courseId.Value));

            var query = baseQuery.Select(r => new
            {
                r.Id,
                r.Name,
                r.TypeId,
                r.IsActive,
                r.URL,
                r.FileStorageId,
                r.CreatedAt,
                fileLength = r.FileStorage != null ? r.FileStorage.Length : 0,
                courseResources = r.CourseResources.Select(cr => new
                {
                    courseId = cr.CourseVersion.CourseId
                }).ToList(),
                courseIdsCount = r.CourseResources.Select(cr => cr.CourseVersion.CourseId).Distinct().Count()
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

        private async Task<Dictionary<int, ResourceFolderStats>> GetPublishedFolderStatsAsync(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(ResourceStatsCache.FolderStatsKey, out Dictionary<int, ResourceFolderStats>? cachedStats) && cachedStats != null)
            {
                return cachedStats;
            }

            var publishedResources = await _repository.GetQuery()
                .Where(r => r.IsActive && !string.IsNullOrEmpty(r.URL))
                .Select(r => new { r.Id, r.URL })
                .ToListAsync(cancellationToken);

            var folderStats = new Dictionary<int, ResourceFolderStats>(publishedResources.Count);
            foreach (var resource in publishedResources)
            {
                var info = _scormService.GetFolderInfo(resource.URL!);
                folderStats[resource.Id] = new ResourceFolderStats(info.FileCount, info.TotalSize);
            }

            _cache.Set(ResourceStatsCache.FolderStatsKey, folderStats, ResourceStatsCache.FolderOptions);

            return folderStats;
        }

        private async Task<Dictionary<int, ResourceFolderStats>> GetCachedServerStatsAsync(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(ResourceStatsCache.ServerStatsKey, out Dictionary<int, ResourceFolderStats>? cachedStats) && cachedStats != null)
            {
                return cachedStats;
            }

            var folderStats = await GetPublishedFolderStatsAsync(cancellationToken);
            var serverStats = new Dictionary<int, ResourceFolderStats>(folderStats);
            _cache.Set(ResourceStatsCache.ServerStatsKey, serverStats, ResourceStatsCache.ServerOptions);
            return serverStats;
        }

        private async Task<ResourceSummaryStats> GetCachedSummaryStatsAsync(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(ResourceStatsCache.SummaryStatsKey, out ResourceSummaryStats cachedSummary))
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

            var summary = new ResourceSummaryStats(
                totalCount,
                publishedCount,
                draftCount,
                totalDbSize,
                totalServerFiles,
                totalServerSize);

            _cache.Set(ResourceStatsCache.SummaryStatsKey, summary, ResourceStatsCache.SummaryOptions);

            return summary;
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var resource = await _repository.GetQuery()
                                .Include(r => r.CourseResources)
                                    .ThenInclude(cr => cr.CourseVersion)
                                .FirstOrDefaultAsync(r => r.Id == key);

            if (resource == null) return NotFound();

            JsonConvert.PopulateObject(values, resource);

            var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);
            if (valuesDict.ContainsKey("CourseIds"))
            {
                var courseIdsJson      = valuesDict["CourseIds"].ToString();
                var selectedCourseIds  = JsonConvert.DeserializeObject<List<int>>(courseIdsJson) ?? new List<int>();
                var currentLinks       = resource.CourseResources.ToList();

                foreach (var link in currentLinks)
                {
                    if (link.CourseVersion != null && !selectedCourseIds.Contains(link.CourseVersion.CourseId))
                        await _courseResourceRepo.DeleteAsync(link);
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
                                await _courseResourceRepo.AddAsync(new CourseResource
                                {
                                    ResourceId      = key,
                                    CourseVersionId = latestVersion.Id
                                });
                            }
                        }
                    }
                }
            }

            await _repository.UpdateAsync(resource);
            ResourceStatsCache.Invalidate(_cache);
            return Ok(resource);
        }

        [HttpDelete("Delete")]
        public override async Task<IActionResult> Delete([FromForm] int key)
        {
            var resource = await _repository.GetByIdAsync(key);
            if (resource == null) return NotFound();

            try
            {
                if (resource.IsActive && !string.IsNullOrEmpty(resource.URL) && resource.URL.StartsWith("scorm/"))
                {
                    var parts = resource.URL.Split('/');
                    if (parts.Length >= 2)
                        _scormService.DeleteScormFolder(parts[1]);
                }

                if (resource.FileStorageId.HasValue)
                {
                    var file = await _fileRepo.GetByIdAsync(resource.FileStorageId.Value);
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
                    "ResourcesCRUDController.Delete: cleanup failed for resource {ResourceId} ({ResourceName})",
                    resource.Id,
                    resource.Name);
            }

            await _repository.DeleteAsync(resource);
            return Ok();
        }
    }
}
