using iLearn.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    public class Assignment : BaseEntity
    {
        // เพิ่มฟิลด์ใหม่
        public string? AssignmentNo { get; set; }
        public string? Description { get; set; }

        public int? CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        public string? EmployeeCodes { get; set; } // เก็บเป็น Comma-separated หรือ JSON string ได้ครับ

        public string? Division { get; set; }
        // ลบ Department, Section, Position ออกไปแล้ว

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}