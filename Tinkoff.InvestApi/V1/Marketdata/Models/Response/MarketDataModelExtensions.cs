namespace Tinkoff.InvestApi.V1.Marketdata.Models.Response;

/// <summary>
/// Методы расширения для преобразования grpc-моделей рыночных данных в независимые модели.
/// </summary>
public static class MarketDataModelExtensions
{
    /// <summary>
    /// Преобразует grpc-модель свечи в независимую модель <see cref="CandleModel"/>.
    /// </summary>
    public static CandleModel ToWrapper(this Candle candle, bool isHistorical) => new()
    {
        Figi = candle.Figi,
        Ticker = candle.Ticker,
        ClassCode = candle.ClassCode,
        Interval = candle.Interval,
        Open = candle.Open,
        High = candle.High,
        Low = candle.Low,
        Close = candle.Close,
        Volume = candle.Volume,
        VolumeBuy = candle.VolumeBuy,
        VolumeSell = candle.VolumeSell,
        Time = TimestampExtensions.ToDateTimeOrNull(candle.Time),
        LastTradeTime = TimestampExtensions.ToDateTimeOrNull(candle.LastTradeTs),
        InstrumentUid = candle.InstrumentUid,
        CandleSourceType = candle.CandleSourceType,
        IsHistorical = isHistorical,
    };

    /// <summary>
    /// Преобразует grpc-модель сделки в независимую модель <see cref="TradeModel"/>.
    /// </summary>
    public static TradeModel ToWrapper(this Trade trade) => new()
    {
        Figi = trade.Figi,
        Ticker = trade.Ticker,
        ClassCode = trade.ClassCode,
        Direction = trade.Direction,
        Price = trade.Price,
        Quantity = trade.Quantity,
        Time = TimestampExtensions.ToDateTimeOrNull(trade.Time),
        InstrumentUid = trade.InstrumentUid,
        TradeSource = trade.TradeSource,
    };

    /// <summary>
    /// Преобразует grpc-модель стакана в независимую модель <see cref="OrderBookModel"/>.
    /// </summary>
    public static OrderBookModel ToWrapper(this OrderBook orderBook) => new()
    {
        Figi = orderBook.Figi,
        Ticker = orderBook.Ticker,
        ClassCode = orderBook.ClassCode,
        Depth = orderBook.Depth,
        IsConsistent = orderBook.IsConsistent,
        Time = TimestampExtensions.ToDateTimeOrNull(orderBook.Time),
        InstrumentUid = orderBook.InstrumentUid,
        OrderBookType = orderBook.OrderBookType,
        Bids = orderBook.Bids.Select(b => b.ToWrapper()).ToList(),
        Asks = orderBook.Asks.Select(a => a.ToWrapper()).ToList(),
    };

    /// <summary>
    /// Преобразует grpc-модель записи стакана в независимую модель <see cref="OrderModel"/>.
    /// </summary>
    public static OrderModel ToWrapper(this Order order) => new()
    {
        Price = order.Price,
        Quantity = order.Quantity,
    };

    /// <summary>
    /// Преобразует grpc-модель торгового статуса в независимую модель <see cref="TradingStatusModel"/>.
    /// </summary>
    public static TradingStatusModel ToWrapper(this TradingStatus tradingStatus) => new()
    {
        Figi = tradingStatus.Figi,
        Ticker = tradingStatus.Ticker,
        ClassCode = tradingStatus.ClassCode,
        TradingStatus = tradingStatus.TradingStatus_,
        LimitOrderAvailableFlag = tradingStatus.LimitOrderAvailableFlag,
        MarketOrderAvailableFlag = tradingStatus.MarketOrderAvailableFlag,
        Time = TimestampExtensions.ToDateTimeOrNull(tradingStatus.Time),
        InstrumentUid = tradingStatus.InstrumentUid,
    };

    /// <summary>
    /// Преобразует grpc-модель последней цены в независимую модель <see cref="LastPriceModel"/>.
    /// </summary>
    public static LastPriceModel ToWrapper(this LastPrice lastPrice) => new()
    {
        Figi = lastPrice.Figi,
        Ticker = lastPrice.Ticker,
        ClassCode = lastPrice.ClassCode,
        Price = lastPrice.Price,
        Time = TimestampExtensions.ToDateTimeOrNull(lastPrice.Time),
        InstrumentUid = lastPrice.InstrumentUid,
        Type = lastPrice.LastPriceType,
    };

    /// <summary>
    /// Преобразует grpc-модель открытого интереса в независимую модель <see cref="OpenInterestModel"/>.
    /// </summary>
    public static OpenInterestModel ToWrapper(this OpenInterest openInterest) => new()
    {
        Ticker = openInterest.Ticker,
        ClassCode = openInterest.ClassCode,
        OpenInterest = openInterest.OpenInterest_,
        InstrumentUid = openInterest.InstrumentUid,
    };
}