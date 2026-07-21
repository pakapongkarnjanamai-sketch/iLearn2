
using iLearn.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace iLearn.Application.Interfaces
{
    /// <summary>
    /// Unit of Work pattern — ใช้ควบคุม transaction ให้ SaveChanges ทีเดียว
    /// แทนที่จะ SaveChanges ทุกครั้งที่เรียก Repository method
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>Persist all pending repository changes in a single round-trip.</summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>Begin an explicit database transaction. The caller must dispose / commit it.</summary>
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk-add a set of entities to the change tracker without saving. Useful where
        /// callers previously reached into <c>AppDbContext.Set&lt;T&gt;().AddRangeAsync</c>.
        /// </summary>
        Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : BaseEntity;

        /// <summary>Detach an entity from the change tracker without saving pending changes.</summary>
        void Detach<T>(T entity) where T : BaseEntity;
    }
}

