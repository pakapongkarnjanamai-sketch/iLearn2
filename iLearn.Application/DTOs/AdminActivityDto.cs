namespace iLearn.Application.DTOs
{
    public class AdminActivityDto
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DivisionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }
}
