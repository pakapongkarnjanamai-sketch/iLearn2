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
                Type = entity.Type,
                TypeName = entity.Type.ToString(), // หรือจะ Map เป็นภาษาไทยที่นี่เลยก็ได้ เช่น entity.Type == CourseType.General ? "วิชาทั่วไป" : "วิชาเฉพาะทาง"

                // [เพิ่มใหม่] Map ข้อมูล Category
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.Name ?? "General", // ป้องกัน Null

                Version = currentVersion?.VersionNumber ?? 0
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
                Type = dto.Type,
                IsActive = true
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
                Id = entity.Id,
                StudentCode = entity.StudentCode,
                CourseId = entity.CourseId,
                CourseCode= entity.Course?.Code ?? string.Empty,
                CourseTitle = entity.Course?.Title ?? string.Empty,
                EnrolledCourseVersion = entity.EnrolledCourseVersion,
                IsCompleted = entity.IsCompleted,
                DueDate = entity.DueDate,
                CompletedDate = entity.CompletedDate,
                Progress = entity.Progress,
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

        // --- Resource Mappings ---

        public static ResourceDto ToDto(this Resource entity)
        {
            if (entity == null) return null;

            return new ResourceDto
            {
                Id = entity.Id,
                Name = entity.Name,
                TypeId = entity.TypeId,
                IsActive = entity.IsActive,
                ContentUrl = $"/api/resources/{entity.Id}/content"
            };
        }

        // --- Division & Role Mappings ---

        public static DivisionDto ToDto(this Division entity)
        {
            if (entity == null) return null;
            return new DivisionDto { Id = entity.Id, Name = entity.Name };
        }

        public static RoleDto ToDto(this Role entity)
        {
            if (entity == null) return null;
            return new RoleDto
            {
                Id = entity.Id,
                Name = entity.Name,
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
        public static AssignmentRuleDto ToDto(this AssignmentRule entity)
        {
            if (entity == null) return null;
            return new AssignmentRuleDto
            {
                Id = entity.Id,
                CourseId = entity.CourseId,
                DivisionId = entity.DivisionId,
                DivisionName = entity.Division?.Name,
                RoleId = entity.RoleId,
                RoleName = entity.Role?.Name
            };
        }

        public static AssignmentRule ToEntity(this CreateAssignmentRuleDto dto)
        {
            if (dto == null) return null;
            return new AssignmentRule
            {
                CourseId = dto.CourseId,
                DivisionId = dto.DivisionId,
                RoleId = dto.RoleId
            };
        }

        // --- Learning Log Mappings ---

        // --- Learning Log Mappings (Updated for SCORM) ---

        //public static LearningLogDto ToDto(this LearningLog entity)
        //{
        //    if (entity == null) return null;
        //    return new LearningLogDto
        //    {
        //        Id = entity.Id,
        //        StudentCode = entity.StudentCode,
        //        CourseId = entity.CourseId,
        //        CourseVersionId = entity.CourseVersionId, // New
        //        ResourceId = entity.ResourceId,           // New (แทน ContentId)

        //        // SCORM Fields
        //        LessonStatus = entity.LessonStatus,
        //        LessonLocation = entity.LessonLocation,
        //        ScoreRaw = entity.ScoreRaw,
        //        TotalTime = entity.TotalTime, // New (แทน LearnTime/ExamTime)

        //        // Metadata
        //        AttemptCount = entity.AttemptCount,
        //        LastAccessDate = entity.LastAccessDate,
        //        CompletedDate = entity.CompletedDate,
        //        IsFinalized = entity.IsFinalized,
        //        CreatedAt = entity.CreatedAt,
        //        UpdatedAt = entity.UpdatedAt
        //    };
        //}

        //public static LearningLog ToEntity(this CreateLearningLogDto dto)
        //{
        //    if (dto == null) return null;
        //    return new LearningLog
        //    {
        //        StudentCode = dto.StudentCode,
        //        CourseId = dto.CourseId,
        //        CourseVersionId = dto.CourseVersionId, // New
        //        ResourceId = dto.ResourceId,           // New

        //        // Optional initial values
        //        LessonStatus = dto.LessonStatus ?? "not attempted",
        //        ScoreRaw = dto.ScoreRaw,
        //        TotalTime = dto.TotalTime ?? "00:00:00",

        //        // Default values for new log
        //        AttemptCount = 1,
        //        IsFinalized = false
        //    };
        //}
    }
}