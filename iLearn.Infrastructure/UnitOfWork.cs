using iLearn.Application.Interfaces;
using iLearn.Domain.Common;
using iLearn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace iLearn.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : BaseEntity
        {
            await _context.Set<T>().AddRangeAsync(entities, cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

