namespace Tinkoff.InvestApi.V1.Cache;

public class CacheOptions
{
    public const string CacheSection = "Caching";

    public int AbsoluteExpirationRelativeToNowMinutes { get; set; } = 60;
    public bool Enabled { get; set; } = true;
    public int SlidingExpirationMinutes { get; set; } = 20;
}
