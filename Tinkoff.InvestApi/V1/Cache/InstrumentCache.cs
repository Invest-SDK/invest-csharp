using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tinkoff.InvestApi.V1.Cache;

internal interface IInstrumentCache
{
    Task<T?> GetOrCreate<T>(string key, Func<Task<T>> fetchFromApi) where T : ICacheable;
}

internal class InstrumentCache(ILogger<InstrumentCache> logger, IOptions<CacheOptions> options, IMemoryCache cache) : IInstrumentCache
{
    private readonly ConcurrentDictionary<string, object> _locks = new();
    private readonly MemoryCacheEntryOptions _entryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.AbsoluteExpirationRelativeToNowMinutes),
        SlidingExpiration = TimeSpan.FromMinutes(options.Value.SlidingExpirationMinutes)
    };

    public async Task<T?> GetOrCreate<T>(string key, Func<Task<T>> fetchFromApi) where T : ICacheable
    {
        if (cache.TryGetValue(key, out T? value))
        {
            logger.LogDebug("Key found in cache");
            return value;
        }

        var lazy = (Lazy<Task<T>>)_locks.GetOrAdd(key, _ => new Lazy<Task<T>>(fetchFromApi));

        try
        {
            var result = await lazy.Value;

            if (result != null)
            {
                cache.Set(key, result, _entryOptions);
                var extraKeys = InstrumentCacheKey.GetKeys(result).Except([key]);
                foreach (var extraKey in extraKeys)
                {
                    cache.Set(extraKey, result, _entryOptions);
                }
                logger.LogDebug("Key added to cache");
            }

            return result;
        }
        finally
        {
            _locks.TryRemove(key, out _);
        }
    }
}
