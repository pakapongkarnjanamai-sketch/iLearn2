namespace iLearn.Application.DTOs
{
    public class PlayerInfoDto
    {
        public int CourseVersionId { get; set; }
        public int ResourceId { get; set; }
        public string LaunchUrl { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
    }
}