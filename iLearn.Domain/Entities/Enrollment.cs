using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class Enrollment : BaseEntity
    {
        public string StudentCode { get; set; } = string.Empty;

        public int CourseId { get; set; } // FK
        public Course? Course { get; set; }

        public int EnrolledVersion { get; set; }
        // Replace string status with boolean flag for completion
        public bool IsCompleted { get; set; } = false;
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        // [เพิ่มใหม่] เพื่อเก็บข้อมูลสรุป
        public double Progress { get; set; } = 0;       // ความคืบหน้า % (0-100)
        public int TotalScore { get; set; } = 0;        // คะแนนรวม
        public int TotalTimeSpent { get; set; } = 0;    // เวลาเรียนรวม (วินาที)
    }
}