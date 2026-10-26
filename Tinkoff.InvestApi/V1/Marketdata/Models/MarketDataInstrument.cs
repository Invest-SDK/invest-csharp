namespace Tinkoff.InvestApi.V1.Marketdata.Models;

public record MarketDataInstrument
(
    string? Figi = null,
    string? TickerClassCode = null,
    string? Uid = null,
    SubscriptionInterval? Interval = null,
    int? Depth = null,
    OrderBookType? OrderBook = null
);