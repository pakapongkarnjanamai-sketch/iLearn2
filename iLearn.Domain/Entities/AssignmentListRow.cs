namespace iLearn.Domain.Entities
{
    /// <summary>
    /// Read-only projection from <c>vw_AssignmentList</c>.
    /// Not a domain aggregate — used for the admin assignment list (CRUD Get) only.
    /// Soft-delete filtering is handled inside the view SQL; do NOT add a global
    /// query filter here.
    /// </summary>
    public class AssignmentListRow
    {
        public int Id { get; set; }
        public string AssignmentNo { get; set; } = string.Empty;
        public int? DivisionId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CourseNames { get; set; } = string.Empty;
        public int CourseCount { get; set; }
        public bool HasDeletedCourse { get; set; }
        /// <summary>Real enrolled-learner count from EnrollmentAssignments (not EmployeeCodes CSV).</summary>
        public int LearnerCount { get; set; }
        public bool HasEnrollments { get; set; }
        /// <summary>Computed in-view: Completed | Upcoming | Expired | InProgress.</summary>
        public string Status { get; set; } = string.Empty;
    }
}
