using iLearn.Domain.Common;
using System.Linq.Expressions;

namespace iLearn.Application.Interfaces.Repositories
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        IQueryable<T> GetQuery();
        // เพิ่มฟังก์ชันค้นหาแบบ Custom
        Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate);
    }
}