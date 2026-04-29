namespace iLearn.Application.DTOs
{
    public class EnrollmentDto
    {
        public int Id { get; set; }
        public string LearnerCode { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public int? EnrolledCourseVersion { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public double Progress { get; set; }
        public string CourseTypeName { get; set; } = string.Empty;
    }
}