using iLearn.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    public class LearnerGroup : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

  
        public int? DivisionId { get; set; }
        [ForeignKey("DivisionId")]
        public Division? Division { get; set; }

        /// <summary>Optional category (folder) that this group lives under.</summary>
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public LearnerGroupCategory? Category { get; set; }

        public ICollection<LearnerGroupMember> Members { get; set; } = new List<LearnerGroupMember>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}