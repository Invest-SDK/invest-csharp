namespace Tinkoff.InvestApi.V1.Marketdata.Wrappers;

/// <summary>
/// Запрос на подписку/отписку от стакана
/// </summary>
public class OrderBookSubscriptionRequest : SubscriptionRequestBase
{
    private readonly List<OrderBookInstrument> _instruments;
    private readonly SubscriptionAction _action;

    public OrderBookSubscriptionRequest(SubscriptionAction action, IEnumerable<OrderBookInstrument> instruments)
    {
        _action = action;
        _instruments = instruments.ToList();
    }

    public override SubscriptionAction Action => _action;

    public IReadOnlyList<OrderBookInstrument> Instruments => _instruments.AsReadOnly();

    public override MarketDataRequest ToMarketDataRequest()
    {
        return new MarketDataRequest
        {
            SubscribeOrderBookRequest = new SubscribeOrderBookRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments }
            }
        };
    }
}
