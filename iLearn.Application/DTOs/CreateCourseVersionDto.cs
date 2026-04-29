using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class CreateCourseVersionDto
    {
        public int CourseId { get; set; }
        public string Note { get; set; }
        public bool IsActive { get; set; }
        public CourseVersionLearnerPolicy LearnerPolicy { get; set; } = CourseVersionLearnerPolicy.NewLearnersOnly;

        // สำหรับรับ ID ของไฟล์เดิมที่เลือกจากในระบบ
        public List<int> ContentItemIds { get; set; } = new List<int>();

        // 🌟 เพิ่มบรรทัดนี้: สำหรับรับค่า Type (1=Learn, 2=Exam) ของแต่ละไฟล์ตามลำดับ
        public List<int> ContentTypeIds { get; set; } = new List<int>();

        // สำหรับรับไฟล์ SCORM ใหม่ที่อัปโหลดเข้ามา
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    }
}