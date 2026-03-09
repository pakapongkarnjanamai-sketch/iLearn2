namespace iLearn.User.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    public class ExternalStudentDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Section { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
    }
}
