using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    public class StudentGroupCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DivisionId { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public int Depth { get; set; }
        public int ChildCount { get; set; }
        public int StudentGroupCount { get; set; }
        public bool HasChildren => ChildCount > 0;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class StudentGroupCategoryDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public int Depth { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public List<StudentGroupCategoryAncestorDto> Ancestors { get; set; } = new();
        public List<StudentGroupCategoryChildDto> Children { get; set; } = new();
        public List<StudentGroupCategoryGroupDto> StudentGroups { get; set; } = new();
    }

    public class StudentGroupCategoryChildDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ChildCount { get; set; }
        public int StudentGroupCount { get; set; }
    }

    public class StudentGroupCategoryGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }

    public class CreateStudentGroupCategoryDto
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
    }

    public class UpdateStudentGroupCategoryDto
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
    }
}
