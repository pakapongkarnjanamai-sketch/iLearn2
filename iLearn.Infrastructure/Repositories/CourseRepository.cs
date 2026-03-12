using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iLearn.Infrastructure.Repositories
{
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
        public CourseRepository(AppDbContext context, IDateTime dateTime) : base(context, dateTime)
        {
        }

        public async Task<IEnumerable<Course>> GetActiveCoursesAsync()
        {
            return await _dbSet
                .Include(c => c.CourseType)
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsCourseCodeUniqueAsync(string code)
        {
            return !await _dbSet.AnyAsync(c => c.Code == code);
        }
    }
}