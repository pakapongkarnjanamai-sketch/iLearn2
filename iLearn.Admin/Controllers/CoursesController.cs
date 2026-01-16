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
        
       

    }
}