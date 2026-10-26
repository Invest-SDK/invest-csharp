namespace Tinkoff.InvestApi.V1.Marketdata.Models;

public record MarketDataSubscriptionResult
(
    MarketDataAction Action = MarketDataAction.UNKNOWN,
    MarketDataResponseType ResponseType = MarketDataResponseType.OTHER,
    Dictionary<MarketDataInstrument, MarketDataSubscriptionStatus>? SubscriptionStatusDictionary = null
);
