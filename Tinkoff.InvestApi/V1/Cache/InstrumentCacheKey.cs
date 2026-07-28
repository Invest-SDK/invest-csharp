namespace Tinkoff.InvestApi.V1.Cache;

internal static class InstrumentCacheKey
{
    public static string GetKey<T>(InstrumentRequest request)
    {
        Type type = typeof(T);
        string key = request.IdType switch
        {
            InstrumentIdType.Figi => GetKeyByFigi(type, request.Id),
            InstrumentIdType.PositionUid => GetKeyByPositionUid(type, request.Id),
            InstrumentIdType.Ticker => GetKeyByTicker(type, request.Id, request.ClassCode),
            InstrumentIdType.Uid => GetKeyByUid(type, request.Id),
            _ => throw new Exception()
        };
        return key;
    }

    public static IReadOnlyCollection<string> GetKeys<T>(T instrument) where T : ICacheable
    {
        Type type = typeof(T);
        return _keyGenerators
            .Select(generator => generator(instrument, type))
            .Where(key => key != null)
            .Cast<string>()
            .ToList()
            .AsReadOnly();
    }

    private static readonly List<Func<ICacheable, Type, string?>> _keyGenerators = new()
    {
        (instrument, type) => instrument is ICacheableByFigi f ? GetKeyByFigi(type, f.Figi) : null,
        (instrument, type) => instrument is ICacheableByPositionUid p ? GetKeyByPositionUid(type, p.PositionUid) : null,
        (instrument, type) => instrument is ICacheableByTicker t ? GetKeyByTicker(type, t.Ticker, t.ClassCode) : null,
        (instrument, type) => instrument is ICacheableByUid u ? GetKeyByUid(type, u.Uid) : null,
    };

    private static string GetKeyByFigi(Type type, string figi)
    {
        return $"{type.Name.ToLower()}:figi:{figi}";
    }

    private static string GetKeyByPositionUid(Type type, string positionUid)
    {
        return $"{type.Name.ToLower()}:positionuid:{positionUid}";
    }

    private static string GetKeyByTicker(Type type, string ticker, string classCode)
    {
        return $"{type.Name.ToLower()}:ticker:{ticker}:{classCode}";
    }

    private static string GetKeyByUid(Type type, string uid)
    {
        return $"{type.Name.ToLower()}:uid:{uid}";
    }
}
