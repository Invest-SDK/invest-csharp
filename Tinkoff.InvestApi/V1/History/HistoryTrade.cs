namespace Tinkoff.InvestApi.V1.History;

public interface IHistoryTrade
{
    DateTimeOffset TradeTs { get; }
    string TickerCc { get; }
    string Direction { get; }
    decimal Price { get; }
    long Quantity { get; }
    string TradeSource { get; }
    Guid? InstrumentUid { get; }
}

public class HistoryTrade : IHistoryTrade
{
    public DateTimeOffset TradeTs { get; set; }
    public string TickerCc { get; set; } = "";
    public string Direction { get; set; } = "";
    public decimal Price { get; set; }
    public long Quantity { get; set; }
    public string TradeSource { get; set; } = "";
    public Guid? InstrumentUid { get; set; }
}
