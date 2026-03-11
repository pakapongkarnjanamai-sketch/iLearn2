using System;
using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class AssignmentDashboardDto
    {
        public string AssignmentNo { get; set; }
        public string Description { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalCourses { get; set; }
        public double CompletionRate { get; set; }
        public DashboardChartDto ChartData { get; set; }
        public List<CourseSummaryDto> Courses { get; set; }
        public List<StudentProgressDto> Students { get; set; }

        // ✅ แจ้ง Dashboard ว่า assignment นี้มี course ที่ถูกลบไปแล้ว
        public bool HasDeletedCourse { get; set; }
    }

    public class DashboardChartDto
    {
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int NotStarted { get; set; }
    }

    public class CourseSummaryDto
    {
        public int AssignmentRuleId { get; set; }
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }
        public int CompletedStudents { get; set; }
        public int TotalStudents { get; set; }

        // ✅ บอกว่า course นี้ถูก soft-delete ไปแล้ว (ข้อมูล progress ยังคงอยู่)
        public bool IsCourseDeleted { get; set; }
    }

    public class StudentProgressDto
    {
        public string StudentCode { get; set; }
        public string? StudentName { get; set; }
        public int? AssignmentRuleId { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseTitle { get; set; }
        public double Progress { get; set; }
        public bool IsCompleted { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? CompletedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}