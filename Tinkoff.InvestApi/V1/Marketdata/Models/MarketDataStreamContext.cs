using System.Collections.Concurrent;
using System.Threading.Channels;
using Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;
using Tinkoff.InvestApi.V1;
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
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<CandleWrapper, Task>>>> localCandleHandlers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<TradeWrapper, Task>>>> localTradeHandlers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<OrderBookWrapper, Task>>>> localOrderBookHandlers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<LastPriceWrapper, Task>>>> localLastPriceHandlers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<OpenInterestWrapper, Task>>>> localOpenInterestHandlers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<TradingStatusWrapper, Task>>>> localTradingStatusHandlers = new();

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
    internal void RegisterLocalCandleHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<CandleWrapper, Task> handler)
        => RegisterLocal(localCandleHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики свечей для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalCandleHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => UnregisterLocal(localCandleHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Регистрирует локальный обработчик сделок для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalTradeHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<TradeWrapper, Task> handler)
        => RegisterLocal(localTradeHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики сделок для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalTradeHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => UnregisterLocal(localTradeHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Регистрирует локальный обработчик стаканов для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalOrderBookHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<OrderBookWrapper, Task> handler)
        => RegisterLocal(localOrderBookHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики стаканов для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalOrderBookHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => UnregisterLocal(localOrderBookHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Регистрирует локальный обработчик цен последней сделки для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalLastPriceHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<LastPriceWrapper, Task> handler)
        => RegisterLocal(localLastPriceHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики цен последней сделки для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalLastPriceHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => UnregisterLocal(localLastPriceHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Регистрирует локальный обработчик открытых позиций для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalOpenInterestHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<OpenInterestWrapper, Task> handler)
        => RegisterLocal(localOpenInterestHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики открытых позиций для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalOpenInterestHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => UnregisterLocal(localOpenInterestHandlers, streamGuid, instrumentKey);

    /// <summary>
    /// Регистрирует локальный обработчик торговых статусов для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalTradingStatusHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<TradingStatusWrapper, Task> handler)
        => RegisterLocal(localTradingStatusHandlers, streamGuid, instrumentKey, handler);

    /// <summary>
    /// Снимает все локальные обработчики торговых статусов для стрима и инструмента.
    /// </summary>
    internal void UnregisterLocalTradingStatusHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => UnregisterLocal(localTradingStatusHandlers, streamGuid, instrumentKey);

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
        localTradingStatusHandlers.Clear();
    }

    private static void RegisterLocal<TWrapper>(
        ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<TWrapper, Task>>>> localHandlers,
        Guid streamGuid,
        LocalSubscriptionKey instrumentKey,
        Func<TWrapper, Task> handler)
    {
        var byKey = localHandlers.GetOrAdd(streamGuid, _ => new ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<TWrapper, Task>>>());
        var queue = byKey.GetOrAdd(instrumentKey, _ => new ConcurrentQueue<Func<TWrapper, Task>>());
        queue.Enqueue(handler);
    }

    private static void UnregisterLocal<TWrapper>(
        ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<TWrapper, Task>>>> localHandlers,
        Guid streamGuid,
        LocalSubscriptionKey instrumentKey)
    {
        if (localHandlers.TryGetValue(streamGuid, out var byKey))
        {
            byKey.TryRemove(instrumentKey, out _);
        }
    }

    private static async Task DispatchLocalAsync<TWrapper>(
        ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<TWrapper, Task>>>> localHandlers,
        Guid? streamGuid,
        TWrapper wrapper,
        LocalSubscriptionKey instrumentKey)
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
        await DispatchLocalAsync(localCandleHandlers, streamGuid, wrapper, new LocalSubscriptionKey(wrapper.Figi(), wrapper.InstrumentUid(), wrapper.Interval().ToString()));
    }

    internal async Task PushTradeAsync(TradeWrapper wrapper, Guid? streamGuid = null)
    {
        await tradesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalTradeEventQueue)
        {
            await handler(wrapper);
        }
        await DispatchLocalAsync(localTradeHandlers, streamGuid, wrapper, new LocalSubscriptionKey(wrapper.Figi(), wrapper.InstrumentUid(), string.Empty));
    }

    internal async Task PushOrderBookAsync(OrderBookWrapper wrapper, Guid? streamGuid = null)
    {
        await orderBooksChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalOrderBookEventQueue)
        {
            await handler(wrapper);
        }
        // Диспатчим по конкретному типу пуша и по подписке All: подписка с конкретным типом
        // получает только пуши того же типа, а подписка All — пуши любого типа.
        await DispatchLocalAsync(localOrderBookHandlers, streamGuid, wrapper, new LocalSubscriptionKey(wrapper.Figi(), wrapper.InstrumentUid(), wrapper.OrderBookType().ToString()));
        await DispatchLocalAsync(localOrderBookHandlers, streamGuid, wrapper, new LocalSubscriptionKey(wrapper.Figi(), wrapper.InstrumentUid(), OrderBookType.All.ToString()));
    }

    internal async Task PushLastPriceAsync(LastPriceWrapper wrapper, Guid? streamGuid = null)
    {
        await lastPricesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalLastPriceEventQueue)
        {
            await handler(wrapper);
        }
        await DispatchLocalAsync(localLastPriceHandlers, streamGuid, wrapper, new LocalSubscriptionKey(wrapper.Figi(), wrapper.InstrumentUid(), string.Empty));
    }

    internal async Task PushOpenInterestAsync(OpenInterestWrapper wrapper, Guid? streamGuid = null)
    {
        await openInterestsChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalOpenInterestEventQueue)
        {
            await handler(wrapper);
        }
        await DispatchLocalAsync(localOpenInterestHandlers, streamGuid, wrapper, new LocalSubscriptionKey(string.Empty, wrapper.InstrumentUid(), string.Empty));
    }

    internal async Task PushTradingStatusAsync(TradingStatusWrapper wrapper, Guid? streamGuid = null)
    {
        await tradingStatusesChannel.Writer.WriteAsync(wrapper);
        foreach (var handler in globalTradingStatusEventQueue)
        {
            await handler(wrapper);
        }
        await DispatchLocalAsync(localTradingStatusHandlers, streamGuid, wrapper, new LocalSubscriptionKey(wrapper.Figi(), wrapper.InstrumentUid(), string.Empty));
    }
}

/// <summary>
/// Ключ локального обработчика: id инструмента (FIGI/UID) и дискриминатор подписки
/// (интервал для свечей, тип для стакана; пуст для остальных типов).
/// Equals сопоставляет id по указанным полям: пустое поле одной стороны не участвует в сравнении —
/// ответ с полным инструментом находит обработчик, зарегистрированный по неполному id.
/// GetHashCode зависит только от дискриминатора, чтобы равные по Equals ключи попадали в одну корзину словаря.
/// </summary>
internal sealed class LocalSubscriptionKey : IEquatable<LocalSubscriptionKey>
{
    public LocalSubscriptionKey(string figi, string uid, string extra)
    {
        Figi = figi;
        Uid = uid;
        Extra = extra;
    }

    public string Figi { get; }
    public string Uid { get; }
    public string Extra { get; }

    public bool Equals(LocalSubscriptionKey? other)
    {
        if (other is null) return false;
        if (Extra != other.Extra) return false;
        if (!string.IsNullOrEmpty(Figi) && Figi != other.Figi) return false;
        if (!string.IsNullOrEmpty(Uid) && Uid != other.Uid) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is LocalSubscriptionKey other && Equals(other);

    public override int GetHashCode() => Extra.GetHashCode();

    public override string ToString() => $"{Figi}:{Uid}:{Extra}";
}