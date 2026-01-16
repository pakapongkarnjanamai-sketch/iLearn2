using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class LearningLog : BaseEntity
    {

        public string StudentCode { get; set; } = string.Empty;

        public int CourseId { get; set; }
        public int ContentId { get; set; }
        public int QuestionId { get; set; }

     
        public string? LearnTime { get; set; }
        public string? ExamTime { get; set; }
    
    }
}