using iLearn.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    public class Assignment : BaseEntity
    {
        public string? AssignmentNo { get; set; }
        public string? Description { get; set; }

        // ── Legacy field — kept for backward-compatible migration; new code uses AssignmentCourses ──
        public int? CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        public string? EmployeeCodes { get; set; }

        public string? Division { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        // FK ไปยังกลุ่มผู้เรียน (nullable — backward compatible กับ EmployeeCodes เดิม)
        public int? StudentGroupId { get; set; }
        [ForeignKey("StudentGroupId")]
        public StudentGroup? StudentGroup { get; set; }

        // ── Normalized detail lines (header → courses) ──
        public ICollection<AssignmentCourse> AssignmentCourses { get; set; } = new List<AssignmentCourse>();
    }
}