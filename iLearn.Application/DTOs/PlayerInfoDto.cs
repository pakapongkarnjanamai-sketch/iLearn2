namespace iLearn.Application.DTOs
{
    public class PlayerInfoDto
    {
        public int CourseVersionId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;

        // รายชื่อบทเรียนทั้งหมดในคอร์สนี้ (Playlist)
        public List<PlayerResourceDto> Resources { get; set; } = new();
    }

    public class PlayerResourceDto
    {
        public int ResourceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Lesson"; // หรือเก็บ TypeId
        public string LaunchUrl { get; set; } = string.Empty;
    }
}