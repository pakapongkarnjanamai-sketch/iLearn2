using iLearn.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    /// <summary>
    /// Hierarchical container that organizes <see cref="StudentGroup"/> records
    /// into a tree structure. Categories do not own members or assignments.
    /// </summary>
    public class StudentGroupCategory : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int? DivisionId { get; set; }
        [ForeignKey("DivisionId")]
        public Division? Division { get; set; }

        // ── Hierarchy (self-reference tree) ───────
        public int? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public StudentGroupCategory? Parent { get; set; }
        public ICollection<StudentGroupCategory> Children { get; set; } = new List<StudentGroupCategory>();

        /// <summary>Materialized path of ancestor ids, e.g. "/12/45/" (root = "/").</summary>
        public string? Path { get; set; }

        /// <summary>0 for root, increments per level. Limited to MaxDepth in the service.</summary>
        public int Depth { get; set; }
        // ──────────────────────────────────────────

        public ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();
    }
}
