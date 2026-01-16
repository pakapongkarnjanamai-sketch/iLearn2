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
        public CourseType Type { get; set; }

        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public ICollection<CourseVersion> Versions { get; set; } = new List<CourseVersion>();

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<AssignmentRule> AssignmentRules { get; set; } = new List<AssignmentRule>();
    }
}