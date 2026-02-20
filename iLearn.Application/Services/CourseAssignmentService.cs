using iLearn.Application.DTOs; // ดึง DTO ของฝั่ง API มาใช้
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
        private readonly IGenericRepository<AssignmentRule> _ruleRepo;
        private readonly IStudentApiService _studentApiService; // <--- นำ API Service เข้ามาแทน UserRepo

        public CourseAssignmentService(
            ICourseRepository courseRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<AssignmentRule> ruleRepo,
            IStudentApiService studentApiService)
        {
            _courseRepo = courseRepo;
            _enrollmentRepo = enrollmentRepo;
            _ruleRepo = ruleRepo;
            _studentApiService = studentApiService;
        }

        // --- 1. ฟังก์ชันจับคู่กฎ (เปลี่ยนมารับ StudentDto จาก API แทน) ---
        private AssignmentRule? GetMatchingRuleForStudent(StudentDto student, IReadOnlyList<AssignmentRule> rules)
        {
            if (rules == null || !rules.Any()) return null;

            foreach (var rule in rules)
            {
                // นำโครงสร้างองค์กรจาก API (student) มาเปรียบเทียบกับกฎ
                bool divisionMatch = string.IsNullOrEmpty(rule.Division) || student.Division == rule.Division;
                bool departmentMatch = string.IsNullOrEmpty(rule.Department) || student.Department == rule.Department;
                bool sectionMatch = string.IsNullOrEmpty(rule.Section) || student.Section == rule.Section;
                bool positionMatch = string.IsNullOrEmpty(rule.Position) || student.Position == rule.Position;

                if (divisionMatch && departmentMatch && sectionMatch && positionMatch)
                {
                    return rule;
                }
            }
            return null;
        }

        // --- 2. กรณีพนักงานใหม่เข้ามา -> เปลี่ยน Parameter เป็นรหัสพนักงาน EId ---
        public async Task AssignGeneralCoursesToNewUserAsync(string employeeId)
        {
            // (ทางเลือก) คุณอาจจะตรวจสอบก่อนว่ารหัสพนักงานนี้มีอยู่จริงในระบบ HR 
            // var studentInfo = await _studentApiService.GetStudentByCodeAsync(employeeId);
            // if (studentInfo == null) return;

            var activeCourses = await _courseRepo.GetActiveCoursesAsync();
            var generalCourses = activeCourses.Where(c => c.Type == CourseType.General);

            foreach (var course in generalCourses)
            {
                // โยน EId ตรงๆ เข้าตาราง Enrollment ได้เลย
                await CreateOrUpdateEnrollment(employeeId, course, null);
            }
        }

        // --- 3. กรณี Admin กด Assign หรือสร้างคอร์สใหม่ -> ดึงคนจาก API ---
        public async Task ProcessAssignmentForCourseAsync(int courseId)
        {
            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null || !course.IsActive) return;

            // ดึงข้อมูลพนักงาน "ทั้งหมด 7000+ คน" จาก API
            var apiResponse = await _studentApiService.GetStudentAsync();
            if (apiResponse == null || !apiResponse.success || apiResponse.data == null) return;

            var allStudents = apiResponse.data;
            var rules = await _ruleRepo.GetAsync(r => r.CourseId == courseId);

            foreach (var student in allStudents)
            {
                AssignmentRule? matchedRule = null;
                bool shouldAssign = false;

                if (course.Type == CourseType.General)
                {
                    shouldAssign = true;
                }
                else if (course.Type == CourseType.Special)
                {
                    // ส่ง StudentDto เข้าไปเช็ค
                    matchedRule = GetMatchingRuleForStudent(student, rules);
                    shouldAssign = matchedRule != null;
                }

                if (shouldAssign)
                {
                    // โยน EId ลงฐานข้อมูล (เช่น "N142715")
                    await CreateOrUpdateEnrollment(student.EId, course, matchedRule);
                }
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

        // --- 4. ฟังก์ชันบันทึกลงฐานข้อมูล (รับ Parameter เป็น EId ทันที) ---
        private async Task CreateOrUpdateEnrollment(string studentCode, Course course, AssignmentRule? matchedRule = null)
        {
            int currentVersion = GetCurrentActiveVersion(course);

            // เช็คว่าในตารางมี EId ของคนนี้หรือยัง
            var existingEnrollments = await _enrollmentRepo.GetAsync(e =>
                e.StudentCode == studentCode &&
                e.CourseId == course.Id);

            var existing = existingEnrollments.FirstOrDefault();

            if (existing == null)
            {
                var newEnrollment = new Enrollment
                {
                    StudentCode = studentCode, // บันทึก EId ลงคอลัมน์ StudentCode
                    CourseId = course.Id,
                    EnrolledCourseVersion = currentVersion,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    AssignmentRuleId = matchedRule?.Id,
                    StartDate = matchedRule?.StartDate,
                    DueDate = matchedRule?.DueDate
                };
                await _enrollmentRepo.AddAsync(newEnrollment);
            }
            else if (existing.EnrolledCourseVersion < currentVersion)
            {
                existing.EnrolledCourseVersion = currentVersion;
                existing.IsCompleted = false;
                existing.CompletedDate = null;
                existing.AssignmentRuleId = matchedRule?.Id;
                existing.StartDate = matchedRule?.StartDate;
                existing.DueDate = matchedRule?.DueDate;

                await _enrollmentRepo.UpdateAsync(existing);
            }
        }
    }
}