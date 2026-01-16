using iLearn.Domain.Common;
using iLearn.Domain.Enums;

namespace iLearn.Domain.Entities
{
    public class Course : BaseEntity
    {
        // ... Properties เดิม ...
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public CourseType Type { get; set; }

        // [แก้ไข] เอา Version เดิมออก (ถ้ามี) หรือเก็บไว้เป็น CurrentVersionNumber ก็ได้
        // public int Version { get; set; } 

        // [ใหม่] ความสัมพันธ์กับ Versions
        public ICollection<CourseVersion> Versions { get; set; } = new List<CourseVersion>();

        // [แก้ไข] เอา CourseResources เดิมออก เพราะย้ายไปอยู่ใน CourseVersion แล้ว
        // public virtual ICollection<CourseResource> CourseResources { get; set; } ...

        // Navigation อื่นๆ คงเดิม
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<AssignmentRule> AssignmentRules { get; set; } = new List<AssignmentRule>();
    }
}