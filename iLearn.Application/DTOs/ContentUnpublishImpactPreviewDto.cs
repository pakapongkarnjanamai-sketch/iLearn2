namespace iLearn.Application.DTOs
{
    public class ContentUnpublishImpactPreviewDto
    {
        public int RequestedCount { get; set; }
        public int EligibleCount { get; set; }
        public int BlockedCount { get; set; }
        public List<int> EligibleIds { get; set; } = new();
        public List<ContentUnpublishImpactItemDto> Items { get; set; } = new();
    }

    public class ContentUnpublishImpactItemDto
    {
        public int ContentItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool CanUnpublish { get; set; }
        public string? BlockingReason { get; set; }
        public List<string> LinkedCourseCodes { get; set; } = new();
    }
}