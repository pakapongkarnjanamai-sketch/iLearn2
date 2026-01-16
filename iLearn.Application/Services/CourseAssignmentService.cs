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
        private readonly IGenericRepository<User> _userRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IGenericRepository<AssignmentRule> _ruleRepo;
        private readonly IGenericRepository<UserRole> _userRoleRepo;
        // [Optional] ถ้า Course ไม่ได้ Include Versions มา อาจต้อง Inject Repository ของ CourseVersion เพิ่ม

        public CourseAssignmentService(
            IGenericRepository<User> userRepo,
            ICourseRepository courseRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IGenericRepository<AssignmentRule> ruleRepo,
            IGenericRepository<UserRole> userRoleRepo)
        {
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _enrollmentRepo = enrollmentRepo;
            _ruleRepo = ruleRepo;
            _userRoleRepo = userRoleRepo;
        }

        // กรณี 1: พนักงานใหม่เข้ามา -> หาคอร์ส General ยัดใส่ให้
        public async Task AssignGeneralCoursesToNewUserAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return;

            // หมายเหตุ: ต้องมั่นใจว่า Method นี้ Include Versions มาด้วย
            var activeCourses = await _courseRepo.GetActiveCoursesAsync();
            var generalCourses = activeCourses.Where(c => c.Type == CourseType.General);

            foreach (var course in generalCourses)
            {
                await CreateOrUpdateEnrollment(user, course);
            }
        }

        // กรณี 2: Admin กด Assign หรือสร้างคอร์สใหม่ -> ระบบวิ่งหาคน
        public async Task ProcessAssignmentForCourseAsync(int courseId)
        {
            // หมายเหตุ: ต้องมั่นใจว่า Method นี้ Include Versions มาด้วย
            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null || !course.IsActive) return;

            var users = await _userRepo.GetAllAsync();
            var rules = await _ruleRepo.GetAsync(r => r.CourseId == courseId);

            foreach (var user in users)
            {
                bool shouldAssign = false;

                if (course.Type == CourseType.General)
                {
                    shouldAssign = true;
                }
                else if (course.Type == CourseType.Special)
                {
                    shouldAssign = await CheckIfUserMatchesRules(user, rules);
                }

                if (shouldAssign)
                {
                    await CreateOrUpdateEnrollment(user, course);
                }
            }
        }

        // --- Helper Logic ---

        private async Task<bool> CheckIfUserMatchesRules(User user, IReadOnlyList<AssignmentRule> rules)
        {
            if (rules == null || !rules.Any()) return false;

            var userRoles = await _userRoleRepo.GetAsync(ur => ur.UserId == user.Id);

            foreach (var rule in rules)
            {
                bool roleMatch = !rule.RoleId.HasValue ||
                                 userRoles.Any(ur => ur.RoleId == rule.RoleId);

                bool divisionMatch = !rule.DivisionId.HasValue;
                // TODO: ถ้าต้องการเช็ค DivisionId ให้แม่นยำ ต้อง Join Role หรือ User มาเช็ค

                if (roleMatch && divisionMatch) return true;
            }

            return false;
        }

        // [Updated] Logic การหา Version ล่าสุด
        private int GetCurrentActiveVersion(Course course)
        {
            if (course.Versions == null || !course.Versions.Any())
            {
                // Fallback กรณีหาไม่เจอ ให้ return 1 หรือ throw exception ตาม Business Logic
                return 1;
            }

            return course.Versions
                .Where(v => v.IsActive)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => v.VersionNumber)
                .FirstOrDefault();
        }

        private async Task CreateOrUpdateEnrollment(User user, Course course)
        {
            // หา Version ปัจจุบันของ Course ที่จะ Assign
            int currentVersion = GetCurrentActiveVersion(course);

            var existingEnrollments = await _enrollmentRepo.GetAsync(e =>
                e.StudentCode == user.Nid &&
                e.CourseId == course.Id);

            var existing = existingEnrollments.FirstOrDefault();

            if (existing == null)
            {
                // ยังไม่เคยเรียน -> สร้างใหม่ด้วย Version ปัจจุบัน
                var newEnrollment = new Enrollment
                {
                    StudentCode = user.Nid,
                    CourseId = course.Id,
                    EnrolledVersion = currentVersion, // ใช้ Version จาก Logic ใหม่
                    Status = "Not Started",
                    CreatedAt = DateTime.UtcNow
                };
                await _enrollmentRepo.AddAsync(newEnrollment);
            }
            else if (existing.EnrolledVersion < currentVersion)
            {
                // เคยเรียนแล้ว แต่มี Version ใหม่ -> Reset ให้ Retake
                existing.EnrolledVersion = currentVersion; // อัปเดต Version
                existing.Status = "Not Started";
                existing.CompletedDate = null;

                await _enrollmentRepo.UpdateAsync(existing);
            }
        }
    }
}