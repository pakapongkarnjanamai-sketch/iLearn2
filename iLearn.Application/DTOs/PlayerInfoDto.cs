namespace iLearn.Application.DTOs
{
    public class PlayerInfoDto
    {
        public int CourseVersionId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;

        // [เพิ่มใหม่] ส่งสถานะ Enrollment กลับเป็น boolean
        public bool IsCompleted { get; set; } = false;

        public List<PlayerResourceDto> Resources { get; set; } = new();
    }

    public class PlayerResourceDto
    {
        public int Id { get; set; } // ResourceId
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Lesson";
        public string LaunchUrl { get; set; } = string.Empty;

        // [เพิ่มใหม่] ส่งสถานะรายบทเรียนกลับไป
        public bool IsCompleted { get; set; }
        public int? Score { get; set; }
        public string? Time { get; set; } // เช่น "00:15:30"
    }
}