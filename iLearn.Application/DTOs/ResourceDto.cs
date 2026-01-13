namespace iLearn.Application.DTOs
{
    public class ResourceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TypeId { get; set; } // 1=Learn, 2=Exam
        public bool IsActive { get; set; }

        // เราจะไม่ส่ง byte[] กลับไปใน DTO นี้ (เพราะมันใหญ่)
        // แต่จะส่ง URL หรือ Path ให้ Frontend เรียกแทน
        public string? ContentUrl { get; set; }
    }

    // DTO สำหรับการสร้าง (Upload)
    // หมายเหตุ: การรับไฟล์จริงจะทำผ่าน IFormFile ใน Controller โดยตรง
    // หรือจะใส่ใน Class นี้ก็ได้ แต่ต้องระวังเรื่อง Binding
    public class CreateResourceDto
    {
        public string Name { get; set; } = string.Empty;
        public int TypeId { get; set; }
    }
}