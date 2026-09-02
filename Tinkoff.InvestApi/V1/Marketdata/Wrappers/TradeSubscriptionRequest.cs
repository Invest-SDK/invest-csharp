namespace Tinkoff.InvestApi.V1.Marketdata.Wrappers;

/// <summary>
/// Запрос на подписку/отписку от сделок
/// </summary>
public class TradeSubscriptionRequest : SubscriptionRequestBase
{
    private readonly List<TradeInstrument> _instruments;
    private readonly SubscriptionAction _action;

    public TradeSubscriptionRequest(SubscriptionAction action, IEnumerable<TradeInstrument> instruments)
    {
        _action = action;
        _instruments = instruments.ToList();
    }

    public override SubscriptionAction Action => _action;

    public IReadOnlyList<TradeInstrument> Instruments => _instruments.AsReadOnly();

    public override MarketDataRequest ToMarketDataRequest()
    {
        return new MarketDataRequest
        {
            SubscribeTradesRequest = new SubscribeTradesRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments }
            }
        };
    }
}
