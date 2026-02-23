using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface ICourseAssignmentService
    {
        // เปลี่ยนพารามิเตอร์เป็น string เพื่อรับรหัสพนักงาน (EId)
        Task AssignGeneralCoursesToNewUserAsync(string employeeId);

        // ดึงพนักงานทั้งหมดจาก API แล้วเช็คเพื่อ Assign
        Task ProcessAssignmentForCourseAsync(int courseId);

        // [New] ฟังก์ชันสำหรับการ Assign รายบุคคลแบบเจาะจง (Bulk Assign)
        Task AssignCourseToEmployees(int courseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, int? assignmentRuleId = null);
    }
}