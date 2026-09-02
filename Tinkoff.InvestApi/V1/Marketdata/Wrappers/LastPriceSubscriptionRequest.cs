namespace Tinkoff.InvestApi.V1.Marketdata.Wrappers;

/// <summary>
/// Запрос на подписку/отписку от цены последней сделки
/// </summary>
public class LastPriceSubscriptionRequest : SubscriptionRequestBase
{
    private readonly List<LastPriceInstrument> _instruments;
    private readonly SubscriptionAction _action;

    public LastPriceSubscriptionRequest(SubscriptionAction action, IEnumerable<LastPriceInstrument> instruments)
    {
        _action = action;
        _instruments = instruments.ToList();
    }

    public override SubscriptionAction Action => _action;

    public IReadOnlyList<LastPriceInstrument> Instruments => _instruments.AsReadOnly();

    public override MarketDataRequest ToMarketDataRequest()
    {
        return new MarketDataRequest
        {
            SubscribeLastPriceRequest = new SubscribeLastPriceRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments }
            }
        };
    }
}
