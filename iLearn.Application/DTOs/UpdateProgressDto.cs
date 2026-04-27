namespace iLearn.Application.DTOs
{
    public class UpdateProgressDto
    {
        public int EnrollmentId { get; set; }

        // รับเป็นรายการ Resource ที่ต้องการบันทึกพร้อมกัน
        public List<ResourceProgressDto> Resources { get; set; } = new List<ResourceProgressDto>();
    }

    public class ResourceProgressDto
    {
        public int ResourceId { get; set; }
        public string? Status { get; set; } // passed, completed, incomplete
        public double? Progress { get; set; }
        public int? Score { get; set; }
        public string? SessionTime { get; set; } // เวลาที่ใช้ในรอบนี้
    }
}