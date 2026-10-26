namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Запрос на подписку/отписку от стакана
/// </summary>
public class OrderBookSubscriptionRequest(SubscriptionAction action, IEnumerable<MarketDataInstrument> instruments) : SubscriptionRequestBase(action, instruments.ToList())
{
    override public MarketDataRequest ToMarketDataRequest()
    {
        return new MarketDataRequest
        {
            SubscribeOrderBookRequest = new SubscribeOrderBookRequest
            {
                SubscriptionAction = _action,
                Instruments = { _instruments.Select(x =>
                {
                    return new OrderBookInstrument
                    {
                        InstrumentId = x.Uid,
                        Figi = x.Figi,
                        Depth = x.Depth ?? -1,
                        OrderBookType = x.OrderBook ?? OrderBookType.Unspecified
                    };
                })
                }
            }
        };
    }
}
