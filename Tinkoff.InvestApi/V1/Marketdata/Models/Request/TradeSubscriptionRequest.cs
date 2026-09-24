namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Запрос на подписку/отписку от сделок
/// </summary>
public class TradeSubscriptionRequest : ISubscriptionRequestBase
{
    private readonly List<MarketDataInstrument> _instruments;
    private readonly SubscriptionAction _action;

    public TradeSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments)
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
            SubscribeTradesRequest = new SubscribeTradesRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments.Select(x =>
                {
                    return new TradeInstrument
                    {
                        InstrumentId = x.Uid,
                        Figi = x.Figi
                    };
                }).ToList() }
            }
        };
    }
}
