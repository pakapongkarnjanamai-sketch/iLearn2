using iLearn.Domain.Common;
namespace iLearn.Domain.Entities
{
    public class Course : BaseEntity
    {
      
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int CourseTypeId { get; set; }
        public virtual CourseType? CourseType { get; set; }

        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public ICollection<CourseVersion> Versions { get; set; } = new List<CourseVersion>();

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}