namespace iLearn.Application.DTOs
{
    public class LearningLogDto
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public int ContentId { get; set; } 

        public string? LearnTime { get; set; }
        public string? ExamTime { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateLearningLogDto
    {
        public string StudentCode { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public int ContentId { get; set; }
        public int QuestionId { get; set; } 
        public string? LearnTime { get; set; }
        public string? ExamTime { get; set; }
    }
}