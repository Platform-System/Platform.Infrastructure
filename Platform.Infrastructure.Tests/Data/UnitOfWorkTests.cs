using Microsoft.EntityFrameworkCore;
using Platform.BuildingBlocks.Abstractions;
using Platform.Domain.Common;
using Platform.Infrastructure.Data;
using Xunit;

namespace Platform.Infrastructure.Tests.Data;

public sealed class UnitOfWorkTests
{
    [Fact]
    public void GetRepository_WhenCalledTwiceForSameType_ReturnsSameInstance()
    {
        using var context = CreateContext("uow-cache");
        var unitOfWork = new UnitOfWork(context);

        var first = unitOfWork.GetRepository<TestEntity>();
        var second = unitOfWork.GetRepository<TestEntity>();

        Assert.Same(first, second);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_PersistsEntity()
    {
        await using var context = CreateContext("uow-save");
        var unitOfWork = new UnitOfWork(context);
        var repository = unitOfWork.GetRepository<TestEntity>();

        await repository.AddAsync(new TestEntity { Name = "saved" });
        var affected = await unitOfWork.SaveChangesAsync();

        Assert.Equal(1, affected);
        Assert.Single(context.Entities);
    }

    [Fact]
    public void HasActiveTransaction_WithoutTransaction_ReturnsFalse()
    {
        using var context = CreateContext("uow-tx");
        var unitOfWork = new UnitOfWork(context);

        Assert.False(unitOfWork.HasActiveTransaction);
    }

    private static TestDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new TestDbContext(options, new TestCurrentUserProvider());
    }

    private sealed class TestCurrentUserProvider : ICurrentUserProvider
    {
        public string? CurrentUserId => null;
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
