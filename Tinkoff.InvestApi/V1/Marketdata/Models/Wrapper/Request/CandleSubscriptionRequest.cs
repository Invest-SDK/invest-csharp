namespace Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper.Request;

/// <summary>
/// Запрос на подписку/отписку от свечей
/// </summary>
public class CandleSubscriptionRequest : SubscriptionRequestBase
{
    private readonly List<MarketDataInstrument> _instruments;
    private readonly SubscriptionAction _action;
    public DateTime? LastInteractionTime {get; set;}

    public CandleSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments)
    {
        _action = action;
        _instruments = instruments.ToList();
    }

    public override SubscriptionAction Action => _action;

    public IReadOnlyList<MarketDataInstrument> Instruments => _instruments.AsReadOnly();

    public override MarketDataRequest ToMarketDataRequest()
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
