using Google.Protobuf;
using Tinkoff.InvestApi.V1.Marketdata.Models;

namespace Tinkoff.InvestApi.V1.Marketdata;

public static class MarketDataUtils
{
    public static MarketDataSubscriptionResult? GetSubscriptionResultFromResponse(MarketDataResponse response)
    {
        if (response.SubscribeCandlesResponse != null && response.SubscribeCandlesResponse.IsInitialized())
        {
            return new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.CANDLE,
                response.SubscribeCandlesResponse.CandlesSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                        figi: x.Figi,
                        uid: x.InstrumentUid,
                        tickerClassCode: x.Ticker + "_" + x.ClassCode,
                        interval: x.Interval
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                ));
        }
        else if (response.SubscribeLastPriceResponse != null && response.SubscribeLastPriceResponse.IsInitialized())
        {
            return new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.LAST_PRICE,
                response.SubscribeLastPriceResponse.LastPriceSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                        figi: x.Figi,
                        uid: x.InstrumentUid,
                        tickerClassCode: x.Ticker + "_" + x.ClassCode
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                ));
        }
        else if (response.SubscribeOrderBookResponse != null && response.SubscribeOrderBookResponse.IsInitialized())
        {
            return new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.ORDER_BOOK,
                response.SubscribeOrderBookResponse.OrderBookSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                        figi: x.Figi,
                        uid: x.InstrumentUid,
                        tickerClassCode: x.Ticker + "_" + x.ClassCode,
                        depth: x.Depth,
                        orderBook: x.OrderBookType
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                ));
        }
        else if (response.SubscribeTradesResponse != null && response.SubscribeTradesResponse.IsInitialized())
        {
            return new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.TRADE,
                response.SubscribeTradesResponse.TradeSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                        figi: x.Figi,
                        uid: x.InstrumentUid,
                        tickerClassCode: x.Ticker + "_" + x.ClassCode
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                ));
        }
        else if (response.SubscribeInfoResponse != null && response.SubscribeInfoResponse.IsInitialized())
        {
            return new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.TRADING_STATUS,
                response.SubscribeInfoResponse.InfoSubscriptions.ToDictionary(
                    x => new MarketDataInstrument(
                        figi: x.Figi,
                        uid: x.InstrumentUid,
                        tickerClassCode: x.Ticker + "_" + x.ClassCode
                    ),
                    x => GetSubscriptionStatusFromResponse(x.SubscriptionStatus)
                ));
        }
        return null;
    }

    public static MarketDataAction GetActionFromResponse(MarketDataResponse response)
    {
        if (response.SubscribeCandlesResponse != null && response.SubscribeCandlesResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeCandlesResponse.CandlesSubscriptions[0].SubscriptionAction);
        }
        else if (response.SubscribeLastPriceResponse != null && response.SubscribeLastPriceResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeLastPriceResponse.LastPriceSubscriptions[0].SubscriptionAction);
        }
        else if (response.SubscribeOrderBookResponse != null && response.SubscribeOrderBookResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeOrderBookResponse.OrderBookSubscriptions[0].SubscriptionAction);
        }
        else if (response.SubscribeTradesResponse != null && response.SubscribeTradesResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeTradesResponse.TradeSubscriptions[0].SubscriptionAction);
        }
        else if (response.SubscribeInfoResponse != null && response.SubscribeInfoResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeInfoResponse.InfoSubscriptions[0].SubscriptionAction);
        }
        return MarketDataAction.UNKNOWN;
    }

    public static MarketDataInstrument GetInstrumentsFromResponse(MarketDataResponse response)
    {

        if (response.Candle != null && response.Candle.IsInitialized())
        {
            return new MarketDataInstrument
            (
                figi: response.Candle.Figi,
                uid: response.Candle.InstrumentUid,
                tickerClassCode: response.Candle.Ticker + "_" + response.Candle.ClassCode,
                interval: response.Candle.Interval
            );
        }
        else if (response.Trade != null && response.Trade.IsInitialized())
        {
            return new MarketDataInstrument
            (
                figi: response.Trade.Figi,
                uid: response.Trade.InstrumentUid,
                tickerClassCode: response.Trade.Ticker + "_" + response.Trade.ClassCode
            );
        }
        else if (response.Orderbook != null && response.Orderbook.IsInitialized())
        {
            return new MarketDataInstrument(
                figi: response.Orderbook.Figi,
                uid: response.Orderbook.InstrumentUid,
                tickerClassCode: response.Orderbook.Ticker + "_" + response.Orderbook.ClassCode,
                depth: response.Orderbook.Depth,
                orderBook: response.Orderbook.OrderBookType
            );
        }
        else if (response.LastPrice != null && response.LastPrice.IsInitialized())
        {
            return new MarketDataInstrument(
                figi: response.LastPrice.Figi,
                uid: response.LastPrice.InstrumentUid,
                tickerClassCode: response.LastPrice.Ticker + "_" + response.LastPrice.ClassCode
            );
        }
        else if (response.OpenInterest != null && response.OpenInterest.IsInitialized())
        {
            return new MarketDataInstrument(
                uid: response.OpenInterest.InstrumentUid,
                tickerClassCode: response.OpenInterest.Ticker + "_" + response.OpenInterest.ClassCode
            );
        }
        else if (response.TradingStatus != null && response.TradingStatus.IsInitialized())
        {
            return new MarketDataInstrument(
                figi: response.TradingStatus.Figi,
                uid: response.TradingStatus.InstrumentUid,
                tickerClassCode: response.TradingStatus.Ticker + "_" + response.TradingStatus.ClassCode
            );
        }
        return null;
    }

    public static MarketDataResponseType? GetResponseType(MarketDataResponse response)
    {

        if (response.Candle != null && response.Candle.IsInitialized())
        {
            return MarketDataResponseType.CANDLE;
        }
        else if (response.Trade != null && response.Trade.IsInitialized())
        {
            return MarketDataResponseType.TRADE;
        }
        else if (response.Orderbook != null && response.Orderbook.IsInitialized())
        {
            return MarketDataResponseType.ORDER_BOOK;
        }
        else if (response.LastPrice != null && response.LastPrice.IsInitialized())
        {
            return MarketDataResponseType.LAST_PRICE;
        }
        else if (response.OpenInterest != null && response.OpenInterest.IsInitialized())
        {
            return MarketDataResponseType.OPEN_INTEREST;
        }
        else if (response.TradingStatus != null && response.TradingStatus.IsInitialized())
        {
            return MarketDataResponseType.TRADING_STATUS;
        }
        return null;
    }

    private static MarketDataAction GetActionFromSubscriptionAction(SubscriptionAction subscriptionAction)
    {
        if (subscriptionAction == SubscriptionAction.Subscribe)
        {
            return MarketDataAction.SUBSCRIBE;
        }
        else if (subscriptionAction == SubscriptionAction.Unsubscribe)
        {
            return MarketDataAction.UNSUBSCRIBE;
        }
        return MarketDataAction.UNKNOWN;
    }

    private static MarketDataSubscriptionStatus GetSubscriptionStatusFromResponse(SubscriptionStatus subscriptionStatus)
    {
        if (subscriptionStatus == SubscriptionStatus.Success)
        {
            return MarketDataSubscriptionStatus.OK;
        }
        return MarketDataSubscriptionStatus.ERROR;
    }
}