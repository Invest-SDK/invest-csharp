using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream.Marketdata.Models;

public class MarketDataInstrument
(
    string? figi = null,
    string? tickerClassCode = null,
    string? uid = null,
    SubscriptionInterval? interval = null,
    int? depth = null,
    OrderBookType? orderBook = null
)
{
    private readonly string? figi = figi;
    private readonly string? tickerClassCode = tickerClassCode;
    private readonly string? uid = uid;
    private readonly SubscriptionInterval? interval = interval;
    private readonly int? depth = depth;
    private readonly OrderBookType? orderBook = orderBook;

    public string? Figi => figi;
    public string? TickerClassCode => tickerClassCode;
    public string? Uid => uid;
    public SubscriptionInterval? Interval => interval;
    public int? Depth => depth;
    public OrderBookType? OrderBook => orderBook;
}