using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class StudentGroupMember : BaseEntity
    {
        public int StudentGroupId { get; set; }
        public StudentGroup StudentGroup { get; set; } = null!;

        // StudentCode ???????????????????? (????? FK ? User)
        public string StudentCode { get; set; } = string.Empty;
    }
}
