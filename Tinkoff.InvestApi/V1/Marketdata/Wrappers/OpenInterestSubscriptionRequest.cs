namespace Tinkoff.InvestApi.V1.Marketdata.Wrappers;

/// <summary>
/// Запрос на подписку/отписку от открытых позиций
/// </summary>
public class OpenInterestSubscriptionRequest : SubscriptionRequestBase
{
    private readonly List<InfoInstrument> _instruments;
    private readonly SubscriptionAction _action;

    public OpenInterestSubscriptionRequest(SubscriptionAction action, IEnumerable<InfoInstrument> instruments)
    {
        _action = action;
        _instruments = instruments.ToList();
    }

    public override SubscriptionAction Action => _action;

    public IReadOnlyList<InfoInstrument> Instruments => _instruments.AsReadOnly();

    public override MarketDataRequest ToMarketDataRequest()
    {
        return new MarketDataRequest
        {
            SubscribeInfoRequest = new SubscribeInfoRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments }
            }
        };
    }
}
