using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories; // ตรวจสอบ Namespace นี้ในโปรเจกต์ของคุณ
using iLearn.Domain.Entities; // ตรวจสอบ Namespace นี้ในโปรเจกต์ของคุณ
// using iLearn.Application.Interfaces.Services; // เปิดใช้ถ้ามี Service

namespace iLearn.Admin.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ILogger<CoursesController> _logger;
     
        public CoursesController(ILogger<CoursesController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Wizard()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // ทำงานคู่กับ header "RequestVerificationToken" ใน AJAX
        public async Task<IActionResult> Create([FromForm] CourseCreateDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // รวบรวม Error ทั้งหมดส่งกลับไป
                    var errors = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return BadRequest(new { message = errors });
                }

                // 1. TODO: Map DTO -> Entity และบันทึก Course ลงฐานข้อมูล
                // var course = new Course {
                //     CourseCode = model.CourseCode,
                //     CourseName = model.CourseName,
                //     Description = model.Description,
                //     CourseType = (CourseType)model.CourseType,
                //     CategoryId = model.CategoryId
                // };
                // await _courseRepo.AddAsync(course);

                // 2. จัดการไฟล์แนบ (Resources)
                if (model.Resources != null && model.Resources.Count > 0)
                {
                    foreach (var file in model.Resources)
                    {
                        if (file.Length > 0)
                        {
                            // TODO: เขียน Logic อัปโหลดไฟล์ (Save to Disk/Blob)
                            // var filePath = await _fileService.UploadAsync(file);

                            // TODO: บันทึกข้อมูล Resource ลง DB และผูกกับ Course
                            // var resource = new Resource { Name = file.FileName, FilePath = filePath ... };
                            // await _resourceRepo.AddAsync(resource);
                        }
                    }
                }

                // ส่ง JSON กลับไปเพื่อให้ AJAX ใน Create.cshtml ทำงานต่อ (Redirect)
                return Json(new { success = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return BadRequest(new { message = "เกิดข้อผิดพลาด: " + ex.Message });
            }
        }
    }
}