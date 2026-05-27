using CsvHelper.Configuration.Attributes;
using Tinkoff.InvestApi.V1.History;

namespace TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Models;

public class HistoryTradeCsvRecord : IHistoryTrade
{
    [Name("TRADE_TS")] public required DateTimeOffset TradeTs { get; set; }
    [Name("TICKER_CC")] public required string TickerCc { get; set; }
    [Name("DIRECTION")] public required string Direction { get; set; }
    [Name("PRICE")] public required decimal Price { get; set; }
    [Name("QUANTITY")] public required long Quantity { get; set; }
    [Name("TRADE_SOURCE")] public required string TradeSource { get; set; }
    [Name("INSTRUMENT_UID")] public required Guid? InstrumentUid { get; set; }
}
