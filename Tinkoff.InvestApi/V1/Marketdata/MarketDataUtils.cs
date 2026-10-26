using Google.Protobuf;
using Tinkoff.InvestApi.V1.Marketdata.Models;

namespace Tinkoff.InvestApi.V1.Marketdata;

public static class MarketDataUtils
{
    public static MarketDataSubscriptionResult? GetSubscriptionResultFromResponse(MarketDataResponse response)
    {
        return GetSubscriptionResponseType(response) switch
        {
            MarketDataResponseType.CANDLE => new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.CANDLE,
                response.SubscribeCandlesResponse.CandlesSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                    Figi: x.Figi,
                        Uid: x.InstrumentUid,
                        TickerClassCode: x.Ticker + "_" + x.ClassCode,
                        Interval: x.Interval
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                )),
            MarketDataResponseType.LAST_PRICE => new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.LAST_PRICE,
                response.SubscribeLastPriceResponse.LastPriceSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                        Figi: x.Figi,
                        Uid: x.InstrumentUid,
                        TickerClassCode: x.Ticker + "_" + x.ClassCode
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                )),
            MarketDataResponseType.ORDER_BOOK => new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.ORDER_BOOK,
                response.SubscribeOrderBookResponse.OrderBookSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                        Figi: x.Figi,
                        Uid: x.InstrumentUid,
                        TickerClassCode: x.Ticker + "_" + x.ClassCode,
                        Depth: x.Depth,
                        OrderBook: x.OrderBookType
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                )),
            MarketDataResponseType.TRADE => new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.TRADE,
                response.SubscribeTradesResponse.TradeSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                        Figi: x.Figi,
                        Uid: x.InstrumentUid,
                        TickerClassCode: x.Ticker + "_" + x.ClassCode
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                )),
            MarketDataResponseType.TRADING_STATUS => new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.TRADING_STATUS,
                response.SubscribeInfoResponse.InfoSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                        Figi: x.Figi,
                        Uid: x.InstrumentUid,
                        TickerClassCode: x.Ticker + "_" + x.ClassCode
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                )),
            _ => null
        };
    }

    private static MarketDataResponseType? GetSubscriptionResponseType(MarketDataResponse response)
    {
        return response switch
        {
            var resp when resp.SubscribeCandlesResponse != null && resp.SubscribeCandlesResponse.IsInitialized() => MarketDataResponseType.CANDLE,
            var resp when resp.SubscribeLastPriceResponse != null && resp.SubscribeLastPriceResponse.IsInitialized() => MarketDataResponseType.LAST_PRICE,
            var resp when resp.SubscribeOrderBookResponse != null && resp.SubscribeOrderBookResponse.IsInitialized() => MarketDataResponseType.ORDER_BOOK,
            var resp when resp.SubscribeTradesResponse != null && resp.SubscribeTradesResponse.IsInitialized() => MarketDataResponseType.TRADE,
            var resp when resp.SubscribeInfoResponse != null && resp.SubscribeInfoResponse.IsInitialized() => MarketDataResponseType.TRADING_STATUS,
            _ => null
        };
    }

    public static MarketDataAction GetActionFromResponse(MarketDataResponse response)
    {
        return response switch
        {
            var resp when resp.SubscribeCandlesResponse != null && resp.SubscribeCandlesResponse.IsInitialized() 
                => GetActionFromSubscriptionAction(resp.SubscribeCandlesResponse.CandlesSubscriptions[0].SubscriptionAction),
            var resp when resp.SubscribeLastPriceResponse != null && resp.SubscribeLastPriceResponse.IsInitialized() 
                => GetActionFromSubscriptionAction(resp.SubscribeLastPriceResponse.LastPriceSubscriptions[0].SubscriptionAction),
            var resp when resp.SubscribeOrderBookResponse != null && resp.SubscribeOrderBookResponse.IsInitialized() 
                => GetActionFromSubscriptionAction(resp.SubscribeOrderBookResponse.OrderBookSubscriptions[0].SubscriptionAction),
            var resp when resp.SubscribeTradesResponse != null && resp.SubscribeTradesResponse.IsInitialized() 
                => GetActionFromSubscriptionAction(resp.SubscribeTradesResponse.TradeSubscriptions[0].SubscriptionAction),
            var resp when resp.SubscribeInfoResponse != null && resp.SubscribeInfoResponse.IsInitialized() 
                => GetActionFromSubscriptionAction(resp.SubscribeInfoResponse.InfoSubscriptions[0].SubscriptionAction),
            _ => MarketDataAction.UNKNOWN
        };
    }

    public static MarketDataInstrument? GetInstrumentsFromResponse(MarketDataResponse response)
    {
        return GetResponseType(response) switch
        {
            MarketDataResponseType.CANDLE => response.Candle.ToMarketDataInstrument(),
            MarketDataResponseType.TRADE => response.Trade.ToMarketDataInstrument(),
            MarketDataResponseType.ORDER_BOOK => response.Orderbook.ToMarketDataInstrument(),
            MarketDataResponseType.LAST_PRICE => response.LastPrice.ToMarketDataInstrument(),
            MarketDataResponseType.OPEN_INTEREST => response.OpenInterest.ToMarketDataInstrument(),
            MarketDataResponseType.TRADING_STATUS => response.TradingStatus.ToMarketDataInstrument(),
            _ => null
        };
    }

    public static MarketDataResponseType? GetResponseType(MarketDataResponse response)
    {
        return response switch
        {
            var resp when resp.Candle != null && resp.Candle.IsInitialized() => MarketDataResponseType.CANDLE,
            var resp when resp.Trade != null && resp.Trade.IsInitialized() => MarketDataResponseType.TRADE,
            var resp when resp.Orderbook != null && resp.Orderbook.IsInitialized() => MarketDataResponseType.ORDER_BOOK,
            var resp when resp.LastPrice != null && resp.LastPrice.IsInitialized() => MarketDataResponseType.LAST_PRICE,
            var resp when resp.OpenInterest != null && resp.OpenInterest.IsInitialized() => MarketDataResponseType.OPEN_INTEREST,
            var resp when resp.TradingStatus != null && resp.TradingStatus.IsInitialized() => MarketDataResponseType.TRADING_STATUS,
            _ => null
        };
    }

    private static MarketDataAction GetActionFromSubscriptionAction(SubscriptionAction subscriptionAction)
    {
        return subscriptionAction switch
        {
            SubscriptionAction.Subscribe => MarketDataAction.SUBSCRIBE,
            SubscriptionAction.Unsubscribe => MarketDataAction.UNSUBSCRIBE,
            _ => MarketDataAction.UNKNOWN
        };
    }

    private static MarketDataSubscriptionStatus GetSubscriptionStatusFromResponse(SubscriptionStatus subscriptionStatus)
    {
        return subscriptionStatus == SubscriptionStatus.Success
            ? MarketDataSubscriptionStatus.OK
            : MarketDataSubscriptionStatus.ERROR;
    }
}