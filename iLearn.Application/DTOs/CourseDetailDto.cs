using System;
using System.Collections.Generic;

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
        public List<CourseResourceDto> Resources { get; set; } = new();
    }

    public class CourseResourceDto
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
        public List<CourseResourceDto> Resources { get; set; } = new();
    }
}
