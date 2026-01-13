using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class LearningLog : BaseEntity
    {
        // ใช้ StudentCode ตาม Enrollment
        public string StudentCode { get; set; } = string.Empty;

        public int CourseId { get; set; }
        public int ContentId { get; set; }
        public int QuestionId { get; set; }

        // เก็บเวลาเรียน/สอบ
        public string? CourseTime { get; set; }
        public string? ExamTime { get; set; }
    }
}