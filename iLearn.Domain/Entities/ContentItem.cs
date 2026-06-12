using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class ContentItem : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int TypeId { get; set; } // 1=Learn, 2=Exam (หรือใช้ Enum)

        public string? LaunchHref { get; set; }
        public string? SchemaVersion { get; set; }
        public string? URL { get; set; }

        // ความสัมพันธ์กับ FileStorage (1-to-1)
        public int? FileStorageId { get; set; }
        public FileStorage? FileStorage { get; set; }
        public long? CachedFileLength { get; set; }

        public ICollection<CourseContentItem> CourseContentItems { get; set; } = new List<CourseContentItem>();
    }
}