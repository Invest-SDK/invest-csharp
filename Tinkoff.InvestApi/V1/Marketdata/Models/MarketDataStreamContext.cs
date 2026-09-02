using System.Collections.Concurrent;
using System.Threading.Channels;
using Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;
using Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper;

namespace Tinkoff.InvestApi.Stream.Marketdata.Models;

public class MarketDataStreamContext
{
    // Candle
    private readonly ConcurrentQueue<Func<CandleWrapper, Task>> globalCandleEventQueue = new();
    private readonly ConcurrentQueue<Func<CandleWrapper, Task>> onCandleEventQueue = new();
    private readonly Channel<CandleWrapper> candlesChannel = Channel.CreateUnbounded<CandleWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // Trade
    private readonly ConcurrentQueue<Func<TradeWrapper, Task>> globalTradeEventQueue = new();
    private readonly ConcurrentQueue<Func<TradeWrapper, Task>> onTradeEventQueue = new();
    private readonly Channel<TradeWrapper> tradesChannel = Channel.CreateUnbounded<TradeWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // OrderBook
    private readonly ConcurrentQueue<Func<OrderBookWrapper, Task>> globalOrderBookEventQueue = new();
    private readonly ConcurrentQueue<Func<OrderBookWrapper, Task>> onOrderBookEventQueue = new();
    private readonly Channel<OrderBookWrapper> orderBooksChannel = Channel.CreateUnbounded<OrderBookWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // LastPrice
    private readonly ConcurrentQueue<Func<LastPriceWrapper, Task>> globalLastPriceEventQueue = new();
    private readonly ConcurrentQueue<Func<LastPriceWrapper, Task>> onLastPriceEventQueue = new();
    private readonly Channel<LastPriceWrapper> lastPricesChannel = Channel.CreateUnbounded<LastPriceWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // OpenInterest
    private readonly ConcurrentQueue<Func<OpenInterestWrapper, Task>> globalOpenInterestEventQueue = new();
    private readonly ConcurrentQueue<Func<OpenInterestWrapper, Task>> onOpenInterestEventQueue = new();
    private readonly Channel<OpenInterestWrapper> openInterestsChannel = Channel.CreateUnbounded<OpenInterestWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // TradingStatus
    private readonly ConcurrentQueue<Func<TradingStatusWrapper, Task>> globalTradingStatusEventQueue = new();
    private readonly ConcurrentQueue<Func<TradingStatusWrapper, Task>> onTradingStatusEventQueue = new();
    private readonly Channel<TradingStatusWrapper> tradingStatusesChannel = Channel.CreateUnbounded<TradingStatusWrapper>(new UnboundedChannelOptions { SingleReader = true });
}
