
using Microsoft.Extensions.Options;

namespace Tinkoff.InvestApi.V1.Cache;

public interface IInstrumentCacheService
{
    Task<T> FindBy<T>(InstrumentRequest request, Func<Task<T>> fetchFromApi) where T : ICacheable;
}

internal class InstrumentCacheService(IOptions<CacheOptions> options, IInstrumentCache instrumentCache) : IInstrumentCacheService
{
    private readonly bool _cachingEnabled = options.Value.Enabled;

    public async Task<T> FindBy<T>(InstrumentRequest request, Func<Task<T>> fetchFromApi) where T : ICacheable
    {
        if (!_cachingEnabled)
        {
            return await fetchFromApi();
        }
        var key = InstrumentCacheKey.GetKey<T>(request);
        var instrument = await instrumentCache.GetOrCreate(key, fetchFromApi);
        return instrument!;
    }
}
