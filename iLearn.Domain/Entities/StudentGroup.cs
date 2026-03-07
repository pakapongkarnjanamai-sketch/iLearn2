using iLearn.Domain.Common;
using System.Collections.Generic;

namespace iLearn.Domain.Entities
{
    public class StudentGroup : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation
        public ICollection<StudentGroupMember> Members { get; set; } = new List<StudentGroupMember>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
