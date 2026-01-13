namespace iLearn.Domain.Entities
{
    // Class นี้อาจจะไม่สืบทอด BaseEntity ถ้าใช้เป็นแค่ตารางเชื่อม (Join Table)
    public class CourseResource
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public int ResourceId { get; set; }
        public Resource? Resource { get; set; }
    }
}