namespace Tinkoff.InvestApi.V1.Cache;

public interface ICacheable
{
}

public interface ICacheableByFigi : ICacheable
{
    public string Figi { get; }
}

public interface ICacheableByPositionUid : ICacheable
{
    public string PositionUid { get; }
}

public interface ICacheableByTicker : ICacheable
{
    public string ClassCode { get; }
    public string Ticker { get; }
}

public interface ICacheableByUid : ICacheable
{
    public string Uid { get; }
}
