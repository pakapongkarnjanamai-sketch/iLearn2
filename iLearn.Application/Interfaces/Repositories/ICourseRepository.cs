using iLearn.Domain.Entities;

namespace iLearn.Application.Interfaces.Repositories
{
    public interface ICourseRepository : IGenericRepository<Course>
    {
        // ตัวอย่าง Method พิเศษที่ไม่ใช่ CRUD ธรรมดา
        Task<bool> IsCourseCodeUniqueAsync(string code);
        Task<IEnumerable<Course>> GetActiveCoursesAsync();
    }
}