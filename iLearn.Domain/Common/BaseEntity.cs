using System.ComponentModel.DataAnnotations;

namespace iLearn.Domain.Common
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [StringLength(100)]
        public string? CreatedBy { get; set; }
        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }
}