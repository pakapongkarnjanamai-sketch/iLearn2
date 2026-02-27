using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLearn.Application.DTOs
{
    public class CreateCourseVersionDto
    {
        public int CourseId { get; set; }
        public string Note { get; set; }
        public bool IsActive { get; set; }

        // สำหรับรับ ID ของไฟล์เดิมที่เลือกจากในระบบ
        public List<int> ResourceIds { get; set; } = new List<int>();

        // สำหรับรับไฟล์ SCORM ใหม่ที่อัปโหลดเข้ามา
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    }
}
