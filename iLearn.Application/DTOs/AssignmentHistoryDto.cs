using System;
using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class AssignmentHistoryDto
    {
        public int Id { get; set; }
        public string AssignmentNo { get; set; }
        public string Description { get; set; }
        public string EmployeeCodes { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string CourseNames { get; set; }
        public string Status { get; set; }

        // ✅ Admin tracking
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // ✅ Summary counts (คำนวณฝั่ง service ไม่ต้องยิง API เพิ่ม)
        public int CourseCount { get; set; }
        public int StudentCount { get; set; }
        public int CompletedEnrollmentCount { get; set; }
        public int TotalEnrollmentCount { get; set; }
    }
}
