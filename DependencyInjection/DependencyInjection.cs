using Azure.Storage.Blobs;
using StackExchange.Redis;
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
        services.AddSingleton<IRedisService, NullRedisService>();
        services.AddScoped<IBlobService, NullBlobService>();

        // 1. Redis: fallback sang no-op service nếu chưa có cấu hình
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => 
                ConnectionMultiplexer.Connect(redisConnectionString));
            
            services.AddSingleton<IRedisService, RedisService>();
        }

        // 2. Blob Storage: fallback sang null object nếu chưa có cấu hình
        var blobConnectionString = configuration.GetConnectionString("BlobStorage");
        if (!string.IsNullOrWhiteSpace(blobConnectionString))
        {
            services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));
            services.AddScoped<IBlobService, BlobService>();
        }

        // 3. Core Data Services
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        
        return services;
    }
}
