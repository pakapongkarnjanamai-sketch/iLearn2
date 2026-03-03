using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
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
        private readonly IGenericRepository<Assignment> _assignmentRepo;
        private readonly IStudentApiService _studentApiService;
        private readonly IGenericRepository<CourseVersion> _versionRepo;

        public CourseAssignmentService(
            ICourseRepository courseRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<Assignment> assignmentRepo,
            IStudentApiService studentApiService,
            IGenericRepository<CourseVersion> versionRepo)
        {
            _courseRepo = courseRepo;
            _enrollmentRepo = enrollmentRepo;
            _assignmentRepo = assignmentRepo;
            _studentApiService = studentApiService;
            _versionRepo = versionRepo;
        }

     
        public async Task AssignGeneralCoursesToNewUserAsync(string employeeId)
        {
            var activeCourses = await _courseRepo.GetActiveCoursesAsync();
            var generalCourses = activeCourses.Where(c => c.Type == CourseType.General);

            foreach (var course in generalCourses)
            {
                await CreateOrUpdateEnrollment(employeeId, course);
            }
        }

       
        public async Task AssignCourseToEmployees(int courseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, int? assignmentRuleId = null)
        {
            if (employeeCodes == null || !employeeCodes.Any()) return;

            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null || !course.IsActive) return;

            // วนลูปรายชื่อพนักงานที่ถูกเลือกมา แล้วสร้าง Enrollment
            foreach (var empCode in employeeCodes)
            {
                await CreateOrUpdateEnrollment(empCode, course, assignmentRuleId, startDate, dueDate);
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

        // --- 5. ฟังก์ชันบันทึกลงฐานข้อมูล (ปรับให้รับ ID กฎ และ วันที่ โดยตรง) ---
        private async Task CreateOrUpdateEnrollment(string studentCode, Course course, int? assignmentRuleId = null, DateTime? startDate = null, DateTime? dueDate = null)
        {
            // 1. ดึง Object ของ Version ที่ Active อยู่มาเลย เพื่อเอา Id
            var activeVersions = await _versionRepo.GetAsync(v => v.CourseId == course.Id && v.IsActive);
            var activeVersion = activeVersions.FirstOrDefault();

            if (activeVersion == null)
                return; // ป้องกัน Error กรณีไม่มีคอร์สไหน Active เลย

            var existingEnrollments = await _enrollmentRepo.GetAsync(e =>
                e.StudentCode == studentCode &&
                e.CourseId == course.Id);

            var existing = existingEnrollments.FirstOrDefault();

            if (existing == null)
            {
                var newEnrollment = new Enrollment
                {
                    StudentCode = studentCode,
                    CourseId = course.Id,
                    EnrolledCourseVersion = activeVersion.Id, // เก็บเป็น ID (Primary Key)
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    AssignmentRuleId = assignmentRuleId,
                    StartDate = startDate,
                    DueDate = dueDate
                };
                await _enrollmentRepo.AddAsync(newEnrollment);
            }
            else
            {
                // 2. ถ้ามีข้อมูลอยู่แล้ว เช็กว่า Version ID ที่เรียนอยู่ ไม่ตรงกับ Version ID ปัจจุบัน ใช่หรือไม่?
                if (existing.EnrolledCourseVersion != activeVersion.Id)
                {
                    existing.EnrolledCourseVersion = activeVersion.Id; // อัปเดตให้มาเรียน Version ล่าสุด
                    existing.IsCompleted = false;
                    existing.CompletedDate = null;
                    existing.AssignmentRuleId = assignmentRuleId;

                    // อัปเดตวันที่เฉพาะตอนที่มีการส่งค่าใหม่มาให้
                    existing.StartDate = startDate ?? existing.StartDate;
                    existing.DueDate = dueDate ?? existing.DueDate;

                    await _enrollmentRepo.UpdateAsync(existing);
                }
            }
        }
    }
}