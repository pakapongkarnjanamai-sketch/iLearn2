using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    //public class LearningLogDto
    //{
    //    public int Id { get; set; }
    //    public string LearnerCode { get; set; } = string.Empty;
    //    public int CourseId { get; set; }
    //    public int ContentId { get; set; } 

    //    public string? LearnTime { get; set; }
    //    public string? ExamTime { get; set; }

    //    public DateTime CreatedAt { get; set; }
    //}
    public class LearningLogDto
    {
        public int Id { get; set; }
        public string LearnerCode { get; set; } = string.Empty;

        public int CourseId { get; set; }
        public int CourseVersionId { get; set; }
        public int ContentItemId { get; set; }

        // แสดงผลสถานะและคะแนน
        public string LessonStatus { get; set; } = string.Empty;
        public string? LessonLocation { get; set; }
        public decimal? ScoreRaw { get; set; }
        public string TotalTime { get; set; } = string.Empty;

        public int AttemptCount { get; set; }
        public DateTime? LastAccessDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool IsFinalized { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    //public class CreateLearningLogDto
    //{
    //    public string LearnerCode { get; set; } = string.Empty;
    //    public int CourseId { get; set; }
    //    public int ContentId { get; set; }
    //    public int QuestionId { get; set; } 
    //    public string? LearnTime { get; set; }
    //    public string? ExamTime { get; set; }
    //}

    public class CreateLearningLogDto
    {
        [Required]
        public string LearnerCode { get; set; } = string.Empty;

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int CourseVersionId { get; set; }

        [Required]
        public int ContentItemId { get; set; }

        // Optional: กรณีต้องการ Set ค่าเริ่มต้น
        public string? LessonStatus { get; set; } = "not attempted";
        public decimal? ScoreRaw { get; set; }
        public string? TotalTime { get; set; } = "00:00:00";
    }
}