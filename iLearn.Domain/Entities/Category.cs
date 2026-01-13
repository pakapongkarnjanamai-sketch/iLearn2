using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public int? DivisionId { get; set; }
        public Division? Division { get; set; } // ต้องแน่ใจว่ามี Class Division อยู่ใน Project

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}