using Platform.Application.Abstractions.Caching;

namespace Platform.Infrastructure.Caching;

public sealed class NullRedisService : IRedisService
{
    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        return Task.CompletedTask;
    }
}
