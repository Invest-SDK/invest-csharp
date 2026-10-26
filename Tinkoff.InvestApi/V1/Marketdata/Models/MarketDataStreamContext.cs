using Tinkoff.InvestApi.V1.Marketdata.Models.Response;

namespace Tinkoff.InvestApi.V1.Marketdata.Models;

/// <summary>
/// Общий контекст стримов market data.
/// Хранит очереди входящих ответов (неограниченные каналы) и обработчики событий,
/// глобальные для всех стримов. Используется менеджером для маршрутизации ответов
/// от нескольких стримов в единую точку потребления.
/// Логика каналов и обработчиков вынесена в generic-шину <see cref="PerTypeBus{TWrapper}"/>.
/// </summary>
public class MarketDataStreamContext
{
    private readonly PerTypeBus<CandleModel> _candles = new(
        c => [new LocalSubscriptionKey(c.Figi, c.InstrumentUid, c.Interval.ToString())]);
    private readonly PerTypeBus<TradeModel> _trades = new(
        t => [new LocalSubscriptionKey(t.Figi, t.InstrumentUid, string.Empty)]);
    private readonly PerTypeBus<OrderBookModel> _orderBooks = new(
        o =>
        [
            new LocalSubscriptionKey(o.Figi, o.InstrumentUid, o.OrderBookType.ToString()),
            new LocalSubscriptionKey(o.Figi, o.InstrumentUid, OrderBookType.All.ToString())
        ]);
    private readonly PerTypeBus<LastPriceModel> _lastPrices = new(
        p => [new LocalSubscriptionKey(p.Figi, p.InstrumentUid, string.Empty)]);
    private readonly PerTypeBus<OpenInterestModel> _openInterests = new(
        o => [new LocalSubscriptionKey(string.Empty, o.InstrumentUid, string.Empty)]);
    private readonly PerTypeBus<TradingStatusModel> _tradingStatuses = new(
        s => [new LocalSubscriptionKey(s.Figi, s.InstrumentUid, string.Empty)]);

    /// <summary>
    /// Регистрирует глобальный обработчик свечей, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnCandle(Func<CandleModel, Task> handler) => _candles.On(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик сделок, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnTrade(Func<TradeModel, Task> handler) => _trades.On(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик стаканов, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnOrderBook(Func<OrderBookModel, Task> handler) => _orderBooks.On(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик цен последней сделки, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnLastPrice(Func<LastPriceModel, Task> handler) => _lastPrices.On(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик открытых позиций, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnOpenInterest(Func<OpenInterestModel, Task> handler) => _openInterests.On(handler);

    /// <summary>
    /// Регистрирует глобальный обработчик статусов торговли, вызываемый для ответов любого стрима.
    /// </summary>
    public void OnTradingStatus(Func<TradingStatusModel, Task> handler) => _tradingStatuses.On(handler);

    /// <summary>
    /// Регистрирует локальный обработчик свечей для конкретного стрима и инструмента.
    /// </summary>
    internal void RegisterLocalCandleHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<CandleModel, Task> handler)
        => _candles.RegisterLocal(streamGuid, instrumentKey, handler);

    internal void UnregisterLocalCandleHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => _candles.UnregisterLocal(streamGuid, instrumentKey);

    internal void RegisterLocalTradeHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<TradeModel, Task> handler)
        => _trades.RegisterLocal(streamGuid, instrumentKey, handler);

    internal void UnregisterLocalTradeHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => _trades.UnregisterLocal(streamGuid, instrumentKey);

    internal void RegisterLocalOrderBookHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<OrderBookModel, Task> handler)
        => _orderBooks.RegisterLocal(streamGuid, instrumentKey, handler);

    internal void UnregisterLocalOrderBookHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => _orderBooks.UnregisterLocal(streamGuid, instrumentKey);

    internal void RegisterLocalLastPriceHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<LastPriceModel, Task> handler)
        => _lastPrices.RegisterLocal(streamGuid, instrumentKey, handler);

    internal void UnregisterLocalLastPriceHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => _lastPrices.UnregisterLocal(streamGuid, instrumentKey);

    internal void RegisterLocalOpenInterestHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<OpenInterestModel, Task> handler)
        => _openInterests.RegisterLocal(streamGuid, instrumentKey, handler);

    internal void UnregisterLocalOpenInterestHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => _openInterests.UnregisterLocal(streamGuid, instrumentKey);

    internal void RegisterLocalTradingStatusHandler(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<TradingStatusModel, Task> handler)
        => _tradingStatuses.RegisterLocal(streamGuid, instrumentKey, handler);

    internal void UnregisterLocalTradingStatusHandlers(Guid streamGuid, LocalSubscriptionKey instrumentKey)
        => _tradingStatuses.UnregisterLocal(streamGuid, instrumentKey);

    /// <summary>
    /// Удаляет все локальные обработчики всех стримов.
    /// </summary>
    internal void ClearLocalHandlers()
    {
        _candles.ClearLocal();
        _trades.ClearLocal();
        _orderBooks.ClearLocal();
        _lastPrices.ClearLocal();
        _openInterests.ClearLocal();
        _tradingStatuses.ClearLocal();
    }

    internal Task PushCandleAsync(CandleModel wrapper, Guid? streamGuid = null) => _candles.PushAsync(wrapper, streamGuid);

    internal Task PushTradeAsync(TradeModel wrapper, Guid? streamGuid = null) => _trades.PushAsync(wrapper, streamGuid);

    internal Task PushOrderBookAsync(OrderBookModel wrapper, Guid? streamGuid = null) => _orderBooks.PushAsync(wrapper, streamGuid);

    internal Task PushLastPriceAsync(LastPriceModel wrapper, Guid? streamGuid = null) => _lastPrices.PushAsync(wrapper, streamGuid);

    internal Task PushOpenInterestAsync(OpenInterestModel wrapper, Guid? streamGuid = null) => _openInterests.PushAsync(wrapper, streamGuid);

    internal Task PushTradingStatusAsync(TradingStatusModel wrapper, Guid? streamGuid = null) => _tradingStatuses.PushAsync(wrapper, streamGuid);
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