using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Platform.Application.Abstractions.Caching;
using Platform.Application.Abstractions.Data;
using Platform.Application.Abstractions.Storage;
using Platform.Infrastructure.Caching;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Storage;

namespace Platform.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var blobConnectionString = configuration.GetConnectionString("BlobStorage")
            ?? throw new InvalidOperationException("Connection string 'BlobStorage' is not configured.");

        services.AddSingleton(new BlobServiceClient(blobConnectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IBlobService, BlobService>();
        services.AddSingleton<IRedisService, RedisService>();
        
        return services;
    }
}
