using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class CourseResource : BaseEntity
    {
        public int Id { get; set; }

        // เปลี่ยนจาก CourseId เป็น CourseVersionId
        public int CourseVersionId { get; set; }
        public CourseVersion? CourseVersion { get; set; }

        public int ResourceId { get; set; }
        public Resource? Resource { get; set; }

        // หมายเหตุ: ไม่จำเป็นต้องมี Property int Version แล้ว เพราะมันผูกกับ CourseVersion แล้ว
    }
}