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
        public CategoriesCRUDController(IGenericRepository<Category> repository) : base(repository)
        {
        }
    }

    public class AssignmentRulesCRUDController : GenericController<AssignmentRule>
    {
        public AssignmentRulesCRUDController(IGenericRepository<AssignmentRule> repository) : base(repository)
        {
        }
    }

    public class CoursesCRUDController : GenericController<Course>
    {
        public CoursesCRUDController(IGenericRepository<Course> repository) : base(repository)
        {
        }
    }

    public class DivisionsCRUDController : GenericController<Division>
    {
        public DivisionsCRUDController(IGenericRepository<Division> repository) : base(repository)
        {
        }
    }

    public class EnrollmentsCRUDController : GenericController<Enrollment>
    {
        public EnrollmentsCRUDController(IGenericRepository<Enrollment> repository) : base(repository)
        {
        }
    }

    public class FileStoragesCRUDController : GenericController<FileStorage>
    {
        public FileStoragesCRUDController(IGenericRepository<FileStorage> repository) : base(repository)
        {
        }
    }

    public class LearningLogsCRUDController : GenericController<LearningLog>
    {
        public LearningLogsCRUDController(IGenericRepository<LearningLog> repository) : base(repository)
        {
        }
    }

    public class ResourcesCRUDController : GenericController<Resource>
    {
        private readonly IGenericRepository<CourseResource> _courseResourceRepo;
        private readonly IGenericRepository<Course> _courseRepo; // [New] เพิ่มเพื่อดึง Version ของ Course
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IScormService _scormService;

        public ResourcesCRUDController(
            IGenericRepository<Resource> repository,
            IGenericRepository<CourseResource> courseResourceRepo,
            IGenericRepository<Course> courseRepo,
            IGenericRepository<FileStorage> fileRepo,
            IScormService scormService) : base(repository)
        {
            _courseResourceRepo = courseResourceRepo;
            _courseRepo = courseRepo;
            _fileRepo = fileRepo;
            _scormService = scormService;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            // [Modified] Include ผ่าน CourseVersion เพื่อให้ได้ข้อมูล Course
            var query = _repository.GetQuery()
                .Include(r => r.CourseResources)
                    .ThenInclude(cr => cr.CourseVersion)
                        .ThenInclude(cv => cv.Course);

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            // [Modified] Include CourseVersion เพื่อเช็ค ID
            var resource = await _repository.GetQuery()
                                .Include(r => r.CourseResources)
                                    .ThenInclude(cr => cr.CourseVersion)
                                .FirstOrDefaultAsync(r => r.Id == key);

            if (resource == null) return NotFound();

            // 1. อัปเดตข้อมูลพื้นฐาน
            JsonConvert.PopulateObject(values, resource);

            // 2. จัดการ Course Mappings
            var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);
            if (valuesDict.ContainsKey("CourseIds"))
            {
                var courseIdsJson = valuesDict["CourseIds"].ToString();
                var selectedCourseIds = JsonConvert.DeserializeObject<List<int>>(courseIdsJson) ?? new List<int>();

                // 2.1 ดึงลิงก์เดิมที่มีอยู่
                var currentLinks = resource.CourseResources.ToList();

                // 2.2 ลบ Resource ออกจาก Course ที่ไม่ได้เลือกแล้ว (ลบทุก Version ของ Course นั้น)
                foreach (var link in currentLinks)
                {
                    // เช็คว่า CourseId ของ Version ที่ผูกอยู่ ยังอยู่ในรายการที่เลือกไหม
                    if (link.CourseVersion != null && !selectedCourseIds.Contains(link.CourseVersion.CourseId))
                    {
                        await _courseResourceRepo.DeleteAsync(link);
                    }
                }

                // 2.3 เพิ่ม Resource ให้กับ Course ใหม่ (ผูกกับ Version ล่าสุด)
                foreach (var courseId in selectedCourseIds)
                {
                    // ตรวจสอบว่าผูกกับ Version ใดๆ ของ Course นี้ไปแล้วหรือยัง
                    bool alreadyLinked = currentLinks.Any(cr => cr.CourseVersion != null && cr.CourseVersion.CourseId == courseId);

                    if (!alreadyLinked)
                    {
                        // ดึง Course และ Version ทั้งหมดเพื่อหาตัวล่าสุด
                        var course = await _courseRepo.GetQuery()
                            .Include(c => c.Versions)
                            .FirstOrDefaultAsync(c => c.Id == courseId);

                        if (course != null && course.Versions.Any())
                        {
                            // หา Version ล่าสุด (เรียงจากเลขมากสุด หรือจะใช้ IsActive ก็ได้)
                            var latestVersion = course.Versions
                                .OrderByDescending(v => v.VersionNumber)
                                .FirstOrDefault();

                            if (latestVersion != null)
                            {
                                await _courseResourceRepo.AddAsync(new CourseResource
                                {
                                    ResourceId = key,
                                    CourseVersionId = latestVersion.Id // [Key Change] ผูกกับ Version แทน CourseId
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

            // 1. ลบไฟล์จริง/Folder (Cleanup)
            try
            {
                if (resource.IsActive && !string.IsNullOrEmpty(resource.URL) && resource.URL.StartsWith("scorm/"))
                {
                    var parts = resource.URL.Split('/');
                    if (parts.Length >= 2)
                    {
                        _scormService.DeleteScormFolder(parts[1]);
                    }
                }

                if (resource.FileStorageId.HasValue)
                {
                    var file = await _fileRepo.GetByIdAsync(resource.FileStorageId.Value);
                    if (file != null)
                    {
                        await _fileRepo.DeleteAsync(file);
                    }
                }
            }
            catch (Exception)
            {
                // Log error but continue
            }

            // 2. ลบ Record ใน DB
            await _repository.DeleteAsync(resource);
            return Ok();
        }
    }
    public class RolesCRUDController : GenericController<Role>
    {
        public RolesCRUDController(IGenericRepository<Role> repository) : base(repository)
        {
        }
    }

    public class UsersCRUDController : GenericController<User>
    {
        // 1. ประกาศตัวแปรสำหรับ Repository ของ UserRole เพิ่มเติม
        private readonly IGenericRepository<UserRole> _userRoleRepo;

        // 2. รับ UserRole Repository ผ่าน Constructor
        public UsersCRUDController(
            IGenericRepository<User> repository,
            IGenericRepository<UserRole> userRoleRepo) : base(repository)
        {
            _userRoleRepo = userRoleRepo;
        }
        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            // ใช้ GetQuery() แล้วสั่ง Include UserRoles
            var query = _repository.GetQuery().Include(u => u.UserRoles);

            // ส่ง Query ให้ DevExtreme จัดการ (มันจะตัดหน้า/ค้นหา ให้เองผ่าน SQL)
            return Ok(DataSourceLoader.Load(query, loadOptions));
        }
        // 3. ย้ายเมธอด Override ออกมาไว้นอก Constructor
        [HttpPut("Put")] // คง Route ให้เหมือน Base Class ("api/admin/UsersCRUD/Put")
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            // ใช้ _repository (จาก Base Class) แทน _userRepo
            var user = await _repository.GetByIdAsync(key);
            if (user == null) return NotFound();

            // 1. Populate ข้อมูลพื้นฐาน (Nid, etc.)
            JsonConvert.PopulateObject(values, user);

            // 2. จัดการ RoleIds
            var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);

            if (valuesDict.ContainsKey("RoleIds"))
            {
                // ดึง RoleIds ใหม่จาก JSON
                // ระวัง: valuesDict["RoleIds"] อาจเป็น JArray หรือ string ขึ้นอยู่กับ Serializer
                // การใช้ JsonConvert.DeserializeObject อีกรอบแบบนี้ปลอดภัยที่สุดสำหรับ JSON string ซ้อน
                var roleIdsJson = valuesDict["RoleIds"].ToString();
                var newRoleIds = JsonConvert.DeserializeObject<List<int>>(roleIdsJson) ?? new List<int>();

                // ดึง UserRoles เดิมที่มีอยู่
                // (สมมติว่า Get รับ Expression ได้)
                var existingUserRoles = (await _userRoleRepo.GetAsync(ur => ur.UserId == key)).ToList();

                // ลบ Roles ที่ไม่ได้เลือกแล้ว
                foreach (var ur in existingUserRoles)
                {
                    if (!newRoleIds.Contains(ur.RoleId))
                    {
                        await _userRoleRepo.DeleteAsync(ur);
                    }
                }

                // เพิ่ม Roles ใหม่ที่ยังไม่มี
                foreach (var roleId in newRoleIds)
                {
                    if (!existingUserRoles.Any(ur => ur.RoleId == roleId))
                    {
                        await _userRoleRepo.AddAsync(new UserRole { UserId = key, RoleId = roleId });
                    }
                }
            }

            // ใช้ _repository.UpdateAsync หรือ SaveAsync ตามโครงสร้างของคุณ
            await _repository.UpdateAsync(user);

            return Ok(user);
        }
    }

    public class UserRolesCRUDController : GenericController<UserRole>
    {
        public UserRolesCRUDController(IGenericRepository<UserRole> repository) : base(repository)
        {
        }
    }
    // เพิ่มในไฟล์ CRUDController.cs หรือแยกไฟล์ใหม่

    public class CourseVersionsCRUDController : GenericController<CourseVersion>
    {
        public CourseVersionsCRUDController(IGenericRepository<CourseVersion> repository) : base(repository) { }
    }

    public class CourseResourcesCRUDController : GenericController<CourseResource>
    {
        // จำเป็นต้อง Include Resource เพื่อแสดงชื่อใน Grid (ถ้าไม่ได้ใช้ Lookup) 
        // แต่ใน View เราใช้ Lookup ไปหา ResourcesCRUD แล้ว ดังนั้น Generic ธรรมดาก็พอใช้ได้
        public CourseResourcesCRUDController(IGenericRepository<CourseResource> repository) : base(repository) { }
    }

}
