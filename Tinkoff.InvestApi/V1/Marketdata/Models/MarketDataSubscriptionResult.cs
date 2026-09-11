namespace Tinkoff.InvestApi.V1.Marketdata.Models;

public class MarketDataSubscriptionResult
(
    MarketDataAction action = MarketDataAction.UNKNOWN,
    MarketDataResponseType responseType = MarketDataResponseType.OTHER,
    Dictionary<MarketDataInstrument, MarketDataSubscriptionStatus>? subsciptionStatusDictionary = null
)
{
    private readonly MarketDataAction action = action;
    private readonly MarketDataResponseType responseType = responseType;
    private readonly Dictionary<MarketDataInstrument, MarketDataSubscriptionStatus> subsciptionStatusDictionary = subsciptionStatusDictionary ?? [];

    public MarketDataAction Action => action;
    public MarketDataResponseType ResponseType => responseType;
    public Dictionary<MarketDataInstrument, MarketDataSubscriptionStatus> SubsciptionStatusDictionary => subsciptionStatusDictionary;
}
