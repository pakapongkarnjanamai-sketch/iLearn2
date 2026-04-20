using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.API.Services;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    public class CategoriesCRUDController : GenericController<Category>
    {
        private readonly IAdminActivityService _adminActivityService;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;

        public CategoriesCRUDController(
            IGenericRepository<Category> repository,
            ICurrentUserService currentUser,
            IAdminActivityService adminActivityService,
            IGenericRepository<Course> courseRepo,
            IGenericRepository<Enrollment> enrollmentRepo) : base(repository, currentUser)
        {
            _adminActivityService = adminActivityService;
            _courseRepo = courseRepo;
            _enrollmentRepo = enrollmentRepo;
        }

        [HttpGet("Get/{id}")]
        public override async Task<IActionResult> Get(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return NotFound();

            if (_currentUser.DivisionId.HasValue && entity.DivisionId != _currentUser.DivisionId.Value)
                return NotFound();

            return Ok(entity);
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            IQueryable<Category> query = _repository.GetQuery().Include(c => c.Division);

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.DivisionId == _currentUser.DivisionId.Value);

            // Load course counts per category
            var courseCounts = await _courseRepo.GetQuery()
                .GroupBy(c => c.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

            var projected = query.Select(c => new
            {
                c.Id,
                c.Name,
                c.DivisionId,
                divisionName = c.Division != null ? c.Division.Name : null,
                c.IsActive,
                c.CreatedAt
            });

            var loadResult = DataSourceLoader.Load(projected, loadOptions);

            if (loadResult.data is IEnumerable<object> items)
            {
                var enriched = items.Cast<dynamic>().Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.DivisionId,
                    c.divisionName,
                    c.IsActive,
                    c.CreatedAt,
                    courseCount = courseCounts.GetValueOrDefault((int)c.Id, 0)
                }).ToList();

                return Ok(new
                {
                    loadResult.totalCount,
                    loadResult.groupCount,
                    loadResult.summary,
                    data = enriched
                });
            }

            return Ok(loadResult);
        }

        [HttpGet("GetPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationParams p)
        {
            IQueryable<Category> query = _repository.GetQuery()
                .Include(c => c.Division)
                .AsNoTracking();

            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.DivisionId == _currentUser.DivisionId.Value);

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var term = p.Search.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    (c.Division != null && c.Division.Name.ToLower().Contains(term)));
            }

            query = (p.SortBy?.ToLower(), p.SortDescending) switch
            {
                ("name",     true)  => query.OrderByDescending(c => c.Name),
                ("name",     false) => query.OrderBy(c => c.Name),
                ("isactive", true)  => query.OrderByDescending(c => c.IsActive),
                ("isactive", false) => query.OrderBy(c => c.IsActive),
                (_,          false) => query.OrderBy(c => c.Id),
                _                   => query.OrderByDescending(c => c.Id),
            };

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, p.Page);
            var pageSize = Math.Clamp(p.PageSize, 1, 100);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.DivisionId,
                    divisionName = c.Division != null ? c.Division.Name : null,
                    c.IsActive,
                    courseCount = c.Courses.Count()
                })
                .ToListAsync();

            return Ok(new { totalCount, data = items });
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats()
        {
            IQueryable<Category> query = _repository.GetQuery();

            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.DivisionId == _currentUser.DivisionId.Value);

            var categories = await query
                .Select(c => new { c.Id, c.IsActive, courseCount = c.Courses.Count() })
                .ToListAsync();

            var totalCategories = categories.Count;
            var activeCategories = categories.Count(c => c.IsActive);
            var totalCourses = categories.Sum(c => c.courseCount);
            var unusedCategories = categories.Count(c => c.courseCount == 0);

            return Ok(new
            {
                totalCategories,
                activeCategories,
                totalCourses,
                unusedCategories
            });
        }

        [HttpGet("GetDashboard/{id}")]
        public async Task<IActionResult> GetDashboard(int id)
        {
            var category = await _repository.GetQuery()
                .Include(c => c.Division)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            if (_currentUser.DivisionId.HasValue && category.DivisionId != _currentUser.DivisionId.Value)
                return NotFound();

            // Courses in this category
            var courses = await _courseRepo.GetQuery()
                .Where(c => c.CategoryId == id)
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.Title,
                    c.IsActive,
                    courseTypeName = c.CourseType != null ? c.CourseType.Name : null,
                    c.CreatedAt
                })
                .ToListAsync();

            var courseIds = courses.Select(c => c.Id).ToList();

            // Enrollment stats for courses in this category
            var enrollments = await _enrollmentRepo.GetQuery()
                .Where(e => e.CourseId.HasValue && courseIds.Contains(e.CourseId.Value))
                .Select(e => new
                {
                    e.CourseId,
                    e.StudentCode,
                    e.IsCompleted,
                    e.Progress
                })
                .ToListAsync();

            var totalEnrollments = enrollments.Count;
            var completedEnrollments = enrollments.Count(e => e.IsCompleted);
            var inProgressEnrollments = enrollments.Count(e => !e.IsCompleted && e.Progress > 0);
            var notStartedEnrollments = enrollments.Count(e => !e.IsCompleted && e.Progress == 0);
            var uniqueStudents = enrollments.Select(e => e.StudentCode).Distinct().Count();
            var avgProgress = totalEnrollments > 0
                ? Math.Round(enrollments.Average(e => e.Progress), 1)
                : 0;

            // Enrollment counts per course
            var enrollmentCountsByCourse = enrollments
                .GroupBy(e => e.CourseId)
                .ToDictionary(g => g.Key ?? 0, g => new
                {
                    total = g.Count(),
                    completed = g.Count(e => e.IsCompleted)
                });

            var coursesWithStats = courses.Select(c => new
            {
                c.Id,
                c.Code,
                c.Title,
                c.IsActive,
                c.courseTypeName,
                c.CreatedAt,
                enrollmentCount = enrollmentCountsByCourse.GetValueOrDefault(c.Id)?.total ?? 0,
                completedCount = enrollmentCountsByCourse.GetValueOrDefault(c.Id)?.completed ?? 0
            }).ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    category = new
                    {
                        category.Id,
                        category.Name,
                        category.IsActive,
                        category.DivisionId,
                        divisionName = category.Division?.Name,
                        category.CreatedAt,
                        category.CreatedBy
                    },
                    courses = coursesWithStats,
                    stats = new
                    {
                        totalCourses = courses.Count,
                        activeCourses = courses.Count(c => c.IsActive),
                        totalEnrollments,
                        completedEnrollments,
                        inProgressEnrollments,
                        notStartedEnrollments,
                        uniqueStudents,
                        avgProgress
                    }
                }
            });
        }

        [HttpPost("Post")]
        public override async Task<IActionResult> Post([FromForm] string values)
        {
            var newEntity = new Category();
            JsonConvert.PopulateObject(values, newEntity);

            if (_currentUser.DivisionId.HasValue)
                newEntity.DivisionId = _currentUser.DivisionId.Value;

            if (!TryValidateModel(newEntity))
                return BadRequest(ModelState);

            await _repository.AddAsync(newEntity);
            await _adminActivityService.LogAsync(
                actionType: "CreateCategory",
                entityType: nameof(Category),
                entityId: newEntity.Id,
                title: $"Created category {newEntity.Name}",
                description: $"Created category '{newEntity.Name}'.",
                divisionId: newEntity.DivisionId);
            return Ok(newEntity);
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var entity = await _repository.GetByIdAsync(key);
            if (entity == null)
                return NotFound();

            if (_currentUser.DivisionId.HasValue && entity.DivisionId != _currentUser.DivisionId.Value)
                return NotFound();

            var originalName = entity.Name;
            JsonConvert.PopulateObject(values, entity);

            if (_currentUser.DivisionId.HasValue)
                entity.DivisionId = _currentUser.DivisionId.Value;

            if (!TryValidateModel(entity))
                return BadRequest(ModelState);

            await _repository.UpdateAsync(entity);
            await _adminActivityService.LogAsync(
                actionType: "UpdateCategory",
                entityType: nameof(Category),
                entityId: entity.Id,
                title: $"Updated category {entity.Name}",
                description: originalName == entity.Name
                    ? $"Updated category '{entity.Name}'."
                    : $"Renamed category from '{originalName}' to '{entity.Name}'.",
                divisionId: entity.DivisionId);
            return Ok(entity);
        }

        [HttpDelete("Delete")]
        public override async Task<IActionResult> Delete([FromForm] int key)
        {
            var entity = await _repository.GetByIdAsync(key);
            if (entity == null)
                return NotFound();

            if (_currentUser.DivisionId.HasValue && entity.DivisionId != _currentUser.DivisionId.Value)
                return NotFound();

            await _repository.DeleteAsync(entity);
            return Ok();
        }
    }

    public class AssignmentsCRUDController : GenericController<Assignment>
    {
        public AssignmentsCRUDController(
            IGenericRepository<Assignment> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        // -- ?????????????????????? --
        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery().AsQueryable();

            // ?????????????????????? Division ????????? (????? DivisionId)
            if (_currentUser.DivisionId.HasValue)
            {
                query = query.Where(a => a.DivisionId == _currentUser.DivisionId.Value);
            }

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }
    }

    public class CoursesCRUDController : GenericController<Course>
    {
        public CoursesCRUDController(
            IGenericRepository<Course> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery()
                .AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.Title,
                    c.IsActive,
                    c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    DivisionId = c.Category != null ? c.Category.DivisionId : null,
                    c.CourseTypeId,
                    CourseTypeName = c.CourseType != null ? c.CourseType.Name : null
                })
                .AsQueryable();

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.DivisionId == _currentUser.DivisionId.Value);

            return Ok(await DataSourceLoader.LoadAsync(query, loadOptions));
        }

        [HttpGet("GetForLookup")]
        public async Task<IActionResult> GetForLookup(DataSourceLoadOptions loadOptions)
        {
            var query = _repository.GetQuery().AsQueryable();

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value);

            return Ok(DataSourceLoader.Load(query.Select(c => new { c.Id, c.Code }), loadOptions));
        }

        [HttpGet("GetActive")]
        public async Task<IActionResult> GetActive(DataSourceLoadOptions loadOptions)
        {
            IQueryable<Course> query = _repository.GetQuery()
                .Include(c => c.Category).ThenInclude(cat => cat.Division)
                .Include(c => c.CourseType)
                .Include(c => c.Versions)
                .Where(c => c.IsActive && c.Versions.Any(v => v.IsActive));

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value);

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public class CourseTypesCRUDController : GenericController<CourseType>
    {
        public CourseTypesCRUDController(
            IGenericRepository<CourseType> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery()
                .Select(ct => new
                {
                    ct.Id,
                    ct.Name,
                    ct.Description,
                    ct.IsActive,
                    courseCount = ct.Courses.Count(),
                    ct.CreatedAt
                });

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats()
        {
            var all = await _repository.GetQuery()
                .Select(ct => new
                {
                    ct.Id,
                    courseCount = ct.Courses.Count()
                })
                .ToListAsync();

            var totalTypes = all.Count;
            var totalCourses = all.Sum(ct => ct.courseCount);

            return Ok(new
            {
                totalTypes,
                totalCourses,
                avgCoursesPerType = totalTypes > 0
                    ? Math.Round((double)totalCourses / totalTypes, 1)
                    : 0,
                unusedTypes = all.Count(ct => ct.courseCount == 0)
            });
        }
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public class DivisionsCRUDController : GenericController<Division>
    {
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly IGenericRepository<Role> _roleRepo;

        public DivisionsCRUDController(
            IGenericRepository<Division> repository,
            ICurrentUserService currentUser,
            IGenericRepository<Category> categoryRepo,
            IGenericRepository<Role> roleRepo) : base(repository, currentUser)
        {
            _categoryRepo = categoryRepo;
            _roleRepo = roleRepo;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery().AsQueryable();

            // -- Data Isolation --
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(d => d.Id == _currentUser.DivisionId.Value);

            // Load counts per division in two small queries
            var categoryCounts = await _categoryRepo.GetQuery()
                .Where(c => c.DivisionId != null)
                .GroupBy(c => c.DivisionId!.Value)
                .Select(g => new { DivisionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DivisionId, x => x.Count);

            var roleCounts = await _roleRepo.GetQuery()
                .Where(r => r.DivisionId != null)
                .GroupBy(r => r.DivisionId!.Value)
                .Select(g => new { DivisionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DivisionId, x => x.Count);

            // Project flat fields for DataSourceLoader (server-side paging/sorting)
            var projected = query.Select(d => new
            {
                d.Id,
                d.Name,
                d.IsActive,
                d.CreatedAt
            });

            var loadResult = DataSourceLoader.Load(projected, loadOptions);

            // Enrich with counts in memory
            if (loadResult.data is IEnumerable<object> items)
            {
                var enriched = items.Cast<dynamic>().Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.IsActive,
                    d.CreatedAt,
                    categoryCount = categoryCounts.GetValueOrDefault((int)d.Id, 0),
                    roleCount = roleCounts.GetValueOrDefault((int)d.Id, 0)
                }).ToList();

                return Ok(new
                {
                    loadResult.totalCount,
                    loadResult.groupCount,
                    loadResult.summary,
                    data = enriched
                });
            }

            return Ok(loadResult);
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats()
        {
            var totalDivisions = await _repository.CountAsync();
            var totalCategories = await _categoryRepo.CountAsync();
            var totalRoles = await _roleRepo.CountAsync();

            var usedByCategoryIds = await _categoryRepo.GetQuery()
                .Where(c => c.DivisionId != null)
                .Select(c => c.DivisionId!.Value)
                .Distinct()
                .ToListAsync();
            var usedByRoleIds = await _roleRepo.GetQuery()
                .Where(r => r.DivisionId != null)
                .Select(r => r.DivisionId!.Value)
                .Distinct()
                .ToListAsync();
            var usedIds = usedByCategoryIds.Union(usedByRoleIds).ToHashSet();
            var unusedDivisions = await _repository.CountAsync(d => !usedIds.Contains(d.Id));

            return Ok(new
            {
                totalDivisions,
                totalCategories,
                totalRoles,
                unusedDivisions
            });
        }
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public class EnrollmentsCRUDController : GenericController<Enrollment>
    {
        public EnrollmentsCRUDController(
            IGenericRepository<Enrollment> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery()
                .Include(e => e.Course)
                .Select(e => new
                {
                    e.Id,
                    e.StudentCode,
                    e.CourseId,
                    courseCode = e.Course != null ? e.Course.Code : "",
                    courseTitle = e.Course != null ? e.Course.Title : "",
                    e.IsCompleted,
                    e.Progress,
                    e.TotalScore,
                    e.TotalTimeSpent,
                    e.StartDate,
                    e.DueDate,
                    e.CompletedDate,
                    e.ResetAt,
                    e.CreatedAt
                });

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats()
        {
            var all = await _repository.GetQuery()
                .Select(e => new { e.IsCompleted, e.Progress })
                .ToListAsync();

            return Ok(new
            {
                totalCount = all.Count,
                completedCount = all.Count(e => e.IsCompleted),
                inProgressCount = all.Count(e => !e.IsCompleted && e.Progress > 0),
                enrolledCount = all.Count(e => !e.IsCompleted && e.Progress == 0),
                avgProgress = all.Count > 0 ? Math.Round(all.Average(e => e.Progress), 1) : 0
            });
        }
    }

    public class FileStoragesCRUDController : GenericController<FileStorage>
    {
        public FileStoragesCRUDController(
            IGenericRepository<FileStorage> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public class LearningLogsCRUDController : GenericController<LearningLog>
    {
        private readonly IGenericRepository<Resource> _resourceRepo;

        public LearningLogsCRUDController(
            IGenericRepository<LearningLog> repository,
            ICurrentUserService currentUser,
            IGenericRepository<Resource> resourceRepo) : base(repository, currentUser)
        {
            _resourceRepo = resourceRepo;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            var query = _repository.GetQuery()
                .Include(l => l.Enrollment)
                    .ThenInclude(e => e!.Course)
                .Select(l => new
                {
                    l.Id,
                    l.StudentCode,
                    l.EnrollmentId,
                    l.ResourceId,
                    l.CourseVersionId,
                    courseCode = l.Enrollment != null && l.Enrollment.Course != null ? l.Enrollment.Course.Code : "",
                    courseTitle = l.Enrollment != null && l.Enrollment.Course != null ? l.Enrollment.Course.Title : "",
                    l.Status,
                    l.Progress,
                    l.Score,
                    l.TotalSecondsPlayed,
                    l.AttemptCount,
                    l.SessionTime,
                    l.CreatedAt,
                    l.UpdatedAt
                });

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats()
        {
            var all = await _repository.GetQuery()
                .Select(l => new { l.Status, l.Score, l.TotalSecondsPlayed })
                .ToListAsync();

            var statusLower = all.Select(l => l.Status?.ToLower() ?? "").ToList();

            return Ok(new
            {
                totalLogs = all.Count,
                completedCount = statusLower.Count(s => s == "completed" || s == "passed"),
                failedCount = statusLower.Count(s => s == "failed"),
                inProgressCount = statusLower.Count(s => s == "incomplete" || s == "in_progress"),
                avgScore = all.Where(l => l.Score.HasValue).Any()
                    ? Math.Round(all.Where(l => l.Score.HasValue).Average(l => l.Score!.Value), 1)
                    : 0,
                totalTimeSpent = all.Sum(l => l.TotalSecondsPlayed)
            });
        }
    }

    public class ResourcesCRUDController : GenericController<Resource>
    {
        private readonly IGenericRepository<CourseResource> _courseResourceRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IScormService _scormService;
        private readonly IMemoryCache _cache;

        public ResourcesCRUDController(
            IGenericRepository<Resource> repository,
            ICurrentUserService currentUser,
            IGenericRepository<CourseResource> courseResourceRepo,
            IGenericRepository<Course> courseRepo,
            IGenericRepository<FileStorage> fileRepo,
            IScormService scormService,
            IMemoryCache cache) : base(repository, currentUser)
        {
            _courseResourceRepo = courseResourceRepo;
            _courseRepo         = courseRepo;
            _fileRepo           = fileRepo;
            _scormService       = scormService;
            _cache              = cache;
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
        public async Task<IActionResult> GetServerStats()
        {
            if (_cache.TryGetValue(ResourceStatsCache.ServerStatsKey, out object? cachedStats) && cachedStats != null)
                return Ok(cachedStats);

            var publishedResources = await _repository.GetQuery()
                .Where(r => r.IsActive && r.URL != null)
                .Select(r => new { r.Id, r.URL })
                .ToListAsync();

            var stats = publishedResources.Select(r =>
            {
                var info = _scormService.GetFolderInfo(r.URL!);
                return new { r.Id, info.FileCount, info.TotalSize };
            }).ToDictionary(x => x.Id, x => new { x.FileCount, x.TotalSize });

            _cache.Set(ResourceStatsCache.ServerStatsKey, stats, ResourceStatsCache.DefaultOptions);

            return Ok(stats);
        }

        [HttpGet("GetSummaryStats")]
        public async Task<IActionResult> GetSummaryStats()
        {
            if (_cache.TryGetValue(ResourceStatsCache.SummaryStatsKey, out object? cachedSummary) && cachedSummary != null)
                return Ok(cachedSummary);

            var allResources = await _repository.GetQuery()
                .Include(r => r.FileStorage)
                .Select(r => new
                {
                    r.Id,
                    r.IsActive,
                    r.URL,
                    dbSize = r.FileStorage != null ? r.FileStorage.Length : 0
                })
                .ToListAsync();

            int totalCount = allResources.Count;
            int publishedCount = allResources.Count(r => r.IsActive);
            int draftCount = totalCount - publishedCount;
            long totalDbSize = allResources.Sum(r => r.dbSize);

            long totalServerSize = 0;
            int totalServerFiles = 0;
            foreach (var r in allResources.Where(r => r.IsActive && !string.IsNullOrEmpty(r.URL)))
            {
                var info = _scormService.GetFolderInfo(r.URL!);
                totalServerFiles += info.FileCount;
                totalServerSize += info.TotalSize;
            }

            var summary = new
            {
                totalCount,
                publishedCount,
                draftCount,
                totalDbSize,
                totalServerFiles,
                totalServerSize
            };

            _cache.Set(ResourceStatsCache.SummaryStatsKey, summary, ResourceStatsCache.DefaultOptions);

            return Ok(summary);
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
            catch (Exception) { }

            await _repository.DeleteAsync(resource);
            return Ok();
        }
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public class RolesCRUDController : GenericController<Role>
    {
        public RolesCRUDController(
            IGenericRepository<Role> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public class UsersCRUDController : GenericController<User>
    {
        private readonly IGenericRepository<UserRole> _userRoleRepo;
        private readonly IStudentApiService _studentApiService;

        public UsersCRUDController(
            IGenericRepository<User> repository,
            ICurrentUserService currentUser,
            IGenericRepository<UserRole> userRoleRepo,
            IStudentApiService studentApiService) : base(repository, currentUser)
        {
            _userRoleRepo = userRoleRepo;
            _studentApiService = studentApiService;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            IQueryable<User> query = _repository.GetQuery()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

            // -- Data Isolation: Admin ????????? User ?? Division ?????? --
            if (_currentUser.DivisionId.HasValue)
            {
                var myDivId = _currentUser.DivisionId.Value;
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.DivisionId == myDivId));
            }

            var projected = query.Select(u => new
            {
                u.Id,
                u.Nid,
                u.LastLogin,
                u.CreatedAt,
                u.IsActive,
                UserRoles = u.UserRoles.Select(ur => new
                {
                    ur.UserId,
                    ur.RoleId,
                    Role = ur.Role == null ? null : new
                    {
                        ur.Role.Id,
                        ur.Role.Name,
                        ur.Role.RoleType,
                        ur.Role.DivisionId
                    },
                    ur.Id,
                    ur.IsActive,
                    ur.CreatedAt,
                    ur.UpdatedAt,
                    ur.CreatedBy,
                    ur.UpdatedBy,
                    ur.IsDeleted,
                    ur.DeletedAt,
                    ur.DeletedBy
                }).ToList()
            });

            var loadResult = DataSourceLoader.Load(projected, loadOptions);

            if (loadResult.data is not IEnumerable<object> items)
                return Ok(loadResult);

            var rows = items.Cast<dynamic>().ToList();
            var employeeLookup = await _studentApiService.GetEmployeesByNidsAsync(
                rows.Select(r => (string?)r.Nid ?? string.Empty));

            var enriched = rows.Select(r =>
            {
                employeeLookup.TryGetValue((string?)r.Nid ?? string.Empty, out var employee);

                return new
                {
                    r.Id,
                    r.Nid,
                    r.LastLogin,
                    r.CreatedAt,
                    r.IsActive,
                    r.UserRoles,
                    EmployeeId = employee?.EId ?? string.Empty,
                    FullName = employee?.FullName ?? string.Empty,
                    Email = employee?.Email ?? string.Empty,
                    Division = employee?.Division ?? string.Empty,
                    Department = employee?.Department ?? string.Empty,
                    Section = employee?.Section ?? string.Empty,
                    Position = employee?.Position ?? string.Empty
                };
            }).ToList();

            return Ok(new
            {
                loadResult.totalCount,
                loadResult.groupCount,
                loadResult.summary,
                data = enriched
            });
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var user = await _repository.GetByIdAsync(key);
            if (user == null) return NotFound();

            JsonConvert.PopulateObject(values, user);

            var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);
            var roleKey    = valuesDict.Keys.FirstOrDefault(k => k.Equals("roleIds", StringComparison.OrdinalIgnoreCase));

            if (roleKey != null)
            {
                var newRoleIds        = JsonConvert.DeserializeObject<List<int>>(valuesDict[roleKey].ToString()) ?? new List<int>();
                var existingUserRoles = (await _userRoleRepo.GetAsync(ur => ur.UserId == key)).ToList();

                foreach (var ur in existingUserRoles)
                {
                    if (!newRoleIds.Contains(ur.RoleId))
                        await _userRoleRepo.DeleteAsync(ur);
                }

                foreach (var roleId in newRoleIds)
                {
                    if (!existingUserRoles.Any(ur => ur.RoleId == roleId))
                        await _userRoleRepo.AddAsync(new UserRole { UserId = key, RoleId = roleId });
                }
            }

            await _repository.UpdateAsync(user);
            return Ok(user);
        }
    }

    public class UserRolesCRUDController : GenericController<UserRole>
    {
        public UserRolesCRUDController(
            IGenericRepository<UserRole> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }
    }

    public class CourseVersionsCRUDController : GenericController<CourseVersion>
    {
        public CourseVersionsCRUDController(
            IGenericRepository<CourseVersion> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get/{id}")]
        public override async Task<IActionResult> Get(int id)
        {
            var entity = await _repository.GetQuery()
                .Include(c => c.Course).ThenInclude(ca => ca.Category)
                .Include(cr => cr.CourseResources).ThenInclude(c => c.Resource)
                .Where(i => i.Id == id).ToListAsync();

            if (entity == null) return NotFound();
            return Ok(entity);
        }
    }

    public class CourseResourcesCRUDController : GenericController<CourseResource>
    {
        public CourseResourcesCRUDController(
            IGenericRepository<CourseResource> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            var query = _repository.GetQuery().Include(c => c.Resource);
            return Ok(DataSourceLoader.Load(query, loadOptions));
        }
    }
}

