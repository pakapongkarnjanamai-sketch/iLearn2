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

        // --- เงื่อนไขโครงสร้างองค์กร ---
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }

        // --- เงื่อนไขระยะเวลา ---
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}