using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    public class BulkAssignDto : IValidatableObject
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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && DueDate.HasValue && StartDate.Value > DueDate.Value)
                yield return new ValidationResult(
                    "StartDate ต้องไม่มากกว่า DueDate",
                    [nameof(StartDate), nameof(DueDate)]);
        }
    }

    public class ExtendDueDateDto
    {
        public DateTime NewDueDate { get; set; }
    }
}