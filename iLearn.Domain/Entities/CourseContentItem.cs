using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class CourseContentItem : BaseEntity
    {
        public int CourseVersionId { get; set; }
        public CourseVersion? CourseVersion { get; set; }

        public int ContentItemId { get; set; }
        public ContentItem? ContentItem { get; set; }
        public int? Order { get; set; }

    }
}