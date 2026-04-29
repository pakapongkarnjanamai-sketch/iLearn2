using iLearn.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    /// <summary>
    /// Hierarchical container that organizes <see cref="LearnerGroup"/> records
    /// into a tree structure. Categories do not own members or assignments.
    /// </summary>
    public class LearnerGroupCategory : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int? DivisionId { get; set; }
        [ForeignKey("DivisionId")]
        public Division? Division { get; set; }

        // ── Hierarchy (self-reference tree) ───────
        public int? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public LearnerGroupCategory? Parent { get; set; }
        public ICollection<LearnerGroupCategory> Children { get; set; } = new List<LearnerGroupCategory>();

        /// <summary>Materialized path of ancestor ids, e.g. "/12/45/" (root = "/").</summary>
        public string? Path { get; set; }

        /// <summary>0 for root, increments per level. Limited to MaxDepth in the service.</summary>
        public int Depth { get; set; }
        // ──────────────────────────────────────────

        public ICollection<LearnerGroup> LearnerGroups { get; set; } = new List<LearnerGroup>();
    }
}
