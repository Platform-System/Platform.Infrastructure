using Microsoft.EntityFrameworkCore;
using Platform.BuildingBlocks.Abstractions;
using Platform.BuildingBlocks.DateTimes;
using Platform.Domain.Common;

namespace Platform.Infrastructure.Data
{
    public abstract class BaseDbContext : DbContext
    {
        private readonly string _currentUserId;
        public BaseDbContext(DbContextOptions options, IDateTimeProvider dateTimeProvider, string? currentUserId = null) : base(options)
        {
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
                        entry.Entity.SetCreated(Clock.Now, userId);
                        break;
                    case EntityState.Modified:
                        if (!entry.Property(nameof(Entity.UpdatedAt)).IsModified &&
                            !entry.Property(nameof(Entity.UpdatedBy)).IsModified)
                        {
                            entry.Entity.SetUpdated(Clock.Now, userId);
                        }
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.SetDeleted(Clock.Now, userId);
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
