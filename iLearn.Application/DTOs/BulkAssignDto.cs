using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    public class BulkAssignDto : IValidatableObject
    {
        public string? Description { get; set; }

        // Selected course identifiers.
        public List<int> CourseIds { get; set; } = new List<int>();

        // Selected employee codes when assigning individual learners.
        public List<string> EmployeeCodes { get; set; } = new List<string>();

        // Optional student group identifier used instead of individual employee codes.
        public int? GroupId { get; set; }

        public string? Division { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        public bool ConfirmReassignInProgress { get; set; }
        public bool ConfirmReassignCompleted { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && DueDate.HasValue && StartDate.Value > DueDate.Value)
                yield return new ValidationResult(
                    "StartDate must be on or before DueDate.",
                    [nameof(StartDate), nameof(DueDate)]);
        }
    }

    public class ExtendDueDateDto
    {
        public DateTime NewDueDate { get; set; }
    }
}