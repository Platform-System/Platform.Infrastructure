using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Abstractions.Caching;
using Platform.Application.Abstractions.Data;
using Platform.Infrastructure.Caching;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddSingleton<IRedisService, RedisService>();
        
        return services;
    }
}
