//namespace iLearn.Domain.Entities
using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class CourseType : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation property ไปยัง Course
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}