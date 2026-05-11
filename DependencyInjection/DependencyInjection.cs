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
        // 1. Redis: Sử dụng Factory để không crash khi startup nếu chưa có Redis
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => 
                ConnectionMultiplexer.Connect(redisConnectionString));
            
            services.AddSingleton<IRedisService, RedisService>();
        }

        // 2. Blob Storage: Chỉ đăng ký nếu có chuỗi kết nối
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
