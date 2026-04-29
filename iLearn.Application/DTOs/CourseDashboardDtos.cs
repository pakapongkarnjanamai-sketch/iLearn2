namespace iLearn.Application.DTOs
{
    /// <summary>
    /// DTO ?????? Learner ??????????????????? Course
    /// </summary>
    public class CourseLearnerDto
    {
        public int Id { get; set; }
        public string LearnerCode { get; set; } = string.Empty;
        public string LearnerName { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public double Progress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO ?????? Assignment History ??? Course ?????
    /// </summary>
    public class CourseAssignmentHistoryDto
    {
        public int Id { get; set; }
        public string? AssignmentNo { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CompletedEnrollmentCount { get; set; }
        public int TotalEnrollmentCount { get; set; }
        public double CompletionPct { get; set; }
        public int? LearnerGroupId { get; set; }
    }

    /// <summary>
    /// DTO ?????? Course Dashboard (??? course info, versions, KPI)
    /// </summary>
    public class CourseDashboardDto
    {
        public CourseDetailDto Course { get; set; } = null!;
        public IEnumerable<CourseVersionDto> Versions { get; set; } = [];
        public CourseDashboardKpiDto Kpi { get; set; } = new();
    }

    public class CourseDashboardKpiDto
    {
        public int VersionCount { get; set; }
        public int LearnerCount { get; set; }
        public int CompletedCount { get; set; }
        public int AssignmentCount { get; set; }
    }

    /// <summary>
    /// DTO ?????? Assignment Group History (????? LearnerGroup)
    /// </summary>
    public class AssignmentGroupHistoryDto
    {
        public int Id { get; set; }
        public string? AssignmentNo { get; set; }
        public string? Description { get; set; }
        public string CourseNames { get; set; } = string.Empty;
        public int CourseCount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CompletedEnrollmentCount { get; set; }
        public int TotalEnrollmentCount { get; set; }
        public double CompletionPct { get; set; }
    }
}
