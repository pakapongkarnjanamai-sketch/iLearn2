using iLearn.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    public class ScormRuntimeState : BaseEntity
    {
        public int EnrollmentId { get; set; }

        [ForeignKey(nameof(EnrollmentId))]
        public Enrollment? Enrollment { get; set; }

        public int ContentItemId { get; set; }

        [ForeignKey(nameof(ContentItemId))]
        public ContentItem? ContentItem { get; set; }

        [StringLength(32)]
        public string ScormVersion { get; set; } = string.Empty;

        [StringLength(2048)]
        public string? LessonLocation { get; set; }

        public string? SuspendData { get; set; }

        [StringLength(64)]
        public string? LessonStatus { get; set; }

        [StringLength(64)]
        public string? CompletionStatus { get; set; }

        [StringLength(64)]
        public string? SuccessStatus { get; set; }

        public decimal? RawScore { get; set; }

        public decimal? ScaledScore { get; set; }

        [StringLength(64)]
        public string? SessionTime { get; set; }

        [StringLength(64)]
        public string? TotalTime { get; set; }

        [StringLength(64)]
        public string? Entry { get; set; }

        [StringLength(64)]
        public string? Exit { get; set; }

        public DateTime? LastCommittedAtUtc { get; set; }

        public string? CmiSnapshotJson { get; set; }
    }
}