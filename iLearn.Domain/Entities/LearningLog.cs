using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class LearningLog : BaseEntity
    {
        // --- 1. Identity (ระบุตัวตนและเนื้อหา) ---
        public string StudentCode { get; set; } = string.Empty;

        // ใช้ VersionId เป็นหลัก เพื่อให้รู้ว่าเรียนเนื้อหาเวอร์ชันไหน
        public int CourseVersionId { get; set; }

        // อ้างอิงว่าเป็น SCORM/Video/Exam ตัวไหน
        public int ResourceId { get; set; }

        // --- 2. Result Data (ผลลัพธ์) ---

        // สถานะ: ส่วนใหญ่จะเป็น "completed" หรือ "passed" เพราะเรากรองมาแล้ว
        public string Status { get; set; } = "completed";

        // ความคืบหน้า: จะเป็น 100.0 เสมอ (ตามเงื่อนไขของคุณ)
        public double Progress { get; set; } = 100.0;

        // คะแนน: เก็บเฉพาะตัวเลขสุทธิ (ถ้ามี)
        public int? Score { get; set; }

        // --- 3. Time (เวลา) ---

        // เวลาที่ใช้เรียน (เช่น "00:45:00")
        public string? SessionTime { get; set; }
        // [เพิ่ม] เก็บเวลาสะสม (วินาที) เพื่อนำไปรวมยอดได้ง่ายขึ้น
        public int TotalSecondsPlayed { get; set; }

        // [เพิ่ม] นับจำนวนครั้งที่เข้าเรียน
        public int AttemptCount { get; set; }

    }
}