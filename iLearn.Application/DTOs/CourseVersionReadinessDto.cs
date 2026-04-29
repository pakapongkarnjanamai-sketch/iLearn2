using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class CourseVersionReadinessDto
    {
        public int VersionId { get; set; }
        public bool IsReady { get; set; }
        public int ResourceCount { get; set; }
        public List<CourseVersionReadinessIssueDto> Issues { get; set; } = [];
    }

    public class CourseVersionReadinessIssueDto
    {
        public int ResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}