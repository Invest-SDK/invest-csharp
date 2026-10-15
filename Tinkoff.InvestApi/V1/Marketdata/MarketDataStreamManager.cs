using System.Collections.Concurrent;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.V1.Marketdata.Models.Request;
using Tinkoff.InvestApi.V1.Marketdata.Models.Response;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata;
using Tinkoff.InvestApi.V1.Marketdata.Models;

namespace Tinkoff.InvestApi.Stream.Marketdata;

/// <summary>
/// Менеджер для управления пулом MarketDataStreamWrapper.
/// Автоматически создаёт новые стримы при достижении лимита подписок на одном стриме
/// и выбрасывает ошибку при достижении лимита на количество стримов.
/// Все ответы от стримов маршрутизируются в общий <see cref="MarketDataStreamContext"/>.
/// </summary>
public class MarketDataStreamManager
{
    private readonly object _lock = new();
    private readonly List<MarketDataStreamWrapper> _streams = [];
    private readonly ConcurrentDictionary<string, MarketDataStreamWrapper> _instrumentToStream = new();
    private readonly int _maxSubscriptionsPerStream;
    private readonly int _maxStreams;
    private readonly MarketDataStreamContext _context = new();
    private readonly ResilienceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> _configurer;
    private readonly MarketDataService.MarketDataServiceClient _client;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<MarketDataStreamManager>? _logger;

    public MarketDataStreamManager(
        MarketDataManagerConfigurer configurer,
        MarketDataService.MarketDataServiceClient client,
        ILoggerFactory? loggerFactory = null)
    {
        _configurer = configurer;
        _maxSubscriptionsPerStream = configurer.MaxSubscriptionsPerStream;
        _maxStreams = configurer.MaxStreams;
        _client = client;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<MarketDataStreamManager>();
    }

    /// <summary>
    /// Общий контекст, в который маршрутизируются ответы всех стримов.
    /// Через него регистрируются глобальные обработчики и читаются входящие ответы.
    /// </summary>
    public MarketDataStreamContext Context => _context;

    /// <summary>
    /// Текущее количество стримов в пуле.
    /// </summary>
    public int StreamCount
    {
        get
        {
            lock (_lock)
            {
                return _streams.Count;
            }
        }
    }

    /// <summary>
    /// Суммарное количество активных подписок по всем стримам.
    /// </summary>
    public int TotalSubscriptionCount
    {
        get
        {
            lock (_lock)
            {
                return _streams.Sum(s => s.GetSubscriptionCount());
            }
        }
    }

    /// <summary>
    /// Прогревает пул: создаёт первый стрим и устанавливает соединение.
    /// Опционален: первая подписка (Subscribe*Async) сама лениво создаёт и подключает стрим через <c>CallAsync</c>.
    /// Повторный вызов не выполняет никаких действий, если стрим уже создан.
    /// Используйте для раннего установления соединения до подписок.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        MarketDataStreamWrapper stream;
        lock (_lock)
        {
            if (_streams.Count > 0)
            {
                return;
            }

            stream = CreateStream();
            _streams.Add(stream);
        }

