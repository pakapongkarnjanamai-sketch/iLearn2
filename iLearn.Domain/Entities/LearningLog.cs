//using iLearn.Domain.Common;

//namespace iLearn.Domain.Entities
//{
//    public class LearningLog : BaseEntity
//    {

//        public string StudentCode { get; set; } = string.Empty;

//        public int CourseId { get; set; }
//        public int ContentId { get; set; }
//        public int QuestionId { get; set; }


//        public string? LearnTime { get; set; }
//        public string? ExamTime { get; set; }

//    }
//}

using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class LearningLog : BaseEntity
    {
        // --- Identity ---
        public string StudentCode { get; set; } = string.Empty; // Key หลักเชื่อมโยงผู้เรียน
        public int CourseId { get; set; }
        public int CourseVersionId { get; set; } // *สำคัญ* ต้องเก็บ Version ด้วย เพราะถ้าออก Course ใหม่ ต้องเริ่มเก็บ Log ใหม่
        public int ResourceId { get; set; }      // ระบุว่าเป็น SCORM ตัวไหน (บทเรียน หรือ ข้อสอบ)

        // --- SCORM CMI Data (หัวใจสำคัญ) ---

        // 1. สถานะ (cmi.core.lesson_status)
        // ค่าที่เป็นไปได้: "passed", "completed", "failed", "incomplete", "browsed", "not attempted"
        public string LessonStatus { get; set; } = "not attempted";

        // 2. คะแนน (cmi.core.score.raw)
        // จำเป็นมากสำหรับ "Type 2: ข้อสอบ"
        public decimal? ScoreRaw { get; set; }
        public decimal? ScoreMax { get; set; } // คะแนนเต็ม
        public decimal? ScoreMin { get; set; } // คะแนนผ่าน

        // 3. เวลาที่ใช้ (cmi.core.total_time & cmi.core.session_time)
        // SCORM ส่งมาเป็น format "00:00:00.00"
        public string TotalTime { get; set; } = "00:00:00";

        // 4. จุดที่เรียนถึง (Bookmark)
        // cmi.core.lesson_location: บอกว่าอยู่สไลด์หน้าไหน
        public string? LessonLocation { get; set; }

        // cmi.suspend_data: ข้อมูลก้อนใหญ่ที่ SCORM ฝากไว้ (เช่น ตัวเลือกที่ตอบไปแล้ว)
        // *ต้องเผื่อ Size เยอะๆ* เช่น nvarchar(MAX)
        public string? SuspendData { get; set; }

        // --- Metadata เพิ่มเติมของเราเอง ---
        public int AttemptCount { get; set; } = 0; // จำนวนครั้งที่เข้าสอบ/เรียน
        public DateTime? LastAccessDate { get; set; }
        public bool IsFinalized { get; set; } = false; // จบการเรียนรู้แล้วหรือยัง
        public DateTime? CompletedDate { get; set; }
    }
}