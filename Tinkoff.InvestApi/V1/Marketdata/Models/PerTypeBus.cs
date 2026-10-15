using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Tinkoff.InvestApi.V1.Marketdata.Models;

/// <summary>
/// Шина для одного типа market data: неограниченный канал входящих ответов,
/// глобальные и локальные (постримовые) обработчики. Обобщает логику,
/// которая ранее дублировалась для каждого типа сообщения в <see cref="MarketDataStreamContext"/>.
/// </summary>
/// <typeparam name="TWrapper">Тип данных market data (CandleModel, TradeModel и т.д.).</typeparam>
internal sealed class PerTypeBus<TWrapper>
{
    private readonly ConcurrentQueue<Func<TWrapper, Task>> _globalHandlers = new();
    private readonly Channel<TWrapper> _channel = Channel.CreateUnbounded<TWrapper>(new UnboundedChannelOptions { SingleReader = true });
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<TWrapper, Task>>>> _localHandlers = new();
    private readonly Func<TWrapper, IEnumerable<LocalSubscriptionKey>> _keySelector;

    public PerTypeBus(Func<TWrapper, IEnumerable<LocalSubscriptionKey>> keySelector)
    {
        _keySelector = keySelector;
    }

    /// <summary>
    /// Регистрирует глобальный обработчик, вызываемый для ответов любого стрима.
    /// </summary>
    public void On(Func<TWrapper, Task> handler) => _globalHandlers.Enqueue(handler);

    /// <summary>
    /// Регистрирует локальный обработчик для конкретного стрима и ключа инструмента.
    /// </summary>
    public void RegisterLocal(Guid streamGuid, LocalSubscriptionKey instrumentKey, Func<TWrapper, Task> handler)
    {
        var byKey = _localHandlers.GetOrAdd(streamGuid, _ => new ConcurrentDictionary<LocalSubscriptionKey, ConcurrentQueue<Func<TWrapper, Task>>>());
        var queue = byKey.GetOrAdd(instrumentKey, _ => new ConcurrentQueue<Func<TWrapper, Task>>());
        queue.Enqueue(handler);
    }

    /// <summary>
    /// Снимает все локальные обработчики для стрима и ключа инструмента.
    /// </summary>
    public void UnregisterLocal(Guid streamGuid, LocalSubscriptionKey instrumentKey)
    {
        if (_localHandlers.TryGetValue(streamGuid, out var byKey))
        {
            byKey.TryRemove(instrumentKey, out _);
        }
    }

    /// <summary>
    /// Удаляет все локальные обработчики всех стримов.
    /// </summary>
    public void ClearLocal() => _localHandlers.Clear();

    /// <summary>
    /// Асинхронно читает следующее сообщение из общего канала.
    /// </summary>
    public ValueTask<TWrapper> ReadAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAsync(cancellationToken);

    /// <summary>
    /// Пишет сообщение в канал, вызывает глобальные обработчики и диспатчит
    /// по локальным обработчикам стрима, если <paramref name="streamGuid"/> задан.
    /// </summary>
    public async Task PushAsync(TWrapper wrapper, Guid? streamGuid = null)
    {
        await _channel.Writer.WriteAsync(wrapper);
        foreach (var handler in _globalHandlers)
        {
            await handler(wrapper);
        }

        if (streamGuid.HasValue && _localHandlers.TryGetValue(streamGuid.Value, out var byKey))
        {
            foreach (var key in _keySelector(wrapper))
            {
                if (byKey.TryGetValue(key, out var queue))
                {
                    foreach (var handler in queue)
                    {
                        await handler(wrapper);
                    }
                }
            }
        }
    }
}