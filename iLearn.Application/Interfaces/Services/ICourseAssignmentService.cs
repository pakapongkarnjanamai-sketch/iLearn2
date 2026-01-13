using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface ICourseAssignmentService
    {
        // เรียกเมื่อมีพนักงานใหม่เข้ามา -> ระบบจะหาคอร์ส General ให้
        Task AssignGeneralCoursesToNewUserAsync(int userId);

        // เรียกเมื่อมีการสร้างคอร์สใหม่ หรือแก้ไขกฎ -> ระบบจะวิ่งหาคนที่มีสิทธิ์เรียน
        Task ProcessAssignmentForCourseAsync(int courseId);
    }
}