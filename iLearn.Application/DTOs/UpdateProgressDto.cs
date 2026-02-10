namespace iLearn.Application.DTOs
{
    public class UpdateProgressDto
    {
        public string StudentCode { get; set; }
        public int EnrollmentId { get; set; }
        public int ResourceId { get; set; }
        public string? Status { get; set; }
        public double? Progress { get; set; }
        public string? SessionTime { get; set; }
        public int? Score { get; set; }
    }
}