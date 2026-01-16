using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class CourseResource : BaseEntity
    {
        public int CourseVersionId { get; set; }
        public CourseVersion? CourseVersion { get; set; }

        public int ResourceId { get; set; }
        public Resource? Resource { get; set; }

    }
}