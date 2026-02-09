using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class PlayerInfoDto
    {
        public int CourseVersionId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;

        // เพิ่มส่วนนี้: Playlist สำหรับเก็บรายการไฟล์ทั้งหมดในคอร์ส
        public List<PlayerResourceDto> Resources { get; set; } = new();
    }

    public class PlayerResourceDto
    {
        public int Id { get; set; } // ResourceId
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Lesson"; // Exam or Lesson
        public string LaunchUrl { get; set; } = string.Empty;

    }
}