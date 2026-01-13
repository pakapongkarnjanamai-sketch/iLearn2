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
                UserId = entity.UserId,
                CourseId = entity.CourseId,
                CourseTitle = entity.Course?.Title ?? string.Empty, // ป้องกัน Null
                EnrolledVersion = entity.EnrolledVersion,
                Status = entity.Status,
                CompletedDate = entity.CompletedDate
            };
        }
    }
}