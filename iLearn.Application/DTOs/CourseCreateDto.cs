using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public int CourseType { get; set; } // ควร map กับ Enum CourseType

        [Display(Name = "หมวดหมู่")]
        public int? CategoryId { get; set; }

        // ส่วนสำคัญ: สำหรับอัปโหลดไฟล์
        [Display(Name = "เอกสารประกอบ (Resources)")]
        public List<IFormFile> Resources { get; set; }
    }
}
