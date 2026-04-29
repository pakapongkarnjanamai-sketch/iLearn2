namespace iLearn.Application.DTOs
{
    public class UpdateProgressDto
    {
        public int EnrollmentId { get; set; }

        // รับเป็นรายการ ContentItem ที่ต้องการบันทึกพร้อมกัน
        public List<ContentItemProgressDto> ContentItems { get; set; } = new List<ContentItemProgressDto>();
    }

    public class ContentItemProgressDto
    {
        public int ContentItemId { get; set; }
        public string? Status { get; set; } // passed, completed, incomplete
        public double? Progress { get; set; }
        public int? Score { get; set; }
        public string? SessionTime { get; set; } // เวลาที่ใช้ในรอบนี้
    }

    public class ResetProgressDto
    {
        public int EnrollmentId { get; set; }
    }
}