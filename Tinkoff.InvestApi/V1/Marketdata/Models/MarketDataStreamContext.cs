using System.Collections.Concurrent;
using System.Threading.Channels;
using Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;
using Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper;

namespace Tinkoff.InvestApi.Stream.Marketdata.Models;

/// <summary>
/// Общий контекст стримов market data.
/// Хранит очереди входящих ответов (неограниченные каналы) и обработчики событий,
/// глобальные для всех стримов. Используется менеджером для маршрутизации ответов
/// от нескольких стримов в единую точку потребления.
/// </summary>
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

    /// <summary>
    /// Регистрирует глобальный обработчик свечей, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnCandle(Func<CandleWrapper, Task> handler) => globalCandleEventQueue.Enqueue(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик сделок, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnTrade(Func<TradeWrapper, Task> handler) => globalTradeEventQueue.Enqueue(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик стаканов, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnOrderBook(Func<OrderBookWrapper, Task> handler) => globalOrderBookEventQueue.Enqueue(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик цен последней сделки, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnLastPrice(Func<LastPriceWrapper, Task> handler) => globalLastPriceEventQueue.Enqueue(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик открытых позиций, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnOpenInterest(Func<OpenInterestWrapper, Task> handler) => globalOpenInterestEventQueue.Enqueue(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик статусов торговли, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnTradingStatus(Func<TradingStatusWrapper, Task> handler) => globalTradingStatusEventQueue.Enqueue(handler);

    /// <summary>
    /// Асинхронно читает следующую свечу из общего канала.
    /// </summary>
    public ValueTask<CandleWrapper> GetCandlesAsync(CancellationToken cancellationToken = default) =>
        candlesChannel.Reader.ReadAsync(cancellationToken);

    /// <summary>
    /// Асинхронно читает следующую сделку из общего канала.
    /// </summary>
    public ValueTask<TradeWrapper> GetTradesAsync(CancellationToken cancellationToken = default) =>
        tradesChannel.Reader.ReadAsync(cancellationToken);

    /// <summary>
    /// Асинхронно читает следующий стакан из общего канала.
    /// </summary>
    public ValueTask<OrderBookWrapper> GetOrderBooksAsync(CancellationToken cancellationToken = default) =>
        orderBooksChannel.Reader.ReadAsync(cancellationToken);

    /// <summary>
    /// Асинхронно читает следующую цену последней сделки из общего канала.
    /// </summary>
    public ValueTask<LastPriceWrapper> GetLastPricesAsync(CancellationToken cancellationToken = default) =>
        lastPricesChannel.Reader.ReadAsync(cancellationToken);

    /// <summary>
    /// Асинхронно читает следующее значение открытых позиций из общего канала.
    /// </summary>
    public ValueTask<OpenInterestWrapper> GetOpenInterestsAsync(CancellationToken cancellationToken = default) =>
        openInterestsChannel.Reader.ReadAsync(cancellationToken);

    /// <summary>
    /// Асинхронно читает следующий статус торговли из общего канала.
    /// </summary>
    public ValueTask<TradingStatusWrapper> GetTradingStatusesAsync(CancellationToken cancellationToken = default) =>
        tradingStatusesChannel.Reader.ReadAsync(cancellationToken);

    internal async Task PushCandleAsync(CandleWrapper wrapper)
    {
        await candlesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalCandleEventQueue)
        {
            await handler(wrapper);
        }
    }

    internal async Task PushTradeAsync(TradeWrapper wrapper)
    {
        await tradesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalTradeEventQueue)
        {
            await handler(wrapper);
        }
    }

    internal async Task PushOrderBookAsync(OrderBookWrapper wrapper)
    {
        await orderBooksChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalOrderBookEventQueue)
        {
            await handler(wrapper);
        }
    }

    internal async Task PushLastPriceAsync(LastPriceWrapper wrapper)
    {
        await lastPricesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalLastPriceEventQueue)
        {
            await handler(wrapper);
        }
    }

    internal async Task PushOpenInterestAsync(OpenInterestWrapper wrapper)
    {
        await openInterestsChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalOpenInterestEventQueue)
        {
            await handler(wrapper);
        }
    }

    internal async Task PushTradingStatusAsync(TradingStatusWrapper wrapper)
    {
        await tradingStatusesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalTradingStatusEventQueue)
        {
            await handler(wrapper);
        }
    }
}