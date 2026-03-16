using BreweryApi.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BreweryApi.Infrastructure.Caching;

public sealed class MemoryBreweryCache : IBreweryCache
{
    private readonly IMemoryCache _memoryCache;

    public MemoryBreweryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        _memoryCache.Set(key, value, options);
        return Task.CompletedTask;
    }
}