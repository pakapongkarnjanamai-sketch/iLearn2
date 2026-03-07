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
        /// <summary>Soft Delete — ตั้ง IsDeleted = true ข้อมูลยังอยู่ใน DB</summary>
        Task DeleteAsync(T entity);
        /// <summary>Hard Delete — ลบ record ออกจาก DB จริง สำหรับ entity ที่ไม่ต้องการ audit trail เช่น FileStorage</summary>
        Task HardDeleteAsync(T entity);
        IQueryable<T> GetQuery();
        // เพิ่มฟังก์ชันค้นหาแบบ Custom
        Task<IReadOnlyList<T>> GetAsync(
            Expression<Func<T, bool>>? filter = null,
            string? includeProperties = null
        );

        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

        Task<IEnumerable<TResult>> GetAsync<TResult>(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, TResult>>? selector = null
        );
    }
}