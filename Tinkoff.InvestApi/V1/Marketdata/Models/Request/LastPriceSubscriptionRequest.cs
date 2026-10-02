namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Запрос на подписку/отписку от цены последней сделки
/// </summary>
public class LastPriceSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments) : SubscriptionRequestBase(action, instruments.ToList())
{
    override public MarketDataRequest ToMarketDataRequest()
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
