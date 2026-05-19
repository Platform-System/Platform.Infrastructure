using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Abstractions.Caching;
using Platform.Application.Abstractions.Storage;
using Platform.Infrastructure.Caching;
using Platform.Infrastructure.DependencyInjection;
using Platform.Infrastructure.Storage;
using Xunit;

namespace Platform.Infrastructure.Tests.DependencyInjection;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WithoutOptionalConnections_RegistersNullServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        services.AddInfrastructure(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var redisService = serviceProvider.GetRequiredService<IRedisService>();
        var blobService = scope.ServiceProvider.GetRequiredService<IBlobService>();

        Assert.IsType<NullRedisService>(redisService);
        Assert.IsType<NullBlobService>(blobService);
    }

    [Fact]
    public async Task NullBlobService_WhenGeneratingReadUrl_ReturnsEmptyString()
    {
        var service = new NullBlobService();

        var singleUrl = service.GenerateReadSasUrl("products", "blob.png");
        var manyUrls = service.GenerateReadSasUrlsAsync("products", ["a.png", "b.png"]);

        Assert.Equal(string.Empty, singleUrl);
        Assert.All(manyUrls, url => Assert.Equal(string.Empty, url));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(Stream.Null, "file.png", "image/png", "products"));
    }

    [Fact]
    public async Task NullRedisService_WhenCalled_DoesNotThrow()
    {
        var service = new NullRedisService();

        var value = await service.GetAsync<string>("missing");
        await service.SetAsync("key", "value");
        await service.RemoveAsync("key");
        await service.RemoveByPrefixAsync("prefix");

        Assert.Null(value);
    }
}
