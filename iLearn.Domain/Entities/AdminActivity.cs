using iLearn.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace iLearn.Domain.Entities
{
    public class AdminActivity : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? DivisionId { get; set; }

        public string? DataJson { get; set; }
    }
}
