using iLearn.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    public class AssignmentRule : BaseEntity
    {
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        // --- เงื่อนไขระบุตัวบุคคล (Specific Target) ---
        // เปลี่ยนเป็น EmployeeCodes เพื่อบ่งบอกว่าเก็บได้หลายคน (รูปแบบ Comma-separated)
        public string? EmployeeCodes { get; set; }

        // --- เงื่อนไขโครงสร้างองค์กร (Group Target) ---
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }

        // --- เงื่อนไขระยะเวลา ---
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}