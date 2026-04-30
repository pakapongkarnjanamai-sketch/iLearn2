using System;
using System.Collections.Generic;
using iLearn.Domain.Enums;

namespace iLearn.Application.DTOs
{
    public class CourseDetailDto
    {
        public int Id { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CourseType { get; set; }
        public int CategoryId { get; set; }
        public bool IsActive { get; set; }
        public CourseStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public bool CanAssign { get; set; }
        public bool CanLearnerAccess { get; set; }
        public List<CourseContentItemDto> ContentItems { get; set; } = new();
    }

    public class CourseContentItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? URL { get; set; }
    }

    public class CourseVersionDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int VersionNumber { get; set; }
        public string Note { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CourseContentItemDto> ContentItems { get; set; } = new();
    }
}
