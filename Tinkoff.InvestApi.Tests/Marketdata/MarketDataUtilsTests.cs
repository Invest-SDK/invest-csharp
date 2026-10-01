using System.Collections.Generic;
using FluentAssertions;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata;
using Tinkoff.InvestApi.V1.Marketdata.Models;
using Xunit;

namespace Tinkoff.InvestApi.Tests.Marketdata;

public class MarketDataUtilsTests
{
    private const string Figi = "BBG000000000";
    private const string Uid = "00000000-0000-0000-0000-000000000000";
    private const string Ticker = "TICK";
    private const string ClassCode = "MOEX";

    #region GetResponseType

    [Theory]
    [MemberData(nameof(GetResponseTypeTestData))]
    public void GetResponseType_ShouldReturnExpectedType(MarketDataResponse response, MarketDataResponseType? expected)
    {
        MarketDataUtils.GetResponseType(response).Should().Be(expected);
    }

    public static IEnumerable<object[]> GetResponseTypeTestData()
    {
        yield return new object[] { new MarketDataResponse { Candle = new Candle { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } }, MarketDataResponseType.CANDLE };
        yield return new object[] { new MarketDataResponse { Trade = new Trade { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } }, MarketDataResponseType.TRADE };
        yield return new object[] { new MarketDataResponse { Orderbook = new OrderBook { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } }, MarketDataResponseType.ORDER_BOOK };
        yield return new object[] { new MarketDataResponse { LastPrice = new LastPrice { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } }, MarketDataResponseType.LAST_PRICE };
        yield return new object[] { new MarketDataResponse { OpenInterest = new OpenInterest { InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } }, MarketDataResponseType.OPEN_INTEREST };
        yield return new object[] { new MarketDataResponse { TradingStatus = new TradingStatus { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } }, MarketDataResponseType.TRADING_STATUS };
        yield return new object[] { new MarketDataResponse(), null };
    }

    #endregion

    #region GetInstrumentsFromResponse

    [Theory]
    [MemberData(nameof(GetInstrumentsFromResponseTestData))]
    public void GetInstrumentsFromResponse_ShouldReturnExpectedInstrument(MarketDataResponse response, MarketDataInstrument expected)
    {
        MarketDataUtils.GetInstrumentsFromResponse(response).Should().BeEquivalentTo(expected);
    }

    public static IEnumerable<object[]> GetInstrumentsFromResponseTestData()
    {
        yield return new object[]
        {
            new MarketDataResponse { Candle = new Candle { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode, Interval = SubscriptionInterval.OneMinute } },
            new MarketDataInstrument(Figi: Figi, Uid: Uid, TickerClassCode: $"{Ticker}_{ClassCode}", Interval: SubscriptionInterval.OneMinute)
        };
        yield return new object[]
        {
            new MarketDataResponse { Trade = new Trade { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } },
            new MarketDataInstrument(Figi: Figi, Uid: Uid, TickerClassCode: $"{Ticker}_{ClassCode}")
        };
        yield return new object[]
        {
            new MarketDataResponse { Orderbook = new OrderBook { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode, Depth = 10, OrderBookType = OrderBookType.Exchange } },
            new MarketDataInstrument(Figi: Figi, Uid: Uid, TickerClassCode: $"{Ticker}_{ClassCode}", Depth: 10, OrderBook: OrderBookType.Exchange)
        };
        yield return new object[]
        {
            new MarketDataResponse { LastPrice = new LastPrice { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } },
            new MarketDataInstrument(Figi: Figi, Uid: Uid, TickerClassCode: $"{Ticker}_{ClassCode}")
        };
        yield return new object[]
        {
            new MarketDataResponse { OpenInterest = new OpenInterest { InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } },
            new MarketDataInstrument(Uid: Uid, TickerClassCode: $"{Ticker}_{ClassCode}")
        };
        yield return new object[]
        {
            new MarketDataResponse { TradingStatus = new TradingStatus { Figi = Figi, InstrumentUid = Uid, Ticker = Ticker, ClassCode = ClassCode } },
            new MarketDataInstrument(Figi: Figi, Uid: Uid, TickerClassCode: $"{Ticker}_{ClassCode}")
        };
        yield return new object[] { new MarketDataResponse(), null };
    }

