namespace iLearn.Application.DTOs
{
    // ── GET api/Reports/compliance ──
    public class ComplianceReportDto
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalLearners { get; set; }
        public int OpenEnrollments { get; set; }
        public int CompletedEnrollments { get; set; }
        public int OverdueEnrollments { get; set; }
        public int OverdueLearners { get; set; }
        public double ComplianceRate { get; set; }
        public List<ComplianceGroupRow> ByDivision { get; set; } = new();
        public List<ComplianceGroupRow> ByDepartment { get; set; } = new();
        public List<ComplianceOverdueRow> OverdueRows { get; set; } = new();
    }

    public class ComplianceGroupRow
    {
        public string GroupName { get; set; } = string.Empty;
        public string? ParentDivision { get; set; }
        public int Learners { get; set; }
        public int Enrollments { get; set; }
        public int Completed { get; set; }
        public int Overdue { get; set; }
        public double CompletionRate { get; set; }
    }

    public class ComplianceOverdueRow
    {
        public string LearnerCode { get; set; } = string.Empty;
        public string? LearnerName { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseTitle { get; set; }
        public string? AssignmentNo { get; set; }
        public DateTime? DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public double Progress { get; set; }
    }

    // ── GET api/Reports/transcript/{learnerCode} ──
    public class TranscriptReportDto
    {
        public DateTime GeneratedAt { get; set; }
        public string LearnerCode { get; set; } = string.Empty;
        public string? LearnerName { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public List<string> LearnerGroups { get; set; } = new();
        public int TotalCourses { get; set; }
        public int CompletedCourses { get; set; }
        public List<TranscriptRow> Rows { get; set; } = new();
    }

    public class TranscriptRow
    {
        public int EnrollmentId { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseTitle { get; set; }
        public string? AssignmentNo { get; set; }
        public string Status { get; set; } = string.Empty;
        public double Progress { get; set; }
        public int TotalScore { get; set; }
        public int TotalTimeSpentSeconds { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    // ── GET api/Reports/course-summary ──
    public class CourseSummaryReportDto
    {
        public DateTime GeneratedAt { get; set; }
        public List<CourseSummaryRow> Rows { get; set; } = new();
    }

    public class CourseSummaryRow
    {
        public int CourseId { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? CategoryName { get; set; }
        public string? DivisionName { get; set; }
        public string? CourseTypeName { get; set; }
        public int AssignmentCount { get; set; }
        public int EnrolledLearners { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
        public double AvgProgress { get; set; }
        public double CompletionRate { get; set; }
        public double? AvgScore { get; set; }
    }

    // ── GET api/Reports/assignments ──
    public class AssignmentSummaryReportDto
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalAssignments { get; set; }
        public int ActiveAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int OverdueAssignments { get; set; }
        public int TotalLearners { get; set; }
        public int TotalEnrollments { get; set; }
        public double CompletionRate { get; set; }
        public List<AssignmentSummaryRow> Rows { get; set; } = new();
    }

    public class AssignmentSummaryRow
    {
        public int AssignmentId { get; set; }
        public string AssignmentNo { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DivisionName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CourseCount { get; set; }
        public int LearnerCount { get; set; }
        public int EnrollmentCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
        public double CompletionRate { get; set; }
        public string Status { get; set; } = string.Empty;
    }


    // ── GET api/Reports/activity?months=12 ──
    public class ActivityReportDto
    {
        public DateTime GeneratedAt { get; set; }
        public List<ActivityMonthRow> Months { get; set; } = new();
    }

    public class ActivityMonthRow
    {
        public string Month { get; set; } = string.Empty;
        public int Completions { get; set; }
        public int ActiveLearners { get; set; }
        public int NewEnrollments { get; set; }
        public double TotalHoursPlayed { get; set; }
    }
}
