using iLearn.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    public class StudentGroup : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

  
        public int? DivisionId { get; set; }
        [ForeignKey("DivisionId")]
        public Division? Division { get; set; }

        /// <summary>Optional category (folder) that this group lives under.</summary>
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public StudentGroupCategory? Category { get; set; }

        public ICollection<StudentGroupMember> Members { get; set; } = new List<StudentGroupMember>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}