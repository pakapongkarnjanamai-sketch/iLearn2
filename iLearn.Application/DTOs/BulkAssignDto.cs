using System;
using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class BulkAssignDto
    {
        public string? Description { get; set; }

        // รับค่าเป็น Array ของ Course ID
        public List<int> CourseIds { get; set; } = new List<int>();

        // รับค่าพนักงานเป็น Array 
        public List<string> EmployeeCodes { get; set; } = new List<string>();

        public string? Division { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}