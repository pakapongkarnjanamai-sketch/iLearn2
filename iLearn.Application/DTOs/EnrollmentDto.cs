namespace iLearn.Application.DTOs
{
    public class EnrollmentDto
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty; // ✅ ใช้ String ตาม Entity
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int EnrolledVersion { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? CompletedDate { get; set; }
    }
}