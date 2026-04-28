namespace iLearn.Application.DTOs
{
    public class ScormRuntimeStateDto
    {
        public int EnrollmentId { get; set; }
        public int ResourceId { get; set; }
        public string ScormVersion { get; set; } = string.Empty;
        public string? LessonLocation { get; set; }
        public string? SuspendData { get; set; }
        public string? LessonStatus { get; set; }
        public string? CompletionStatus { get; set; }
        public string? SuccessStatus { get; set; }
        public decimal? RawScore { get; set; }
        public string? SessionTime { get; set; }
        public string? TotalTime { get; set; }
        public string? Entry { get; set; }
        public string? Exit { get; set; }
        public DateTime? LastCommittedAtUtc { get; set; }
        public string? CmiSnapshotJson { get; set; }
    }

    public class ScormRuntimeCommitRequestDto
    {
        public int EnrollmentId { get; set; }
        public List<ScormRuntimeResourceCommitDto> Resources { get; set; } = new();
    }

    public class ScormRuntimeResourceCommitDto
    {
        public int ResourceId { get; set; }
        public string ScormVersion { get; set; } = string.Empty;
        public string? LessonLocation { get; set; }
        public string? SuspendData { get; set; }
        public string? LessonStatus { get; set; }
        public string? CompletionStatus { get; set; }
        public string? SuccessStatus { get; set; }
        public decimal? RawScore { get; set; }
        public string? SessionTime { get; set; }
        public string? TotalTime { get; set; }
        public string? Entry { get; set; }
        public string? Exit { get; set; }
        public DateTime? LastCommittedAtUtc { get; set; }
        public string? CmiSnapshotJson { get; set; }
    }
}