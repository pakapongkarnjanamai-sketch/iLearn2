using System;
using System.Collections.Generic;
using iLearn.Application.Common;

namespace iLearn.Application.DTOs
{
    public class AssignmentDashboardDto
    {
        public string AssignmentNo { get; set; }
        public string Description { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalCourses { get; set; }
        public double CompletionRate { get; set; }
        public DashboardChartDto ChartData { get; set; }
        public List<CourseSummaryDto> Courses { get; set; }
        public List<LearnerProgressDto> Learners { get; set; }

        public int? LearnerGroupId { get; set; }
        public string? LearnerGroupName { get; set; }

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
        public int CompletedLearners { get; set; }
        public int TotalLearners { get; set; }

        // ✅ บอกว่า course นี้ถูก soft-delete ไปแล้ว (ข้อมูล progress ยังคงอยู่)
        public bool IsCourseDeleted { get; set; }
    }

    public class LearnerProgressDto
    {
        public string LearnerCode { get; set; }
        public string? LearnerName { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public int? AssignmentRuleId { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseTitle { get; set; }
        public double Progress { get; set; }
        public bool IsCompleted { get; set; }
        public string Status { get; set; } = AssignmentStatusKeys.Learner.NotStarted;
        public DateTime? CompletedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public List<string> LearnerGroups { get; set; } = new();
    }
}