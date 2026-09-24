namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Запрос на подписку/отписку от свечей
/// </summary>
public class CandleSubscriptionRequest : ISubscriptionRequestBase
{
    private readonly List<MarketDataInstrument> _instruments;
    private readonly SubscriptionAction _action;
    public DateTimeOffset? LastInteractionTime {get; set;}

    public CandleSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments)
    {
        _action = action;
        _instruments = instruments.ToList();
    }

    public SubscriptionAction Action => _action;

    public IReadOnlyList<MarketDataInstrument> Instruments => _instruments.AsReadOnly();

    public MarketDataRequest ToMarketDataRequest()
    {
        return new MarketDataRequest
        {
            SubscribeCandlesRequest = new SubscribeCandlesRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments.Select(x => new CandleInstrument
                {
                    InstrumentId = x.Uid ?? "",
                    Figi = x.Figi ?? "",
                    Interval = (SubscriptionInterval) (int) (x.Interval ?? 0)
                }).ToList() }
            }
        };
    }
}
