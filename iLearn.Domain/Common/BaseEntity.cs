using System.ComponentModel.DataAnnotations;

namespace iLearn.Domain.Common
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [StringLength(100)]
        public string? CreatedBy { get; set; }
        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // ── Soft Delete ──────────────────────────────────────────────
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        [StringLength(100)]
        public string? DeletedBy { get; set; }
    }
}