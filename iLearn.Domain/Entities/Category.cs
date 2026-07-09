using iLearn.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace iLearn.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public int? DivisionId { get; set; }
        public Division? Division { get; set; } // ต้องแน่ใจว่ามี Class Division อยู่ใน Project

        [StringLength(500)]
        public string? Description { get; set; }

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}