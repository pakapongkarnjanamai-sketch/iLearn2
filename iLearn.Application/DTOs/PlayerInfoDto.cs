namespace iLearn.Application.DTOs
{
    public class PlayerInfoDto
    {
        public int CourseVersionId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CourseTypeName { get; set; } = string.Empty;
        public double Progress { get; set; }

        // สถานะจบการศึกษา (Completed)
        public bool IsCompleted { get; set; } = false;

        // [เพิ่มใหม่] สถานะ View Only (ไม่มี Enrollment หรือดูตัวอย่าง)
        public bool IsReadOnly { get; set; } = false;
        public int? EnrollmentId { get; set; } // [เพิ่ม] ส่ง ID กลับไปเพื่อให้ Frontend ใช้ตอน Save
        public List<PlayerResourceDto> Resources { get; set; } = new();
    }

    public class PlayerResourceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Lesson";
        public string LaunchUrl { get; set; } = string.Empty;
        public string ScormVersion { get; set; } = string.Empty;

        public string Status { get; set; } = "incomplete";
        public double Progress { get; set; }
        public double ActivityProgress { get; set; }

        public bool IsCompleted { get; set; }
        public decimal? Score { get; set; }
        public string? Time { get; set; }
        public ScormRuntimeStateDto? RuntimeState { get; set; }
    }
}