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
    private readonly Channel<CandleWrapper> candlesChannel = Channel.CreateUnbounded<CandleWrapper>(new UnboundedChannelOptions { SingleReader = true });
    // Trade
    private readonly ConcurrentQueue<Func<TradeWrapper, Task>> globalTradeEventQueue = new();
    private readonly Channel<TradeWrapper> tradesChannel = Channel.CreateUnbounded<TradeWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // OrderBook
    private readonly ConcurrentQueue<Func<OrderBookWrapper, Task>> globalOrderBookEventQueue = new();
    private readonly Channel<OrderBookWrapper> orderBooksChannel = Channel.CreateUnbounded<OrderBookWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // LastPrice
    private readonly ConcurrentQueue<Func<LastPriceWrapper, Task>> globalLastPriceEventQueue = new();
    private readonly Channel<LastPriceWrapper> lastPricesChannel = Channel.CreateUnbounded<LastPriceWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // OpenInterest
    private readonly ConcurrentQueue<Func<OpenInterestWrapper, Task>> globalOpenInterestEventQueue = new();
    private readonly Channel<OpenInterestWrapper> openInterestsChannel = Channel.CreateUnbounded<OpenInterestWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // TradingStatus
    private readonly ConcurrentQueue<Func<TradingStatusWrapper, Task>> globalTradingStatusEventQueue = new();
    private readonly Channel<TradingStatusWrapper> tradingStatusesChannel = Channel.CreateUnbounded<TradingStatusWrapper>(new UnboundedChannelOptions { SingleReader = true });

    // Локальные (постримовые) обработчики: streamGuid -> (instrumentKey -> список обработчиков).
    // Регистрируются через Subscribe*Async и вызываются только для ответов конкретного стрима и инструмента.
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentQueue<Func<CandleWrapper, Task>>>> localCandleHandlers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentQueue<Func<TradeWrapper, Task>>>> localTradeHandlers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentQueue<Func<OrderBookWrapper, Task>>>> localOrderBookHandlers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentQueue<Func<LastPriceWrapper, Task>>>> localLastPriceHandlers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentQueue<Func<OpenInterestWrapper, Task>>>> localOpenInterestHandlers = new();

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
    /// Регистрирует локальный обработчик свечей для конкретного стрима и инструмента.
    /// Вызывается только для ответов со стрима <paramref name="streamGuid"/> по инструменту <paramref name="instrumentKey"/>.
    /// </summary>
    internal void RegisterLocalCandleHandler(Guid streamGuid, string instrumentKey, Func<CandleWrapper, Task> handler)
        => RegisterLocal(localCandleHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики свечей для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalCandleHandlers(Guid streamGuid, string instrumentKey)
        => UnregisterLocal(localCandleHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Регистрирует локальный обработчик сделок для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalTradeHandler(Guid streamGuid, string instrumentKey, Func<TradeWrapper, Task> handler)
        => RegisterLocal(localTradeHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики сделок для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalTradeHandlers(Guid streamGuid, string instrumentKey)
        => UnregisterLocal(localTradeHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Регистрирует локальный обработчик стаканов для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalOrderBookHandler(Guid streamGuid, string instrumentKey, Func<OrderBookWrapper, Task> handler)
        => RegisterLocal(localOrderBookHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики стаканов для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalOrderBookHandlers(Guid streamGuid, string instrumentKey)
        => UnregisterLocal(localOrderBookHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Регистрирует локальный обработчик цен последней сделки для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalLastPriceHandler(Guid streamGuid, string instrumentKey, Func<LastPriceWrapper, Task> handler)
        => RegisterLocal(localLastPriceHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики цен последней сделки для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalLastPriceHandlers(Guid streamGuid, string instrumentKey)
        => UnregisterLocal(localLastPriceHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Регистрирует локальный обработчик открытых позиций для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalOpenInterestHandler(Guid streamGuid, string instrumentKey, Func<OpenInterestWrapper, Task> handler)
        => RegisterLocal(localOpenInterestHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики открытых позиций для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalOpenInterestHandlers(Guid streamGuid, string instrumentKey)
        => UnregisterLocal(localOpenInterestHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Удаляет все локальные обработчики всех стримов.
    /// </summary>
    internal void ClearLocalHandlers()
    {
        localCandleHandlers.Clear();
        localTradeHandlers.Clear();
        localOrderBookHandlers.Clear();
        localLastPriceHandlers.Clear();
        localOpenInterestHandlers.Clear();
    }

    private static void RegisterLocal<TWrapper>(
        ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentQueue<Func<TWrapper, Task>>>> localHandlers,
        Guid streamGuid,
        string instrumentKey,
        Func<TWrapper, Task> handler)
    {
        var byKey = localHandlers.GetOrAdd(streamGuid, _ => new ConcurrentDictionary<string, ConcurrentQueue<Func<TWrapper, Task>>>());
        var queue = byKey.GetOrAdd(instrumentKey, _ => new ConcurrentQueue<Func<TWrapper, Task>>());
        queue.Enqueue(handler);
    }

    private static void UnregisterLocal<TWrapper>(
        ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentQueue<Func<TWrapper, Task>>>> localHandlers,
        Guid streamGuid,
        string instrumentKey)
    {
        if (localHandlers.TryGetValue(streamGuid, out var byKey))
        {
            byKey.TryRemove(instrumentKey, out _);
        }
    }

    private static async Task DispatchLocalAsync<TWrapper>(
        ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentQueue<Func<TWrapper, Task>>>> localHandlers,
        Guid? streamGuid,
        TWrapper wrapper,
        string instrumentKey)
    {
        if (streamGuid.HasValue
            && localHandlers.TryGetValue(streamGuid.Value, out var byKey)
            && byKey.TryGetValue(instrumentKey, out var queue))
        {
            foreach (var handler in queue)
            {
                await handler(wrapper);
            }
        }
    }

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

    internal async Task PushCandleAsync(CandleWrapper wrapper, Guid? streamGuid = null)
    {
        await candlesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalCandleEventQueue)
        {
            await handler(wrapper);
        }
        await DispatchLocalAsync(localCandleHandlers, streamGuid, wrapper, $"{wrapper.Figi()}:{wrapper.InstrumentUid()}");
    }

    internal async Task PushTradeAsync(TradeWrapper wrapper, Guid? streamGuid = null)
    {
        await tradesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalTradeEventQueue)
        {
            await handler(wrapper);
        }
        await DispatchLocalAsync(localTradeHandlers, streamGuid, wrapper, $"{wrapper.Figi()}:{wrapper.InstrumentUid()}");
    }

    internal async Task PushOrderBookAsync(OrderBookWrapper wrapper, Guid? streamGuid = null)
    {
        await orderBooksChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalOrderBookEventQueue)
        {
            await handler(wrapper);
        }
        await DispatchLocalAsync(localOrderBookHandlers, streamGuid, wrapper, $"{wrapper.Figi()}:{wrapper.InstrumentUid()}");
    }

    internal async Task PushLastPriceAsync(LastPriceWrapper wrapper, Guid? streamGuid = null)
    {
        await lastPricesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalLastPriceEventQueue)
        {
            await handler(wrapper);
        }
        await DispatchLocalAsync(localLastPriceHandlers, streamGuid, wrapper, $"{wrapper.Figi()}:{wrapper.InstrumentUid()}");
    }

    internal async Task PushOpenInterestAsync(OpenInterestWrapper wrapper, Guid? streamGuid = null)
    {
        await openInterestsChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalOpenInterestEventQueue)
        {
            await handler(wrapper);
        }
        await DispatchLocalAsync(localOpenInterestHandlers, streamGuid, wrapper, $"{wrapper.InstrumentUid()}");
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