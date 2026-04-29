using System.Text.Json.Serialization;

namespace iLearn.Application.DTOs
{
    public enum CourseVersionLearnerPolicy
    {
        NewLearnersOnly = 0,
        MoveNotStarted = 1,
        ResetInProgress = 2
    }

    public class CourseVersionLearnerPolicyDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CourseVersionLearnerPolicy Policy { get; set; } = CourseVersionLearnerPolicy.NewLearnersOnly;
    }

    public class CourseVersionLearnerImpactDto
    {
        public int CourseId { get; set; }
        public int NotStartedCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int OtherOpenCount { get; set; }
        public int EligibleOpenCount => NotStartedCount + InProgressCount;
        public bool HasEligibleOpenLearners => EligibleOpenCount > 0;
    }
}
