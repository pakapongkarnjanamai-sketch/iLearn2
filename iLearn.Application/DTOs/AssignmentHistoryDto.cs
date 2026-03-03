using System;

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
        public string Status { get; set; } // ฟิลด์ใหม่สำหรับแสดงสถานะ
    }
}