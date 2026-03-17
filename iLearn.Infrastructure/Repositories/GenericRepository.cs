using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using iLearn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;

namespace iLearn.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;
        private readonly IDateTime _dateTime;

        public GenericRepository(AppDbContext context, IDateTime dateTime)
        {
            _context = context;
            _dbSet = context.Set<T>();
            _dateTime = dateTime;
        }
        public IQueryable<T> GetQuery()
        {
            return _dbSet.AsQueryable();
        }
        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> AddWithoutSaveAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public void UpdateWithoutSave(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
        }

        public async Task DeleteAsync(T entity)
        {
            entity.IsDeleted  = true;
            entity.DeletedAt  = _dateTime.Now;
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public void DeleteWithoutSave(T entity)
        {
            entity.IsDeleted  = true;
            entity.DeletedAt  = _dateTime.Now;
            _context.Entry(entity).State = EntityState.Modified;
        }

        public async Task HardDeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

       

        public async Task<IReadOnlyList<T>> GetAsync(
            Expression<Func<T, bool>>? filter = null,
            string? includeProperties = null,
            bool ignoreQueryFilters = false)
        {
            IQueryable<T> query = _dbSet;

            // 0. Ignore global query filters (e.g., soft-delete) สำหรับ navigation properties ที่ถูกลบ
            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            // 1. Apply Filter (ถ้ามี)
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 2. Apply Includes (ถ้ามี ส่งมาเป็น string คั่นด้วย comma เช่น "Course,User")
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            return await query.ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.CountAsync();
        }

        public async Task<IEnumerable<TResult>> GetAsync<TResult>(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, TResult>>? selector = null)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (selector != null)
            {
                return await query.Select(selector).ToListAsync();
            }

            throw new ArgumentException("Selector is required");
        }
    }
}