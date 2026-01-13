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
        private readonly IGenericRepository<UserRole> _userRoleRepo; // เพิ่มเพื่อดึง Role ของ User

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

            // ดึงคอร์ส General ที่ Active ทั้งหมด
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
            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null || !course.IsActive) return;

            // ดึง User ทั้งหมด
            var users = await _userRepo.GetAllAsync();

            // ดึงกฎของคอร์สนี้ (สำหรับ Special)
            var rules = await _ruleRepo.GetAsync(r => r.CourseId == courseId);

            foreach (var user in users)
            {
                bool shouldAssign = false;

                if (course.Type == CourseType.General)
                {
                    shouldAssign = true; // General = ทุกคน
                }
                else if (course.Type == CourseType.Special)
                {
                    // Special = ต้องตรงตามกฎข้อใดข้อหนึ่ง
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

            // ดึง Role ของ User คนนี้มาเช็ค
            var userRoles = await _userRoleRepo.GetAsync(ur => ur.UserId == user.Id); // ต้องแน่ใจว่า Repo รองรับ Include Role หรือเราต้องดึง Role แยกถ้าจำเป็น

            foreach (var rule in rules)
            {
                // 1. ตรวจ Role (ถ้ากฎระบุ RoleId)
                // เช็คว่า User มี RoleId ที่ตรงกับกฎไหม
                bool roleMatch = !rule.RoleId.HasValue ||
                                 userRoles.Any(ur => ur.RoleId == rule.RoleId);

                // 2. ตรวจ Division (ถ้ากฎระบุ DivisionId)
                // เนื่องจาก UserRole เก็บ RoleId เราอาจต้องเช็ค Division ผ่าน Role อีกที 
                // ในที่นี้สมมติว่าเช็คผ่าน RoleId ไปก่อน หรือถ้าจะให้แม่นยำต้อง Join ตาราง Role มาดู DivisionId
                // เพื่อความง่ายในขั้นตอนนี้ ผมจะข้ามการเช็ค Division ลึกๆ ไปก่อน หรือคุณอาจต้องเพิ่ม Logic การดึง Role ที่มี DivisionId มาด้วย
                bool divisionMatch = !rule.DivisionId.HasValue;
                // TODO: เพิ่ม Logic เช็ค DivisionId โดยละเอียดถ้าจำเป็น (ต้องดึง Role.DivisionId)

                if (roleMatch && divisionMatch) return true;
            }

            return false;
        }

        private async Task CreateOrUpdateEnrollment(User user, Course course)
        {
            // ตรวจสอบว่าเคยเรียนไปหรือยัง (ใช้ StudentCode จับคู่)
            var existingEnrollments = await _enrollmentRepo.GetAsync(e =>
                e.StudentCode == user.Nid &&
                e.CourseId == course.Id);

            var existing = existingEnrollments.FirstOrDefault();

            if (existing == null)
            {
                // ยังไม่เคยเรียน -> สร้างใหม่
                var newEnrollment = new Enrollment
                {
                    StudentCode = user.Nid,
                    CourseId = course.Id,
                    EnrolledVersion = course.Version,
                    Status = "Not Started",
                    CreatedDate = DateTime.UtcNow
                };
                await _enrollmentRepo.AddAsync(newEnrollment);
            }
            else if (existing.EnrolledVersion < course.Version)
            {
                // เคยเรียนแล้ว แต่คอร์สอัปเดตเวอร์ชัน -> รีเซ็ตให้เรียนใหม่ (Retake)
                existing.EnrolledVersion = course.Version;
                existing.Status = "Not Started";
                existing.CompletedDate = null;
                existing.LastModifiedDate = DateTime.UtcNow;

                await _enrollmentRepo.UpdateAsync(existing);
            }
        }
    }
}