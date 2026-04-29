using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class CourseVersionReadinessDto
    {
        public int VersionId { get; set; }
        public bool IsReady { get; set; }
        public int ContentItemCount { get; set; }
        public List<CourseVersionReadinessIssueDto> Issues { get; set; } = [];
    }

    public class CourseVersionReadinessIssueDto
    {
        public int ContentItemId { get; set; }
        public string ContentItemName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}