    #endregion

    #region GetActionFromResponse

    [Theory]
    [MemberData(nameof(GetActionFromResponseTestData))]
    public void GetActionFromResponse_ShouldReturnExpectedAction(MarketDataResponse response, MarketDataAction expected)
    {
        MarketDataUtils.GetActionFromResponse(response).Should().Be(expected);
    }

    public static IEnumerable<object[]> GetActionFromResponseTestData()
    {
        yield return new object[] { BuildCandleSubscriptionResponse(SubscriptionAction.Subscribe), MarketDataAction.SUBSCRIBE };
        yield return new object[] { BuildCandleSubscriptionResponse(SubscriptionAction.Unsubscribe), MarketDataAction.UNSUBSCRIBE };
        yield return new object[] { BuildLastPriceSubscriptionResponse(SubscriptionAction.Subscribe), MarketDataAction.SUBSCRIBE };
        yield return new object[] { BuildOrderBookSubscriptionResponse(SubscriptionAction.Subscribe), MarketDataAction.SUBSCRIBE };
        yield return new object[] { BuildTradesSubscriptionResponse(SubscriptionAction.Subscribe), MarketDataAction.SUBSCRIBE };
        yield return new object[] { BuildInfoSubscriptionResponse(SubscriptionAction.Unsubscribe), MarketDataAction.UNSUBSCRIBE };
        yield return new object[] { new MarketDataResponse(), MarketDataAction.UNKNOWN };
    }

    #endregion

    #region GetSubscriptionResultFromResponse

    [Theory]
    [MemberData(nameof(GetSubscriptionResultFromResponseTestData))]
    public void GetSubscriptionResultFromResponse_ShouldReturnExpectedResult(MarketDataResponse response, MarketDataResponseType expectedType, MarketDataAction expectedAction)
    {
        var result = MarketDataUtils.GetSubscriptionResultFromResponse(response);

        result.Should().NotBeNull();
        result!.ResponseType.Should().Be(expectedType);
        result.Action.Should().Be(expectedAction);
    }

    public static IEnumerable<object[]> GetSubscriptionResultFromResponseTestData()
    {
        yield return new object[] { BuildCandleSubscriptionResponse(SubscriptionAction.Subscribe), MarketDataResponseType.CANDLE, MarketDataAction.SUBSCRIBE };
        yield return new object[] { BuildLastPriceSubscriptionResponse(SubscriptionAction.Subscribe), MarketDataResponseType.LAST_PRICE, MarketDataAction.SUBSCRIBE };
        yield return new object[] { BuildOrderBookSubscriptionResponse(SubscriptionAction.Subscribe), MarketDataResponseType.ORDER_BOOK, MarketDataAction.SUBSCRIBE };
        yield return new object[] { BuildTradesSubscriptionResponse(SubscriptionAction.Subscribe), MarketDataResponseType.TRADE, MarketDataAction.SUBSCRIBE };
        yield return new object[] { BuildInfoSubscriptionResponse(SubscriptionAction.Unsubscribe), MarketDataResponseType.TRADING_STATUS, MarketDataAction.UNSUBSCRIBE };
    }

    [Theory]
    [MemberData(nameof(GetSubscriptionStatusTestData))]
    public void GetSubscriptionResultFromResponse_ShouldMapSubscriptionStatus(MarketDataResponse response, MarketDataSubscriptionStatus expected)
    {
        var result = MarketDataUtils.GetSubscriptionResultFromResponse(response);

        result.Should().NotBeNull();
        result!.SubsciptionStatusDictionary.Values.Should().ContainSingle().Which.Should().Be(expected);
    }

