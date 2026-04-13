using Microsoft.EntityFrameworkCore;
using Platform.BuildingBlocks.Abstractions;
using Platform.Domain.Common;

namespace Platform.Infrastructure.Data
{
    public abstract class BaseDbContext : DbContext
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly string _currentUserId;
        public BaseDbContext(DbContextOptions options, IDateTimeProvider dateTimeProvider, string? currentUserId = null) : base(options)
        {
            _dateTimeProvider = dateTimeProvider;
            _currentUserId = currentUserId ?? "system";
        }
        private string NormalizeUserId()
        {
            if (Guid.TryParse(_currentUserId, out var guid)) return guid.ToString();
            return "system";
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<Entity>();
            var userId = NormalizeUserId();
            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.SetCreated(_dateTimeProvider.UtcNow, userId);
                        break;
                    case EntityState.Modified:
                        if (!entry.Property(nameof(Entity.UpdatedAt)).IsModified &&
                            !entry.Property(nameof(Entity.UpdatedBy)).IsModified)
                        {
                            entry.Entity.SetUpdated(_dateTimeProvider.UtcNow, userId);
                        }
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.SetDeleted(_dateTimeProvider.UtcNow, userId);
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
