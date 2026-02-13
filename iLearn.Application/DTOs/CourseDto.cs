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

        public string TypeName { get; set; } = string.Empty;
        public CourseType Type { get; set; }

        // [เพิ่มใหม่] เพื่อให้ Frontend นำไป Group ได้ง่าย
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "Uncategorized"; // ค่า Default

        public int Version { get; set; }

        // [เพิ่มใหม่] ถ้าต้องการส่ง URL รูปภาพปก (ถ้ามีในอนาคต)
        public string? CoverImageUrl { get; set; }
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