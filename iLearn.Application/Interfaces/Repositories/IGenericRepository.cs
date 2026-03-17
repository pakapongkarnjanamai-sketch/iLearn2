using iLearn.Domain.Common;
using System.Linq.Expressions;

namespace iLearn.Application.Interfaces.Repositories
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<T> AddAsync(T entity);
        Task<T> AddWithoutSaveAsync(T entity);
        Task UpdateAsync(T entity);
        void UpdateWithoutSave(T entity);
        /// <summary>Soft delete by setting IsDeleted = true while keeping the row in the database.</summary>
        Task DeleteAsync(T entity);
        void DeleteWithoutSave(T entity);
        /// <summary>Hard delete for entities that should not keep an audit trail, such as file storage records.</summary>
        Task HardDeleteAsync(T entity);
        IQueryable<T> GetQuery();
        // Custom query helper.
        Task<IReadOnlyList<T>> GetAsync(
            Expression<Func<T, bool>>? filter = null,
            string? includeProperties = null,
            bool ignoreQueryFilters = false
        );

        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

        Task<IEnumerable<TResult>> GetAsync<TResult>(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, TResult>>? selector = null
        );
    }
}