using iLearn.Application.DTOs;
using iLearn.Domain.Entities;

namespace iLearn.Application.Mappings
{
    public static class MappingExtensions
    {
        // --- Course Mappings ---

        public static CourseDto ToDto(this Course entity)
        {
            if (entity == null) return null;

            return new CourseDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Title = entity.Title,
                Description = entity.Description,
                IsActive = entity.IsActive,
                Type = entity.Type,
                TypeName = entity.Type.ToString(), // แปลง Enum เป็น String
                Version = entity.Version
            };
        }

        public static Course ToEntity(this CreateCourseDto dto)
        {
            if (dto == null) return null;

            return new Course
            {
                Code = dto.Code,
                Title = dto.Title,
                Description = dto.Description,
                Type = dto.Type,
                IsActive = true, // Default
                Version = 1      // Default
            };
        }

        // --- Enrollment Mappings ---

        public static EnrollmentDto ToDto(this Enrollment entity)
        {
            if (entity == null) return null;

            return new EnrollmentDto
            {
                Id = entity.Id,
                StudentCode = entity.StudentCode, // ✅ Map StudentCode
                CourseId = entity.CourseId,
                CourseTitle = entity.Course?.Title ?? string.Empty,
                EnrolledVersion = entity.EnrolledVersion,
                Status = entity.Status,
                CompletedDate = entity.CompletedDate
            };
        }

        public static UserDto ToDto(this User entity)
        {
            if (entity == null) return null;

            return new UserDto
            {
                Id = entity.Id,
                NID = entity.Nid,

            };
        }

        public static User ToEntity(this CreateUserDto dto)
        {
            if (dto == null) return null;

            return new User
            {
                Nid = dto.Nid,

            };
        }
        public static ResourceDto ToDto(this Resource entity)
        {
            if (entity == null) return null;

            return new ResourceDto
            {
                Id = entity.Id,
                Name = entity.Name,
                TypeId = entity.TypeId,
                IsActive = entity.IsActive,
                // สร้าง URL จำลอง (เดี๋ยวเราต้องทำ Action 'Download' หรือ 'View' มารองรับ)
                ContentUrl = $"/api/resources/{entity.Id}/content"
            };
        }
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
                // ระวัง Null Reference ถ้าไม่ได้ Include Division มา
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
                DivisionName = entity.Division?.Name, // ถ้ามี Include
                RoleId = entity.RoleId,
                RoleName = entity.Role?.Name // ถ้ามี Include
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

        public static LearningLogDto ToDto(this LearningLog entity)
        {
            if (entity == null) return null;
            return new LearningLogDto
            {
                Id = entity.Id,
                StudentCode = entity.StudentCode,
                CourseId = entity.CourseId,
                ContentId = entity.ContentId,
                CourseTime = entity.CourseTime,
                ExamTime = entity.ExamTime,
                CreatedDate = entity.CreatedDate
            };
        }

        public static LearningLog ToEntity(this CreateLearningLogDto dto)
        {
            if (dto == null) return null;
            return new LearningLog
            {
                StudentCode = dto.StudentCode,
                CourseId = dto.CourseId,
                ContentId = dto.ContentId,
                QuestionId = dto.QuestionId,
                CourseTime = dto.CourseTime,
                ExamTime = dto.ExamTime,
                // CreatedDate จะถูก Set โดย BaseEntity หรือ Database
            };
        }
    } 
}