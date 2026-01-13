using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class Resource : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int TypeId { get; set; } // 1=Learn, 2=Exam (หรือใช้ Enum)

        // ข้อมูลสำหรับ SCORM/Url
        public string? ResourceHref { get; set; }
        public string? SchemaVersion { get; set; }
        public string? URL { get; set; }

        // ความสัมพันธ์กับ FileStorage (1-to-1)
        public int? FileStorageId { get; set; }
        public FileStorage? FileStorage { get; set; }

        public ICollection<CourseResource> CourseResources { get; set; } = new List<CourseResource>();
    }
}