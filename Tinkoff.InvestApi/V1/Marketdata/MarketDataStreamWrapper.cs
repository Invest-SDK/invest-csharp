using System.Collections.Concurrent;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.V1.Marketdata.Models;
using Tinkoff.InvestApi.V1.Marketdata.Models.Request;

namespace Tinkoff.InvestApi.V1.Marketdata;

/// <summary>
/// Обёртка для market data stream с поддержкой автоматической переподписки при реконнекте.
/// Сохраняет все запросы на подписку и автоматически восстанавливает их при переподключении.
/// </summary>
public class MarketDataStreamWrapper
{
    private readonly ILogger<MarketDataStreamWrapper>? _logger;
    private readonly ConcurrentQueue<ISubscriptionRequestBase> _subscriptionRequests = new();
    private readonly ConcurrentDictionary<string, ISubscriptionRequestBase> _activeSubscriptions = new();
    private readonly ConcurrentDictionary<PendingSubscriptionKey, TaskCompletionSource<MarketDataSubscriptionResult>> _pendingSubscriptions = new();
    private readonly ResilenceDuplexStreamWrapper<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> _streamWrapper;
    private readonly Func<MarketDataResponse, Task>? _responseHandler;
    private readonly Func<StreamInfo, Task>? _connectionHandler;
    private readonly SemaphoreSlim _connectionHandlerLock = new(1, 1);
    private volatile int _firstConnectionCompleted = 0;

    public MarketDataStreamWrapper(
        ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> configurer,
        Func<MarketDataResponse, Task>? responseHandler = null,
        Func<StreamInfo, Task>? connectionHandler = null,
        ILoggerFactory? loggerFactory = null)
    {
        _responseHandler = responseHandler;
        _connectionHandler = connectionHandler;
        _streamWrapper = new ResilenceDuplexStreamWrapper<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(
            configurer,
            HandleResponseAsync,
            HandleConnectionAsync,
            loggerFactory?.CreateLogger<ResilenceDuplexStreamWrapper<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>>()
            );
        _logger = loggerFactory?.CreateLogger<MarketDataStreamWrapper>();
    }

