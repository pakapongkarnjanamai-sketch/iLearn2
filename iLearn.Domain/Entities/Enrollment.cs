using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class Enrollment : BaseEntity
    {
        // แก้ไข: กลับมาใช้ StudentCode (String) ตามเดิม
        public string StudentCode { get; set; } = string.Empty;

        // ตัด User Navigation ทิ้ง เพราะไม่ได้ใช้ ID เชื่อม
        // public int UserId { get; set; } 
        // public User? User { get; set; }

        public int CourseId { get; set; } // FK
        public Course? Course { get; set; }

        public int EnrolledVersion { get; set; }
        public string Status { get; set; } = "Not Started";
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}