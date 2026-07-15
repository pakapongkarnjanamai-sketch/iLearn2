using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string RecipientUserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? LinkPath { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
