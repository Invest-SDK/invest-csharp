namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Запрос на подписку/отписку от сделок
/// </summary>
public class TradeSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments) : SubscriptionRequestBase(action, instruments.ToList())
{
    override public MarketDataRequest ToMarketDataRequest()
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
