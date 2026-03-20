// File: iLearn.Application/DTOs/BulkOperationResultDto.cs

namespace iLearn.Application.DTOs
{
    public class BulkOperationResultDto
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public TimeSpan Duration { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<BulkOperationItemDto> Results { get; set; } = new();
    }

    public class BulkOperationItemDto
    {
        public int ResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Details { get; set; }
    }

    // เพิ่มใน DTOs
    public class BulkOperationProgressDto
    {
        public int CurrentItem { get; set; }
        public int TotalItems { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public string? CurrentResourceName { get; set; }
        public string? CurrentStep { get; set; }
        public bool IsComplete { get; set; }
        public BulkOperationItemDto? LatestResult { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}