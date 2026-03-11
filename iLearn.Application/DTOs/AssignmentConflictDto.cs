namespace iLearn.Application.DTOs
{
    public class AssignmentConflictDto
    {
        public bool HasConflict { get; set; }
        public List<string> ValidEmployeeCodes { get; set; } = new List<string>();
        public List<string> ConflictMessages { get; set; } = new List<string>();
    }
}
