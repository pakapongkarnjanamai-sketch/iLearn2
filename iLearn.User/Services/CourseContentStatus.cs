namespace iLearn.User.Services
{
    /// <summary>
    /// สถานะการ mount course static files ที่บันทึกไว้ตอน startup — ใช้โดย HealthController
    /// เพื่อแยกเคส "โฟลเดอร์หายตอนนี้" ออกจากเคส "โฟลเดอร์หายตอน startup ทำให้ middleware
    /// ไม่ถูก mount และต้อง restart แอปแม้โฟลเดอร์จะกลับมาแล้ว"
    /// </summary>
    public static class CourseContentStatus
    {
        public static bool MountedAtStartup { get; set; }
        public static string PhysicalPath { get; set; } = string.Empty;
        public static string RequestPath { get; set; } = string.Empty;
    }
}
