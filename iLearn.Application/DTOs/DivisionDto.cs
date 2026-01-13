namespace iLearn.Application.DTOs
{
    // --- Division ---
    public class DivisionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // --- Role ---
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? DivisionId { get; set; }
        public string? DivisionName { get; set; } = string.Empty;
    }

    // --- Category ---
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? DivisionId { get; set; }
    }

    // --- Assignment Rule (สำคัญมาก!) ---
    public class AssignmentRuleDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }

        // เงื่อนไข
        public int? DivisionId { get; set; }
        public string? DivisionName { get; set; }

        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
    }

    public class CreateAssignmentRuleDto
    {
        public int CourseId { get; set; }
        public int? DivisionId { get; set; }
        public int? RoleId { get; set; }
    }
}