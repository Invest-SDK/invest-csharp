namespace Tinkoff.InvestApi.V1.Marketdata.Models;

public static class MarketDataInstrumentExtension
{

    public static MarketDataInstrument ToMarketDataInstrument(this Candle candle)
    {
        return new MarketDataInstrument
        (
            figi: candle.Figi,
            uid: candle.InstrumentUid,
            tickerClassCode: candle.Ticker + "_" + candle.ClassCode,
            interval: candle.Interval
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this Trade trade)
    {
        return new MarketDataInstrument
        (
            figi: trade.Figi,
            uid: trade.InstrumentUid,
            tickerClassCode: trade.Ticker + "_" + trade.ClassCode
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this OrderBook orderbook)
    {
        return new MarketDataInstrument
        (
            figi: orderbook.Figi,
            uid: orderbook.InstrumentUid,
            tickerClassCode: orderbook.Ticker + "_" + orderbook.ClassCode,
            depth: orderbook.Depth,
            orderBook: orderbook.OrderBookType
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this LastPrice lastPrice)
    {
        return new MarketDataInstrument
        (
            figi: lastPrice.Figi,
            uid: lastPrice.InstrumentUid,
            tickerClassCode: lastPrice.Ticker + "_" + lastPrice.ClassCode
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this OpenInterest openInterest)
    {
        return new MarketDataInstrument
        (
            uid: openInterest.InstrumentUid,
            tickerClassCode: openInterest.Ticker + "_" + openInterest.ClassCode
        );
    }

    public static MarketDataInstrument ToMarketDataInstrument(this TradingStatus tradingStatus)
    {
        return new MarketDataInstrument
        (
            figi: tradingStatus.Figi,
            uid: tradingStatus.InstrumentUid,
            tickerClassCode: tradingStatus.Ticker + "_" + tradingStatus.ClassCode
        );
    }
}