using Microsoft.EntityFrameworkCore;
using Platform.BuildingBlocks.Abstractions;
using Platform.BuildingBlocks.DateTimes;
using Platform.Domain.Common;

namespace Platform.Infrastructure.Data
{
    public abstract class BaseDbContext : DbContext
    {
        private readonly ICurrentUserProvider? _currentUserProvider;
        public BaseDbContext(DbContextOptions options, ICurrentUserProvider? currentUserProvider = null) : base(options)
        {
            _currentUserProvider = currentUserProvider;
        }
        private string NormalizeUserId()
        {
            var currentUserId = _currentUserProvider?.CurrentUserId;
            if (Guid.TryParse(currentUserId, out var guid)) return guid.ToString();
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
