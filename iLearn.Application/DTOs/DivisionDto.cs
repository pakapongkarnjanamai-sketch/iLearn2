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
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }

    // --- Create Assignment Rule (ใช้สำหรับรับค่าตอนกด Save/Add New Rule) ---
    public class CreateAssignmentRuleDto
    {
        public int CourseId { get; set; }

        // เปลี่ยนจาก ID เป็น String เพื่อให้ตรงกับ Entity ใหม่ที่คุณต้องการ
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }

        // เพิ่มวันที่เพื่อให้ Admin วางแผนล่วงหน้าได้ตามที่คุยกัน
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}