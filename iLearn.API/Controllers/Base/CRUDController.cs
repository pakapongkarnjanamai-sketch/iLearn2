using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    public class CategoriesCRUDController : GenericController<Category>
    {
        public CategoriesCRUDController(
            IGenericRepository<Category> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            IQueryable<Category> query = _repository.GetQuery().Include(c => c.Division);

            // ── Data Isolation ──
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.DivisionId == _currentUser.DivisionId.Value);

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }
    }

    public class AssignmentsCRUDController : GenericController<Assignment>
    {
        public AssignmentsCRUDController(
            IGenericRepository<Assignment> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        // ── เพิ่มโค้ดส่วนนี้เข้าไป ──
        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            var query = _repository.GetQuery().AsQueryable();

            // กรองข้อมูลให้เห็นเฉพาะ Division ของตัวเอง (ถ้ามี DivisionId)
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
            IQueryable<Course> query = _repository.GetQuery()
                .Include(c => c.Category).ThenInclude(cat => cat.Division)
                .Include(c => c.CourseType);

            // ── Data Isolation ──
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value);

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpGet("GetForLookup")]
        public async Task<IActionResult> GetForLookup(DataSourceLoadOptions loadOptions)
        {
            var query = _repository.GetQuery().AsQueryable();

            // ── Data Isolation ──
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

            // ── Data Isolation ──
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.Category != null && c.Category.DivisionId == _currentUser.DivisionId.Value);

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }
    }

    public class CourseTypesCRUDController : GenericController<CourseType>
    {
        public CourseTypesCRUDController(
            IGenericRepository<CourseType> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }
    }

    public class DivisionsCRUDController : GenericController<Division>
    {
        public DivisionsCRUDController(
            IGenericRepository<Division> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            var query = _repository.GetQuery().AsQueryable();

            // ── Data Isolation ──
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(d => d.Id == _currentUser.DivisionId.Value);

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }
    }

    public class EnrollmentsCRUDController : GenericController<Enrollment>
    {
        public EnrollmentsCRUDController(
            IGenericRepository<Enrollment> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }
    }

    public class FileStoragesCRUDController : GenericController<FileStorage>
    {
        public FileStoragesCRUDController(
            IGenericRepository<FileStorage> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }
    }

    public class LearningLogsCRUDController : GenericController<LearningLog>
    {
        public LearningLogsCRUDController(
            IGenericRepository<LearningLog> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }
    }

    public class ResourcesCRUDController : GenericController<Resource>
    {
        private readonly IGenericRepository<CourseResource> _courseResourceRepo;
        private readonly IGenericRepository<Course> _courseRepo;
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IScormService _scormService;

        public ResourcesCRUDController(
            IGenericRepository<Resource> repository,
            ICurrentUserService currentUser,
            IGenericRepository<CourseResource> courseResourceRepo,
            IGenericRepository<Course> courseRepo,
            IGenericRepository<FileStorage> fileRepo,
            IScormService scormService) : base(repository, currentUser)
        {
            _courseResourceRepo = courseResourceRepo;
            _courseRepo         = courseRepo;
            _fileRepo           = fileRepo;
            _scormService       = scormService;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            var query = _repository.GetQuery()
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.TypeId,
                    r.IsActive,
                    r.URL,
                    r.FileStorageId,
                    r.CreatedAt,
                    courseResources = r.CourseResources.Select(cr => new
                    {
                        courseId = cr.CourseVersion.CourseId
                    }).ToList(),
                    courseIdsCount = r.CourseResources.Select(cr => cr.CourseVersion.CourseId).Distinct().Count()
                });

            return Ok(DataSourceLoader.Load(query, loadOptions));
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

    public class RolesCRUDController : GenericController<Role>
    {
        public RolesCRUDController(
            IGenericRepository<Role> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }
    }

    public class UsersCRUDController : GenericController<User>
    {
        private readonly IGenericRepository<UserRole> _userRoleRepo;

        public UsersCRUDController(
            IGenericRepository<User> repository,
            ICurrentUserService currentUser,
            IGenericRepository<UserRole> userRoleRepo) : base(repository, currentUser)
        {
            _userRoleRepo = userRoleRepo;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            IQueryable<User> query = _repository.GetQuery()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

            // ── Data Isolation: Admin เห็นเฉพาะ User ใน Division ตัวเอง ──
            if (_currentUser.DivisionId.HasValue)
            {
                var myDivId = _currentUser.DivisionId.Value;
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.DivisionId == myDivId));
            }

            return Ok(DataSourceLoader.Load(query, loadOptions));
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

