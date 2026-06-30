namespace iLearn.Application.DTOs
{
    /// <summary>
    /// Mirrors learner course catalog item contract used by iLearn.User MyLearning catalog UI.
    /// </summary>
    public sealed class LearnerCourseCatalogDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int CourseTypeId { get; set; }
        public string CourseTypeName { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
    }
}
