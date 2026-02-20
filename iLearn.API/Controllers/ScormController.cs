//using iLearn.Domain.Common;
//using iLearn.Domain.Entities;
//using iLearn.Infrastructure.Persistence;
//using iLearn.Application.Services; // สำหรับ ICurrentUserService
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace iLearn.API.Controllers
//{
//    [Authorize]
//    [Route("api/[controller]")]
//    [ApiController]
//    public class ScormController : ControllerBase
//    {
//        private readonly AppDbContext _context;
//        private readonly ICurrentUserService _currentUser;
//        private readonly IDateTime _dateTime;

//        public ScormController(AppDbContext context, ICurrentUserService currentUser, IDateTime dateTime)
//        {
//            _context = context;
//            _currentUser = currentUser;
//            _dateTime = dateTime;
//        }

//        // 1. Initialize: โหลดข้อมูลการเรียนล่าสุดเมื่อเปิดหน้าต่าง SCORM
//        [HttpGet("initialize")]
//        public async Task<IActionResult> Initialize(int courseVersionId, int resourceId)
//        {
//            var studentCode = _currentUser.UserId; // หรือดึงจาก StudentCode ถ้าใช้ระบบนั้น

//            // ค้นหา Log เก่า
//            var log = await _context.LearningLogs
//                .FirstOrDefaultAsync(l => l.StudentCode == studentCode.ToString() && // แปลงตาม Type จริงของคุณ
//                                          l.CourseVersionId == courseVersionId &&
//                                          l.ResourceId == resourceId);

//            if (log == null)
//            {
//                // ถ้าไม่มี สร้างใหม่ (First Launch)
//                log = new LearningLog
//                {
//                    StudentCode = studentCode.ToString(),
//                    CourseVersionId = courseVersionId,
//                    ResourceId = resourceId,
//                    LessonStatus = "not attempted",
//                    ScoreRaw = 0,
//                    LessonLocation = "",
//                    SuspendData = "",
//                    TotalTime = "00:00:00",
//                    AttemptCount = 1,
//                    CreatedAt = _dateTime.Now
//                };

//                _context.LearningLogs.Add(log);
//                await _context.SaveChangesAsync();
//            }
//            else
//            {
//                // ถ้ามีแล้ว เพิ่มรอบการเข้าชม (Optional)
//                log.AttemptCount++;
//                log.LastAccessDate = _dateTime.Now;
//                await _context.SaveChangesAsync();
//            }

//            // ส่งข้อมูล CMI Data กลับไปให้ JavaScript Adapter
//            return Ok(new ApiResponse<object>
//            {
//                Success = true,
//                Data = new
//                {
//                    cmi_core_lesson_status = log.LessonStatus,
//                    cmi_core_lesson_location = log.LessonLocation,
//                    cmi_suspend_data = log.SuspendData,
//                    cmi_core_score_raw = log.ScoreRaw,
//                    cmi_core_total_time = log.TotalTime,
//                    // StudentDto Info (SCORM บังคับส่งให้ Content)
//                    cmi_core_student_id = studentCode,
//                    cmi_core_student_name = "StudentDto Name" // ควรดึงชื่อจริงจาก User Table
//                }
//            });
//        }

//        // 2. Commit: บันทึกค่าที่ SCORM ส่งมา (ใช้ Commit ทีเดียวเพื่อลด Request)
//        [HttpPost("commit")]
//        public async Task<IActionResult> Commit([FromBody] ScormCommitRequest request)
//        {
//            var studentCode = _currentUser.UserId;

//            var log = await _context.LearningLogs
//                .FirstOrDefaultAsync(l => l.StudentCode == studentCode.ToString() &&
//                                          l.CourseVersionId == request.CourseVersionId &&
//                                          l.ResourceId == request.ResourceId);

//            if (log == null) return NotFound(new ApiResponse<string> { Success = false, Message = "Log not found" });

//            // อัปเดตค่าตามที่ SCORM ส่งมา
//            if (!string.IsNullOrEmpty(request.LessonStatus))
//                log.LessonStatus = request.LessonStatus;

//            if (!string.IsNullOrEmpty(request.LessonLocation))
//                log.LessonLocation = request.LessonLocation;

//            if (!string.IsNullOrEmpty(request.SuspendData))
//                log.SuspendData = request.SuspendData;

//            if (request.ScoreRaw.HasValue)
//                log.ScoreRaw = request.ScoreRaw.Value;

//            if (!string.IsNullOrEmpty(request.SessionTime))
//            {
//                // SCORM ส่งเวลา Session มา ต้องเอาไปบวกกับ TotalTime เดิม (Logic ซับซ้อนเล็กน้อยเรื่อง Time Format)
//                // เพื่อความง่าย ในตัวอย่างนี้ขอเซฟทับหรือเก็บแยกตาม Business Logic ของคุณ
//                // log.TotalTime = AddScormTime(log.TotalTime, request.SessionTime); 
//            }

//            log.LastAccessDate = _dateTime.Now;

//            // ตรวจสอบว่าจบการศึกษาหรือยัง
//            if (log.LessonStatus == "completed" || log.LessonStatus == "passed")
//            {
//                log.IsFinalized = true;
//                log.CompletedDate ??= _dateTime.Now; // บันทึกวันที่จบครั้งแรก
//            }

//            await _context.SaveChangesAsync();

//            return Ok(new ApiResponse<string> { Success = true, Message = "Saved successfully" });
//        }
//    }

//    // DTO สำหรับรับค่าจาก Client (JavaScript)
//    public class ScormCommitRequest
//    {
//        public int CourseVersionId { get; set; }
//        public int ResourceId { get; set; }
//        public string? LessonStatus { get; set; }   // cmi.core.lesson_status
//        public string? LessonLocation { get; set; } // cmi.core.lesson_location
//        public string? SuspendData { get; set; }    // cmi.suspend_data
//        public decimal? ScoreRaw { get; set; }      // cmi.core.score.raw
//        public string? SessionTime { get; set; }    // cmi.core.session_time
//    }
//}