    public static IEnumerable<object[]> GetSubscriptionStatusTestData()
    {
        yield return new object[] { BuildCandleSubscriptionResponse(SubscriptionAction.Subscribe, SubscriptionStatus.Success), MarketDataSubscriptionStatus.OK };
        yield return new object[] { BuildCandleSubscriptionResponse(SubscriptionAction.Subscribe, SubscriptionStatus.SubscriptionActionIsInvalid), MarketDataSubscriptionStatus.ERROR };
    }

    [Fact]
    public void GetSubscriptionResultFromResponse_EmptyResponse_ShouldReturnNull()
    {
        MarketDataUtils.GetSubscriptionResultFromResponse(new MarketDataResponse()).Should().BeNull();
    }

    #endregion

    #region Builders

    private static MarketDataResponse BuildCandleSubscriptionResponse(SubscriptionAction action, SubscriptionStatus status = SubscriptionStatus.Success)
    {
        var item = new CandleSubscription
        {
            Figi = Figi,
            InstrumentUid = Uid,
            Ticker = Ticker,
            ClassCode = ClassCode,
            Interval = SubscriptionInterval.OneMinute,
            SubscriptionAction = action,
            SubscriptionStatus = status,
        };
        return new MarketDataResponse
        {
            SubscribeCandlesResponse = new SubscribeCandlesResponse { CandlesSubscriptions = { item } }
        };
    }

    private static MarketDataResponse BuildLastPriceSubscriptionResponse(SubscriptionAction action, SubscriptionStatus status = SubscriptionStatus.Success)
    {
        var item = new LastPriceSubscription
        {
            Figi = Figi,
            InstrumentUid = Uid,
            Ticker = Ticker,
            ClassCode = ClassCode,
            SubscriptionAction = action,
            SubscriptionStatus = status,
        };
        return new MarketDataResponse
        {
            SubscribeLastPriceResponse = new SubscribeLastPriceResponse { LastPriceSubscriptions = { item } }
        };
    }

    private static MarketDataResponse BuildOrderBookSubscriptionResponse(SubscriptionAction action, SubscriptionStatus status = SubscriptionStatus.Success)
    {
        var item = new OrderBookSubscription
        {
            Figi = Figi,
            InstrumentUid = Uid,
            Ticker = Ticker,
            ClassCode = ClassCode,
            Depth = 10,
            OrderBookType = OrderBookType.Exchange,
            SubscriptionAction = action,
            SubscriptionStatus = status,
        };
        return new MarketDataResponse
        {
            SubscribeOrderBookResponse = new SubscribeOrderBookResponse { OrderBookSubscriptions = { item } }
        };
    }

    private static MarketDataResponse BuildTradesSubscriptionResponse(SubscriptionAction action, SubscriptionStatus status = SubscriptionStatus.Success)
    {
        var item = new TradeSubscription
        {
            Figi = Figi,
            InstrumentUid = Uid,
            Ticker = Ticker,
            ClassCode = ClassCode,
            SubscriptionAction = action,
            SubscriptionStatus = status,
        };
        return new MarketDataResponse
        {
            SubscribeTradesResponse = new SubscribeTradesResponse { TradeSubscriptions = { item } }
        };
    }

    private static MarketDataResponse BuildInfoSubscriptionResponse(SubscriptionAction action, SubscriptionStatus status = SubscriptionStatus.Success)
    {
        var item = new InfoSubscription
        {
            Figi = Figi,
            InstrumentUid = Uid,
            Ticker = Ticker,
            ClassCode = ClassCode,
            SubscriptionAction = action,
            SubscriptionStatus = status,
        };
        return new MarketDataResponse
        {
            SubscribeInfoResponse = new SubscribeInfoResponse { InfoSubscriptions = { item } }
        };
    }

    #endregion
}