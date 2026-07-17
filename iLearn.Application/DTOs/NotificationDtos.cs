namespace iLearn.Application.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? LinkPath { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationListDto
    {
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
        public List<NotificationDto> Items { get; set; } = new();
    }
}
