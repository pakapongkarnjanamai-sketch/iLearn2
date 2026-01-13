using iLearn.Application.Interfaces;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iLearn.Infrastructure.Repositories
{
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
        public CourseRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Course>> GetActiveCoursesAsync()
        {
            return await _dbSet
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> IsCourseCodeUniqueAsync(string code)
        {
            return !await _dbSet.AnyAsync(c => c.Code == code);
        }
    }
}