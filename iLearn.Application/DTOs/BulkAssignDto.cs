using System;
using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class BulkAssignDto
    {
        public string? Description { get; set; }

        // รับค่าเป็น Array ของ Course ID
        public List<int> CourseIds { get; set; } = new List<int>();

        // รับค่าพนักงานเป็น Array (ใช้เมื่อเลือกรายคน)
        public List<string> EmployeeCodes { get; set; } = new List<string>();

        // ใช้เมื่อ Assign จาก Student Group (แทน EmployeeCodes)
        public int? GroupId { get; set; }

        public string? Division { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class ExtendDueDateDto
    {
        public DateTime NewDueDate { get; set; }
    }
}