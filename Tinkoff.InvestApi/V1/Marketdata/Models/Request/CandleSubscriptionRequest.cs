namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Запрос на подписку/отписку от свечей
/// </summary>
public class CandleSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments) : SubscriptionRequestBase(action, instruments.ToList())
{
    public DateTimeOffset? LastInteractionTime {get; set;}

    override public MarketDataRequest ToMarketDataRequest()
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
                    Interval = (SubscriptionInterval) (int) (x.Interval ?? SubscriptionInterval.Unspecified)
                }).ToList() }
            }
        };
    }
}
