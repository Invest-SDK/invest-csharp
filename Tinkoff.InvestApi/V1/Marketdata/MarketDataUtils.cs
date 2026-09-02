using Google.Protobuf;
using Tinkoff.InvestApi.Stream.Marketdata.Models;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream.Marketdata;

public static class MarketDataUtils
{
    public static MarketDataSubscriptionResult? GetSubscriptionResultFromResponse(MarketDataResponse response)
    {
        if (response.SubscribeCandlesResponse.IsInitialized())
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
        else if (response.SubscribeLastPriceResponse.IsInitialized())
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
        else if (response.SubscribeOrderBookResponse.IsInitialized())
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
        else if (response.SubscribeTradesResponse.IsInitialized())
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
        else if (response.SubscribeInfoResponse.IsInitialized())
        {
            return new MarketDataSubscriptionResult(
                GetActionFromResponse(response),
                MarketDataResponseType.OPEN_INTEREST,
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
        if (response.SubscribeCandlesResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeCandlesResponse.CandlesSubscriptions[0].SubscriptionAction);
        }
        else if (response.SubscribeLastPriceResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeLastPriceResponse.LastPriceSubscriptions[0].SubscriptionAction);
        }
        else if (response.SubscribeOrderBookResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeOrderBookResponse.OrderBookSubscriptions[0].SubscriptionAction);
        }
        else if (response.SubscribeTradesResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeTradesResponse.TradeSubscriptions[0].SubscriptionAction);
        }
        else if (response.SubscribeInfoResponse.IsInitialized())
        {
            return GetActionFromSubscriptionAction(response.SubscribeInfoResponse.InfoSubscriptions[0].SubscriptionAction);
        }
        return MarketDataAction.UNKNOWN;
    }

    public static MarketDataResponseType? GetResponseType(MarketDataResponse response)
    {

        if (response.Candle.IsInitialized())
        {
            return MarketDataResponseType.CANDLE;
        }
        else if (response.LastPrice.IsInitialized())
        {
            return MarketDataResponseType.LAST_PRICE;
        }
        else if (response.Orderbook.IsInitialized())
        {
            return MarketDataResponseType.ORDER_BOOK;
        }
        else if (response.Trade.IsInitialized())
        {
            return MarketDataResponseType.TRADE;
        }
        else if (response.TradingStatus.IsInitialized())
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