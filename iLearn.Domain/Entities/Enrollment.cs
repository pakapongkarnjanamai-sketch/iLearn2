using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class Enrollment : BaseEntity
    {
        public string StudentCode { get; set; } = string.Empty;

        public int CourseId { get; set; } // FK
        public Course? Course { get; set; }

        public int EnrolledVersion { get; set; }
        public string Status { get; set; } = "Not Started";
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}