using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    public class CourseWizardDto
    {
        // --- Course Info ---
        public int? CourseId { get; set; } // ถ้าเป็น null = สร้างใหม่, ถ้ามีค่า = อัปเดตเวอร์ชัน
        public string Code { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public CourseType Type { get; set; }

        // --- Version Info ---
        public string VersionNote { get; set; }

        // --- Resources ---
        public List<int> ResourceIds { get; set; } = new List<int>();

        // --- Rules (สำหรับ Type = Special) ---
        // รับเป็น List ของ IDs หรือ Object ตามความซับซ้อน (ในที่นี้สมมติรับเป็น RoleIds/DivisionIds แบบง่าย)
        public List<CreateAssignmentRuleDto> Rules { get; set; } = new List<CreateAssignmentRuleDto>();
    }
}



