using Tinkoff.InvestApi.V1.Marketdata.Wrappers;

namespace Tinkoff.InvestApi.V1.Marketdata.Wrappers;

/// <summary>
/// Запрос на подписку/отписку от свечей
/// </summary>
public class CandleSubscriptionRequest : SubscriptionRequestBase
{
    private readonly List<CandleInstrument> _instruments;
    private readonly SubscriptionAction _action;

    public CandleSubscriptionRequest(SubscriptionAction action, IEnumerable<CandleInstrument> instruments)
    {
        _action = action;
        _instruments = instruments.ToList();
    }

    public override SubscriptionAction Action => _action;

    public IReadOnlyList<CandleInstrument> Instruments => _instruments.AsReadOnly();

    public override MarketDataRequest ToMarketDataRequest()
    {
        return new MarketDataRequest
        {
            SubscribeCandlesRequest = new SubscribeCandlesRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments }
            }
        };
    }
}
