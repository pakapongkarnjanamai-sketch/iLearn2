using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class Enrollment : BaseEntity
    {
        public int UserId { get; set; } // FK
        public User? User { get; set; }

        public int CourseId { get; set; } // FK
        public Course? Course { get; set; }

        public int EnrolledVersion { get; set; }
        public string Status { get; set; } = "Not Started";
        public DateTime? CompletedDate { get; set; }
    }
}