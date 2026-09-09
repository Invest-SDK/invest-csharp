using Tinkoff.InvestApi.Stream.Marketdata.Models;

namespace Tinkoff.InvestApi.V1.Marketdata.Wrappers;

/// <summary>
/// Запрос на подписку/отписку от цены последней сделки
/// </summary>
public class LastPriceSubscriptionRequest : SubscriptionRequestBase
{
    private readonly List<MarketDataInstrument> _instruments;
    private readonly SubscriptionAction _action;

    public LastPriceSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments)
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
            SubscribeLastPriceRequest = new SubscribeLastPriceRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments.Select(x =>
                {
                    return new LastPriceInstrument
                    {
                        InstrumentId = x.Uid,
                        Figi = x.Figi
                    };
                }).ToList() }
            }
        };
    }
}
