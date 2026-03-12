using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class CourseAssignmentService : ICourseAssignmentService
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepo;
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IStudentApiService _studentApiService;
        private readonly IGenericRepository<CourseVersion> _versionRepo;
        private readonly IDateTime _dateTime;

        public CourseAssignmentService(
            ICourseRepository courseRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            IGenericRepository<Assignment> assignmentRepo,
            IStudentApiService studentApiService,
            IGenericRepository<CourseVersion> versionRepo,
            IDateTime dateTime)
        {
            _courseRepo = courseRepo;
            _enrollmentRepo = enrollmentRepo;
            _enrollmentAssignmentRepo = enrollmentAssignmentRepo;
            _assignmentRepo = assignmentRepo;
            _studentApiService = studentApiService;
            _versionRepo = versionRepo;
            _dateTime = dateTime;
        }

     
        public async Task AssignGeneralCoursesToNewUserAsync(string employeeId)
        {
            var activeCourses = await _courseRepo.GetActiveCoursesAsync();
            var generalCourses = activeCourses.Where(c => c.CourseType != null && c.CourseType.Name == "General");

            foreach (var course in generalCourses)
            {
                await CreateOrUpdateEnrollment(employeeId, course);
            }
        }

       
        public async Task AssignCourseToEmployees(int courseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, int? assignmentRuleId = null, bool forceReset = false)
        {
            if (employeeCodes == null || !employeeCodes.Any()) return;

            if (startDate.HasValue && dueDate.HasValue && startDate.Value > dueDate.Value)
                throw new ArgumentException("StartDate ต้องไม่มากกว่า DueDate");

            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null || !course.IsActive) return;

            // วนลูปรายชื่อพนักงานที่ถูกเลือกมา แล้วสร้าง Enrollment
            foreach (var empCode in employeeCodes)
            {
                await CreateOrUpdateEnrollment(empCode, course, assignmentRuleId, startDate, dueDate, forceReset);
            }
        }

        // --- Helper Logic ---
        private int GetCurrentActiveVersion(Course course)
        {
            if (course.Versions == null || !course.Versions.Any()) return 1;

            return course.Versions
                .Where(v => v.IsActive)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => v.VersionNumber)
                .FirstOrDefault();
        }

        // --- ฟังก์ชันบันทึกลงฐานข้อมูล ---
        private async Task CreateOrUpdateEnrollment(string studentCode, Course course, int? assignmentRuleId = null, DateTime? startDate = null, DateTime? dueDate = null, bool forceReset = false)
        {
            // 1. หา Active Version
            var activeVersions = await _versionRepo.GetAsync(v => v.CourseId == course.Id && v.IsActive);
            var activeVersion = activeVersions.FirstOrDefault();
            if (activeVersion == null) return;

            // 2. หา Enrollment เดิมของ Student+Course (1 row เท่านั้น)
            var existingEnrollments = await _enrollmentRepo.GetAsync(e =>
                e.StudentCode == studentCode &&
                e.CourseId    == course.Id);

            var existing = existingEnrollments.FirstOrDefault();

            if (existing == null)
            {
                // ยังไม่มี Enrollment → สร้างใหม่
                existing = new Enrollment
                {
                    StudentCode           = studentCode,
                    CourseId              = course.Id,
                    EnrolledCourseVersion = activeVersion.Id,
                    IsCompleted           = false,
                    StartDate             = startDate,
                    DueDate               = dueDate
                };
                await _enrollmentRepo.AddAsync(existing);
            }
            else if (forceReset || existing.EnrolledCourseVersion != activeVersion.Id)
            {
                // ── Snapshot สถานะปัจจุบันไปที่ EnrollmentAssignment links ก่อน reset ──
                // เพื่อให้ Assignment เดิมยังคงเห็นว่าเรียนจบแล้ว แม้ Enrollment จะถูก reset
                if (existing.IsCompleted)
                {
                    var existingEaLinks = await _enrollmentAssignmentRepo.GetAsync(
                        ea => ea.EnrollmentId == existing.Id);
                    foreach (var eaLink in existingEaLinks)
                    {
                        eaLink.SnapshotCompleted     = existing.IsCompleted;
                        eaLink.SnapshotCompletedDate = existing.CompletedDate;
                        eaLink.SnapshotProgress      = existing.Progress;
                        await _enrollmentAssignmentRepo.UpdateAsync(eaLink);
                    }
                }

                // ตั้ง ResetAt เพื่อให้ player-info กรอง Log เก่าออก (Log ยังอยู่ใน DB เพื่อเก็บ history)
                existing.ResetAt               = _dateTime.Now;
                existing.EnrolledCourseVersion = activeVersion.Id;
                existing.IsCompleted           = false;
                existing.CompletedDate         = null;
                existing.Progress              = 0;
                existing.TotalScore            = 0;
                existing.StartDate             = startDate ?? existing.StartDate;
                existing.DueDate               = dueDate   ?? existing.DueDate;
                await _enrollmentRepo.UpdateAsync(existing);
            }

            // 3. เชื่อม EnrollmentAssignment ถ้ามี assignmentRuleId
            if (assignmentRuleId.HasValue)
            {
                var linkRepo = _enrollmentAssignmentRepo;
                var existingLinks = await linkRepo.GetAsync(ea =>
                    ea.EnrollmentId == existing.Id &&
                    ea.AssignmentId == assignmentRuleId.Value);

                if (!existingLinks.Any())
                {
                    await linkRepo.AddAsync(new EnrollmentAssignment
                    {
                        EnrollmentId = existing.Id,
                        AssignmentId = assignmentRuleId.Value,
                        StartDate    = startDate,
                        DueDate      = dueDate
                    });
                }
                else
                {
                    // อัปเดต dates ถ้า link มีอยู่แล้ว (กรณี Assign ซ้ำ)
                    var link = existingLinks.First();
                    link.StartDate = startDate ?? link.StartDate;
                    link.DueDate   = dueDate   ?? link.DueDate;
                    await linkRepo.UpdateAsync(link);
                }
            }
        }

        public async Task<List<AssignmentHistoryDto>> GetAssignmentHistoryAsync()
        {
            var assignments = await _assignmentRepo.GetAsync(includeProperties: "Course");

            // ดึง links ทั้งหมดผ่าน EnrollmentAssignment
            var links = await _enrollmentAssignmentRepo.GetAsync(
                filter: null,
                includeProperties: "Enrollment"
            );

            var currentDate = _dateTime.Now;

            var groupedHistory = assignments
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo))
                .GroupBy(r => r.AssignmentNo)
                .Select(g =>
                {
                    var first         = g.First();
                    var assignmentIds = g.Select(a => a.Id).ToList();

                    var relatedLinks = links
                        .Where(ea => assignmentIds.Contains(ea.AssignmentId) && ea.Enrollment != null)
                        .ToList();

                    bool isCompleted = relatedLinks.Any()
                        && relatedLinks.All(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);

                    string status = AssignmentDashboardService.CalculateStatus(
                        relatedLinks.Any(), isCompleted, first.StartDate, first.DueDate, currentDate);

                    return new AssignmentHistoryDto
                    {
                        Id           = first.Id,
                        AssignmentNo = g.Key,
                        Description  = first.Description,
                        EmployeeCodes = first.EmployeeCodes,
                        StartDate    = first.StartDate,
                        DueDate      = first.DueDate,
                        CourseNames  = string.Join(", ", g.Select(c => c.Course?.Title ?? "Unknown Course").Distinct()),
                        Status       = status,
                        CreatedBy    = first.CreatedBy,
                        CreatedAt    = first.CreatedAt,
                        CourseCount  = g.Select(a => a.CourseId).Distinct().Count(),
                        StudentCount = string.IsNullOrEmpty(first.EmployeeCodes)
                            ? 0
                            : first.EmployeeCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
                        CompletedEnrollmentCount = relatedLinks.Count(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted),
                        TotalEnrollmentCount     = relatedLinks.Count
                    };
                })
                .OrderByDescending(x => x.AssignmentNo)
                .ToList();

            return groupedHistory;
        }

        public async Task<AssignmentConflictDto> CheckAssignmentConflictsAsync(int courseId, List<string> employeeCodes, DateTime startDate, DateTime dueDate)
        {
            var result = new AssignmentConflictDto();

            // Step 1: ตรวจสอบว่าคอร์สนี้มี Active Version หรือไม่
            var activeVersions = await _versionRepo.GetAsync(v => v.CourseId == courseId && v.IsActive);
            var activeVersion = activeVersions.FirstOrDefault();

            if (activeVersion == null)
            {
                result.HasConflict = true;
                result.ConflictMessages.Add("ไม่พบเวอร์ชันที่เปิดใช้งาน (Active Version) สำหรับคอร์สนี้ ไม่สามารถมอบหมายงานได้");
                return result;
            }

            // Step 2: ดึง Enrollment ของพนักงานทั้งหมดในคอร์สนี้
            var enrollments = await _enrollmentRepo.GetAsync(e =>
                e.CourseId == courseId &&
                employeeCodes.Contains(e.StudentCode));

            // Step 3: วนตรวจสอบแต่ละพนักงาน
            foreach (var empCode in employeeCodes)
            {
                var enrollment = enrollments.FirstOrDefault(e => e.StudentCode == empCode);

                if (enrollment != null)
                {
                    // Rule A: ตรวจสอบว่าเรียนจบแล้วหรือยัง
                    if (enrollment.IsCompleted)
                    {
                        if (enrollment.EnrolledCourseVersion == activeVersion.Id)
                        {
                            // เรียนจบเวอร์ชันล่าสุดแล้ว → Conflict
                            result.HasConflict = true;
                            result.ConflictMessages.Add($"พนักงานรหัส {empCode} ได้เรียนจบคอร์สนี้ (เวอร์ชันล่าสุด) ไปแล้ว");
                            continue;
                        }
                        // เรียนจบเวอร์ชันเก่า → ไม่ติด Conflict อนุญาตให้ Assign เวอร์ชันใหม่ได้
                    }
                    else
                    {
                        // Rule B: ตรวจสอบช่วงเวลาทับซ้อน (เฉพาะที่ยังไม่จบ)
                        if (enrollment.StartDate.HasValue && enrollment.DueDate.HasValue &&
                            startDate <= enrollment.DueDate.Value && dueDate >= enrollment.StartDate.Value)
                        {
                            result.HasConflict = true;
                            result.ConflictMessages.Add(
                                $"พนักงานรหัส {empCode} อยู่ในระหว่างการเรียนคอร์สนี้อยู่แล้ว " +
                                $"(มีกำหนดส่ง {enrollment.DueDate.Value:dd/MM/yyyy}) " +
                                $"ระบบไม่สามารถมอบหมายงานช่วงเวลาที่ทับซ้อนกันได้");
                            continue;
                        }
                    }
                }

                // ผ่านทุกเงื่อนไข → เพิ่มเข้า ValidEmployeeCodes
                result.ValidEmployeeCodes.Add(empCode);
            }

            return result;
        }
    }
}