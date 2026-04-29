using iLearn.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    public class Enrollment : BaseEntity, IValidatableObject
    {
        public string LearnerCode { get; set; } = string.Empty;

        public int? CourseId { get; set; }
        public Course? Course { get; set; }

        public int? EnrolledCourseVersion { get; set; }
        public bool IsCompleted { get; set; } = false;
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public double Progress { get; set; } = 0;
        public int TotalScore { get; set; } = 0;
        public int TotalTimeSpent { get; set; } = 0;

        // Log ที่ CreatedAt < ResetAt ถือเป็น "รอบก่อน" ไม่นำมาแสดงใน Player
        public DateTime? ResetAt { get; set; }

        // Navigation: Enrollment 1 รายการ เชื่อมได้หลาย Assignment
        public ICollection<EnrollmentAssignment> AssignmentLinks { get; set; } = new List<EnrollmentAssignment>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && DueDate.HasValue && StartDate.Value > DueDate.Value)
                yield return new ValidationResult(
                    "StartDate ต้องไม่มากกว่า DueDate",
                    [nameof(StartDate), nameof(DueDate)]);
        }
    }
}
