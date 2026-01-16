using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class CourseVersion : BaseEntity
    {

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public int VersionNumber { get; set; } // เช่น 1, 2, 3
        public string? Note { get; set; }      // เช่น "Initial Release", "Updated materials"
        public bool IsActive { get; set; } = true; // ใช้สำหรับบอกว่าเป็น Version ปัจจุบันหรือไม่

        // เชื่อมโยงไปหา Resource ผ่านตาราง CourseResource
        public ICollection<CourseResource> CourseResources { get; set; } = new List<CourseResource>();
    }
}