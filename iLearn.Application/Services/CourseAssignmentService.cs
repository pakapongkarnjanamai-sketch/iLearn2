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
        private readonly IGenericRepository<Assignment> _ruleRepo;
        private readonly IStudentApiService _studentApiService;

        public CourseAssignmentService(
            ICourseRepository courseRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<Assignment> ruleRepo,
            IStudentApiService studentApiService)
        {
            _courseRepo = courseRepo;
            _enrollmentRepo = enrollmentRepo;
            _ruleRepo = ruleRepo;
            _studentApiService = studentApiService;
        }

        // --- 1. ฟังก์ชันจับคู่กฎ ---
        private Assignment? GetMatchingRuleForStudent(StudentDto student, IReadOnlyList<Assignment> rules)
        {
            if (rules == null || !rules.Any()) return null;

            foreach (var rule in rules)
            {
                bool divisionMatch = string.IsNullOrEmpty(rule.Division) || student.Division == rule.Division;
                if (divisionMatch)
                {
                    return rule;
                }
            }
            return null;
        }

        // --- 2. กรณีพนักงานใหม่เข้ามา ---
        public async Task AssignGeneralCoursesToNewUserAsync(string employeeId)
        {
            var activeCourses = await _courseRepo.GetActiveCoursesAsync();
            var generalCourses = activeCourses.Where(c => c.Type == CourseType.General);

            foreach (var course in generalCourses)
            {
                await CreateOrUpdateEnrollment(employeeId, course);
            }
        }

        // --- 3. กรณี Admin กด Assign หรือสร้างคอร์สใหม่ ---
        public async Task ProcessAssignmentForCourseAsync(int courseId)
        {
            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null || !course.IsActive) return;

            var apiResponse = await _studentApiService.GetStudentAsync();
            if (apiResponse == null || !apiResponse.success || apiResponse.data == null) return;

            var allStudents = apiResponse.data;
            var rules = await _ruleRepo.GetAsync(r => r.CourseId == courseId);

            foreach (var student in allStudents)
            {
                Assignment? matchedRule = null;
                bool shouldAssign = false;

                if (course.Type == CourseType.General)
                {
                    shouldAssign = true;
                }
                else if (course.Type == CourseType.Special)
                {
                    matchedRule = GetMatchingRuleForStudent(student, rules);
                    shouldAssign = matchedRule != null;
                }

                if (shouldAssign)
                {
                    // ส่งข้อมูล ID และ Date แยกส่วนเข้าไป เพื่อให้ Helper method รับค่าง่ายขึ้น
                    await CreateOrUpdateEnrollment(student.EId, course, matchedRule?.Id, matchedRule?.StartDate, matchedRule?.DueDate);
                }
            }
        }

        // --- [NEW] 4. ฟังก์ชันสำหรับการ Assign จากหน้าจอมอบหมายงานโดยตรง ---
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
            int currentVersion = GetCurrentActiveVersion(course);

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
                    EnrolledCourseVersion = currentVersion,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    AssignmentRuleId = assignmentRuleId, // <--- เชื่อม Rule ID
                    StartDate = startDate,               // <--- กำหนด StartDate
                    DueDate = dueDate                    // <--- กำหนด DueDate
                };
                await _enrollmentRepo.AddAsync(newEnrollment);
            }
            else if (existing.EnrolledCourseVersion < currentVersion)
            {
                existing.EnrolledCourseVersion = currentVersion;
                existing.IsCompleted = false;
                existing.CompletedDate = null;
                existing.AssignmentRuleId = assignmentRuleId;
                existing.StartDate = startDate;
                existing.DueDate = dueDate;

                await _enrollmentRepo.UpdateAsync(existing);
            }
        }
    }
}