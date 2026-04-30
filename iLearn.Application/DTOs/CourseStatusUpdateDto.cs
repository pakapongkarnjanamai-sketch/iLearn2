using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iLearn.Domain.Enums;

namespace iLearn.Application.DTOs
{
    public class CourseStatusUpdateDto
    {
        public bool? IsActive { get; set; }
        public CourseStatus? Status { get; set; }
        public string? Reason { get; set; }
    }

    public class CourseStatusImpactDto
    {
        public int CourseId { get; set; }
        public CourseStatus CurrentStatus { get; set; }
        public string CurrentStatusName => CurrentStatus.ToString();
        public int NotStartedCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int OpenEnrollmentCount => NotStartedCount + InProgressCount;
        public int ActiveAssignmentCount { get; set; }
        public int FutureAssignmentCount { get; set; }
        public bool HasOpenEnrollments => OpenEnrollmentCount > 0;
        public bool CanClose => CurrentStatus == CourseStatus.Open;
        public bool CanOpen { get; set; }
        public bool CanRetire { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CourseStatusResultDto
    {
        public int CourseId { get; set; }
        public CourseStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public bool IsActive { get; set; }
        public bool CanAssign { get; set; }
        public bool CanLearnerAccess { get; set; }
        public CourseStatusImpactDto Impact { get; set; } = new();
    }
}
