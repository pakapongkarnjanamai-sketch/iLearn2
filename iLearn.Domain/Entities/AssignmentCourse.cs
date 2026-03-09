using iLearn.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace iLearn.Domain.Entities
{
    /// <summary>
    /// Detail line of an Assignment — each row links one Course to the Assignment header.
    /// Replaces the old pattern where Assignment itself held a CourseId.
    /// </summary>
    public class AssignmentCourse : BaseEntity
    {
        public int AssignmentId { get; set; }
        [ForeignKey("AssignmentId")]
        public Assignment? Assignment { get; set; }

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }
    }
}
