namespace iLearn.Application.DTOs
{
    public class ContentItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TypeId { get; set; } // 1=Learn, 2=Exam
        public bool IsActive { get; set; }
        public bool IsPublished => IsActive;
        public string PublishState => IsPublished ? "Published" : "Unpublished";
        public string? LaunchHref { get; set; }
        public string? SchemaVersion { get; set; }
        public string? Url { get; set; }
        public int? FileStorageId { get; set; }
        public long FileLength { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CourseIdsCount { get; set; }
        public List<ContentItemCourseReferenceDto> CourseContentItems { get; set; } = [];

        // เราจะไม่ส่ง byte[] กลับไปใน DTO นี้ (เพราะมันใหญ่)
        // แต่จะส่ง URL หรือ Path ให้ Frontend เรียกแทน
        public string? ContentUrl { get; set; }
    }

    public class ContentItemCourseReferenceDto
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int CourseVersionId { get; set; }
        public int VersionNumber { get; set; }
    }

    // DTO สำหรับการสร้าง (Upload)
    // หมายเหตุ: การรับไฟล์จริงจะทำผ่าน IFormFile ใน Controller โดยตรง
    // หรือจะใส่ใน Class นี้ก็ได้ แต่ต้องระวังเรื่อง Binding
    public class CreateContentItemDto
    {
        public string Name { get; set; } = string.Empty;
        public int TypeId { get; set; }
    }
}