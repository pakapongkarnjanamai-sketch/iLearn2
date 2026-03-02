namespace iLearn.Application.DTOs
{
    public class EnrollmentDto
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty; // ✅ ใช้ String ตาม Entity
        public int? CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public int? EnrolledCourseVersion { get; set; }
        // Replace string Status with boolean flag
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public double Progress { get; set; }


    }
}