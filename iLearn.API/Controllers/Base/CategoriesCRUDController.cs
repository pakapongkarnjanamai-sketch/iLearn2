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
}
