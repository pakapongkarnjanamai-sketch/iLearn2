using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class CourseVersion : BaseEntity
    {

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public int VersionNumber { get; set; } // เช่น 1, 2, 3
        public string? Note { get; set; }      // เช่น "Initial Release", "Updated materials"

        // เชื่อมโยงไปหา ContentItem ผ่านตาราง CourseContentItem
        public ICollection<CourseContentItem> CourseContentItems { get; set; } = new List<CourseContentItem>();
    }
}