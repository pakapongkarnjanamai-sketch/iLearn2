namespace iLearn.Application.DTOs
{
    public class LearningLogDto
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public int ContentId { get; set; } // หรือ ResourceId

        // เวลาที่ใช้เรียน (ส่งมาเป็น String Format เช่น "00:30:00")
        public string? CourseTime { get; set; }
        public string? ExamTime { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    public class CreateLearningLogDto
    {
        public string StudentCode { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public int ContentId { get; set; }
        public int QuestionId { get; set; } // ถ้ามี
        public string? CourseTime { get; set; }
        public string? ExamTime { get; set; }
    }
}