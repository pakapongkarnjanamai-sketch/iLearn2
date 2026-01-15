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
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IScormService _scormService;

        public ResourcesCRUDController(
            IGenericRepository<Resource> repository,
            IGenericRepository<CourseResource> courseResourceRepo,
            IGenericRepository<FileStorage> fileRepo,
            IScormService scormService) : base(repository)
        {
            _courseResourceRepo = courseResourceRepo;
            _fileRepo = fileRepo;
            _scormService = scormService;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            // Override เพื่อ Include ข้อมูลที่จำเป็น
            // - CourseResources: เพื่อแสดงว่า Resource นี้ผูกกับวิชาอะไรบ้าง (ใน TagBox)
            var query = _repository.GetQuery()
                .Include(r => r.CourseResources);

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var resource = await _repository.GetQuery()
                                .Include(r => r.CourseResources)
                                .FirstOrDefaultAsync(r => r.Id == key);

            if (resource == null) return NotFound();

            // 1. อัปเดตข้อมูลพื้นฐาน (Name, IsActive, TypeId, URL)
            JsonConvert.PopulateObject(values, resource);

            // 2. จัดการ Course Mappings (CourseIds)
            var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);
            if (valuesDict.ContainsKey("CourseIds"))
            {
                var courseIdsJson = valuesDict["CourseIds"].ToString();
                var newCourseIds = JsonConvert.DeserializeObject<List<int>>(courseIdsJson) ?? new List<int>();

                // 2.1 ดึงรายการเดิม
                var existingLinks = await _courseResourceRepo.GetAsync(cr => cr.ResourceId == key);

                // 2.2 ลบรายการที่ไม่อยู่ในลิสต์ใหม่
                foreach (var link in existingLinks)
                {
                    if (!newCourseIds.Contains(link.CourseId))
                    {
                        await _courseResourceRepo.DeleteAsync(link);
                    }
                }

                // 2.3 เพิ่มรายการใหม่ที่ยังไม่มี
                foreach (var courseId in newCourseIds)
                {
                    if (!existingLinks.Any(cr => cr.CourseId == courseId))
                    {
                        await _courseResourceRepo.AddAsync(new CourseResource
                        {
                            ResourceId = key,
                            CourseId = courseId
                        });
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
                // ถ้าเป็น SCORM (มี URL และ IsActive) ให้ลบ Folder
                if (resource.IsActive && !string.IsNullOrEmpty(resource.URL) && resource.URL.StartsWith("scorm/"))
                {
                    // URL Format: "scorm/{Guid}/{launchFile}"
                    var parts = resource.URL.Split('/');
                    if (parts.Length >= 2)
                    {
                        _scormService.DeleteScormFolder(parts[1]);
                    }
                }

                // ลบ FileStorage ที่ผูกอยู่ (ถ้ามี)
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
                // Log error but continue to delete record
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

}
