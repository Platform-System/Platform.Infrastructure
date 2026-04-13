using Platform.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Platform.Domain.Common;

namespace Platform.Infrastructure.Data
{
    public class GenericRepository<T> : IGenericRepository<T> where T : Entity
    {
        protected readonly BaseDbContext _context;

        public GenericRepository(BaseDbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            if (includes != null)
            {
                foreach (var include in includes) { query = query.Include(include); }
            }
            return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>().AsNoTracking();
            if (includes != null)
            {
                foreach (var include in includes) { query = query.Include(include); }
            }
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>().Where(predicate).AsNoTracking();
            if (includes != null)
            {
                foreach (var include in includes) { query = query.Include(include); }
            }
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<(List<T> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter = null, Expression<Func<T, object>>? orderBy = null, bool isDescending = false, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            IQueryable<T> baseQuery = _context.Set<T>().AsNoTracking();
            if (filter != null) baseQuery = baseQuery.Where(filter);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            IQueryable<T> query = baseQuery;
            if (includes != null)
            {
                foreach (var include in includes) { query = query.Include(include); }
            }

            if (orderBy != null)
            {
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }
            else
            {
                query = query.OrderBy(x => x.Id);
            }

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            if (asNoTracking) query = query.AsNoTracking();
            if (includes != null)
            {
                foreach (var include in includes) { query = query.Include(include); }
            }
            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default) => await _context.Set<T>().AddAsync(entity, cancellationToken);
        public void Update(T entity) => _context.Set<T>().Update(entity);
        public void Remove(T entity) => _context.Set<T>().Remove(entity);
        public async Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate) => await _context.Set<T>().Where(predicate).ExecuteDeleteAsync();

        public async Task<int> DeleteInBatchesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, DateTime>> orderBy, Expression<Func<T, Guid>> keySelector, int batchSize = 100)
        {
            int totalDeleted = 0;
            while (true)
            {
                var ids = await _context.Set<T>().Where(predicate).OrderBy(orderBy).Select(keySelector).Take(batchSize).ToListAsync();
                if (!ids.Any()) break;
                var deleted = await _context.Set<T>().Where(BuildContainsExpression(keySelector, ids)).ExecuteDeleteAsync();
                totalDeleted += deleted;
                if (deleted < batchSize) break;
                await Task.Delay(50);
            }
            return totalDeleted;
        }

        public IQueryable<T> GetQueryable() => _context.Set<T>().AsQueryable();
        public async Task<int> TotalAsync(Expression<Func<T, bool>> predicate) => await _context.Set<T>().CountAsync(predicate);

        private static Expression<Func<T, bool>> BuildContainsExpression(Expression<Func<T, Guid>> keySelector, List<Guid> ids)
        {
            var param = keySelector.Parameters[0];
            var body = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { typeof(Guid) }, Expression.Constant(ids), keySelector.Body);
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
    }
}
