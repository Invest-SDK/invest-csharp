namespace Tinkoff.InvestApi.V1.Marketdata.Models;

public static class MarketDataInstrumentExtension
{

    public static MarketDataInstrument ToMarketDataInstrument(this Candle candle)
    {
        return new MarketDataInstrument
        (
            Figi: candle.Figi,
            Uid: candle.InstrumentUid,
            TickerClassCode: candle.Ticker + "_" + candle.ClassCode,
            Interval: candle.Interval
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this Trade trade)
    {
        return new MarketDataInstrument
        (
            Figi: trade.Figi,
            Uid: trade.InstrumentUid,
            TickerClassCode: trade.Ticker + "_" + trade.ClassCode
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this OrderBook orderbook)
    {
        return new MarketDataInstrument
        (
            Figi: orderbook.Figi,
            Uid: orderbook.InstrumentUid,
            TickerClassCode: orderbook.Ticker + "_" + orderbook.ClassCode,
            Depth: orderbook.Depth,
            OrderBook: orderbook.OrderBookType
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this LastPrice lastPrice)
    {
        return new MarketDataInstrument
        (
            Figi: lastPrice.Figi,
            Uid: lastPrice.InstrumentUid,
            TickerClassCode: lastPrice.Ticker + "_" + lastPrice.ClassCode
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this OpenInterest openInterest)
    {
        return new MarketDataInstrument
        (
            Uid: openInterest.InstrumentUid,
            TickerClassCode: openInterest.Ticker + "_" + openInterest.ClassCode
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this TradingStatus tradingStatus)
    {
        return new MarketDataInstrument
        (
            Figi: tradingStatus.Figi,
            Uid: tradingStatus.InstrumentUid,
            TickerClassCode: tradingStatus.Ticker + "_" + tradingStatus.ClassCode
        );
    }
}