using iLearn.Application.DTOs;
using iLearn.Domain.Entities;
using System.Linq; // จำเป็นสำหรับการใช้ LINQ กับ Versions

namespace iLearn.Application.Mappings
{
    public static class MappingExtensions
    {
        // --- Course Mappings ---

        public static CourseDto ToDto(this Course entity)
        {
            if (entity == null) return null;

            var currentVersion = entity.Versions?
                .Where(v => v.IsActive)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();

            return new CourseDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Title = entity.Title,
                Description = entity.Description,
                IsActive = entity.IsActive,
                Status = entity.Status,
                CanAssign = entity.CanAssign,
                CanLearnerAccess = entity.CanLearnerAccess,
                CourseTypeId = entity.CourseTypeId,
                TypeName = entity.CourseType?.Name ?? string.Empty,

                // [เพิ่มใหม่] Map ข้อมูล Category
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.Name ?? "General", // ป้องกัน Null

                Version = currentVersion?.VersionNumber ?? 0,

                // 🆕 Data Isolation: ดึง DivisionId จาก Category
                DivisionId = entity.Category?.DivisionId
            };
        }

        public static Course ToEntity(this CreateCourseDto dto)
        {
            if (dto == null) return null;

            var course = new Course
            {
                Code = dto.Code,
                Title = dto.Title,
                Description = dto.Description,
                CourseTypeId = dto.CourseTypeId,
                IsActive = true,
                Status = iLearn.Domain.Enums.CourseStatus.Open
            };

            // [New] สร้าง Version แรก (v1) ให้โดยอัตโนมัติเมื่อสร้างคอร์ส
            course.Versions.Add(new CourseVersion
            {
                VersionNumber = 1,
                IsActive = true,
                Note = "Initial Release"
            });

            return course;
        }

        // --- Enrollment Mappings ---

        public static EnrollmentDto ToDto(this Enrollment entity)
        {
            if (entity == null) return null;

            return new EnrollmentDto
            {
                Id                    = entity.Id,
                LearnerCode           = entity.LearnerCode,
                CourseId              = entity.CourseId,
                CourseCode            = entity.Course?.Code  ?? string.Empty,
                CourseTitle           = entity.Course?.Title ?? string.Empty,
                EnrolledCourseVersion = entity.EnrolledCourseVersion,
                IsCompleted           = entity.IsCompleted,
                StartDate             = entity.StartDate,
                DueDate               = entity.DueDate,
                CompletedDate         = entity.CompletedDate,
                Progress              = entity.Progress,
                CourseTypeName        = entity.Course?.CourseType?.Name ?? string.Empty,
            };
        }

        // --- User Mappings ---

        public static UserDto ToDto(this User entity)
        {
            if (entity == null) return null;

            return new UserDto
            {
                Id = entity.Id,
                NID = entity.Nid,
                // Map fields อื่นๆ ตาม UserDto
            };
        }

        public static User ToEntity(this CreateUserDto dto)
        {
            if (dto == null) return null;

            return new User
            {
                Nid = dto.Nid,
                // Map fields อื่นๆ
            };
        }

        // --- ContentItem Mappings ---

        public static ContentItemDto ToDto(this ContentItem entity)
        {
            if (entity == null) return null;

            return new ContentItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                TypeId = entity.TypeId,
                IsActive = entity.IsActive,
                LaunchHref = entity.LaunchHref,
                SchemaVersion = entity.SchemaVersion,
                Url = entity.URL,
                FileStorageId = entity.FileStorageId,
                FileLength = entity.CachedFileLength ?? 0,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CourseIdsCount = entity.CourseContentItems
                    .Where(courseContentItem => courseContentItem.CourseVersion != null)
                    .Select(courseContentItem => courseContentItem.CourseVersion!.CourseId)
                    .Distinct()
                    .Count(),
                CourseContentItems = entity.CourseContentItems
                    .Where(courseContentItem => courseContentItem.CourseVersion?.Course != null)
                    .Select(courseContentItem => new ContentItemCourseReferenceDto
                {
                    CourseId = courseContentItem.CourseVersion!.CourseId,
                    CourseTitle = courseContentItem.CourseVersion.Course!.Title,
                    CourseCode = courseContentItem.CourseVersion.Course!.Code,
                    CourseVersionId = courseContentItem.CourseVersionId,
                    VersionNumber = courseContentItem.CourseVersion!.VersionNumber
                }).ToList(),
                ContentUrl = $"/api/contentItems/{entity.Id}/content"
            };
        }

        // --- Division & Role Mappings ---

        public static DivisionDto ToDto(this Division entity)
        {
            if (entity == null) return null;
            return new DivisionDto
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };
        }

        public static AdminActivityDto ToDto(this AdminActivity entity)
        {
            if (entity == null) return null;
            return new AdminActivityDto
            {
                Id = entity.Id,
                ActionType = entity.ActionType,
                EntityType = entity.EntityType,
                EntityId = entity.EntityId,
                Title = entity.Title,
                Description = entity.Description,
                DivisionId = entity.DivisionId,
                CreatedAt = entity.CreatedAt,
                CreatedBy = entity.CreatedBy
            };
        }

        public static RoleDto ToDto(this Role entity)
        {
            if (entity == null) return null;
            return new RoleDto
            {
                Id = entity.Id,
                Name = entity.Name,
                RoleType = entity.RoleType,
                DivisionId = entity.DivisionId,
                DivisionName = entity.Division?.Name ?? string.Empty
            };
        }

        public static CategoryDto ToDto(this Category entity)
        {
            if (entity == null) return null;
            return new CategoryDto
            {
                Id = entity.Id,
                Name = entity.Name,
                DivisionId = entity.DivisionId
            };
        }

        // --- Assignment Rule Mappings ---
        public static AssignmentRuleDto ToDto(this Assignment entity)
        {
            if (entity == null) return null;
            return new AssignmentRuleDto
            {
                Id = entity.Id,
                CourseId = entity.CourseId,
                Division = entity.Division,
              
                StartDate = entity.StartDate,
                DueDate = entity.DueDate
            };
        }

        public static Assignment ToEntity(this CreateAssignmentRuleDto dto)
        {
            if (dto == null) return null;
            return new Assignment
            {
                CourseId = dto.CourseId,
                Division = dto.Division,
         
                StartDate = dto.StartDate,
                DueDate = dto.DueDate
            };
        }


    }
}