    /// <summary>
    /// Подключается к стриму
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _streamWrapper.ConnectAsync(cancellationToken);
    }

    /// <summary>
    /// Подписывается на свечи
    /// </summary>
    public async Task SubscribeCandlesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new CandleSubscriptionRequest(SubscriptionAction.Subscribe, instruments);
        await AddSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Отписывается от свечей
    /// </summary>
    public async Task UnsubscribeCandlesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new CandleSubscriptionRequest(SubscriptionAction.Unsubscribe, instruments);
        await RemoveSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Подписывается на стакан
    /// </summary>
    public async Task SubscribeOrderBookAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new OrderBookSubscriptionRequest(SubscriptionAction.Subscribe, instruments);
        await AddSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Отписывается от стакана
    /// </summary>
    public async Task UnsubscribeOrderBookAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new OrderBookSubscriptionRequest(SubscriptionAction.Unsubscribe, instruments);
        await RemoveSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Подписывается на сделки
    /// </summary>
    public async Task SubscribeTradesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new TradeSubscriptionRequest(SubscriptionAction.Subscribe, instruments);
        await AddSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Отписывается от сделок
    /// </summary>
    public async Task UnsubscribeTradesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new TradeSubscriptionRequest(SubscriptionAction.Unsubscribe, instruments);
        await RemoveSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Подписывается на статус сделки
    /// </summary>
    public async Task SubscribeTradingStatusesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new TradingStatusesSubscriptionRequest(SubscriptionAction.Subscribe, instruments);
        await AddSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Отписывается от статуса сделок
    /// </summary>
    public async Task UnsubscribeTradingStatusesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new TradingStatusesSubscriptionRequest(SubscriptionAction.Unsubscribe, instruments);
        await RemoveSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Подписывается на цену последней сделки
    /// </summary>
    public async Task SubscribeLastPriceAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new LastPriceSubscriptionRequest(SubscriptionAction.Subscribe, instruments);
        await AddSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Отписывается от цены последней сделки
    /// </summary>
    public async Task UnsubscribeLastPriceAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new LastPriceSubscriptionRequest(SubscriptionAction.Unsubscribe, instruments);
        await RemoveSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Подписывается на открытые позиции
    /// </summary>
    public async Task SubscribeOpenInterestAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new OpenInterestSubscriptionRequest(SubscriptionAction.Subscribe, instruments);
        await AddSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Отписывается от открытых позиций
    /// </summary>
    public async Task UnsubscribeOpenInterestAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
    {
        var request = new OpenInterestSubscriptionRequest(SubscriptionAction.Unsubscribe, instruments);
        await RemoveSubscriptionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Отключается от стрима
    /// </summary>
    public void Disconnect()
    {
        _streamWrapper.Disconnect();
    }

    /// <summary>
    /// Проверяет, подключен ли стрим
    /// </summary>
    public bool IsConnected()
    {
        return _streamWrapper.IsConnected();
    }

    /// <summary>
    /// Возвращает количество активных подписок на стриме.
    /// Используется менеджером для контроля лимита подписок на один стрим.
    /// </summary>
    public int GetSubscriptionCount()
    {
        return _activeSubscriptions.Count;
    }

    /// <summary>
    /// Возвращает количество активных подписок на стриме.
    /// Используется менеджером для контроля лимита подписок на один стрим.
    /// </summary>
    public int GetPendingSubscriptionsCount()
    {
        return _pendingSubscriptions.Count;
    }

    public ConcurrentDictionary<string, ISubscriptionRequestBase> GetActiveSubscriptions => _activeSubscriptions;

    public Guid Guid => _streamWrapper.StreamGuid;

    private async Task AddSubscriptionAsync(ISubscriptionRequestBase request, CancellationToken cancellationToken)
    {
        _subscriptionRequests.Enqueue(request);

        // Создаём ключи для всех инструментов в запросе
        var responseType = GetResponseTypeFromRequest(request);
        var instruments = GetInstrumentsFromRequest(request);

        // Создаём TaskCompletionSource для каждого инструмента и захватываем задачи локально:
        // обработчик ответа удаляет записи из _pendingSubscriptions, поэтому перечитывать их нельзя.
        // Ключ — обёртка с responseType и инструментом: Equals сопоставляет по указанным полям,
        // поэтому ответ с полным инструментом находит ожидание даже при неполном запросе.
        var pending = new List<(string Key, Task<MarketDataSubscriptionResult> Task)>();
        foreach (var i in instruments)
        {
            var key = GetSubscriptionKey(responseType, i);
            var tcs = new TaskCompletionSource<MarketDataSubscriptionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingSubscriptions.TryAdd(new PendingSubscriptionKey(responseType, i), tcs);
            pending.Add((key, tcs.Task));
        }

        await _streamWrapper.CallAsync(request.ToMarketDataRequest(), cancellationToken);

        // Ждём завершения HandleConnectionAsync перед продолжением
        await _connectionHandlerLock.WaitAsync(cancellationToken);
        _connectionHandlerLock.Release();

        // Дожидаемся ответа для всех инструментов и проверяем, что подписки успешны
        foreach (var (key, task) in pending)
        {
            var result = await task;
            if (result.SubsciptionStatusDictionary.Values.Any(s => s != MarketDataSubscriptionStatus.OK))
            {
                throw new InvalidOperationException($"Подписка не удалась для {key}");
            }
        }
    }

    private async Task RemoveSubscriptionAsync(ISubscriptionRequestBase request, CancellationToken cancellationToken)
    {
        _subscriptionRequests.Enqueue(request);

        // Создаём ключи для всех инструментов в запросе
        var responseType = GetResponseTypeFromRequest(request);
        var instruments = GetInstrumentsFromRequest(request);

        // Создаём TaskCompletionSource для каждого инструмента и захватываем задачи локально:
        // обработчик ответа удаляет записи из _pendingSubscriptions, поэтому перечитывать их нельзя.
        // Ключ — обёртка с responseType и инструментом: Equals сопоставляет по указанным полям,
        // поэтому ответ с полным инструментом находит ожидание даже при неполном запросе.
        var pending = new List<(string Key, Task<MarketDataSubscriptionResult> Task)>();
        foreach (var i in instruments)
        {
            var key = GetSubscriptionKey(responseType, i);
            var tcs = new TaskCompletionSource<MarketDataSubscriptionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingSubscriptions.TryAdd(new PendingSubscriptionKey(responseType, i), tcs);
            pending.Add((key, tcs.Task));
        }

        await _streamWrapper.CallAsync(request.ToMarketDataRequest(), cancellationToken);

        // Ждём завершения HandleConnectionAsync перед продолжением
        await _connectionHandlerLock.WaitAsync(cancellationToken);
        _connectionHandlerLock.Release();

        // Дожидаемся ответа для всех инструментов и проверяем, что отписки успешны
        foreach (var (key, task) in pending)
        {
            var result = await task;
            if (result.SubsciptionStatusDictionary.Values.Any(s => s != MarketDataSubscriptionStatus.OK))
            {
                throw new InvalidOperationException($"Отписка не удалась для {key}");
            }
        }
    }

    private async Task HandleConnectionAsync(AsyncDuplexStreamingCall<MarketDataRequest, MarketDataResponse> stream)
    {
        await _connectionHandlerLock.WaitAsync();
        try
        {
            // Пропускаем первое подключение — запросы будут отправлены через CallAsync
            if (Interlocked.CompareExchange(ref _firstConnectionCompleted, 1, 0) == 0)
            {
                return;
            }

            if (_connectionHandler != null)
            {
                await _connectionHandler(new StreamInfo(_streamWrapper.StreamGuid, stream));
            }

            // При переподключении отправляем все накопленные запросы на подписку
            var requests = _subscriptionRequests.ToList();
            foreach (var request in requests)
            {
                await stream.RequestStream.WriteAsync(request.ToMarketDataRequest());
            }
        }
        finally
        {
            _connectionHandlerLock.Release();
        }
    }

    private async Task HandleResponseAsync(MarketDataResponse response)
    {
        // Обработка ответа через MarketDataUtils
        var subscriptionResult = MarketDataUtils.GetSubscriptionResultFromResponse(response);
        if (subscriptionResult != null)
        {
            // Обновляем активные подписки на основе результата и завершаем ожидающие задачи
            foreach (var kvp in subscriptionResult.SubsciptionStatusDictionary)
            {
                // Завершаем ожидающую подписку: ключ-обёртка сопоставляется по указанным в запросе
                // полям инструмента, поэтому ответ с полным инструментом находит ожидание неполного запроса.
                if (_pendingSubscriptions.TryRemove(new PendingSubscriptionKey(subscriptionResult.ResponseType, kvp.Key), out var tcs))
                {
                    tcs.TrySetResult(subscriptionResult);
                }

                var key = GetSubscriptionKey(subscriptionResult.ResponseType, kvp.Key);

                // Обновляем активные подписки
                if (subscriptionResult.Action == MarketDataAction.SUBSCRIBE &&
                    kvp.Value == MarketDataSubscriptionStatus.OK)
                {
                    _activeSubscriptions.TryAdd(key, CreateSubscriptionRequest(subscriptionResult.ResponseType, kvp.Key, SubscriptionAction.Subscribe));
                }
                else if (subscriptionResult.Action == MarketDataAction.UNSUBSCRIBE &&
                         kvp.Value == MarketDataSubscriptionStatus.OK)
                {
                    _activeSubscriptions.TryRemove(key, out _);
                }
            }
        }

        if (response.Candle != null && response.Candle.IsInitialized())
        {
            var key = GetSubscriptionKey(MarketDataResponseType.CANDLE, new MarketDataInstrument(figi: response.Candle.Figi, uid: response.Candle.InstrumentUid, interval: response.Candle.Interval));
            _activeSubscriptions.TryGetValue(key, out var sub);
            if (sub != null)
            {
                var candleSub = (CandleSubscriptionRequest) sub;
                candleSub.LastInteractionTime = DateTimeOffset.UtcNow;
                _activeSubscriptions.AddOrUpdate(key, candleSub, (k, v) => candleSub);
            }
        }

        // Вызываем пользовательский обработчик, если есть
        if (_responseHandler != null)
        {
            await _responseHandler(response);
        }
    }

    private static string GetSubscriptionKey(MarketDataResponseType responseType, MarketDataInstrument instrument)
    {
        return $"{responseType}:{instrument.Figi}:{instrument.Uid}";
    }

    private static MarketDataResponseType GetResponseTypeFromRequest(ISubscriptionRequestBase request)
    {
        return request switch
        {
            CandleSubscriptionRequest => MarketDataResponseType.CANDLE,
            OrderBookSubscriptionRequest => MarketDataResponseType.ORDER_BOOK,
            TradeSubscriptionRequest => MarketDataResponseType.TRADE,
            LastPriceSubscriptionRequest => MarketDataResponseType.LAST_PRICE,
            OpenInterestSubscriptionRequest => MarketDataResponseType.OPEN_INTEREST,
            TradingStatusesSubscriptionRequest => MarketDataResponseType.TRADING_STATUS,
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.GetType(), "Неизвестный тип запроса подписки")
        };
    }

    private static List<MarketDataInstrument> GetInstrumentsFromRequest(ISubscriptionRequestBase request)
    {
        return request switch
        {
            CandleSubscriptionRequest r => [.. r.Instruments],
            OrderBookSubscriptionRequest r => [.. r.Instruments],
            TradeSubscriptionRequest r => [.. r.Instruments],
            LastPriceSubscriptionRequest r => [.. r.Instruments],
            OpenInterestSubscriptionRequest r => [.. r.Instruments],
            TradingStatusesSubscriptionRequest r => [.. r.Instruments],
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.GetType(), "Неизвестный тип запроса подписки")
        };
    }

    private static ISubscriptionRequestBase CreateSubscriptionRequest(
        MarketDataResponseType responseType,
        MarketDataInstrument instrument,
        SubscriptionAction action)
    {
        return responseType switch
        {
            MarketDataResponseType.CANDLE => new CandleSubscriptionRequest(action, [instrument]),
            MarketDataResponseType.ORDER_BOOK => new OrderBookSubscriptionRequest(action, [instrument]),
            MarketDataResponseType.TRADE => new TradeSubscriptionRequest(action, [instrument]),
            MarketDataResponseType.LAST_PRICE => new LastPriceSubscriptionRequest(action, [instrument]),
            MarketDataResponseType.OPEN_INTEREST => new OpenInterestSubscriptionRequest(action, [instrument]),
            MarketDataResponseType.TRADING_STATUS => new TradingStatusesSubscriptionRequest(action, [instrument]),
            _ => throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null)
        };
    }

    /// <summary>
    /// Проверяет, совпадает ли инструмент ответа с инструментом запроса по полям,
    /// указанным в запросе. Поля, оставленные в запросе пустыми, не проверяются.
    /// </summary>
    private static bool InstrumentsMatch(MarketDataInstrument request, MarketDataInstrument response)
    {
        if (!string.IsNullOrEmpty(request.Figi) && request.Figi != response.Figi) return false;
        if (!string.IsNullOrEmpty(request.Uid) && request.Uid != response.Uid) return false;
        return true;
    }

    /// <summary>
    /// Ключ ожидающей подписки: тип ответа и инструмент запроса.
    /// Equals сопоставляет инструменты по указанным в запросе полям, поэтому ответ с полным
    /// инструментом находит ожидание, созданное по неполному запросу (например, только по FIGI).
    /// GetHashCode зависит только от типа ответа — так ключи, считающиеся равными по Equals,
    /// гарантированно попадают в одну корзину словаря.
    /// </summary>
    private sealed class PendingSubscriptionKey : IEquatable<PendingSubscriptionKey>
    {
        public PendingSubscriptionKey(MarketDataResponseType responseType, MarketDataInstrument instrument)
        {
            ResponseType = responseType;
            Instrument = instrument;
        }

        private MarketDataResponseType ResponseType { get; }
        private MarketDataInstrument Instrument { get; }

        public bool Equals(PendingSubscriptionKey? other)
        {
            if (other is null) return false;
            if (ResponseType != other.ResponseType) return false;
            return InstrumentsMatch(Instrument, other.Instrument);
        }

        public override bool Equals(object? obj) => obj is PendingSubscriptionKey other && Equals(other);

        public override int GetHashCode() => ResponseType.GetHashCode();

        public override string ToString() =>
            $"{ResponseType}:{Instrument.Figi}:{Instrument.Uid}";
    }

    public async ValueTask DisposeAsync()
    {
        await _streamWrapper.DisposeAsync();
        _connectionHandlerLock?.Dispose();
    }
}
