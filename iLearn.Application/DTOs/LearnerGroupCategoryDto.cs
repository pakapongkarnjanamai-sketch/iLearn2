using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    public class LearnerGroupCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DivisionId { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public int Depth { get; set; }
        public int ChildCount { get; set; }
        public int LearnerGroupCount { get; set; }
        public bool HasChildren => ChildCount > 0;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class LearnerGroupCategoryDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public int Depth { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public List<LearnerGroupCategoryAncestorDto> Ancestors { get; set; } = new();
        public List<LearnerGroupCategoryChildDto> Children { get; set; } = new();
        public List<LearnerGroupCategoryGroupDto> LearnerGroups { get; set; } = new();
    }

    public class LearnerGroupCategoryChildDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ChildCount { get; set; }
        public int LearnerGroupCount { get; set; }
    }

    public class LearnerGroupCategoryGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }

    public class CreateLearnerGroupCategoryDto
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public int? DivisionId { get; set; }
    }

    public class UpdateLearnerGroupCategoryDto
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
    }
}
