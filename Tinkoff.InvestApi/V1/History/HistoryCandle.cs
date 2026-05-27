namespace Tinkoff.InvestApi.V1.History;

public class HistoryCandle
{
    public Guid InstrumentUid { get; set; }
    public DateTimeOffset Utc { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public long Volume { get; set; }
}
