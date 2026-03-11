using Microsoft.AspNetCore.Http; // จำเป็นสำหรับ IFormFile
using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    public class CourseCreateDto
    {
        [Display(Name = "รหัสวิชา")]
        [Required(ErrorMessage = "กรุณาระบุรหัสวิชา")]
        public string CourseCode { get; set; } // ใช้ชื่อนี้ตามที่คุณต้องการ

        [Display(Name = "ชื่อวิชา")]
        [Required(ErrorMessage = "กรุณาระบุชื่อวิชา")]
        public string CourseName { get; set; } // ใช้ชื่อนี้ตามที่คุณต้องการ

        [Display(Name = "รายละเอียด")]
        public string? Description { get; set; }

        [Display(Name = "ประเภทหลักสูตร")]
        public int CourseType { get; set; }

        [Display(Name = "หมวดหมู่")]
        public int CategoryId { get; set; }

        [Display(Name = "เอกสารประกอบ (Resource IDs)")]
        public List<int>? ResourceIds { get; set; }

        public List<IFormFile>? Files { get; set; }
    }
}