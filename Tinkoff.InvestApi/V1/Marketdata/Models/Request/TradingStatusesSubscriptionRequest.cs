namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Запрос на подписку/отписку от сделок
/// </summary>
public class TradingStatusesSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments) : SubscriptionRequestBase(action, instruments.ToList())
{
    override public MarketDataRequest ToMarketDataRequest()
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
