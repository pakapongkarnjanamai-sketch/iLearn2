using iLearn.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema; // เพิ่ม namespace นี้

namespace iLearn.Domain.Entities
{
    public class LearningLog : BaseEntity
    {
        // ... (Properties เดิม) ...

        public string StudentCode { get; set; } = string.Empty;
        public int CourseVersionId { get; set; }
        public int ResourceId { get; set; }

        // ✅ [เพิ่ม] เชื่อมกับ Enrollment เพื่อให้รู้ว่าเป็นของรอบการลงทะเบียนไหน
        public int EnrollmentId { get; set; }
        [ForeignKey("EnrollmentId")]
        public Enrollment? Enrollment { get; set; }

        public string Status { get; set; } = "completed";
        public double Progress { get; set; } = 100.0;
        public int? Score { get; set; }

        public string? SessionTime { get; set; }

        // ✅ [ปรับปรุง] ใช้เก็บเวลาสะสมรวมทั้งหมด (วินาที) จากทุกรอบการเรียน
        public int TotalSecondsPlayed { get; set; }

        public int AttemptCount { get; set; }
    }
}