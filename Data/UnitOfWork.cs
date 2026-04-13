using Platform.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Platform.Domain.Common;

namespace Platform.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BaseDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();

        public UnitOfWork(BaseDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<T> GetRepository<T>() where T : Entity
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                var repoInstance = new GenericRepository<T>(_context);
                _repositories[type] = repoInstance;
            }
            return (IGenericRepository<T>)_repositories[type];
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken);
        }
    }
}
