using iLearn.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace iLearn.Domain.Entities
{
    public class Assignment : BaseEntity
    {
        public string? AssignmentNo { get; set; }
        public string? Description { get; set; }

        public int? CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        public string? EmployeeCodes { get; set; }

        public string? Division { get; set; } // ฟิลด์เดิม (อาจจะเก็บเป็นชื่อ)

        // ── สิ่งที่เพิ่มเข้ามาใหม่สำหรับการทำ Data Isolation ──
        public int? DivisionId { get; set; }
        [ForeignKey("DivisionId")]
        public Division? DivisionNavigation { get; set; }
        // ──────────────────────────────────────────

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        public int? StudentGroupId { get; set; }
        [ForeignKey("StudentGroupId")]
        public StudentGroup? StudentGroup { get; set; }

        public ICollection<AssignmentCourse> AssignmentCourses { get; set; } = new List<AssignmentCourse>();
    }
}