        await stream.ConnectAsync(cancellationToken);
    }

    /// <summary>
    /// Подписывается на свечи, распределяя инструменты между стримами пула.
    /// Переданный <paramref name="onCandle"/> регистрируется как локальный обработчик,
    /// вызываемый только для ответов со стрима, где размещена подписка, по инструментам этой подписки.
    /// </summary>
    public Task SubscribeCandlesAsync(
        IEnumerable<MarketDataInstrument> instruments,
        CancellationToken cancellationToken = default,
        Func<CandleModel, Task>? onCandle = null)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.CANDLE, i),
            (s, batch, ct) => s.SubscribeCandlesAsync(batch, ct),
            cancellationToken,
            onCandle == null ? null : (s, batch) => RegisterLocalHandler(MarketDataResponseType.CANDLE, s, batch, onCandle));

    /// <summary>
    /// Отписывается от свечей, направляя запрос в стрим, удерживающий подписку,
    /// и снимает связанные с ней локальные обработчики.
    /// </summary>
    public Task UnsubscribeCandlesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.CANDLE, i),
            (s, batch, ct) => s.UnsubscribeCandlesAsync(batch, ct),
            cancellationToken,
            (s, batch) => UnregisterLocalHandler(MarketDataResponseType.CANDLE, s, batch));

    /// <summary>
    /// Подписывается на стаканы, распределяя инструменты между стримами пула.
    /// Переданный <paramref name="onOrderBook"/> регистрируется как локальный обработчик для стрима и подписки.
    /// </summary>
    public Task SubscribeOrderBookAsync(
        IEnumerable<MarketDataInstrument> instruments,
        CancellationToken cancellationToken = default,
        Func<OrderBookModel, Task>? onOrderBook = null)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.ORDER_BOOK, i),
            (s, batch, ct) => s.SubscribeOrderBookAsync(batch, ct),
            cancellationToken,
            onOrderBook == null ? null : (s, batch) => RegisterLocalHandler(MarketDataResponseType.ORDER_BOOK, s, batch, onOrderBook));

    /// <summary>
    /// Отписывается от стаканов, направляя запрос в стрим, удерживающий подписку,
    /// и снимает связанные с ней локальные обработчики.
    /// </summary>
    public Task UnsubscribeOrderBookAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.ORDER_BOOK, i),
            (s, batch, ct) => s.UnsubscribeOrderBookAsync(batch, ct),
            cancellationToken,
            (s, batch) => UnregisterLocalHandler(MarketDataResponseType.ORDER_BOOK, s, batch));

    /// <summary>
    /// Подписывается на сделки, распределяя инструменты между стримами пула.
    /// Переданный <paramref name="onTrade"/> регистрируется как локальный обработчик для стрима и подписки.
    /// </summary>
    public Task SubscribeTradesAsync(
        IEnumerable<MarketDataInstrument> instruments,
        CancellationToken cancellationToken = default,
        Func<TradeModel, Task>? onTrade = null)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.TRADE, i),
            (s, batch, ct) => s.SubscribeTradesAsync(batch, ct),
            cancellationToken,
            onTrade == null ? null : (s, batch) => RegisterLocalHandler(MarketDataResponseType.TRADE, s, batch, onTrade));

    /// <summary>
    /// Отписывается от сделок, направляя запрос в стрим, удерживающий подписку,
    /// и снимает связанные с ней локальные обработчики.
    /// </summary>
    public Task UnsubscribeTradesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.TRADE, i),
            (s, batch, ct) => s.UnsubscribeTradesAsync(batch, ct),
            cancellationToken,
            (s, batch) => UnregisterLocalHandler(MarketDataResponseType.TRADE, s, batch));

    /// <summary>
    /// Подписывается на статус сделок, распределяя инструменты между стримами пула.
    /// Переданный <paramref name="onTradingStatuses"/> регистрируется как локальный обработчик для стрима и подписки.
    /// </summary>
    public Task SubscribeTradingStatusesAsync(
        IEnumerable<MarketDataInstrument> instruments,
        CancellationToken cancellationToken = default,
        Func<TradingStatusModel, Task>? onTradingStatuses = null)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.TRADING_STATUS, i),
            (s, batch, ct) => s.SubscribeTradingStatusesAsync(batch, ct),
            cancellationToken,
            onTradingStatuses == null ? null : (s, batch) => RegisterLocalHandler(MarketDataResponseType.TRADING_STATUS, s, batch, onTradingStatuses));

    /// <summary>
    /// Отписывается от статуса сделок, направляя запрос в стрим, удерживающий подписку,
    /// и снимает связанные с ней локальные обработчики.
    /// </summary>
    public Task UnsubscribeTradingStatusesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.TRADING_STATUS, i),
            (s, batch, ct) => s.UnsubscribeTradingStatusesAsync(batch, ct),
            cancellationToken,
            (s, batch) => UnregisterLocalHandler(MarketDataResponseType.TRADING_STATUS, s, batch));

    /// <summary>
    /// Подписывается на цены последней сделки, распределяя инструменты между стримами пула.
    /// Переданный <paramref name="onLastPrice"/> регистрируется как локальный обработчик для стрима и подписки.
    /// </summary>
    public Task SubscribeLastPriceAsync(
        IEnumerable<MarketDataInstrument> instruments,
        CancellationToken cancellationToken = default,
        Func<LastPriceModel, Task>? onLastPrice = null)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.LAST_PRICE, i),
            (s, batch, ct) => s.SubscribeLastPriceAsync(batch, ct),
            cancellationToken,
            onLastPrice == null ? null : (s, batch) => RegisterLocalHandler(MarketDataResponseType.LAST_PRICE, s, batch, onLastPrice));

    /// <summary>
    /// Отписывается от цен последней сделки, направляя запрос в стрим, удерживающий подписку,
    /// и снимает связанные с ней локальные обработчики.
    /// </summary>
    public Task UnsubscribeLastPriceAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.LAST_PRICE, i),
            (s, batch, ct) => s.UnsubscribeLastPriceAsync(batch, ct),
            cancellationToken,
            (s, batch) => UnregisterLocalHandler(MarketDataResponseType.LAST_PRICE, s, batch));

    /// <summary>
    /// Подписывается на открытые позиции, распределяя инструменты между стримами пула.
    /// Переданный <paramref name="onOpenInterest"/> регистрируется как локальный обработчик для стрима и подписки.
    /// </summary>
    public Task SubscribeOpenInterestAsync(
        IEnumerable<MarketDataInstrument> instruments,
        CancellationToken cancellationToken = default,
        Func<OpenInterestModel, Task>? onOpenInterest = null)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.OPEN_INTEREST, i),
            (s, batch, ct) => s.SubscribeOpenInterestAsync(batch, ct),
            cancellationToken,
            onOpenInterest == null ? null : (s, batch) => RegisterLocalHandler(MarketDataResponseType.OPEN_INTEREST, s, batch, onOpenInterest));

    /// <summary>
    /// Отписывается от открытых позиций, направляя запрос в стрим, удерживающий подписку,
    /// и снимает связанные с ней локальные обработчики.
    /// </summary>
    public Task UnsubscribeOpenInterestAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKeyBuilder.Build(MarketDataResponseType.OPEN_INTEREST, i),
            (s, batch, ct) => s.UnsubscribeOpenInterestAsync(batch, ct),
            cancellationToken,
            (s, batch) => UnregisterLocalHandler(MarketDataResponseType.OPEN_INTEREST, s, batch));

    private MarketDataStreamWrapper CreateStream()
    {
        return new MarketDataStreamWrapper(
            _configurer,
            (id, response) => RouteResponseAsync(id, response),
            RecoveringConnectionHandler,
            _loggerFactory);
    }

    /// <summary>
    /// Распределяет подписки между стримами пула, автоматически создавая новые стримы
    /// при заполнении текущих и выбрасывая ошибку при достижении лимита стримов.
    /// </summary>
    private async Task RouteSubscribeAsync<T>(
        IReadOnlyList<T> instruments,
        Func<T, string> keySelector,
        Func<MarketDataStreamWrapper, IReadOnlyList<T>, CancellationToken, Task> subscribeOnStream,
        CancellationToken cancellationToken,
        Action<MarketDataStreamWrapper, IReadOnlyList<T>>? registerLocal = null)
    {
        if (instruments.Count == 0)
        {
            return;
        }

        var remaining = new Queue<T>(instruments);
        while (remaining.Count > 0)
        {
            int capacity = 0;
            MarketDataStreamWrapper? stream;
            lock (_lock)
            {
                stream = _streams.FirstOrDefault(s => (capacity = _maxSubscriptionsPerStream - s.GetSubscriptionCount() - s.GetPendingSubscriptionsCount()) > 0);
                if (stream == null)
                {
                    if (_streams.Count >= _maxStreams)
                    {
                        throw new InvalidOperationException($"Stream count limit reached: {_maxStreams}. Increase the limit or remove unused subscriptions.");
                    }

                    stream = CreateStream();
                    _streams.Add(stream);
                    capacity = _maxSubscriptionsPerStream;
                }
            }

            var batch = new List<T>();
            while (remaining.Count > 0 && batch.Count < capacity)
            {
                batch.Add(remaining.Dequeue());
            }

            await subscribeOnStream(stream, batch, cancellationToken);

            registerLocal?.Invoke(stream, batch);

            foreach (var item in batch)
            {
                _instrumentToStream[keySelector(item)] = stream;
            }
        }
    }

    /// <summary>
    /// Направляет отписку в стрим, удерживающий соответствующую подписку.
    /// </summary>
    private async Task RouteUnsubscribeAsync<T>(
        IReadOnlyList<T> instruments,
        Func<T, string> keySelector,
        Func<MarketDataStreamWrapper, IReadOnlyList<T>, CancellationToken, Task> unsubscribeOnStream,
        CancellationToken cancellationToken,
        Action<MarketDataStreamWrapper, IReadOnlyList<T>>? unregisterLocal = null)
    {
        if (instruments.Count == 0)
        {
            return;
        }

        var byStream = new Dictionary<MarketDataStreamWrapper, List<T>>();
        foreach (var item in instruments)
        {
            var key = keySelector(item);
            if (_instrumentToStream.TryGetValue(key, out var stream))
            {
                if (!byStream.TryGetValue(stream, out var list))
                {
                    byStream[stream] = list = new List<T>();
                }

                list.Add(item);
            }
            else
            {
                _logger?.LogWarning("No subscription found to unsubscribe: {Key}", key);
            }
        }

        foreach (var kvp in byStream)
        {
            await unsubscribeOnStream(kvp.Key, kvp.Value, cancellationToken);

            unregisterLocal?.Invoke(kvp.Key, kvp.Value);

            foreach (var item in kvp.Value)
            {
                _instrumentToStream.TryRemove(keySelector(item), out _);
            }
        }
    }

    /// <summary>
    /// Маршрутизирует входящий ответ стрима в общий контекст.
    /// <paramref name="id"/> используется для адресации локальных обработчиков конкретного стрима.
    /// </summary>
    private async Task RouteResponseAsync(Guid id, MarketDataResponse response)
    {
        switch (MarketDataUtils.GetResponseType(response))
        {
            case MarketDataResponseType.CANDLE:
                await _context.PushCandleAsync(response.Candle.ToModel(isHistorical: false), id);
                break;
            case MarketDataResponseType.TRADE:
                await _context.PushTradeAsync(response.Trade.ToModel(), id);
                break;
            case MarketDataResponseType.ORDER_BOOK:
                await _context.PushOrderBookAsync(response.Orderbook.ToWrapper(), id);
                break;
            case MarketDataResponseType.LAST_PRICE:
                await _context.PushLastPriceAsync(response.LastPrice.ToModel(), id);
                break;
            case MarketDataResponseType.OPEN_INTEREST:
                await _context.PushOpenInterestAsync(response.OpenInterest.ToModel(), id);
                break;
            case MarketDataResponseType.TRADING_STATUS:
                await _context.PushTradingStatusAsync(response.TradingStatus.ToModel(), id);
                break;
        }
    }

    private async Task RecoveringConnectionHandler(StreamInfo streamInfo)
    {
        var stream = _streams.FirstOrDefault(s => s.Guid == streamInfo.Guid);
        if (stream != null)
        {
            var candleSubscriptions = stream.GetActiveSubscriptions
                .Where(s => s.Value.ResponseType == MarketDataResponseType.CANDLE)
                .Select(s => s.Value)
                .ToList();
            foreach (ActiveSubscription sub in candleSubscriptions)
            {
                await RecoverCandleSubscription(sub.Instrument, sub.LastInteractionTime, streamInfo.Guid);
            }
        }
    }

    private async Task RecoverCandleSubscription(MarketDataInstrument instrument, DateTimeOffset? lastInteractionTime, Guid guid)
    {
        if (lastInteractionTime == null)
        {
            return;
        }
        _logger?.LogDebug("Recovering candle subscription for {Figi}...", instrument.Figi);
        var now = DateTime.UtcNow;
        int intervalNumber = (int)(instrument.Interval ?? SubscriptionInterval.Unspecified);
        var request = new GetCandlesRequest
        {
            InstrumentId = instrument.Uid,
            Interval = Enum.IsDefined(typeof(CandleInterval), intervalNumber) ? (CandleInterval)intervalNumber : CandleInterval.Unspecified,
            From = lastInteractionTime?.ToTimestamp(),
            To = now.ToTimestamp(),
        };
        var response = _client.GetCandles(request);
        var candles = response.Candles.Where(c => c.IsComplete)
            .Select(c => MapCandle(c, instrument).ToModel(isHistorical: true))
            .ToList();
        candles.Sort((c1, c2) => (c1.Time ?? DateTime.MinValue).CompareTo(c2.Time ?? DateTime.MinValue));

        candles.ForEach(async c =>
        {
            _logger?.LogDebug("Recovered candle: {Candle}", c);
            await _context.PushCandleAsync(c, guid);
        });
    }

    /// <summary>
    /// Регистрирует локальный обработчик <paramref name="handler"/> для каждого инструмента из <paramref name="batch"/>
    /// на стриме <paramref name="stream"/> в общем контексте.
    /// </summary>
    private void RegisterLocalHandler(
        MarketDataResponseType responseType,
        MarketDataStreamWrapper stream,
        IReadOnlyList<MarketDataInstrument> batch,
        Delegate handler)
    {
        foreach (var item in batch)
        {
            var key = LocalInstrumentKey(responseType, item);
            switch (responseType)
            {
                case MarketDataResponseType.CANDLE:
                    _context.RegisterLocalCandleHandler(stream.Guid, key, (Func<CandleModel, Task>)handler);
                    break;
                case MarketDataResponseType.ORDER_BOOK:
                    _context.RegisterLocalOrderBookHandler(stream.Guid, key, (Func<OrderBookModel, Task>)handler);
                    break;
                case MarketDataResponseType.TRADE:
                    _context.RegisterLocalTradeHandler(stream.Guid, key, (Func<TradeModel, Task>)handler);
                    break;
                case MarketDataResponseType.LAST_PRICE:
                    _context.RegisterLocalLastPriceHandler(stream.Guid, key, (Func<LastPriceModel, Task>)handler);
                    break;
                case MarketDataResponseType.OPEN_INTEREST:
                    _context.RegisterLocalOpenInterestHandler(stream.Guid, key, (Func<OpenInterestModel, Task>)handler);
                    break;
                case MarketDataResponseType.TRADING_STATUS:
                    _context.RegisterLocalTradingStatusHandler(stream.Guid, key, (Func<TradingStatusModel, Task>)handler);
                    break;
            }
        }
    }

    /// <summary>
    /// Снимает локальные обработчики для каждого инструмента из <paramref name="batch"/> на стриме <paramref name="stream"/>.
    /// </summary>
    private void UnregisterLocalHandler(
        MarketDataResponseType responseType,
        MarketDataStreamWrapper stream,
        IReadOnlyList<MarketDataInstrument> batch)
    {
        foreach (var item in batch)
        {
            var key = LocalInstrumentKey(responseType, item);
            switch (responseType)
            {
                case MarketDataResponseType.CANDLE:
                    _context.UnregisterLocalCandleHandlers(stream.Guid, key);
                    break;
                case MarketDataResponseType.ORDER_BOOK:
                    _context.UnregisterLocalOrderBookHandlers(stream.Guid, key);
                    break;
                case MarketDataResponseType.TRADE:
                    _context.UnregisterLocalTradeHandlers(stream.Guid, key);
                    break;
                case MarketDataResponseType.LAST_PRICE:
                    _context.UnregisterLocalLastPriceHandlers(stream.Guid, key);
                    break;
                case MarketDataResponseType.OPEN_INTEREST:
                    _context.UnregisterLocalOpenInterestHandlers(stream.Guid, key);
                    break;
                case MarketDataResponseType.TRADING_STATUS:
                    _context.UnregisterLocalTradingStatusHandlers(stream.Guid, key);
                    break;
            }
        }
    }

    /// <summary>
    /// Ключ инструмента для локальных обработчиков. Для открытых позиций ответ не содержит FIGI,
    /// поэтому ключ строится только по UID. Дискриминатор — интервал (свечи) или тип (стакан);
    /// для остальных типов пуст. Id сопоставляется по указанным полям (см. LocalSubscriptionKey).
    /// </summary>
    private static LocalSubscriptionKey LocalInstrumentKey(MarketDataResponseType responseType, MarketDataInstrument item)
    {
        return responseType switch
        {
            // Свечи одного инструмента с разными интервалами обрабатываются раздельно.
            MarketDataResponseType.CANDLE => new LocalSubscriptionKey(item.Figi, item.Uid, item.Interval?.ToString() ?? string.Empty),
            // Стакан: подписка с конкретным типом получает только пуши того же типа,
            // а ALL (или незаданный тип) — пуши любого типа.
            MarketDataResponseType.ORDER_BOOK => new LocalSubscriptionKey(item.Figi, item.Uid, (item.OrderBook ?? OrderBookType.All).ToString()),
            MarketDataResponseType.OPEN_INTEREST => new LocalSubscriptionKey(string.Empty, item.Uid, string.Empty),
            _ => new LocalSubscriptionKey(item.Figi, item.Uid, string.Empty)
        };
    }

    private static Candle MapCandle(HistoricCandle candle, MarketDataInstrument instrument)
    {
        var tickerClassCode = TickerClassCode.Parse(instrument.TickerClassCode!);
        return new Candle
        {
            InstrumentUid = instrument.Uid,
            Figi = instrument.Figi,
            Ticker = tickerClassCode.Ticker,
            ClassCode = tickerClassCode.ClassCode,
            Interval = instrument.Interval ?? SubscriptionInterval.Unspecified,
            Open = candle.Open,
            Close = candle.Close,
            High = candle.High,
            Low = candle.Low,
            Time = candle.Time,
            Volume = candle.Volume,
            VolumeBuy = candle.VolumeBuy,
            VolumeSell = candle.VolumeSell,
            CandleSourceType = candle.CandleSource,
        };
    }

    public async ValueTask DisposeAsync()
    {
        List<MarketDataStreamWrapper> streams;
        lock (_lock)
        {
            streams = _streams.ToList();
            _streams.Clear();
        }

        _context.ClearLocalHandlers();

        foreach (var stream in streams)
        {
            try
            {
                await stream.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error while disposing stream");
            }
        }
    }

    record TickerClassCode(string Ticker, string ClassCode)
    {
        public static TickerClassCode Parse(string tickerClassCode)
        {
            var arr = tickerClassCode.Split('_');
            return new TickerClassCode(arr[0], arr[1]);
        }
    }
}
