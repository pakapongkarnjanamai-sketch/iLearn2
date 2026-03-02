using iLearn.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace iLearn.Domain.Entities
{
    public class Enrollment : BaseEntity
    {
        public string StudentCode { get; set; } = string.Empty;

        public int? CourseId { get; set; } // FK
        public Course? Course { get; set; }
        //EnrolledVersion -> EnrolledCourseVersion
        public int? EnrolledCourseVersion { get; set; }
        // Replace string status with boolean flag for completion
        public bool IsCompleted { get; set; } = false;
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        // [เพิ่มใหม่] เพื่อเก็บข้อมูลสรุป
        public double Progress { get; set; } = 0;       // ความคืบหน้า % (0-100)
        public int TotalScore { get; set; } = 0;        // คะแนนรวม
        public int TotalTimeSpent { get; set; } = 0;    // เวลาเรียนรวม (วินาที)

        // --- สิ่งที่ต้องเพิ่มเข้าไปใหม่ ---
        public int? AssignmentRuleId { get; set; }

        [ForeignKey("AssignmentRuleId")]
        public Assignment? AssignmentRule { get; set; }
    }
}