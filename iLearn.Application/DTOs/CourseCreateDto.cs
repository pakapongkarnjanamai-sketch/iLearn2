using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    public class CourseCreateDto
    {
        [Display(Name = "รหัสวิชา")]
        [Required(ErrorMessage = "กรุณาระบุรหัสวิชา")]
        public string CourseCode { get; set; }

        [Display(Name = "ชื่อวิชา")]
        [Required(ErrorMessage = "กรุณาระบุชื่อวิชา")]
        public string CourseName { get; set; }

        [Display(Name = "รายละเอียด")]
        public string Description { get; set; }

        [Display(Name = "ประเภทหลักสูตร")]
        public int CourseType { get; set; }

        [Display(Name = "หมวดหมู่")]
        public int? CategoryId { get; set; }

        // [แก้ไข] เปลี่ยนจาก List<IFormFile> เป็น List<int> เพื่อรับ ID
        [Display(Name = "เอกสารประกอบ (Resource IDs)")]
        public List<int> ResourceIds { get; set; } = new List<int>();
    }
}