using iLearn.Domain.Enums;

namespace iLearn.Application.DTOs
{
    // ใช้แสดงผล (Response)
    public class CourseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        // แปลง Enum เป็น String ให้ Frontend อ่านง่าย
        public string TypeName { get; set; } = string.Empty;
        public CourseType Type { get; set; }

        public int Version { get; set; }
    }

    // ใช้สร้าง/แก้ไข (Request)
    public class CreateCourseDto
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CourseType Type { get; set; }
    }
}