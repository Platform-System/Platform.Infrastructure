using Microsoft.EntityFrameworkCore;
using Platform.BuildingBlocks.Abstractions;
using Platform.Domain.Common;
using Platform.Infrastructure.Data;
using Xunit;

namespace Platform.Infrastructure.Tests.Data;

public sealed class BaseDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_SetsCreatedAuditFields()
    {
        await using var context = CreateContext("created", "2b938e77-2d0c-4dad-b41a-0d78adbe9547");
        var entity = new TestEntity();

        await context.Entities.AddAsync(entity);
        await context.SaveChangesAsync();

        Assert.NotEqual(default, entity.CreatedAt);
        Assert.Equal("2b938e77-2d0c-4dad-b41a-0d78adbe9547", entity.CreatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityDeleted_PerformsSoftDelete()
    {
        await using var context = CreateContext("deleted", "invalid-user");
        var entity = new TestEntity();

        await context.Entities.AddAsync(entity);
        await context.SaveChangesAsync();

        context.Entities.Remove(entity);
        await context.SaveChangesAsync();

        Assert.True(entity.IsSoftDeleted);
        Assert.Equal("system", entity.DeletedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_SetsUpdatedAuditFields()
    {
        await using var context = CreateContext("updated", "e6b5c3a8-a958-4a4c-9b63-93f0169d8bf0");
        var entity = new TestEntity();

        await context.Entities.AddAsync(entity);
        await context.SaveChangesAsync();

        entity.Name = "changed";
        await context.SaveChangesAsync();

        Assert.NotNull(entity.UpdatedAt);
        Assert.Equal("e6b5c3a8-a958-4a4c-9b63-93f0169d8bf0", entity.UpdatedBy);
    }

    private static TestDbContext CreateContext(string dbName, string? currentUserId)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new TestDbContext(options, new TestCurrentUserProvider(currentUserId));
    }

    private sealed class TestCurrentUserProvider(string? currentUserId) : ICurrentUserProvider
    {
        public string? CurrentUserId { get; } = currentUserId;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options, ICurrentUserProvider currentUserProvider)
        : BaseDbContext(options, currentUserProvider)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }

    private sealed class TestEntity : Entity
    {
        public string? Name { get; set; }
    }
}
