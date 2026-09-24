namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Запрос на подписку/отписку от стакана
/// </summary>
public class OrderBookSubscriptionRequest : ISubscriptionRequestBase
{
    private readonly List<MarketDataInstrument> _instruments;
    private readonly SubscriptionAction _action;

    public OrderBookSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments)
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
            SubscribeOrderBookRequest = new SubscribeOrderBookRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments.Select(x =>
                {
                    return new OrderBookInstrument
                    {
                        InstrumentId = x.Uid,
                        Figi = x.Figi,
                        Depth = x.Depth ?? -1,
                        OrderBookType = x.OrderBook ?? OrderBookType.Unspecified
                    };
                })
                }
            }
        };
    }
}
