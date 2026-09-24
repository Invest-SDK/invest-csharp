namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Запрос на подписку/отписку от сделок
/// </summary>
public class TradingStatusesSubscriptionRequest : ISubscriptionRequestBase
{
    private readonly List<MarketDataInstrument> _instruments;
    private readonly SubscriptionAction _action;

    public TradingStatusesSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments)
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
            SubscribeInfoRequest = new SubscribeInfoRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments.Select(x =>
                {
                    return new InfoInstrument
                    {
                        InstrumentId = x.Uid,
                        Figi = x.Figi
                    };
                }).ToList() }
            }
        };
    }
}
