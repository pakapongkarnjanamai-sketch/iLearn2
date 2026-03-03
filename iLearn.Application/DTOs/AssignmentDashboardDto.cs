using System;
using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class AssignmentDashboardDto
    {
        public string AssignmentNo { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalCourses { get; set; }
        public double CompletionRate { get; set; }
        public DashboardChartDto ChartData { get; set; }
        public List<CourseSummaryDto> Courses { get; set; }
        public List<StudentProgressDto> Students { get; set; }
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
    }

    public class StudentProgressDto
    {
        public string StudentCode { get; set; }
        public int? AssignmentRuleId { get; set; }
        public double Progress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}