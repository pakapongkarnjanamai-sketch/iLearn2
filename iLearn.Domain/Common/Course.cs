using iLearn.Domain.Common;
using iLearn.Domain.Enums;

namespace iLearn.Domain.Entities
{
    public class Course : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Features ใหม่
        public CourseType Type { get; set; }
        public int Version { get; set; } = 1;

        // Navigation
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<AssignmentRule> AssignmentRules { get; set; } = new List<AssignmentRule>();
    }
}