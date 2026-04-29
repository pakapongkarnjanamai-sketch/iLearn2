using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class LearnerGroupMember : BaseEntity
    {
        public int LearnerGroupId { get; set; }
        public LearnerGroup LearnerGroup { get; set; } = null!;

        public string LearnerCode { get; set; } = string.Empty;
    }
}
